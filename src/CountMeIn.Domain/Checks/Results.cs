namespace CountMeIn.Domain.Checks;

/// <summary>Outcome of a check-in attempt. A refusal can still change state (a waitlisted attempt is remembered).</summary>
public record CheckInResult(string? Refusal)
{
    public bool Succeeded => Refusal is null;
    public static CheckInResult Ok { get; } = new((string?)null);
}

public record VenueView(bool Visible, string? Venue, DateTimeOffset? RevealAt, string Message);
