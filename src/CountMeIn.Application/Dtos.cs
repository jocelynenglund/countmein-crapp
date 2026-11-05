namespace CountMeIn.Application;

public record StartInterestCheckRequest(string? Title, string? Pitch, int Goal, DateOnly? ClosesOn);

/// <summary>The share link id (goes in the public URL) and the owner key (shown once, keep it secret).</summary>
public record StartedInterestCheckDto(string CheckId, string OwnerKey);

public record CountMeInRequest(string? PersonId, string? Name, string? Email);

/// <summary>What anyone with the link may see. No names, no emails.</summary>
public record PitchDto(string Title, string Pitch, int Goal, int Count, bool GoalReached, bool Open, DateOnly? ClosesOn);

public record SignUpsPerDayDto(DateOnly Day, int SignUps);

public record PersonDto(string Name, string? Email);

/// <summary>What the owner sees, including who is in.</summary>
public record ProgressDto(
    int Count, int Goal, bool GoalReached, IReadOnlyList<SignUpsPerDayDto> SignUpsPerDay, IReadOnlyList<PersonDto> People);
