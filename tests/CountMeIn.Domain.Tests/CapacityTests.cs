using static CountMeIn.Domain.Tests.Fixture;

namespace CountMeIn.Domain.Tests;

public class CapacityTests
{
    [Fact]
    public void The_first_people_hold_spots_and_the_rest_wait()
    {
        var c = Check(capacity: 2).In("p1", "p2", "p3");
        Assert.True(c.Find("p1")!.HoldsASpot);
        Assert.True(c.Find("p2")!.HoldsASpot);
        Assert.True(c.Find("p3")!.IsWaitlisted);
        Assert.Equal(3, c.Count);
        Assert.Equal(0, c.SpotsLeft);
        Assert.Equal(1, c.WaitlistCount);
    }

    [Fact]
    public void Unlimited_means_everyone_holds_a_spot_and_spots_left_is_null()
    {
        var c = Check().In("p1", "p2", "p3");
        Assert.Null(c.SpotsLeft);
        Assert.Equal(0, c.WaitlistCount);
    }

    [Fact]
    public void When_a_holder_changes_their_mind_the_next_waitlisted_moves_up()
    {
        var c = Check(capacity: 1).In("p1", "p2", "p3");
        c.ChangeMyMind("p1", Now);
        Assert.True(c.Find("p2")!.HoldsASpot);
        Assert.True(c.Find("p3")!.IsWaitlisted);
    }

    [Fact]
    public void A_waitlisted_person_leaving_frees_nothing()
    {
        var c = Check(capacity: 1).In("p1", "p2");
        c.ChangeMyMind("p2", Now);
        Assert.True(c.Find("p1")!.HoldsASpot);
        Assert.Equal(0, c.SpotsLeft);
    }

    [Fact]
    public void Rejoining_puts_you_at_the_back()
    {
        var c = Check(capacity: 1).In("p1", "p2");
        c.ChangeMyMind("p1", Now);          // p2 moves up
        c.CountMeIn("p1", "P1", null, Now); // p1 is back, behind p2
        Assert.True(c.Find("p2")!.HoldsASpot);
        Assert.True(c.Find("p1")!.IsWaitlisted);
    }

    [Fact]
    public void Rejoining_with_a_free_spot_takes_it()
    {
        var c = Check(capacity: 2).In("p1");
        c.ChangeMyMind("p1", Now);
        c.CountMeIn("p1", "P1", null, Now);
        Assert.True(c.Find("p1")!.HoldsASpot);
    }

    [Fact]
    public void Promotion_follows_sign_up_order()
    {
        var c = Check(capacity: 1).In("p1", "p2", "p3", "p4");
        c.ChangeMyMind("p1", Now);
        c.ChangeMyMind("p2", Now);
        Assert.True(c.Find("p3")!.HoldsASpot);
        Assert.True(c.Find("p4")!.IsWaitlisted);
    }
}
