using CountMeIn.Application;
using CountMeIn.Domain;

namespace CountMeIn.Application.Tests;

/// <summary>An in-memory repository: enough to test the use cases without a database.</summary>
public class InMemoryInterestCheckRepository : IInterestCheckRepository
{
    private readonly Dictionary<string, InterestCheck> _checks = [];

    public Task<InterestCheck?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(_checks.GetValueOrDefault(id));

    public Task AddAsync(InterestCheck check, CancellationToken ct = default)
    {
        _checks[check.Id] = check;
        return Task.CompletedTask;
    }
}

public class CountingUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        Saves++;
        return Task.CompletedTask;
    }
}
