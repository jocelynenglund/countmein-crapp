using CountMeIn.Domain;

namespace CountMeIn.Domain.Tests;

public class InterestCheckTests
{
    private static readonly DateOnly Today = new(2026, 11, 4);
    private static readonly DateTimeOffset At = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
    private const string OwnerKeyValue = "k1";

    private static InterestCheck NewCheck(int goal = 10, DateOnly? closesOn = null) =>
        InterestCheck.Start("c1", "Board games", "Monthly games at the library", goal, closesOn, OwnerKeyValue, Today);

    // Starting

    [Fact]
    public void Starting_a_check_keeps_its_details_and_starts_open_and_empty()
    {
        var check = InterestCheck.Start("c1", "  Saturday board-game club ", "Monthly games", 10,
            new DateOnly(2026, 12, 1), OwnerKeyValue, Today);

        Assert.Equal("c1", check.Id);
        Assert.Equal("Saturday board-game club", check.Title);
        Assert.Equal("Monthly games", check.Pitch);
        Assert.Equal(10, check.Goal);
        Assert.Equal(new DateOnly(2026, 12, 1), check.ClosesOn);
        Assert.False(check.IsClosed);
        Assert.Equal(0, check.Count);
    }

    [Fact]
    public void The_owner_key_is_stored_hashed()
    {
        var check = NewCheck();

        Assert.NotEqual(OwnerKeyValue, check.OwnerKeyHash);
        Assert.True(check.IsOwnedBy(OwnerKeyValue));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("")]
    [InlineData(null)]
    public void An_interest_check_needs_a_title(string? title)
    {
        var ex = Assert.Throws<InvalidInterestCheckException>(() =>
            InterestCheck.Start("c1", title, null, 10, null, OwnerKeyValue, Today));
        Assert.Equal("What's the idea called?", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(InterestCheck.MaxGoal + 1)]
    public void The_goal_must_be_between_1_and_the_maximum(int goal)
    {
        var ex = Assert.Throws<InvalidInterestCheckException>(() =>
            InterestCheck.Start("c1", "Board games", null, goal, null, OwnerKeyValue, Today));
        Assert.StartsWith("How many people would make it worth doing?", ex.Message);
    }

    [Fact]
    public void A_closing_date_in_the_past_is_refused()
    {
        var ex = Assert.Throws<InvalidInterestCheckException>(() =>
            InterestCheck.Start("c1", "Board games", null, 10, new DateOnly(2026, 11, 1), OwnerKeyValue, Today));
        Assert.Equal("The closing date has already passed", ex.Message);
    }

    [Fact]
    public void A_closing_date_of_today_is_fine()
    {
        var check = NewCheck(closesOn: Today);
        Assert.Equal(Today, check.ClosesOn);
    }

    [Fact]
    public void An_overlong_title_is_refused()
    {
        Assert.Throws<InvalidInterestCheckException>(() =>
            InterestCheck.Start("c1", new string('x', InterestCheck.MaxTitleLength + 1), null, 10, null, OwnerKeyValue, Today));
    }

    // Counting in

    [Fact]
    public void Counting_yourself_in_adds_you_to_the_count()
    {
        var check = NewCheck();

        check.CountIn("p1", " Ana ", "ana@example.com", At);

        Assert.Equal(1, check.Count);
        var ana = Assert.Single(check.Participants);
        Assert.Equal("Ana", ana.Name);
        Assert.Equal("ana@example.com", ana.Email);
        Assert.Equal(At, ana.SignedUpAt);
        Assert.True(check.IsCountedIn("p1"));
    }

    [Fact]
    public void A_blank_email_is_stored_as_no_email()
    {
        var check = NewCheck();
        check.CountIn("p1", "Ana", "  ", At);
        Assert.Null(check.Participants.Single().Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void You_need_a_name(string? name)
    {
        var check = NewCheck();
        var ex = Assert.Throws<InvalidInterestCheckException>(() => check.CountIn("p1", name, null, At));
        Assert.Equal("What's your name?", ex.Message);
        Assert.Equal(0, check.Count);
    }

    [Fact]
    public void Counting_in_twice_is_refused_and_does_not_add_a_second_person()
    {
        var check = NewCheck();
        check.CountIn("p1", "Ana", null, At);

        Assert.Throws<AlreadyCountedInException>(() => check.CountIn("p1", "Ana", null, At));
        Assert.Equal(1, check.Count);
    }

    [Fact]
    public void A_closed_check_takes_no_more_interest()
    {
        var check = NewCheck();
        check.Close(OwnerKeyValue);

        Assert.Throws<InterestCheckClosedException>(() => check.CountIn("p2", "Ben", null, At));
        Assert.Equal(0, check.Count);
    }

    [Fact]
    public void Reaching_the_goal_is_reported()
    {
        var check = NewCheck(goal: 2);
        check.CountIn("p1", "Ana", null, At);
        Assert.False(check.GoalReached);

        check.CountIn("p2", "Ben", null, At);
        Assert.True(check.GoalReached);
    }

    // Changing your mind

    [Fact]
    public void Changing_your_mind_takes_you_out_of_the_count()
    {
        var check = NewCheck();
        check.CountIn("p1", "Ana", null, At);
        check.CountIn("p2", "Ben", null, At);

        check.Withdraw("p1");

        Assert.Equal(1, check.Count);
        Assert.False(check.IsCountedIn("p1"));
        Assert.True(check.IsCountedIn("p2"));
    }

    [Fact]
    public void You_cant_take_back_what_you_never_gave()
    {
        var check = NewCheck();
        Assert.Throws<NotCountedInException>(() => check.Withdraw("p1"));
    }

    [Fact]
    public void Changed_your_mind_then_back_in()
    {
        var check = NewCheck();
        check.CountIn("p1", "Ana", null, At);
        check.Withdraw("p1");

        check.CountIn("p1", "Ana", null, At);

        Assert.Equal(1, check.Count);
    }

    // Closing

    [Fact]
    public void The_owner_can_close_the_check()
    {
        var check = NewCheck();
        check.Close(OwnerKeyValue);
        Assert.True(check.IsClosed);
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData("")]
    [InlineData(null)]
    public void Only_the_owner_can_close_it(string? key)
    {
        var check = NewCheck();
        var ex = Assert.Throws<NotOwnerException>(() => check.Close(key));
        Assert.Equal("Only the owner can close this", ex.Message);
        Assert.False(check.IsClosed);
    }

    [Fact]
    public void Closing_twice_is_refused()
    {
        var check = NewCheck();
        check.Close(OwnerKeyValue);
        Assert.Throws<AlreadyClosedException>(() => check.Close(OwnerKeyValue));
    }

    // Capacity and the waitlist

    private static InterestCheck WithCapacity(int capacity) =>
        InterestCheck.Start("c1", "Board games", null, 10, null, OwnerKeyValue, Today, capacity: capacity);

    private static void CountIn(InterestCheck check, string personId, int minute) =>
        check.CountIn(personId, personId.ToUpper(), null, At.AddMinutes(minute));

    private static string[] Names(IEnumerable<Participant> people) => people.Select(p => p.PersonId).ToArray();

    [Fact]
    public void Capacity_must_be_at_least_1()
    {
        Assert.Throws<InvalidInterestCheckException>(() =>
            InterestCheck.Start("c1", "Board games", null, 10, null, OwnerKeyValue, Today, capacity: 0));
    }

    [Fact]
    public void Without_a_capacity_everyone_has_a_spot()
    {
        var check = NewCheck();
        CountIn(check, "p1", 0);
        CountIn(check, "p2", 1);

        Assert.Null(check.SpotsLeft);
        Assert.Equal(0, check.WaitlistCount);
        Assert.Equal(["p1", "p2"], Names(check.SpotHolders));
    }

    [Fact]
    public void The_first_people_in_hold_the_spots_and_the_rest_wait()
    {
        var check = WithCapacity(2);
        CountIn(check, "p1", 0);
        Assert.Equal(1, check.SpotsLeft);

        CountIn(check, "p2", 1);
        CountIn(check, "p3", 2);
        CountIn(check, "p4", 3);

        Assert.Equal(0, check.SpotsLeft);
        Assert.Equal(2, check.WaitlistCount);
        Assert.Equal(["p1", "p2"], Names(check.SpotHolders));
        Assert.Equal(["p3", "p4"], Names(check.Waitlist));
    }

    [Fact]
    public void When_a_spot_holder_changes_their_mind_the_next_in_line_moves_up()
    {
        var check = WithCapacity(2);
        CountIn(check, "p1", 0);
        CountIn(check, "p2", 1);
        CountIn(check, "p3", 2);
        CountIn(check, "p4", 3);

        check.Withdraw("p1");

        Assert.Equal(["p2", "p3"], Names(check.SpotHolders));
        Assert.Equal(["p4"], Names(check.Waitlist));
    }

    [Fact]
    public void Re_joining_puts_you_at_the_back()
    {
        var check = WithCapacity(2);
        CountIn(check, "p1", 0);
        CountIn(check, "p2", 1);
        CountIn(check, "p3", 2);
        check.Withdraw("p1");

        CountIn(check, "p1", 3);

        Assert.Equal(["p2", "p3"], Names(check.SpotHolders));
        Assert.Equal(["p1"], Names(check.Waitlist));
    }

    // Checking in

    private static readonly DateTimeOffset DoorsOpen = At.AddHours(1);
    private static readonly DateTimeOffset DoorsClose = At.AddHours(3);
    private static readonly DateTimeOffset DuringDoors = At.AddHours(2);

    private static InterestCheck WithDoors(int? capacity = null) =>
        InterestCheck.Start("c1", "Board games", null, 10, null, OwnerKeyValue, Today,
            capacity, DoorsOpen, DoorsClose);

    [Fact]
    public void Doors_must_open_before_they_close()
    {
        Assert.Throws<InvalidInterestCheckException>(() =>
            InterestCheck.Start("c1", "Board games", null, 10, null, OwnerKeyValue, Today,
                doorsOpen: DoorsClose, doorsClose: DoorsOpen));
        Assert.Throws<InvalidInterestCheckException>(() =>
            InterestCheck.Start("c1", "Board games", null, 10, null, OwnerKeyValue, Today,
                doorsOpen: DoorsOpen, doorsClose: DoorsOpen));
    }

    [Fact]
    public void A_window_needs_both_ends()
    {
        Assert.Throws<InvalidInterestCheckException>(() =>
            InterestCheck.Start("c1", "Board games", null, 10, null, OwnerKeyValue, Today, doorsOpen: DoorsOpen));
    }

    [Fact]
    public void Checking_in_during_the_window_records_the_time()
    {
        var check = WithDoors();
        CountIn(check, "p1", 0);

        check.CheckIn("p1", DuringDoors);

        Assert.Equal(DuringDoors, check.Participants.Single().CheckedInAt);
    }

    [Fact]
    public void You_cant_check_in_if_you_are_not_counted_in()
    {
        var check = WithDoors();
        var ex = Assert.Throws<NotOnTheListException>(() => check.CheckIn("p1", DuringDoors));
        Assert.Equal("You're not on the list", ex.Message);
    }

    [Theory]
    [InlineData(-1)]   // before the doors open
    [InlineData(120)]  // at the closing time
    [InlineData(500)]  // long after
    public void You_cant_check_in_outside_the_window(int minutesFromOpening)
    {
        var check = WithDoors();
        CountIn(check, "p1", 0);

        Assert.Throws<OutsideEntryWindowException>(() => check.CheckIn("p1", DoorsOpen.AddMinutes(minutesFromOpening)));
        Assert.Null(check.Participants.Single().CheckedInAt);
    }

    [Fact]
    public void Without_a_window_nobody_can_check_in()
    {
        var check = NewCheck();
        CountIn(check, "p1", 0);
        Assert.Throws<OutsideEntryWindowException>(() => check.CheckIn("p1", DuringDoors));
    }

    [Fact]
    public void A_waitlisted_person_cant_check_in_until_a_spot_opens_up()
    {
        var check = WithDoors(capacity: 1);
        CountIn(check, "p1", 0);
        CountIn(check, "p2", 1);

        var ex = Assert.Throws<OnTheWaitlistException>(() => check.CheckIn("p2", DuringDoors));
        Assert.Equal("You're on the waitlist", ex.Message);

        check.Withdraw("p1");
        check.CheckIn("p2", DuringDoors);
        Assert.NotNull(check.Participants.Single().CheckedInAt);
    }

    [Fact]
    public void You_cant_check_in_twice()
    {
        var check = WithDoors();
        CountIn(check, "p1", 0);
        check.CheckIn("p1", DuringDoors);

        Assert.Throws<AlreadyCheckedInException>(() => check.CheckIn("p1", DuringDoors.AddMinutes(1)));
        Assert.Equal(DuringDoors, check.Participants.Single().CheckedInAt);
    }

    // Releasing unclaimed spots

    private static readonly DateTimeOffset ReleaseMoment = DoorsClose.AddMinutes(-10);

    /// <summary>Capacity 2: Ana and Ben hold spots; Cy, Di and Ed wait.</summary>
    private static InterestCheck FiveSignedUp()
    {
        var check = WithDoors(capacity: 2);
        foreach (var (id, minute) in new[] { ("p1", 0), ("p2", 1), ("p3", 2), ("p4", 3), ("p5", 4) })
            CountIn(check, id, minute);
        return check;
    }

    [Fact]
    public void The_release_moment_is_ten_minutes_before_the_doors_close()
    {
        Assert.Equal(ReleaseMoment, WithDoors().ReleaseAt);
    }

    [Fact]
    public void A_window_shorter_than_ten_minutes_releases_when_the_doors_open()
    {
        var check = InterestCheck.Start("c1", "Board games", null, 10, null, OwnerKeyValue, Today,
            1, DoorsOpen, DoorsOpen.AddMinutes(5));
        Assert.Equal(DoorsOpen, check.ReleaseAt);
    }

    [Fact]
    public void Nothing_is_released_before_the_release_moment()
    {
        var check = FiveSignedUp();
        check.ReleaseUnclaimedSpots(ReleaseMoment.AddSeconds(-1));

        Assert.Null(check.SpotsReleasedAt);
        Assert.Empty(check.LostTheirSpot);
        Assert.Equal(["p1", "p2"], Names(check.SpotHolders));
    }

    [Fact]
    public void Unclaimed_spots_go_to_the_waitlisted_who_tried_the_door_first_come_first_served()
    {
        var check = FiveSignedUp();
        check.CheckIn("p1", DoorsOpen.AddMinutes(10));                       // Ana arrives
        Assert.Throws<OnTheWaitlistException>(() => check.CheckIn("p5", DoorsOpen.AddMinutes(20))); // Ed tries first
        Assert.Throws<OnTheWaitlistException>(() => check.CheckIn("p3", DoorsOpen.AddMinutes(30))); // then Cy; Di never tries

        check.ReleaseUnclaimedSpots(ReleaseMoment);

        Assert.Equal(["p2"], Names(check.LostTheirSpot));
        Assert.Equal(["p5"], Names(check.LetInFromWaitlist));    // only Ben's spot was unclaimed
        Assert.Equal(["p1", "p5"], Names(check.SpotHolders));
        Assert.Equal(["p3", "p4"], Names(check.Waitlist));
        Assert.Equal(ReleaseMoment, check.SpotsReleasedAt);
    }

    [Fact]
    public void Someone_let_in_can_then_check_in_normally()
    {
        var check = FiveSignedUp();
        Assert.Throws<OnTheWaitlistException>(() => check.CheckIn("p3", DoorsOpen.AddMinutes(20)));
        check.ReleaseUnclaimedSpots(ReleaseMoment);

        check.CheckIn("p3", ReleaseMoment.AddMinutes(1));

        Assert.NotNull(check.Participants.Single(p => p.PersonId == "p3").CheckedInAt);
    }

    [Fact]
    public void Spot_holders_who_lost_their_spot_are_refused_if_they_arrive_later()
    {
        var check = FiveSignedUp();
        check.ReleaseUnclaimedSpots(ReleaseMoment);

        var ex = Assert.Throws<SpotWentToWaitlistException>(() => check.CheckIn("p1", ReleaseMoment.AddMinutes(1)));
        Assert.Equal("Your spot went to the waitlist", ex.Message);
    }

    [Fact]
    public void Waitlisted_people_who_never_tried_the_door_are_not_let_in()
    {
        var check = FiveSignedUp();
        check.ReleaseUnclaimedSpots(ReleaseMoment);

        Assert.Empty(check.LetInFromWaitlist);
        Assert.Equal(["p3", "p4", "p5"], Names(check.Waitlist));
        Assert.Throws<OnTheWaitlistException>(() => check.CheckIn("p3", ReleaseMoment.AddMinutes(1)));
    }

    [Fact]
    public void Trying_the_door_after_the_release_does_not_earn_a_spot()
    {
        var check = FiveSignedUp();
        check.ReleaseUnclaimedSpots(ReleaseMoment);
        Assert.Throws<OnTheWaitlistException>(() => check.CheckIn("p3", ReleaseMoment.AddMinutes(1)));

        Assert.Null(check.Participants.Single(p => p.PersonId == "p3").DoorAttemptAt);
    }

    [Fact]
    public void Only_the_first_attempt_counts()
    {
        var check = FiveSignedUp();
        Assert.Throws<OnTheWaitlistException>(() => check.CheckIn("p3", DoorsOpen.AddMinutes(10)));
        Assert.Throws<OnTheWaitlistException>(() => check.CheckIn("p3", DoorsOpen.AddMinutes(50)));

        Assert.Equal(DoorsOpen.AddMinutes(10), check.Participants.Single(p => p.PersonId == "p3").DoorAttemptAt);
    }

    [Fact]
    public void A_refused_attempt_outside_the_window_is_not_recorded()
    {
        var check = FiveSignedUp();
        Assert.Throws<OutsideEntryWindowException>(() => check.CheckIn("p3", DoorsOpen.AddMinutes(-5)));
        Assert.Null(check.Participants.Single(p => p.PersonId == "p3").DoorAttemptAt);
    }

    [Fact]
    public void The_release_only_happens_once()
    {
        var check = FiveSignedUp();
        Assert.Throws<OnTheWaitlistException>(() => check.CheckIn("p3", DoorsOpen.AddMinutes(10)));
        check.ReleaseUnclaimedSpots(ReleaseMoment);

        check.ReleaseUnclaimedSpots(ReleaseMoment.AddMinutes(5));

        Assert.Equal(["p1", "p2"], Names(check.LostTheirSpot));
        Assert.Equal(ReleaseMoment, check.SpotsReleasedAt);
    }

    [Fact]
    public void An_unlimited_check_has_nothing_to_release()
    {
        var check = WithDoors();
        CountIn(check, "p1", 0);
        check.ReleaseUnclaimedSpots(ReleaseMoment);

        Assert.Null(check.SpotsReleasedAt);
        Assert.Empty(check.LostTheirSpot);
    }

    [Fact]
    public void Released_spots_nobody_asked_for_stay_empty_and_are_not_counted_as_spots_left()
    {
        var check = FiveSignedUp();
        check.ReleaseUnclaimedSpots(ReleaseMoment);

        Assert.Equal(0, check.SpotsLeft);
        Assert.Equal(3, check.WaitlistCount);
    }
}
