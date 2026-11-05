namespace CountMeIn.Domain.Checks;

/// <summary>One person's current standing on one interest check (one row per person per check).</summary>
public class Participant
{
    private Participant() { }

    internal Participant(Guid checkId, string personId, string name, string? email)
    {
        Id = Guid.NewGuid();
        CheckId = checkId;
        PersonId = personId;
        Name = name;
        Email = email;
    }

    public Guid Id { get; private set; }
    public Guid CheckId { get; private set; }
    public string PersonId { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string? Email { get; private set; }

    public ParticipantStatus Status { get; private set; }

    /// <summary>Sign-up order. Re-joining takes a new, higher number (back of the queue).</summary>
    public int QueueNumber { get; private set; }

    /// <summary>True once the person has ever changed their mind on this check. Never reset (early birds).</summary>
    public bool HasChangedMind { get; private set; }

    public DateTimeOffset? CheckedInAt { get; private set; }

    /// <summary>When a waitlisted person first tried to get in at the door.</summary>
    public DateTimeOffset? FirstDoorAttemptAt { get; private set; }

    /// <summary>When a released spot was handed to this (previously waitlisted) person.</summary>
    public DateTimeOffset? LetInAt { get; private set; }

    /// <summary>When this person's spot was released because they had not arrived.</summary>
    public DateTimeOffset? SpotReleasedAt { get; private set; }

    public bool IsCountedIn => Status != ParticipantStatus.Withdrawn;
    public bool HoldsASpot => Status == ParticipantStatus.HoldsSpot;
    public bool IsWaitlisted => Status == ParticipantStatus.Waitlisted;

    /// <summary>Counted in, holds a spot, and has never changed their mind.</summary>
    public bool IsEarlyBird => HoldsASpot && !HasChangedMind;

    internal void CountIn(string name, string? email, int queueNumber, bool getsSpot)
    {
        Name = name;
        Email = email;
        QueueNumber = queueNumber;
        Status = getsSpot ? ParticipantStatus.HoldsSpot : ParticipantStatus.Waitlisted;
        CheckedInAt = null;
        FirstDoorAttemptAt = null;
        LetInAt = null;
        SpotReleasedAt = null;
    }

    internal void ChangeMind()
    {
        Status = ParticipantStatus.Withdrawn;
        HasChangedMind = true;
        CheckedInAt = null;
        FirstDoorAttemptAt = null;
        LetInAt = null;
        SpotReleasedAt = null;
    }

    internal void PromoteToSpot() => Status = ParticipantStatus.HoldsSpot;

    internal void CheckIn(DateTimeOffset at) => CheckedInAt = at;

    internal void RecordDoorAttempt(DateTimeOffset at) => FirstDoorAttemptAt ??= at;

    internal void ReleaseSpot(DateTimeOffset at)
    {
        Status = ParticipantStatus.Waitlisted;
        SpotReleasedAt = at;
    }

    internal void LetIn(DateTimeOffset at)
    {
        Status = ParticipantStatus.HoldsSpot;
        LetInAt = at;
    }
}
