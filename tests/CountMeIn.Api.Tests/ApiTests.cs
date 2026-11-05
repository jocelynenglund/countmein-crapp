using System.Net;
using System.Net.Http.Json;
using CountMeIn.Application.Dtos;
using CountMeIn.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CountMeIn.Api.Tests;

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient http = factory.CreateClient();

    private static DateTimeOffset At(int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0, TimeSpan.Zero);

    private async Task<StartCheckResponse> Start(int? capacity = null, bool door = false)
    {
        factory.Clock.UtcNow = At(4, 10);
        var r = await http.PostAsJsonAsync("/api/checks", new StartCheckRequest(
            "Board games", "Monthly games", 3, null, capacity, door ? At(10, 18) : null, door ? At(10, 20) : null));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<StartCheckResponse>())!;
    }

    private Task<HttpResponseMessage> Join(string id, string person) =>
        http.PostAsJsonAsync($"/api/checks/{id}/join", new CountMeInRequest(person, person.ToUpper(), null));

    private HttpRequestMessage Owner(HttpMethod m, string url, string key, object? body = null)
    {
        var req = new HttpRequestMessage(m, url) { Content = body is null ? null : JsonContent.Create(body) };
        req.Headers.Add("X-Owner-Key", key);
        return req;
    }

    private static async Task<string> Error(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<Dictionary<string, string>>())!["error"];

    [Fact]
    public async Task Start_then_read_the_pitch()
    {
        var c = await Start(capacity: 2);
        var pitch = await http.GetFromJsonAsync<PitchDto>($"/api/checks/{c.ShareId}");
        Assert.Equal(new PitchDto("Board games", "Monthly games", 3, 0, false, true, null, 2, 0), pitch);
    }

    [Fact]
    public async Task Bad_start_is_a_400_with_the_reason()
    {
        var r = await http.PostAsJsonAsync("/api/checks", new StartCheckRequest(" ", null, 3, null, null, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("What's the idea called?", await Error(r));
    }

    [Fact]
    public async Task Unknown_check_is_404() =>
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/api/checks/doesnotexist")).StatusCode);

    [Fact]
    public async Task Count_in_twice_then_change_mind()
    {
        var c = await Start(capacity: 1);
        Assert.Equal(HttpStatusCode.NoContent, (await Join(c.ShareId, "p1")).StatusCode);
        var again = await Join(c.ShareId, "p1");
        Assert.Equal("You're already counted in", await Error(again));
        await Join(c.ShareId, "p2");

        var pitch = await http.GetFromJsonAsync<PitchDto>($"/api/checks/{c.ShareId}");
        Assert.Equal(2, pitch!.Count);
        Assert.Equal(1, pitch.WaitlistCount);
        Assert.Equal(0, pitch.SpotsLeft);

        await http.PostAsJsonAsync($"/api/checks/{c.ShareId}/leave", new ChangeMyMindRequest("p1"));
        pitch = await http.GetFromJsonAsync<PitchDto>($"/api/checks/{c.ShareId}");
        Assert.Equal(1, pitch!.Count);
        Assert.Equal(0, pitch.WaitlistCount); // p2 moved up
    }

    [Fact]
    public async Task Owner_endpoints_refuse_a_wrong_key()
    {
        var c = await Start();
        var r = await http.SendAsync(Owner(HttpMethod.Get, $"/api/checks/{c.ShareId}/progress", "wrong"));
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        var close = await http.SendAsync(Owner(HttpMethod.Post, $"/api/checks/{c.ShareId}/close", "wrong"));
        Assert.Equal("Only the owner can close this", await Error(close));
    }

    [Fact]
    public async Task Progress_by_day_and_close()
    {
        var c = await Start();
        factory.Clock.UtcNow = At(4, 9);
        await Join(c.ShareId, "p1"); await Join(c.ShareId, "p2");
        factory.Clock.UtcNow = At(7, 9);
        await http.PostAsJsonAsync($"/api/checks/{c.ShareId}/leave", new ChangeMyMindRequest("p2"));

        var p = await (await http.SendAsync(Owner(HttpMethod.Get, $"/api/checks/{c.ShareId}/progress", c.OwnerKey)))
            .Content.ReadFromJsonAsync<ProgressDto>();
        Assert.Equal(1, p!.Count);
        Assert.Equal([new DayDto(new(2026, 11, 4), 2, 0), new DayDto(new(2026, 11, 7), 0, 1)], p.ByDay);
        Assert.Equal(["P1"], p.People.Select(x => x.Name));

        Assert.Equal(HttpStatusCode.NoContent, (await http.SendAsync(Owner(HttpMethod.Post, $"/api/checks/{c.ShareId}/close", c.OwnerKey))).StatusCode);
        Assert.Equal("This interest check has closed", await Error(await Join(c.ShareId, "p3")));
    }

    [Fact]
    public async Task Door_release_and_venue_end_to_end()
    {
        var c = await Start(capacity: 1, door: true);
        await Join(c.ShareId, "p1"); await Join(c.ShareId, "p2");
        await http.SendAsync(Owner(HttpMethod.Put, $"/api/checks/{c.ShareId}/venue", c.OwnerKey,
            new SetVenueRequest("The Library", At(10, 19))));

        // the waitlisted person tries the door and is remembered
        var turned = await http.PostAsJsonAsync($"/api/checks/{c.ShareId}/check-in", new CheckInRequest("p2", At(10, 18, 30)));
        Assert.Equal("You're on the waitlist", await Error(turned));

        // the background job's work, run by hand at release time
        factory.Clock.UtcNow = At(10, 19, 50);
        using (var scope = factory.Services.CreateScope())
            Assert.True(await scope.ServiceProvider.GetRequiredService<DoorService>().ReleaseDueSpotsAsync() >= 1);

        var lost = await http.PostAsJsonAsync($"/api/checks/{c.ShareId}/check-in", new CheckInRequest("p1", At(10, 19, 55)));
        Assert.Equal("Your spot went to the waitlist", await Error(lost));
        var ok = await http.PostAsJsonAsync($"/api/checks/{c.ShareId}/check-in", new CheckInRequest("p2", At(10, 19, 56)));
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);

        var list = await (await http.SendAsync(Owner(HttpMethod.Get, $"/api/checks/{c.ShareId}/door-list", c.OwnerKey)))
            .Content.ReadFromJsonAsync<DoorListDto>();
        Assert.Equal(["p2"], list!.CheckedIn.Select(x => x.PersonId));
        Assert.Equal(["p2"], list.LetIn.Select(x => x.PersonId));
        Assert.Equal(["p1"], list.LostSpot.Select(x => x.PersonId));
        Assert.Equal("The Library", list.Venue);

        var venue = await http.GetFromJsonAsync<VenueDto>($"/api/checks/{c.ShareId}/venue?personId=p2");
        Assert.True(venue!.Visible);
        Assert.Equal("The Library", venue.Venue);
        Assert.False((await http.GetFromJsonAsync<VenueDto>($"/api/checks/{c.ShareId}/venue?personId=p1"))!.Visible);
    }

    [Fact]
    public async Task Venue_stays_hidden_before_the_reveal()
    {
        var c = await Start(capacity: 5);
        await Join(c.ShareId, "p1");
        await http.SendAsync(Owner(HttpMethod.Put, $"/api/checks/{c.ShareId}/venue", c.OwnerKey,
            new SetVenueRequest("The Library", At(10, 19))));
        factory.Clock.UtcNow = At(10, 12);
        var v = await http.GetFromJsonAsync<VenueDto>($"/api/checks/{c.ShareId}/venue?personId=p1");
        Assert.False(v!.Visible);
        Assert.Null(v.Venue);
        Assert.Equal("Venue revealed at 2026-11-10 19:00 UTC", v.Message);
    }
}
