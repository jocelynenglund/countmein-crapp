namespace CountMeIn.Domain.Checks;

/// <summary>
/// One row each time someone counted in or changed their mind. Kept (insert-only) because
/// "how it's going" reports both directions per day; current state lives on <see cref="Participant"/>.
/// </summary>
public class ParticipationChange
{
    private ParticipationChange() { }

    internal ParticipationChange(Guid checkId, string personId, ChangeKind kind, DateTimeOffset at)
    {
        Id = Guid.NewGuid();
        CheckId = checkId;
        PersonId = personId;
        Kind = kind;
        OccurredAt = at;
        Day = DateOnly.FromDateTime(at.UtcDateTime);
    }

    public Guid Id { get; private set; }
    public Guid CheckId { get; private set; }
    public string PersonId { get; private set; } = "";
    public ChangeKind Kind { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateOnly Day { get; private set; }
}
