using CountMeIn.Application.Abstractions;
using CountMeIn.Application.Services;
using CountMeIn.Domain.Checks;

namespace CountMeIn.Application.Tests;

public class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 4, 10, 0, 0, TimeSpan.Zero);
}

public class FakeUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }
    public Task SaveChangesAsync(CancellationToken ct = default) { Saves++; return Task.CompletedTask; }
}

public class FakeInterestCheckRepository : IInterestCheckRepository
{
    private readonly Dictionary<string, InterestCheck> _byShareId = new();

    public void Add(InterestCheck aggregate) => _byShareId[aggregate.ShareId] = aggregate;

    public Task<InterestCheck?> GetByShareIdAsync(string shareId, CancellationToken ct = default) =>
        Task.FromResult(_byShareId.GetValueOrDefault(shareId));

    public Task<bool> ShareIdExistsAsync(string shareId, CancellationToken ct = default) =>
        Task.FromResult(_byShareId.ContainsKey(shareId));

    public Task<IReadOnlyList<DailyActivity>> GetDailyActivityAsync(Guid checkId, CancellationToken ct = default)
    {
        var check = _byShareId.Values.Single(c => c.Id == checkId);
        IReadOnlyList<DailyActivity> days = check.Changes.GroupBy(x => x.Day).OrderBy(g => g.Key)
            .Select(g => new DailyActivity(g.Key, g.Count(x => x.Kind == ChangeKind.CountedIn), g.Count(x => x.Kind == ChangeKind.ChangedMind)))
            .ToList();
        return Task.FromResult(days);
    }

    public Task<IReadOnlyList<string>> GetShareIdsDueForReleaseAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        IReadOnlyList<string> due = _byShareId.Values
            .Where(c => c.SpotsReleasedAt is null && c.ReleaseAt is { } r && r <= now)
            .Select(c => c.ShareId).ToList();
        return Task.FromResult(due);
    }
}

/// <summary>Wires the real services to in-memory fakes.</summary>
public class World
{
    public FakeClock Clock { get; } = new();
    public FakeUnitOfWork Uow { get; } = new();
    public FakeInterestCheckRepository Repo { get; } = new();
    public InterestCheckService Checks { get; }
    public DoorService Door { get; }
    public VenueService Venue { get; }

    public World()
    {
        Checks = new(Repo, Uow, Clock);
        Door = new(Repo, Uow, Clock);
        Venue = new(Repo, Uow, Clock);
    }

    public static DateTimeOffset At(int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0, TimeSpan.Zero);
}
