namespace CountMeIn.Domain;

/// <summary>Someone who counted themselves in. Only reachable through its <see cref="InterestCheck"/>.</summary>
public class Participant
{
    public const int MaxNameLength = 60;
    public const int MaxEmailLength = 200;

    private Participant() { } // EF Core

    internal Participant(string personId, string name, string? email, DateTimeOffset signedUpAt)
    {
        PersonId = personId;
        Name = name;
        Email = email;
        SignedUpAt = signedUpAt;
    }

    public int Id { get; private set; }

    /// <summary>The caller-chosen identity (kept in the person's browser). Unique within a check.</summary>
    public string PersonId { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Email { get; private set; }

    public DateTimeOffset SignedUpAt { get; private set; }

    public DateTimeOffset? CheckedInAt { get; private set; }

    /// <summary>When a waitlisted person first tried to get in at the door.</summary>
    public DateTimeOffset? DoorAttemptAt { get; private set; }

    /// <summary>When the spot they held went to the waitlist because they hadn't arrived.</summary>
    public DateTimeOffset? SpotLostAt { get; private set; }

    /// <summary>When they were let in from the waitlist into a released spot.</summary>
    public DateTimeOffset? LetInAt { get; private set; }

    internal void TriedTheDoor(DateTimeOffset at) => DoorAttemptAt ??= at;

    internal void LoseSpot(DateTimeOffset at) => SpotLostAt = at;

    internal void LetIn(DateTimeOffset at) => LetInAt = at;

    internal void CheckIn(DateTimeOffset at) => CheckedInAt = at;
}
