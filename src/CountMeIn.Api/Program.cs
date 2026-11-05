using CountMeIn.Api;
using CountMeIn.Application.Services;
using CountMeIn.Infrastructure;
using CountMeIn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Default")!);
builder.Services.AddScoped<InterestCheckService>();
builder.Services.AddScoped<DoorService>();
builder.Services.AddScoped<VenueService>();
if (builder.Configuration.GetValue("Release:Enabled", true))
    builder.Services.AddHostedService<SpotReleaseBackgroundService>();

var app = builder.Build();

if (app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CountMeInDbContext>().Database.Migrate();
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.MapControllers();
app.Run();

public partial class Program;
