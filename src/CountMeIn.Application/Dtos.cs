namespace CountMeIn.Application;

public record StartInterestCheckRequest(string? Title, string? Pitch, int Goal, DateOnly? ClosesOn,
    int? Capacity = null, DateTimeOffset? DoorsOpen = null, DateTimeOffset? DoorsClose = null);

/// <summary>The share link id (goes in the public URL) and the owner key (shown once, keep it secret).</summary>
public record StartedInterestCheckDto(string CheckId, string OwnerKey);

public record CountMeInRequest(string? PersonId, string? Name, string? Email);

/// <summary>What anyone with the link may see. No names, no emails.</summary>
public record PitchDto(string Title, string Pitch, int Goal, int Count, bool GoalReached, bool Open, DateOnly? ClosesOn,
    int? SpotsLeft, int WaitlistCount);

public record SignUpsPerDayDto(DateOnly Day, int SignUps);

public record PersonDto(string Name, string? Email);

/// <summary>What the owner sees, including who is in.</summary>
public record ProgressDto(
    int Count, int Goal, bool GoalReached, IReadOnlyList<SignUpsPerDayDto> SignUpsPerDay, IReadOnlyList<PersonDto> People);

public record CheckInRequest(string? PersonId);

public record CheckedInDto(string Name, DateTimeOffset At);

/// <summary>What the owner sees at the door: who is in, who is still expected, and who is waiting for a spot.</summary>
public record DoorListDto(
    IReadOnlyList<CheckedInDto> CheckedIn, IReadOnlyList<string> Expected, IReadOnlyList<string> Waitlist,
    IReadOnlyList<string> LetInFromWaitlist, IReadOnlyList<string> LostTheirSpot,
    string? Venue, DateTimeOffset? VenueRevealAt, IReadOnlyList<string> EarlyBirds);

public record SetVenueRequest(string? Venue, DateTimeOffset RevealAt);

/// <summary>Venue is null until the reveal, and for anyone who isn't in with a spot. RevealAt is null if none is set.</summary>
public record VenueDto(DateTimeOffset? RevealAt, string? Venue);
