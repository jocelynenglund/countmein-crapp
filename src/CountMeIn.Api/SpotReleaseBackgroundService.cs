using CountMeIn.Application.Services;

namespace CountMeIn.Api;

/// <summary>Polls for interest checks whose door is about to close and releases spots nobody turned up for.</summary>
public class SpotReleaseBackgroundService(IServiceScopeFactory scopes, IConfiguration config, ILogger<SpotReleaseBackgroundService> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(config.GetValue("Release:PollSeconds", 30));
        using var timer = new PeriodicTimer(delay);
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var released = await scope.ServiceProvider.GetRequiredService<DoorService>().ReleaseDueSpotsAsync(stoppingToken);
                if (released > 0) log.LogInformation("Released spots on {Count} interest check(s)", released);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Releasing spots failed; will retry");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
