using CountMeIn.Application;
using CountMeIn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CountMeIn.Infrastructure;

public static class DependencyInjection
{
    public const string DefaultConnectionString =
        "Host=localhost;Port=5435;Database=countmein_crapp;Username=postgres;Password=countmein";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CountMeInDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IInterestCheckRepository, InterestCheckRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
