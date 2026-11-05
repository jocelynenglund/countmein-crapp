using CountMeIn.Domain.Checks;

namespace CountMeIn.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface IRepository<T> where T : class
{
    void Add(T aggregate);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}

public record DailyActivity(DateOnly Day, int CountedIn, int ChangedMind);

public interface IInterestCheckRepository : IRepository<InterestCheck>
{
    /// <summary>Loads the check with its people (not its change history).</summary>
    Task<InterestCheck?> GetByShareIdAsync(string shareId, CancellationToken ct = default);

    Task<bool> ShareIdExistsAsync(string shareId, CancellationToken ct = default);

    /// <summary>Per day: how many counted in and how many changed their mind.</summary>
    Task<IReadOnlyList<DailyActivity>> GetDailyActivityAsync(Guid checkId, CancellationToken ct = default);

    /// <summary>Share ids of checks whose spots are due to be released and haven't been yet.</summary>
    Task<IReadOnlyList<string>> GetShareIdsDueForReleaseAsync(DateTimeOffset now, CancellationToken ct = default);
}
