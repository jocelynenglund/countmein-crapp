using static CountMeIn.Domain.Tests.Fixture;

namespace CountMeIn.Domain.Tests;

public class ReleaseTests
{
    [Fact]
    public void Nothing_happens_before_the_release_time()
    {
        var c = WithDoor(1).In("p1", "p2");
        c.CheckIn("p2", Door(19));
        Assert.Equal(0, c.ReleaseUnclaimedSpots(Door(19, 49)));
        Assert.True(c.Find("p1")!.HoldsASpot);
    }

    [Fact]
    public void Unclaimed_spots_go_to_waitlisted_people_who_tried_the_door()
    {
        var c = WithDoor(1).In("p1", "p2", "p3");
        c.CheckIn("p3", Door(19));
        c.ReleaseUnclaimedSpots(Door(19, 50));

        Assert.True(c.Find("p3")!.HoldsASpot);
        Assert.Equal(Door(19, 50), c.Find("p3")!.LetInAt);
        Assert.True(c.Find("p1")!.IsWaitlisted);
        Assert.NotNull(c.Find("p1")!.SpotReleasedAt);
        Assert.True(c.Find("p2")!.IsWaitlisted); // never tried, stays put
        Assert.Null(c.Find("p2")!.LetInAt);
    }

    [Fact]
    public void First_come_first_served_by_first_attempt()
    {
        var c = WithDoor(2).In("p1", "p2", "p3", "p4");
        c.CheckIn("p4", Door(19, 0));
        c.CheckIn("p3", Door(19, 10));
        c.CheckIn("p3", Door(19, 5)); // later retries don't jump the line
        c.ReleaseUnclaimedSpots(Door(19, 50));
        Assert.True(c.Find("p4")!.HoldsASpot);
        Assert.True(c.Find("p3")!.HoldsASpot);
    }

    [Fact]
    public void Only_as_many_are_let_in_as_spots_were_released()
    {
        var c = WithDoor(2).In("p1", "p2", "p3", "p4", "p5");
        c.CheckIn("p1", Door(19));
        c.CheckIn("p3", Door(19, 1));
        c.CheckIn("p4", Door(19, 2));
        c.CheckIn("p5", Door(19, 3));
        c.ReleaseUnclaimedSpots(Door(19, 50)); // p2 is the only no-show
        Assert.True(c.Find("p3")!.HoldsASpot);
        Assert.True(c.Find("p4")!.IsWaitlisted);
        Assert.True(c.Find("p5")!.IsWaitlisted);
    }

    [Fact]
    public void Checked_in_people_keep_their_spot()
    {
        var c = WithDoor(1).In("p1", "p2");
        c.CheckIn("p1", Door(19));
        c.CheckIn("p2", Door(19, 1));
        c.ReleaseUnclaimedSpots(Door(19, 50));
        Assert.True(c.Find("p1")!.HoldsASpot);
        Assert.True(c.Find("p2")!.IsWaitlisted);
    }

    [Fact]
    public void Someone_who_withdrew_after_trying_is_not_let_in()
    {
        var c = WithDoor(1).In("p1", "p2", "p3");
        c.CheckIn("p2", Door(19));
        c.CheckIn("p3", Door(19, 1));
        c.ChangeMyMind("p2", Now);
        c.ReleaseUnclaimedSpots(Door(19, 50));
        Assert.True(c.Find("p3")!.HoldsASpot);
        Assert.False(c.Find("p2")!.IsCountedIn);
    }

    [Fact]
    public void A_let_in_person_then_checks_in_normally()
    {
        var c = WithDoor(1).In("p1", "p2");
        c.CheckIn("p2", Door(19));
        c.ReleaseUnclaimedSpots(Door(19, 50));
        Assert.True(c.CheckIn("p2", Door(19, 55)).Succeeded);
    }

    [Fact]
    public void Someone_who_lost_their_spot_is_told_so()
    {
        var c = WithDoor(1).In("p1");
        c.ReleaseUnclaimedSpots(Door(19, 50));
        Assert.Equal("Your spot went to the waitlist", c.CheckIn("p1", Door(19, 55)).Refusal);
        Assert.Null(c.Find("p1")!.FirstDoorAttemptAt);
    }

    [Fact]
    public void Releasing_happens_only_once()
    {
        var c = WithDoor(1).In("p1");
        c.ReleaseUnclaimedSpots(Door(19, 50));
        Assert.Equal(0, c.ReleaseUnclaimedSpots(Door(19, 51)));
        Assert.Equal(Door(19, 50), c.SpotsReleasedAt);
    }

    [Fact]
    public void A_short_window_releases_from_doors_open()
    {
        var c = Check(capacity: 1, open: Door(19, 55), close: Door(20));
        c.In("p1");
        Assert.Equal(0, c.ReleaseUnclaimedSpots(Door(19, 54)));
        c.ReleaseUnclaimedSpots(Door(19, 55));
        Assert.True(c.Find("p1")!.IsWaitlisted);
    }
}
