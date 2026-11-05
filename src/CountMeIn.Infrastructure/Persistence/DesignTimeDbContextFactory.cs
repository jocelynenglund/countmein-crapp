using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CountMeIn.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef migrations</c> create the context without starting the API.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CountMeInDbContext>
{
    public CountMeInDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CountMeInDbContext>()
            .UseNpgsql(DependencyInjection.DefaultConnectionString)
            .Options);
}
