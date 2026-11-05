using CountMeIn.Domain;

namespace CountMeIn.Domain.Tests;

public class InterestCheckTests
{
    private static readonly DateOnly Today = new(2026, 11, 4);
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

        check.CountIn("p1", " Ana ", "ana@example.com", Today);

        Assert.Equal(1, check.Count);
        var ana = Assert.Single(check.Participants);
        Assert.Equal("Ana", ana.Name);
        Assert.Equal("ana@example.com", ana.Email);
        Assert.Equal(Today, ana.JoinedOn);
        Assert.True(check.IsCountedIn("p1"));
    }

    [Fact]
    public void A_blank_email_is_stored_as_no_email()
    {
        var check = NewCheck();
        check.CountIn("p1", "Ana", "  ", Today);
        Assert.Null(check.Participants.Single().Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void You_need_a_name(string? name)
    {
        var check = NewCheck();
        var ex = Assert.Throws<InvalidInterestCheckException>(() => check.CountIn("p1", name, null, Today));
        Assert.Equal("What's your name?", ex.Message);
        Assert.Equal(0, check.Count);
    }

    [Fact]
    public void Counting_in_twice_is_refused_and_does_not_add_a_second_person()
    {
        var check = NewCheck();
        check.CountIn("p1", "Ana", null, Today);

        Assert.Throws<AlreadyCountedInException>(() => check.CountIn("p1", "Ana", null, Today));
        Assert.Equal(1, check.Count);
    }

    [Fact]
    public void A_closed_check_takes_no_more_interest()
    {
        var check = NewCheck();
        check.Close(OwnerKeyValue);

        Assert.Throws<InterestCheckClosedException>(() => check.CountIn("p2", "Ben", null, Today));
        Assert.Equal(0, check.Count);
    }

    [Fact]
    public void Reaching_the_goal_is_reported()
    {
        var check = NewCheck(goal: 2);
        check.CountIn("p1", "Ana", null, Today);
        Assert.False(check.GoalReached);

        check.CountIn("p2", "Ben", null, Today);
        Assert.True(check.GoalReached);
    }

    // Changing your mind

    [Fact]
    public void Changing_your_mind_takes_you_out_of_the_count()
    {
        var check = NewCheck();
        check.CountIn("p1", "Ana", null, Today);
        check.CountIn("p2", "Ben", null, Today);

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
        check.CountIn("p1", "Ana", null, Today);
        check.Withdraw("p1");

        check.CountIn("p1", "Ana", null, Today);

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
}
