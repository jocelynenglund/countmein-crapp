namespace CountMeIn.Application.Dtos;

public record StartCheckRequest(
    string Title, string? Pitch, int Goal, DateOnly? ClosesOn,
    int? Capacity, DateTimeOffset? DoorsOpen, DateTimeOffset? DoorsClose);

public record StartCheckResponse(string ShareId, string OwnerKey);

public record PitchDto(
    string Title, string Pitch, int Goal, int Count, bool GoalReached, bool Open,
    DateOnly? ClosesOn, int? SpotsLeft, int WaitlistCount);

public record CountMeInRequest(string PersonId, string Name, string? Email);

public record ChangeMyMindRequest(string PersonId);

public record PersonDto(string PersonId, string Name);

public record DayDto(DateOnly Day, int In, int Out);

public record ProgressDto(int Count, int Goal, bool GoalReached, IReadOnlyList<DayDto> ByDay, IReadOnlyList<PersonDto> People);

public record CheckInRequest(string PersonId, DateTimeOffset At);

public record TimedPersonDto(string PersonId, string Name, DateTimeOffset At);

public record DoorListDto(
    string? Venue, DateTimeOffset? RevealAt,
    IReadOnlyList<TimedPersonDto> CheckedIn,
    IReadOnlyList<PersonDto> NotArrived,
    IReadOnlyList<PersonDto> Waitlist,
    IReadOnlyList<TimedPersonDto> TurnedAway,
    IReadOnlyList<TimedPersonDto> LetIn,
    IReadOnlyList<TimedPersonDto> LostSpot,
    IReadOnlyList<PersonDto> EarlyBirds);

public record SetVenueRequest(string Venue, DateTimeOffset RevealAt);

public record VenueDto(bool Visible, string? Venue, DateTimeOffset? RevealAt, string Message);
