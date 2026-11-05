namespace CountMeIn.Domain.Checks;

public enum ParticipantStatus
{
    /// <summary>Changed their mind (or never counted in). Kept as a row so a later re-join and early-bird rules know the history.</summary>
    Withdrawn = 0,
    /// <summary>Counted in and holds a spot.</summary>
    HoldsSpot = 1,
    /// <summary>Counted in but waiting for a spot.</summary>
    Waitlisted = 2,
}

public enum ChangeKind
{
    CountedIn = 1,
    ChangedMind = 2,
}
