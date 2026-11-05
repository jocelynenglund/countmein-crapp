using CountMeIn.Application.Dtos;
using CountMeIn.Domain;
using static CountMeIn.Application.Tests.World;

namespace CountMeIn.Application.Tests;

public class ServiceTests
{
    private readonly World w = new();

    private Task<StartCheckResponse> StartWithDoor(int capacity) =>
        w.Checks.StartAsync(new("Board games", "Monthly", 3, null, capacity, At(10, 18), At(10, 20)));

    private Task Join(string shareId, string id) => w.Checks.CountMeInAsync(shareId, new(id, id.ToUpper(), null));

    [Fact]
    public async Task Starting_returns_a_share_id_and_an_owner_key_and_stores_only_the_hash()
    {
        var r = await w.Checks.StartAsync(new("Board games", "Monthly", 10, null, null, null, null));
        var stored = await w.Repo.GetByShareIdAsync(r.ShareId);
        Assert.NotEqual(r.OwnerKey, stored!.OwnerKeyHash);
        Assert.Equal(OwnerKeys.Hash(r.OwnerKey), stored.OwnerKeyHash);
        Assert.Equal(1, w.Uow.Saves);
    }

    [Fact]
    public async Task Pitch_page_shows_the_numbers()
    {
        var r = await StartWithDoor(2);
        await Join(r.ShareId, "p1"); await Join(r.ShareId, "p2"); await Join(r.ShareId, "p3");
        var pitch = await w.Checks.GetPitchAsync(r.ShareId);
        Assert.Equal(new PitchDto("Board games", "Monthly", 3, 3, true, true, null, 0, 1), pitch);
    }

    [Fact]
    public async Task Unknown_check_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => w.Checks.GetPitchAsync("nope"));

    [Fact]
    public async Task Progress_is_for_the_owner_only()
    {
        var r = await StartWithDoor(2);
        await Assert.ThrowsAsync<NotOwnerException>(() => w.Checks.GetProgressAsync(r.ShareId, "wrong"));
        await Assert.ThrowsAsync<NotOwnerException>(() => w.Checks.GetProgressAsync(r.ShareId, null));
    }

    [Fact]
    public async Task Progress_counts_in_and_out_per_day()
    {
        var r = await w.Checks.StartAsync(new("Board games", null, 10, null, null, null, null));
        w.Clock.UtcNow = At(4, 9);
        await Join(r.ShareId, "p1"); await Join(r.ShareId, "p2");
        w.Clock.UtcNow = At(6, 9);
        await Join(r.ShareId, "p3");
        w.Clock.UtcNow = At(7, 9);
        await w.Checks.ChangeMyMindAsync(r.ShareId, new("p2"));

        var p = await w.Checks.GetProgressAsync(r.ShareId, r.OwnerKey);
        Assert.Equal(2, p.Count);
        Assert.Equal(["P1", "P3"], p.People.Select(x => x.Name));
        Assert.Equal(
            [new DayDto(new(2026, 11, 4), 2, 0), new DayDto(new(2026, 11, 6), 1, 0), new DayDto(new(2026, 11, 7), 0, 1)],
            p.ByDay);
    }

    [Fact]
    public async Task Closing_needs_the_owner_key()
    {
        var r = await StartWithDoor(2);
        await Assert.ThrowsAsync<NotOwnerException>(() => w.Checks.CloseAsync(r.ShareId, "wrong"));
        await w.Checks.CloseAsync(r.ShareId, r.OwnerKey);
        Assert.False((await w.Checks.GetPitchAsync(r.ShareId)).Open);
    }

    [Fact]
    public async Task A_waitlisted_door_attempt_is_saved_even_though_it_is_refused()
    {
        var r = await StartWithDoor(1);
        await Join(r.ShareId, "p1"); await Join(r.ShareId, "p2");
        var saves = w.Uow.Saves;
        var ex = await Assert.ThrowsAsync<DomainException>(() => w.Door.CheckInAsync(r.ShareId, new("p2", At(10, 19))));
        Assert.Equal("You're on the waitlist", ex.Message);
        Assert.Equal(saves + 1, w.Uow.Saves);

        var list = await w.Door.GetDoorListAsync(r.ShareId, r.OwnerKey);
        Assert.Equal([new TimedPersonDto("p2", "P2", At(10, 19))], list.TurnedAway);
    }

    [Fact]
    public async Task Door_list_after_the_release()
    {
        var r = await StartWithDoor(2);
        foreach (var p in new[] { "p1", "p2", "p3", "p4" }) await Join(r.ShareId, p);
        await w.Door.CheckInAsync(r.ShareId, new("p1", At(10, 18, 30)));
        await Assert.ThrowsAsync<DomainException>(() => w.Door.CheckInAsync(r.ShareId, new("p4", At(10, 19))));

        w.Clock.UtcNow = At(10, 19, 50);
        Assert.Equal(1, await w.Door.ReleaseDueSpotsAsync());
        Assert.Equal(0, await w.Door.ReleaseDueSpotsAsync()); // only once

        var list = await w.Door.GetDoorListAsync(r.ShareId, r.OwnerKey);
        Assert.Equal(["p1"], list.CheckedIn.Select(x => x.PersonId));
        Assert.Equal(["p4"], list.LetIn.Select(x => x.PersonId));
        Assert.Equal(["p2"], list.LostSpot.Select(x => x.PersonId));
        Assert.Equal(["p4"], list.NotArrived.Select(x => x.PersonId));
        Assert.Equal(["p2", "p3"], list.Waitlist.Select(x => x.PersonId));
        Assert.Empty(list.TurnedAway);
        await w.Door.CheckInAsync(r.ShareId, new("p4", At(10, 19, 55)));
    }

    [Fact]
    public async Task Door_list_is_for_the_owner_only()
    {
        var r = await StartWithDoor(2);
        await Assert.ThrowsAsync<NotOwnerException>(() => w.Door.GetDoorListAsync(r.ShareId, "wrong"));
    }

    [Fact]
    public async Task Venue_flow_with_early_birds_on_the_door_list()
    {
        var r = await StartWithDoor(3);
        await Join(r.ShareId, "p1"); await Join(r.ShareId, "p2");
        await w.Checks.ChangeMyMindAsync(r.ShareId, new("p2"));
        await Join(r.ShareId, "p2");

        await Assert.ThrowsAsync<NotOwnerException>(() => w.Venue.SetVenueAsync(r.ShareId, "wrong", new("X", At(10, 12))));
        await w.Venue.SetVenueAsync(r.ShareId, r.OwnerKey, new("The Library", At(10, 12)));

        w.Clock.UtcNow = At(10, 11, 5);
        Assert.True((await w.Venue.GetVenueAsync(r.ShareId, "p1")).Visible);
        Assert.False((await w.Venue.GetVenueAsync(r.ShareId, "p2")).Visible);
        Assert.Equal("Venue revealed at 2026-11-10 12:00 UTC", (await w.Venue.GetVenueAsync(r.ShareId, "p2")).Message);

        var list = await w.Door.GetDoorListAsync(r.ShareId, r.OwnerKey);
        Assert.Equal("The Library", list.Venue);
        Assert.Equal(At(10, 12), list.RevealAt);
        Assert.Equal(["p1"], list.EarlyBirds.Select(x => x.PersonId));

        w.Clock.UtcNow = At(10, 12);
        var ex = await Assert.ThrowsAsync<DomainException>(() => w.Venue.SetVenueAsync(r.ShareId, r.OwnerKey, new("Elsewhere", At(10, 13))));
        Assert.Equal("The venue has already been revealed", ex.Message);
    }
}
