using CountMeIn.Domain.Checks;

namespace CountMeIn.Domain.Tests;

public static class Fixture
{
    public static readonly DateTimeOffset Now = new(2026, 11, 4, 10, 0, 0, TimeSpan.Zero);
    public const string Key = "hash-of-k1";

    public static InterestCheck Check(int goal = 10, int? capacity = null, DateTimeOffset? open = null, DateTimeOffset? close = null,
        DateOnly? closesOn = null) =>
        InterestCheck.Start("abc", Key, "Board games", "Monthly", goal, closesOn, capacity, open, close, Now);

    /// <summary>A check with a 18:00-20:00 door on the 10th.</summary>
    public static InterestCheck WithDoor(int capacity) => Check(capacity: capacity, open: Door(18), close: Door(20));

    public static DateTimeOffset Door(int hour, int minute = 0) => new(2026, 11, 10, hour, minute, 0, TimeSpan.Zero);

    public static InterestCheck In(this InterestCheck c, params string[] people)
    {
        foreach (var p in people) c.CountMeIn(p, p.ToUpperInvariant(), null, Now);
        return c;
    }

    public static void ShouldRefuse(this Action act, string message) =>
        Assert.Equal(message, Assert.Throws<DomainException>(act).Message);
}
