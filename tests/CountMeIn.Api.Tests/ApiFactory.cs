using CountMeIn.Application.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CountMeIn.Api.Tests;

public class TestClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 4, 10, 0, 0, TimeSpan.Zero);
}

/// <summary>Runs the real API against the real Postgres scratch database (migrated on startup). Time is ours to set.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public TestClock Clock { get; } = new();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("Release:Enabled", "false"); // tests drive the release themselves
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.ConfigureServices(s => s.Replace(ServiceDescriptor.Singleton<IClock>(Clock)));
    }
}
