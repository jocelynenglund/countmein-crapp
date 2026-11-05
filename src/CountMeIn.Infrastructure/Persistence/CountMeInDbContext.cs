using CountMeIn.Domain;
using Microsoft.EntityFrameworkCore;

namespace CountMeIn.Infrastructure.Persistence;

public class CountMeInDbContext(DbContextOptions<CountMeInDbContext> options) : DbContext(options)
{
    public DbSet<InterestCheck> InterestChecks => Set<InterestCheck>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CountMeInDbContext).Assembly);
}
