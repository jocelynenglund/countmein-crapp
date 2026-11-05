namespace CountMeIn.Domain;

/// <summary>
/// Aggregate root: an idea somebody wants to find out whether enough people care about.
/// All changes to the check and its participants go through this class so the rules hold.
/// </summary>
public class InterestCheck
{
    public const int MaxTitleLength = 120;
    public const int MaxPitchLength = 1000;
    public const int MaxGoal = 100_000;

    private readonly List<Participant> _participants = [];

    private InterestCheck() { } // EF Core

    public string Id { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string Pitch { get; private set; } = "";

    public int Goal { get; private set; }

    public DateOnly? ClosesOn { get; private set; }

    public string OwnerKeyHash { get; private set; } = null!;

    public bool IsClosed { get; private set; }

    public IReadOnlyCollection<Participant> Participants => _participants;

    public int Count => _participants.Count;

    public bool GoalReached => Count >= Goal;

    public static InterestCheck Start(
        string id, string? title, string? pitch, int goal, DateOnly? closesOn, string ownerKey, DateOnly today)
    {
        if (!Identifiers.IsValid(id)) throw new InvalidInterestCheckException("invalid checkId");

        title = (title ?? "").Trim();
        if (title.Length == 0) throw new InvalidInterestCheckException("What's the idea called?");
        if (title.Length > MaxTitleLength)
            throw new InvalidInterestCheckException($"The title must be {MaxTitleLength} characters or fewer");

        pitch = (pitch ?? "").Trim();
        if (pitch.Length > MaxPitchLength)
            throw new InvalidInterestCheckException($"The pitch must be {MaxPitchLength} characters or fewer");

        if (goal < 1 || goal > MaxGoal)
            throw new InvalidInterestCheckException(
                $"How many people would make it worth doing? Between 1 and {MaxGoal}.");

        if (closesOn is { } date && date < today)
            throw new InvalidInterestCheckException("The closing date has already passed");

        if (string.IsNullOrEmpty(ownerKey)) throw new InvalidInterestCheckException("An owner key is required");

        return new InterestCheck
        {
            Id = id,
            Title = title,
            Pitch = pitch,
            Goal = goal,
            ClosesOn = closesOn,
            OwnerKeyHash = OwnerKey.Hash(ownerKey),
        };
    }

    public bool IsCountedIn(string personId) => _participants.Any(p => p.PersonId == personId);

    public bool IsOwnedBy(string? ownerKey) => OwnerKey.Matches(ownerKey, OwnerKeyHash);

    public void CountIn(string personId, string? name, string? email, DateOnly today)
    {
        if (IsClosed) throw new InterestCheckClosedException();

        name = (name ?? "").Trim();
        if (name.Length == 0) throw new InvalidInterestCheckException("What's your name?");
        if (name.Length > Participant.MaxNameLength)
            throw new InvalidInterestCheckException($"Your name must be {Participant.MaxNameLength} characters or fewer");

        email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (email is { Length: > Participant.MaxEmailLength })
            throw new InvalidInterestCheckException($"Your email must be {Participant.MaxEmailLength} characters or fewer");

        if (!Identifiers.IsValid(personId)) throw new InvalidInterestCheckException("invalid personId");
        if (IsCountedIn(personId)) throw new AlreadyCountedInException();

        _participants.Add(new Participant(personId, name, email, today));
    }

    /// <summary>
    /// Changing your mind removes your row, so you are simply not in any more (and may join again).
    /// Allowed on a closed check too: you can always take your details back.
    /// </summary>
    public void Withdraw(string personId)
    {
        var participant = _participants.FirstOrDefault(p => p.PersonId == personId)
                          ?? throw new NotCountedInException();
        _participants.Remove(participant);
    }

    public void Close(string? ownerKey)
    {
        if (!IsOwnedBy(ownerKey)) throw new NotOwnerException("Only the owner can close this");
        if (IsClosed) throw new AlreadyClosedException();
        IsClosed = true;
    }
}
