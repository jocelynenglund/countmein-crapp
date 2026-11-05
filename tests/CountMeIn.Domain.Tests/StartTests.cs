using CountMeIn.Domain.Checks;
using static CountMeIn.Domain.Tests.Fixture;

namespace CountMeIn.Domain.Tests;

public class StartTests
{
    [Fact]
    public void Starting_keeps_the_details()
    {
        var c = Check(goal: 5, capacity: 8, open: Door(18), close: Door(20));
        Assert.Equal("Board games", c.Title);
        Assert.Equal(8, c.Capacity);
        Assert.Equal(8, c.SpotsLeft);
        Assert.True(c.IsOpen(Now));
    }

    [Theory]
    [InlineData(" ", 10, "What's the idea called?")]
    [InlineData("x", 0, "How many people would make it worth doing? At least 1.")]
    public void Needs_a_title_and_a_goal(string title, int goal, string message) =>
        new Action(() => InterestCheck.Start("a", "h", title, null, goal, null, null, null, null, Now)).ShouldRefuse(message);

    [Fact]
    public void Closing_date_in_the_past_is_refused() =>
        new Action(() => Check(closesOn: new DateOnly(2026, 11, 1))).ShouldRefuse("The closing date has already passed");

    [Fact]
    public void Capacity_must_be_at_least_one() =>
        new Action(() => Check(capacity: 0)).ShouldRefuse("Capacity must be at least 1");

    [Fact]
    public void Window_needs_both_ends() =>
        new Action(() => Check(open: Door(18))).ShouldRefuse("Give both doors open and doors close, or neither");

    [Fact]
    public void Window_must_open_before_it_closes() =>
        new Action(() => Check(open: Door(20), close: Door(18))).ShouldRefuse("Doors must open before they close");

    [Theory]
    [InlineData(18, 0, 19, 50)]  // long window: ten minutes before doors close
    [InlineData(19, 55, 19, 55)] // window shorter than ten minutes: from doors open
    public void Release_time_is_ten_minutes_before_close_or_doors_open(int oh, int om, int eh, int em) =>
        Assert.Equal(Door(eh, em), Check(capacity: 1, open: Door(oh, om), close: Door(20)).ReleaseAt);

    [Fact]
    public void No_window_means_no_release_time() => Assert.Null(Check(capacity: 1).ReleaseAt);
}
