namespace CountMeIn.Domain;

/// <summary>Someone who counted themselves in. Only reachable through its <see cref="InterestCheck"/>.</summary>
public class Participant
{
    public const int MaxNameLength = 60;
    public const int MaxEmailLength = 200;

    private Participant() { } // EF Core

    internal Participant(string personId, string name, string? email, DateOnly joinedOn)
    {
        PersonId = personId;
        Name = name;
        Email = email;
        JoinedOn = joinedOn;
    }

    public int Id { get; private set; }

    /// <summary>The caller-chosen identity (kept in the person's browser). Unique within a check.</summary>
    public string PersonId { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Email { get; private set; }

    public DateOnly JoinedOn { get; private set; }
}
