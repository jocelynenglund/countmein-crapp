using CountMeIn.Application.Abstractions;
using CountMeIn.Domain.Checks;
using Microsoft.EntityFrameworkCore;

namespace CountMeIn.Infrastructure.Persistence;

public class CountMeInDbContext(DbContextOptions<CountMeInDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<InterestCheck> InterestChecks => Set<InterestCheck>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<ParticipationChange> ParticipationChanges => Set<ParticipationChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CountMeInDbContext).Assembly);

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken ct) => await base.SaveChangesAsync(ct);
}
