using Microsoft.Extensions.DependencyInjection;

namespace CountMeIn.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IInterestCheckService, InterestCheckService>();
        return services;
    }
}
