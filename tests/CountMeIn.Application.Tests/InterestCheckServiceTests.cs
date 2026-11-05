using CountMeIn.Domain;
using Microsoft.Extensions.Time.Testing;

namespace CountMeIn.Application.Tests;

public class InterestCheckServiceTests
{
    private readonly InMemoryInterestCheckRepository _repo = new();
    private readonly CountingUnitOfWork _uow = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));
    private readonly InterestCheckService _service;

    public InterestCheckServiceTests() => _service = new InterestCheckService(_repo, _uow, _clock);

    private Task<StartedInterestCheckDto> StartCheck(int goal = 10, DateOnly? closesOn = null) =>
        _service.StartAsync(new StartInterestCheckRequest("Board games", "Monthly games at the library", goal, closesOn));

    private Task CountIn(StartedInterestCheckDto check, string personId, string name, string? email = null) =>
        _service.CountMeInAsync(check.CheckId, new CountMeInRequest(personId, name, email));

    // 1. Start an interest check

    [Fact]
    public async Task Starting_an_interest_check_returns_a_share_link_id_and_an_owner_key()
    {
        var started = await _service.StartAsync(
            new StartInterestCheckRequest("Saturday board-game club", "Monthly games at the library", 10, null));

        Assert.True(Identifiers.IsValid(started.CheckId));
        Assert.NotEmpty(started.OwnerKey);
        Assert.Equal(1, _uow.Saves);

        var pitch = await _service.GetPitchAsync(started.CheckId);
        Assert.Equal("Saturday board-game club", pitch.Title);
        Assert.Equal(10, pitch.Goal);
    }

    [Fact]
    public async Task Every_check_gets_its_own_id_and_key()
    {
        var a = await StartCheck();
        var b = await StartCheck();
        Assert.NotEqual(a.CheckId, b.CheckId);
        Assert.NotEqual(a.OwnerKey, b.OwnerKey);
    }

    [Fact]
    public async Task An_interest_check_needs_a_title()
    {
        var ex = await Assert.ThrowsAsync<InvalidInterestCheckException>(() =>
            _service.StartAsync(new StartInterestCheckRequest(" ", null, 10, null)));
        Assert.Equal("What's the idea called?", ex.Message);
        Assert.Equal(0, _uow.Saves);
    }

    [Fact]
    public async Task The_goal_is_at_least_1()
    {
        var ex = await Assert.ThrowsAsync<InvalidInterestCheckException>(() =>
            _service.StartAsync(new StartInterestCheckRequest("Board games", null, 0, null)));
        Assert.StartsWith("How many people would make it worth doing?", ex.Message);
    }

    [Fact]
    public async Task A_closing_date_in_the_past_is_refused()
    {
        var ex = await Assert.ThrowsAsync<InvalidInterestCheckException>(() =>
            _service.StartAsync(new StartInterestCheckRequest("Board games", null, 10, new DateOnly(2026, 11, 1))));
        Assert.Equal("The closing date has already passed", ex.Message);
    }

    // 2. Pitch page

    [Fact]
    public async Task The_pitch_counts_who_is_in()
    {
        var check = await StartCheck(goal: 3);
        await CountIn(check, "p1", "Ana");
        await CountIn(check, "p2", "Ben");
        await _service.ChangeMyMindAsync(check.CheckId, "p1");

        var pitch = await _service.GetPitchAsync(check.CheckId);

        Assert.Equal(new PitchDto("Board games", "Monthly games at the library", 3, 1, false, true, null, null, 0), pitch);
    }

    [Fact]
    public async Task Reaching_the_goal()
    {
        var check = await StartCheck(goal: 2);
        await CountIn(check, "p1", "Ana");
        await CountIn(check, "p2", "Ben");

        var pitch = await _service.GetPitchAsync(check.CheckId);

        Assert.Equal(2, pitch.Count);
        Assert.True(pitch.GoalReached);
    }

    [Fact]
    public async Task A_closed_check_shows_as_closed()
    {
        var check = await StartCheck(closesOn: new DateOnly(2026, 11, 10));
        await CountIn(check, "p1", "Ana");
        await _service.CloseAsync(check.CheckId, check.OwnerKey);

        var pitch = await _service.GetPitchAsync(check.CheckId);

        Assert.Equal(1, pitch.Count);
        Assert.False(pitch.Open);
        Assert.Equal(new DateOnly(2026, 11, 10), pitch.ClosesOn);
    }

    [Fact]
    public async Task The_pitch_of_a_check_that_does_not_exist_is_not_found()
    {
        await Assert.ThrowsAsync<InterestCheckNotFoundException>(() => _service.GetPitchAsync("nope"));
    }

    // 3. Count me in

    [Fact]
    public async Task Counting_yourself_in()
    {
        var check = await StartCheck();

        await CountIn(check, "p1", "Ana");

        Assert.Equal(1, (await _service.GetPitchAsync(check.CheckId)).Count);
    }

    [Fact]
    public async Task You_need_a_name()
    {
        var check = await StartCheck();

        var ex = await Assert.ThrowsAsync<InvalidInterestCheckException>(() => CountIn(check, "p1", ""));

        Assert.Equal("What's your name?", ex.Message);
        Assert.Equal(0, (await _service.GetPitchAsync(check.CheckId)).Count);
    }

    [Fact]
    public async Task Counting_in_twice_is_refused()
    {
        var check = await StartCheck();
        await CountIn(check, "p1", "Ana");

        await Assert.ThrowsAsync<AlreadyCountedInException>(() => CountIn(check, "p1", "Ana"));

        Assert.Equal(1, (await _service.GetPitchAsync(check.CheckId)).Count);
    }

    [Fact]
    public async Task Changed_your_mind_then_back_in()
    {
        var check = await StartCheck();
        await CountIn(check, "p1", "Ana");
        await _service.ChangeMyMindAsync(check.CheckId, "p1");

        await CountIn(check, "p1", "Ana");

        Assert.Equal(1, (await _service.GetPitchAsync(check.CheckId)).Count);
    }

    [Fact]
    public async Task A_closed_check_takes_no_more_interest()
    {
        var check = await StartCheck();
        await _service.CloseAsync(check.CheckId, check.OwnerKey);

        await Assert.ThrowsAsync<InterestCheckClosedException>(() => CountIn(check, "p2", "Ben"));
    }

    [Fact]
    public async Task No_such_interest_check()
    {
        var ex = await Assert.ThrowsAsync<InterestCheckNotFoundException>(() =>
            _service.CountMeInAsync("c9", new CountMeInRequest("p1", "Ana", null)));
        Assert.Equal("No such interest check", ex.Message);
    }

    // 4. Change my mind

    [Fact]
    public async Task Changing_your_mind()
    {
        var check = await StartCheck();
        await CountIn(check, "p1", "Ana");

        await _service.ChangeMyMindAsync(check.CheckId, "p1");

        Assert.Equal(0, (await _service.GetPitchAsync(check.CheckId)).Count);
    }

    [Fact]
    public async Task You_cant_take_back_what_you_never_gave()
    {
        var check = await StartCheck();

        var ex = await Assert.ThrowsAsync<NotCountedInException>(() => _service.ChangeMyMindAsync(check.CheckId, "p1"));

        Assert.Equal("You're not counted in", ex.Message);
    }

    // 5. How it's going (owner only)

    [Fact]
    public async Task Sign_ups_per_day_and_who_is_in()
    {
        var check = await StartCheck();
        await CountIn(check, "p1", "Ana", "ana@example.com");
        await CountIn(check, "p2", "Ben");
        _clock.Advance(TimeSpan.FromDays(2));
        await CountIn(check, "p3", "Dev");
        _clock.Advance(TimeSpan.FromDays(1));
        await _service.ChangeMyMindAsync(check.CheckId, "p2");

        var progress = await _service.GetProgressAsync(check.CheckId, check.OwnerKey);

        Assert.Equal(2, progress.Count);
        Assert.Equal(10, progress.Goal);
        Assert.False(progress.GoalReached);
        // Ben took himself out, so his sign-up day only shows Ana now: we keep current state, not history.
        Assert.Equal(
            [new SignUpsPerDayDto(new DateOnly(2026, 11, 4), 1), new SignUpsPerDayDto(new DateOnly(2026, 11, 6), 1)],
            progress.SignUpsPerDay);
        Assert.Equal([new PersonDto("Ana", "ana@example.com"), new PersonDto("Dev", null)], progress.People);
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Only_the_owner_can_see_how_it_is_going(string? key)
    {
        var check = await StartCheck();

        var ex = await Assert.ThrowsAsync<NotOwnerException>(() => _service.GetProgressAsync(check.CheckId, key));

        Assert.Equal("Only the owner can see this", ex.Message);
    }

    // 6. Close the check

    [Fact]
    public async Task Closing_an_interest_check()
    {
        var check = await StartCheck();

        await _service.CloseAsync(check.CheckId, check.OwnerKey);

        Assert.False((await _service.GetPitchAsync(check.CheckId)).Open);
    }

    [Fact]
    public async Task Only_the_owner_can_close_it()
    {
        var check = await StartCheck();

        var ex = await Assert.ThrowsAsync<NotOwnerException>(() => _service.CloseAsync(check.CheckId, "wrong"));

        Assert.Equal("Only the owner can close this", ex.Message);
        Assert.True((await _service.GetPitchAsync(check.CheckId)).Open);
    }

    [Fact]
    public async Task Closing_twice_is_refused()
    {
        var check = await StartCheck();
        await _service.CloseAsync(check.CheckId, check.OwnerKey);

        var ex = await Assert.ThrowsAsync<AlreadyClosedException>(() => _service.CloseAsync(check.CheckId, check.OwnerKey));

        Assert.Equal("It's already closed", ex.Message);
    }

    [Fact]
    public async Task Closing_a_check_that_does_not_exist_is_not_found()
    {
        await Assert.ThrowsAsync<InterestCheckNotFoundException>(() => _service.CloseAsync("nope", "k"));
    }

    // Capacity, waitlist and check-in

    private static readonly DateTimeOffset DoorsOpen = new(2026, 11, 5, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DoorsClose = new(2026, 11, 5, 21, 0, 0, TimeSpan.Zero);

    private Task<StartedInterestCheckDto> StartWithDoors(int? capacity) =>
        _service.StartAsync(new StartInterestCheckRequest(
            "Board games", null, 10, null, capacity, DoorsOpen, DoorsClose));

    private Task CheckIn(StartedInterestCheckDto check, string personId) =>
        _service.CheckInAsync(check.CheckId, new CheckInRequest(personId));

    [Fact]
    public async Task The_pitch_shows_spots_left_and_the_waitlist()
    {
        var check = await StartWithDoors(capacity: 2);
        Assert.Equal(2, (await _service.GetPitchAsync(check.CheckId)).SpotsLeft);

        await CountIn(check, "p1", "Ana");
        await CountIn(check, "p2", "Ben");
        await CountIn(check, "p3", "Dev");

        var pitch = await _service.GetPitchAsync(check.CheckId);
        Assert.Equal(0, pitch.SpotsLeft);
        Assert.Equal(1, pitch.WaitlistCount);
    }

    [Fact]
    public async Task An_unlimited_check_has_no_spots_left_figure()
    {
        var check = await StartCheck();
        await CountIn(check, "p1", "Ana");

        var pitch = await _service.GetPitchAsync(check.CheckId);
        Assert.Null(pitch.SpotsLeft);
        Assert.Equal(0, pitch.WaitlistCount);
    }

    [Fact]
    public async Task Doors_must_open_before_they_close()
    {
        await Assert.ThrowsAsync<InvalidInterestCheckException>(() =>
            _service.StartAsync(new StartInterestCheckRequest("Board games", null, 10, null, null, DoorsClose, DoorsOpen)));
    }

    [Fact]
    public async Task Checking_in_during_the_window()
    {
        var check = await StartWithDoors(capacity: null);
        await CountIn(check, "p1", "Ana");
        _clock.SetUtcNow(DoorsOpen.AddMinutes(30));

        await CheckIn(check, "p1");

        var doorList = await _service.GetDoorListAsync(check.CheckId, check.OwnerKey);
        Assert.Equal([new CheckedInDto("Ana", DoorsOpen.AddMinutes(30))], doorList.CheckedIn);
        Assert.Empty(doorList.Expected);
    }

    [Fact]
    public async Task Check_in_refusals()
    {
        var check = await StartWithDoors(capacity: 1);
        await CountIn(check, "p1", "Ana");
        await CountIn(check, "p2", "Ben");

        _clock.SetUtcNow(DoorsOpen.AddMinutes(-5));
        await Assert.ThrowsAsync<OutsideEntryWindowException>(() => CheckIn(check, "p1"));

        _clock.SetUtcNow(DoorsOpen.AddMinutes(5));
        var notOnList = await Assert.ThrowsAsync<NotOnTheListException>(() => CheckIn(check, "p9"));
        Assert.Equal("You're not on the list", notOnList.Message);
        var waitlisted = await Assert.ThrowsAsync<OnTheWaitlistException>(() => CheckIn(check, "p2"));
        Assert.Equal("You're on the waitlist", waitlisted.Message);

        await CheckIn(check, "p1");
        await Assert.ThrowsAsync<AlreadyCheckedInException>(() => CheckIn(check, "p1"));
    }

    [Fact]
    public async Task Nobody_can_check_in_to_a_check_without_a_window()
    {
        var check = await StartCheck();
        await CountIn(check, "p1", "Ana");

        await Assert.ThrowsAsync<OutsideEntryWindowException>(() => CheckIn(check, "p1"));
    }

    [Fact]
    public async Task Check_in_to_a_check_that_does_not_exist_is_not_found()
    {
        await Assert.ThrowsAsync<InterestCheckNotFoundException>(() =>
            _service.CheckInAsync("nope", new CheckInRequest("p1")));
    }

    [Fact]
    public async Task The_door_list_shows_arrived_expected_and_waitlisted_in_order()
    {
        var check = await StartWithDoors(capacity: 2);
        await CountIn(check, "p1", "Ana");
        await CountIn(check, "p2", "Ben");
        await CountIn(check, "p3", "Dev");
        await CountIn(check, "p4", "Eli");
        _clock.SetUtcNow(DoorsOpen.AddMinutes(10));
        await CheckIn(check, "p2");

        var doorList = await _service.GetDoorListAsync(check.CheckId, check.OwnerKey);

        Assert.Equal([new CheckedInDto("Ben", DoorsOpen.AddMinutes(10))], doorList.CheckedIn);
        Assert.Equal(["Ana"], doorList.Expected);
        Assert.Equal(["Dev", "Eli"], doorList.Waitlist);
    }

    [Fact]
    public async Task When_a_spot_holder_changes_their_mind_the_waitlist_moves_up()
    {
        var check = await StartWithDoors(capacity: 1);
        await CountIn(check, "p1", "Ana");
        await CountIn(check, "p2", "Ben");

        await _service.ChangeMyMindAsync(check.CheckId, "p1");
        await CountIn(check, "p1", "Ana");

        var doorList = await _service.GetDoorListAsync(check.CheckId, check.OwnerKey);
        Assert.Equal(["Ben"], doorList.Expected);
        Assert.Equal(["Ana"], doorList.Waitlist);
    }

    [Fact]
    public async Task Only_the_owner_can_see_the_door_list()
    {
        var check = await StartWithDoors(capacity: null);
        await Assert.ThrowsAsync<NotOwnerException>(() => _service.GetDoorListAsync(check.CheckId, "wrong"));
    }
}
