using CountMeIn.Application.Abstractions;
using CountMeIn.Domain.Checks;
using Microsoft.EntityFrameworkCore;

namespace CountMeIn.Infrastructure.Persistence.Repositories;

public class InterestCheckRepository(CountMeInDbContext db) : IInterestCheckRepository
{
    public void Add(InterestCheck aggregate) => db.InterestChecks.Add(aggregate);

    public Task<InterestCheck?> GetByShareIdAsync(string shareId, CancellationToken ct = default) =>
        db.InterestChecks.Include(c => c.Participants).FirstOrDefaultAsync(c => c.ShareId == shareId, ct);

    public Task<bool> ShareIdExistsAsync(string shareId, CancellationToken ct = default) =>
        db.InterestChecks.AnyAsync(c => c.ShareId == shareId, ct);

    public async Task<IReadOnlyList<DailyActivity>> GetDailyActivityAsync(Guid checkId, CancellationToken ct = default)
    {
        var rows = await db.ParticipationChanges.AsNoTracking()
            .Where(x => x.CheckId == checkId)
            .GroupBy(x => x.Day)
            .Select(g => new
            {
                Day = g.Key,
                In = g.Count(x => x.Kind == ChangeKind.CountedIn),
                Out = g.Count(x => x.Kind == ChangeKind.ChangedMind),
            })
            .OrderBy(x => x.Day)
            .ToListAsync(ct);
        return rows.Select(r => new DailyActivity(r.Day, r.In, r.Out)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetShareIdsDueForReleaseAsync(DateTimeOffset now, CancellationToken ct = default) =>
        await db.InterestChecks.AsNoTracking()
            .Where(c => c.SpotsReleasedAt == null && c.ReleaseAt != null && c.ReleaseAt <= now)
            .Select(c => c.ShareId)
            .ToListAsync(ct);
}
