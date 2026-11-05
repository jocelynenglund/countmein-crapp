using CountMeIn.Application;
using CountMeIn.Domain;
using Microsoft.EntityFrameworkCore;

namespace CountMeIn.Infrastructure.Persistence;

public class InterestCheckRepository(CountMeInDbContext db) : IInterestCheckRepository
{
    public Task<InterestCheck?> GetByIdAsync(string id, CancellationToken ct = default) =>
        db.InterestChecks.Include(c => c.Participants).SingleOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<InterestCheck>> GetDueForSpotReleaseAsync(DateTimeOffset now, CancellationToken ct = default) =>
        await db.InterestChecks.Include(c => c.Participants)
            .Where(c => c.Capacity != null && c.SpotsReleasedAt == null
                        && c.DoorsOpen <= now && c.DoorsClose <= now + InterestCheck.ReleaseLead)
            .ToListAsync(ct);

    public async Task AddAsync(InterestCheck check, CancellationToken ct = default) =>
        await db.InterestChecks.AddAsync(check, ct);
}
