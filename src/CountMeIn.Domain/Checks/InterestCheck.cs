namespace CountMeIn.Domain.Checks;

/// <summary>
/// Aggregate root. Owns the people on the check, their spots, the door and the venue,
/// so every rule that crosses them (capacity, waitlist, release) is enforced in one place.
/// </summary>
public class InterestCheck
{
    public static readonly TimeSpan ReleaseLeadTime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan EarlyBirdHead = TimeSpan.FromHours(1);

    private readonly List<Participant> _participants = [];
    private readonly List<ParticipationChange> _changes = [];

    private InterestCheck() { }

    public Guid Id { get; private set; }
    public string ShareId { get; private set; } = "";
    public string OwnerKeyHash { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string Pitch { get; private set; } = "";
    public int Goal { get; private set; }
    public DateOnly? ClosesOn { get; private set; }
    public int? Capacity { get; private set; }
    public DateTimeOffset? DoorsOpen { get; private set; }
    public DateTimeOffset? DoorsClose { get; private set; }

    /// <summary>When unclaimed spots are released. Derived from the window once, so a job can query on it.</summary>
    public DateTimeOffset? ReleaseAt { get; private set; }
    public DateTimeOffset? SpotsReleasedAt { get; private set; }

    public bool IsClosed { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public string? Venue { get; private set; }
    public DateTimeOffset? VenueRevealAt { get; private set; }

    private int NextQueueNumber { get; set; } = 1;

    /// <summary>Optimistic concurrency token, bumped by every change to the aggregate.</summary>
    public int Version { get; private set; }

    public IReadOnlyCollection<Participant> Participants => _participants;
    public IReadOnlyCollection<ParticipationChange> Changes => _changes;

    public static InterestCheck Start(
        string shareId, string ownerKeyHash, string title, string? pitch, int goal, DateOnly? closesOn,
        int? capacity, DateTimeOffset? doorsOpen, DateTimeOffset? doorsClose, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("What's the idea called?");
        if (goal < 1) throw new DomainException("How many people would make it worth doing? At least 1.");
        if (closesOn is { } c && c < DateOnly.FromDateTime(now.UtcDateTime))
            throw new DomainException("The closing date has already passed");
        if (capacity is < 1) throw new DomainException("Capacity must be at least 1");
        if ((doorsOpen is null) != (doorsClose is null))
            throw new DomainException("Give both doors open and doors close, or neither");
        if (doorsOpen >= doorsClose) throw new DomainException("Doors must open before they close");

        return new InterestCheck
        {
            Id = Guid.NewGuid(),
            ShareId = shareId,
            OwnerKeyHash = ownerKeyHash,
            Title = title.Trim(),
            Pitch = pitch?.Trim() ?? "",
            Goal = goal,
            ClosesOn = closesOn,
            Capacity = capacity,
            DoorsOpen = doorsOpen,
            DoorsClose = doorsClose,
            ReleaseAt = doorsOpen is { } open && doorsClose is { } close
                ? Max(open, close - ReleaseLeadTime)
                : null,
            CreatedAt = now,
        };
    }

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;

    // ---- queries over the aggregate -------------------------------------------------------

    public int Count => _participants.Count(p => p.IsCountedIn);
    public int SpotsHeld => _participants.Count(p => p.HoldsASpot);
    public int WaitlistCount => _participants.Count(p => p.IsWaitlisted);
    public bool GoalReached => Count >= Goal;
    public int? SpotsLeft => Capacity is { } cap ? Math.Max(0, cap - SpotsHeld) : null;
    public bool HasWindow => DoorsOpen is not null;

    public bool IsOpen(DateTimeOffset now) =>
        !IsClosed && (ClosesOn is null || DateOnly.FromDateTime(now.UtcDateTime) <= ClosesOn);

    public bool IsOwner(string ownerKeyHash) =>
        System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(OwnerKeyHash), System.Text.Encoding.UTF8.GetBytes(ownerKeyHash));

    public void EnsureOwner(string ownerKeyHash, string message = "Only the owner can do this")
    {
        if (!IsOwner(ownerKeyHash)) throw new NotOwnerException(message);
    }

    public Participant? Find(string personId) => _participants.FirstOrDefault(p => p.PersonId == personId);

    // ---- people ---------------------------------------------------------------------------

    public void CountMeIn(string personId, string name, string? email, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(personId)) throw new DomainException("Who are you? A person id is needed");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("What's your name?");
        if (!IsOpen(now)) throw new DomainException("This interest check has closed");

        var person = Find(personId);
        if (person is { IsCountedIn: true }) throw new DomainException("You're already counted in");
        if (person is null)
        {
            person = new Participant(Id, personId, name.Trim(), email);
            _participants.Add(person);
        }

        var getsSpot = Capacity is null || SpotsHeld < Capacity;
        person.CountIn(name.Trim(), string.IsNullOrWhiteSpace(email) ? null : email.Trim(), NextQueueNumber++, getsSpot);
        _changes.Add(new ParticipationChange(Id, personId, ChangeKind.CountedIn, now));
        Version++;
    }

