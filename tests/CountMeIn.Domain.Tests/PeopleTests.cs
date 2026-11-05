using static CountMeIn.Domain.Tests.Fixture;

namespace CountMeIn.Domain.Tests;

public class PeopleTests
{
    [Fact]
    public void Counting_in_adds_one_to_the_count()
    {
        var c = Check().In("p1");
        Assert.Equal(1, c.Count);
    }

    [Fact]
    public void Same_person_twice_is_not_a_second_person()
    {
        var c = Check().In("p1");
        new Action(() => c.CountMeIn("p1", "Ana", null, Now)).ShouldRefuse("You're already counted in");
        Assert.Equal(1, c.Count);
    }

    [Fact]
    public void A_name_is_needed() =>
        new Action(() => Check().CountMeIn("p1", "", null, Now)).ShouldRefuse("What's your name?");

    [Fact]
    public void A_closed_check_takes_no_more_interest()
    {
        var c = Check();
        c.Close(Key);
        new Action(() => c.CountMeIn("p2", "Ben", null, Now)).ShouldRefuse("This interest check has closed");
    }

    [Fact]
    public void Past_the_closing_date_the_check_is_shut()
    {
        var c = Check(closesOn: new DateOnly(2026, 11, 5));
        new Action(() => c.CountMeIn("p1", "Ana", null, Now.AddDays(2))).ShouldRefuse("This interest check has closed");
        Assert.False(c.IsOpen(Now.AddDays(2)));
    }

    [Fact]
    public void Changing_your_mind_takes_you_off_the_count()
    {
        var c = Check().In("p1", "p2");
        c.ChangeMyMind("p1", Now);
        Assert.Equal(1, c.Count);
    }

    [Fact]
    public void You_cant_take_back_what_you_never_gave() =>
        new Action(() => Check().ChangeMyMind("p1", Now)).ShouldRefuse("You're not counted in");

    [Fact]
    public void Changed_your_mind_then_back_in_works()
    {
        var c = Check().In("p1");
        c.ChangeMyMind("p1", Now);
        c.CountMeIn("p1", "Ana", null, Now);
        Assert.Equal(1, c.Count);
        Assert.Equal(2, c.Changes.Count(x => x.Kind == Checks.ChangeKind.CountedIn));
    }

    [Fact]
    public void Reaching_the_goal()
    {
        var c = Check(goal: 2).In("p1");
        Assert.False(c.GoalReached);
        c.In("p2");
        Assert.True(c.GoalReached);
    }

    [Fact]
    public void Only_the_owner_can_close()
    {
        var c = Check();
        Assert.Throws<NotOwnerException>(() => c.Close("wrong"));
        c.Close(Key);
        new Action(() => c.Close(Key)).ShouldRefuse("It's already closed");
    }
}
