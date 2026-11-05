using CountMeIn.Application;

namespace CountMeIn.Api;

/// <summary>
/// Hands unclaimed spots to the waitlist once a check reaches its release moment, with no owner action.
/// It looks at the database on every tick, so it also catches up after a restart.
/// </summary>
public class SpotReleaseWorker(
    IServiceScopeFactory scopes, IConfiguration config, ILogger<SpotReleaseWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(config.GetValue("SpotRelease:IntervalSeconds", 30));
        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IInterestCheckService>().ReleaseDueSpotsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Releasing unclaimed spots failed; will try again");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
