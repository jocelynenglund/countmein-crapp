using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CountMeIn.Application;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CountMeIn.Api.Tests;

/// <summary>
/// End to end through HTTP, EF Core and a real Postgres (the countmein_crapp database from
/// appsettings.json; the Development environment applies the migrations on startup).
/// Every test starts its own check, so tests do not need cleaning up after.
/// </summary>
public class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<StartedInterestCheckDto> Start(int goal = 10, string title = "Saturday board-game club")
    {
        var response = await _client.PostAsJsonAsync("/api/checks",
            new { title, pitch = "Monthly games at the library", goal });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StartedInterestCheckDto>())!;
    }

    private Task<HttpResponseMessage> CountIn(string checkId, string personId, string name, string? email = null) =>
        _client.PostAsJsonAsync($"/api/checks/{checkId}/interest", new { personId, name, email });

    private Task<HttpResponseMessage> AsOwner(HttpMethod method, string url, string? key)
    {
        var request = new HttpRequestMessage(method, url);
        if (key is not null) request.Headers.Add("X-Owner-Key", key);
        return _client.SendAsync(request);
    }

    private static async Task<string> Error(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()!;

    [Fact]
    public async Task A_whole_interest_check_from_start_to_close()
    {
        var check = await Start(goal: 2);

        Assert.Equal(HttpStatusCode.NoContent, (await CountIn(check.CheckId, "p1", "Ana", "ana@example.com")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await CountIn(check.CheckId, "p2", "Ben")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await _client.DeleteAsync($"/api/checks/{check.CheckId}/interest/p2")).StatusCode);

        var pitch = (await _client.GetFromJsonAsync<PitchDto>($"/api/checks/{check.CheckId}"))!;
        Assert.Equal(new PitchDto("Saturday board-game club", "Monthly games at the library", 2, 1, false, true, null), pitch);

        var progress = (await (await AsOwner(HttpMethod.Get, $"/api/checks/{check.CheckId}/progress", check.OwnerKey))
            .Content.ReadFromJsonAsync<ProgressDto>())!;
        Assert.Equal(1, progress.Count);
        Assert.Equal([new PersonDto("Ana", "ana@example.com")], progress.People);
        Assert.Single(progress.SignUpsPerDay);

        Assert.Equal(HttpStatusCode.NoContent,
            (await AsOwner(HttpMethod.Post, $"/api/checks/{check.CheckId}/close", check.OwnerKey)).StatusCode);

        var closed = (await _client.GetFromJsonAsync<PitchDto>($"/api/checks/{check.CheckId}"))!;
        Assert.False(closed.Open);

        var tooLate = await CountIn(check.CheckId, "p3", "Dev");
        Assert.Equal(HttpStatusCode.Conflict, tooLate.StatusCode);
        Assert.Equal("This interest check has closed", await Error(tooLate));
    }

    [Fact]
    public async Task The_public_pitch_never_shows_names_emails_or_the_owner_key()
    {
        var check = await Start();
        await CountIn(check.CheckId, "p1", "Ana", "ana@example.com");

        var body = await _client.GetStringAsync($"/api/checks/{check.CheckId}");

        Assert.DoesNotContain("Ana", body);
        Assert.DoesNotContain("ana@example.com", body);
        Assert.DoesNotContain("p1", body);
        Assert.DoesNotContain(check.OwnerKey, body);
        Assert.DoesNotContain("ownerKeyHash", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData(null)]
    public async Task Only_the_owner_can_see_progress_or_close(string? key)
    {
        var check = await Start();

        var progress = await AsOwner(HttpMethod.Get, $"/api/checks/{check.CheckId}/progress", key);
        var close = await AsOwner(HttpMethod.Post, $"/api/checks/{check.CheckId}/close", key);

        Assert.Equal(HttpStatusCode.Forbidden, progress.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, close.StatusCode);
        Assert.True((await _client.GetFromJsonAsync<PitchDto>($"/api/checks/{check.CheckId}"))!.Open);
    }

    [Fact]
    public async Task Refusals_come_back_with_a_status_and_a_message()
    {
        var check = await Start();
        await CountIn(check.CheckId, "p1", "Ana");

        var noTitle = await _client.PostAsJsonAsync("/api/checks", new { title = " ", goal = 10 });
        Assert.Equal(HttpStatusCode.BadRequest, noTitle.StatusCode);
        Assert.Equal("What's the idea called?", await Error(noTitle));

        var noName = await CountIn(check.CheckId, "p2", "");
        Assert.Equal(HttpStatusCode.BadRequest, noName.StatusCode);
        Assert.Equal("What's your name?", await Error(noName));

        var twice = await CountIn(check.CheckId, "p1", "Ana");
        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
        Assert.Equal("You're already counted in", await Error(twice));

        var notIn = await _client.DeleteAsync($"/api/checks/{check.CheckId}/interest/p9");
        Assert.Equal(HttpStatusCode.Conflict, notIn.StatusCode);
        Assert.Equal("You're not counted in", await Error(notIn));

        var missing = await CountIn("c9", "p1", "Ana");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("No such interest check", await Error(missing));
    }

    [Fact]
    public async Task Changed_your_mind_then_back_in_is_stored_as_one_person()
    {
        var check = await Start();
        await CountIn(check.CheckId, "p1", "Ana");
        await _client.DeleteAsync($"/api/checks/{check.CheckId}/interest/p1");

        var again = await CountIn(check.CheckId, "p1", "Ana");

        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        Assert.Equal(1, (await _client.GetFromJsonAsync<PitchDto>($"/api/checks/{check.CheckId}"))!.Count);
    }

    [Fact]
    public async Task Two_simultaneous_sign_ups_for_the_same_person_count_once()
    {
        var check = await Start();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(_ => CountIn(check.CheckId, "p1", "Ana")));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.NoContent),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        Assert.Equal(1, (await _client.GetFromJsonAsync<PitchDto>($"/api/checks/{check.CheckId}"))!.Count);
    }
}
