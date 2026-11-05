using CountMeIn.Domain;

namespace CountMeIn.Application;

public interface IInterestCheckRepository
{
    /// <summary>The check with its participants, or null when there is no such check.</summary>
    Task<InterestCheck?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Checks with spots to release that haven't been released yet, as of <paramref name="now"/>.</summary>
    Task<IReadOnlyList<InterestCheck>> GetDueForSpotReleaseAsync(DateTimeOffset now, CancellationToken ct = default);

    Task AddAsync(InterestCheck check, CancellationToken ct = default);
}
