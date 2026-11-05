using CountMeIn.Application;
using CountMeIn.Domain;
using Microsoft.EntityFrameworkCore;

namespace CountMeIn.Infrastructure.Persistence;

public class InterestCheckRepository(CountMeInDbContext db) : IInterestCheckRepository
{
    public Task<InterestCheck?> GetByIdAsync(string id, CancellationToken ct = default) =>
        db.InterestChecks.Include(c => c.Participants).SingleOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(InterestCheck check, CancellationToken ct = default) =>
        await db.InterestChecks.AddAsync(check, ct);
}
