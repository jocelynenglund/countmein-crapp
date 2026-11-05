using CountMeIn.Application.Abstractions;
using CountMeIn.Infrastructure.Persistence;
using CountMeIn.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CountMeIn.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CountMeInDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CountMeInDbContext>());
        services.AddScoped<IInterestCheckRepository, InterestCheckRepository>();
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
