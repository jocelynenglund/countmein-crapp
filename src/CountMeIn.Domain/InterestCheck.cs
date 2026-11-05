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

    /// <summary>How many people get a spot; null means unlimited. Everyone after that is on the waitlist.</summary>
    public int? Capacity { get; private set; }

    /// <summary>The entry window. Without one, nobody can check in.</summary>
    public DateTimeOffset? DoorsOpen { get; private set; }

    public DateTimeOffset? DoorsClose { get; private set; }

    public string OwnerKeyHash { get; private set; } = null!;

    public bool IsClosed { get; private set; }

    public IReadOnlyCollection<Participant> Participants => _participants;

    public int Count => _participants.Count;

    public bool GoalReached => Count >= Goal;

    public int? SpotsLeft => Capacity is { } capacity ? Math.Max(0, capacity - Count) : null;

    public int WaitlistCount => Capacity is { } capacity ? Math.Max(0, Count - capacity) : 0;

    /// <summary>Everyone counted in, in sign-up order.</summary>
    public IEnumerable<Participant> InSignUpOrder => _participants.OrderBy(p => p.SignedUpAt).ThenBy(p => p.Id);

    /// <summary>The first <c>Capacity</c> people in sign-up order hold a spot.</summary>
    public IEnumerable<Participant> SpotHolders => Capacity is { } capacity ? InSignUpOrder.Take(capacity) : InSignUpOrder;

    /// <summary>Everyone after the first <c>Capacity</c> people, in the order they will move up.</summary>
    public IEnumerable<Participant> Waitlist => Capacity is { } capacity ? InSignUpOrder.Skip(capacity) : [];

    public static InterestCheck Start(
        string id, string? title, string? pitch, int goal, DateOnly? closesOn, string ownerKey, DateOnly today,
        int? capacity = null, DateTimeOffset? doorsOpen = null, DateTimeOffset? doorsClose = null)
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

        if (capacity is < 1) throw new InvalidInterestCheckException("Capacity must be at least 1");

        if ((doorsOpen is null) != (doorsClose is null))
            throw new InvalidInterestCheckException("Give both when the doors open and when they close");
        if (doorsOpen >= doorsClose) throw new InvalidInterestCheckException("The doors must open before they close");

        if (string.IsNullOrEmpty(ownerKey)) throw new InvalidInterestCheckException("An owner key is required");

        return new InterestCheck
        {
            Id = id,
            Title = title,
            Pitch = pitch,
            Goal = goal,
            ClosesOn = closesOn,
            Capacity = capacity,
            DoorsOpen = doorsOpen,
            DoorsClose = doorsClose,
            OwnerKeyHash = OwnerKey.Hash(ownerKey),
        };
    }

    public bool IsCountedIn(string personId) => _participants.Any(p => p.PersonId == personId);

    public bool IsOwnedBy(string? ownerKey) => OwnerKey.Matches(ownerKey, OwnerKeyHash);

    public void CountIn(string personId, string? name, string? email, DateTimeOffset at)
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

        _participants.Add(new Participant(personId, name, email, at));
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

    /// <summary>Anyone who holds a spot and arrives while the doors are open is checked in.</summary>
    public void CheckIn(string personId, DateTimeOffset at)
    {
        var participant = _participants.FirstOrDefault(p => p.PersonId == personId)
                          ?? throw new NotOnTheListException();
        if (DoorsOpen is not { } open || DoorsClose is not { } close || at < open || at >= close)
            throw new OutsideEntryWindowException();
        if (Waitlist.Contains(participant)) throw new OnTheWaitlistException();
        if (participant.CheckedInAt is not null) throw new AlreadyCheckedInException();

        participant.CheckIn(at);
    }

    public void Close(string? ownerKey)
    {
        if (!IsOwnedBy(ownerKey)) throw new NotOwnerException("Only the owner can close this");
        if (IsClosed) throw new AlreadyClosedException();
        IsClosed = true;
    }
}
