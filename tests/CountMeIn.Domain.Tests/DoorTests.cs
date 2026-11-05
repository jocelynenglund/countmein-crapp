using static CountMeIn.Domain.Tests.Fixture;

namespace CountMeIn.Domain.Tests;

public class DoorTests
{
    [Fact]
    public void Someone_with_a_spot_checks_in()
    {
        var c = WithDoor(2).In("p1");
        Assert.True(c.CheckIn("p1", Door(18, 30)).Succeeded);
        Assert.Equal(Door(18, 30), c.Find("p1")!.CheckedInAt);
    }

    [Fact]
    public void Not_on_the_list() =>
        Assert.Equal("You're not on the list", WithDoor(2).CheckIn("p9", Door(19)).Refusal);

    [Fact]
    public void Withdrawn_people_are_not_on_the_list()
    {
        var c = WithDoor(2).In("p1");
        c.ChangeMyMind("p1", Now);
        Assert.Equal("You're not on the list", c.CheckIn("p1", Door(19)).Refusal);
    }

    [Theory]
    [InlineData(17, 59)]
    [InlineData(20, 1)]
    public void Outside_the_window_is_refused(int h, int m) =>
        Assert.Equal("The doors aren't open", WithDoor(2).In("p1").CheckIn("p1", Door(h, m)).Refusal);

    [Fact]
    public void Waitlisted_people_are_turned_away_and_remembered()
    {
        var c = WithDoor(1).In("p1", "p2");
        Assert.Equal("You're on the waitlist", c.CheckIn("p2", Door(19)).Refusal);
        Assert.Equal(Door(19), c.Find("p2")!.FirstDoorAttemptAt);
        c.CheckIn("p2", Door(19, 30));
        Assert.Equal(Door(19), c.Find("p2")!.FirstDoorAttemptAt); // first try is kept
    }

    [Fact]
    public void Checking_in_twice_is_refused()
    {
        var c = WithDoor(2).In("p1");
        c.CheckIn("p1", Door(19));
        Assert.Equal("You're already checked in", c.CheckIn("p1", Door(19, 5)).Refusal);
    }
}
