using static CountMeIn.Domain.Tests.Fixture;

namespace CountMeIn.Domain.Tests;

public class VenueTests
{
    private static readonly DateTimeOffset Reveal = Door(12);

    private static Checks.InterestCheck WithVenue(int? capacity = 2)
    {
        var c = Check(capacity: capacity).In("p1", "p2", "p3");
        c.SetVenue(Key, "The Library", Reveal, Now);
        return c;
    }

    [Fact]
    public void Only_the_owner_sets_the_venue() =>
        Assert.Throws<NotOwnerException>(() => Check().SetVenue("wrong", "X", Reveal, Now));

    [Fact]
    public void Can_change_it_until_it_is_revealed()
    {
        var c = WithVenue();
        c.SetVenue(Key, "The Cafe", Reveal.AddHours(1), Reveal.AddMinutes(-1));
        Assert.Equal("The Cafe", c.Venue);
    }

    [Fact]
    public void After_reveal_time_it_is_locked() =>
        new Action(() => WithVenue().SetVenue(Key, "X", Reveal, Reveal)).ShouldRefuse("The venue has already been revealed");

    [Fact]
    public void Before_the_reveal_only_the_time_is_shown()
    {
        var v = WithVenue().VenueFor("p2", Reveal.AddHours(-3));
        Assert.False(v.Visible);
        Assert.Null(v.Venue);
        Assert.Equal("Venue revealed at 2026-11-10 12:00 UTC", v.Message);
    }

    [Fact]
    public void From_the_reveal_a_spot_holder_sees_it()
    {
        // p1 and p2 hold spots; p3 waits
        var c = WithVenue();
        Assert.Equal("The Library", c.VenueFor("p2", Reveal).Venue);
        Assert.False(c.VenueFor("p3", Reveal).Visible);
        Assert.False(c.VenueFor("stranger", Reveal).Visible);
    }

    [Fact]
    public void Someone_who_changed_their_mind_cant_see_it()
    {
        var c = WithVenue();
        c.ChangeMyMind("p1", Now);
        Assert.False(c.VenueFor("p1", Reveal).Visible);
    }

    [Fact]
    public void Early_birds_see_it_an_hour_early()
    {
        var c = WithVenue();
        Assert.True(c.VenueFor("p1", Reveal.AddMinutes(-60)).Visible);
        Assert.False(c.VenueFor("p1", Reveal.AddMinutes(-61)).Visible);
    }

    [Fact]
    public void Changing_your_mind_even_if_you_come_back_costs_the_early_bird_perk()
    {
        var c = Check(capacity: 5).In("p1", "p2");
        c.SetVenue(Key, "The Library", Reveal, Now);
        c.ChangeMyMind("p1", Now);
        c.CountMeIn("p1", "P1", null, Now);
        Assert.True(c.Find("p1")!.HoldsASpot);
        Assert.False(c.VenueFor("p1", Reveal.AddMinutes(-30)).Visible);
        Assert.True(c.VenueFor("p1", Reveal).Visible);
        Assert.True(c.VenueFor("p2", Reveal.AddMinutes(-30)).Visible);
    }

    [Fact]
    public void A_waitlisted_early_bird_candidate_gets_nothing_early()
    {
        var c = WithVenue();
        Assert.False(c.VenueFor("p3", Reveal.AddMinutes(-30)).Visible);
    }

    [Fact]
    public void A_waitlisted_person_who_is_let_in_sees_it_from_the_reveal_not_early()
    {
        var c = Check(capacity: 1, open: Door(18), close: Door(20)).In("p1", "p2");
        c.SetVenue(Key, "The Library", Door(19, 40), Now);
        c.CheckIn("p2", Door(19, 45));
        c.ReleaseUnclaimedSpots(Door(19, 50));
        Assert.True(c.Find("p2")!.HoldsASpot);
        Assert.True(c.Find("p2")!.IsEarlyBird); // never changed mind, now holds a spot
        Assert.True(c.VenueFor("p2", Door(19, 50)).Visible);
    }
}