    public void ChangeMyMind(string personId, DateTimeOffset now)
    {
        var person = Find(personId);
        if (person is not { IsCountedIn: true }) throw new DomainException("You're not counted in");

        person.ChangeMind();
        _changes.Add(new ParticipationChange(Id, personId, ChangeKind.ChangedMind, now));
        FillFreeSpots();
        Version++;
    }

    /// <summary>The next waitlisted person (sign-up order) moves up. Someone whose spot was released has lost it for good.</summary>
    private void FillFreeSpots()
    {
        while (Capacity is { } cap && SpotsHeld < cap)
        {
            var next = _participants
                .Where(p => p.IsWaitlisted && p.SpotReleasedAt is null)
                .OrderBy(p => p.QueueNumber)
                .FirstOrDefault();
            if (next is null) return;
            next.PromoteToSpot();
        }
    }

    // ---- closing --------------------------------------------------------------------------

    public void Close(string ownerKeyHash)
    {
        EnsureOwner(ownerKeyHash, "Only the owner can close this");
        if (IsClosed) throw new DomainException("It's already closed");
        IsClosed = true;
        Version++;
    }

    // ---- the door -------------------------------------------------------------------------

    public CheckInResult CheckIn(string personId, DateTimeOffset at)
    {
        var person = Find(personId);
        if (person is not { IsCountedIn: true }) return new("You're not on the list");
        if (HasWindow && (at < DoorsOpen || at > DoorsClose)) return new("The doors aren't open");

        if (person.IsWaitlisted)
        {
            if (person.SpotReleasedAt is not null) return new("Your spot went to the waitlist");
            person.RecordDoorAttempt(at);
            Version++;
            return new("You're on the waitlist");
        }

        if (person.CheckedInAt is not null) return new("You're already checked in");

        person.CheckIn(at);
        Version++;
        return CheckInResult.Ok;
    }

    /// <summary>
    /// Releases spots nobody turned up for and hands them to waitlisted people who already tried the door,
    /// earliest first. Runs once per check. Returns how many people were let in.
    /// </summary>
    public int ReleaseUnclaimedSpots(DateTimeOffset now)
    {
        if (ReleaseAt is null || SpotsReleasedAt is not null || now < ReleaseAt) return 0;

        SpotsReleasedAt = now;
        Version++;
        if (Capacity is null) return 0;

        var released = _participants.Where(p => p.HoldsASpot && p.CheckedInAt is null).ToList();
        foreach (var p in released) p.ReleaseSpot(now);

        var waiting = _participants
            .Where(p => p.IsWaitlisted && p.SpotReleasedAt is null && p.FirstDoorAttemptAt is not null)
            .OrderBy(p => p.FirstDoorAttemptAt)
            .Take(released.Count)
            .ToList();
        foreach (var p in waiting) p.LetIn(now);
        return waiting.Count;
    }

    // ---- venue ----------------------------------------------------------------------------

    public void SetVenue(string ownerKeyHash, string venue, DateTimeOffset revealAt, DateTimeOffset now)
    {
        EnsureOwner(ownerKeyHash, "Only the owner can set the venue");
        if (VenueRevealAt is { } current && current <= now)
            throw new DomainException("The venue has already been revealed");
        if (string.IsNullOrWhiteSpace(venue)) throw new DomainException("Where is it?");
        Venue = venue.Trim();
        VenueRevealAt = revealAt;
        Version++;
    }

    public VenueView VenueFor(string personId, DateTimeOffset now)
    {
        if (Venue is null || VenueRevealAt is not { } revealAt)
            return new(false, null, null, "The venue hasn't been set yet");

        var person = Find(personId);
        var eligible = person is { IsCountedIn: true, HoldsASpot: true };
        var shownFrom = person is { IsEarlyBird: true } ? revealAt - EarlyBirdHead : revealAt;

        if (eligible && now >= shownFrom) return new(true, Venue, revealAt, "");
        if (now >= revealAt) return new(false, null, revealAt, "The venue is only shown to people who hold a spot");
        return new(false, null, revealAt, $"Venue revealed at {revealAt.UtcDateTime:yyyy-MM-dd HH:mm} UTC");
    }
}
