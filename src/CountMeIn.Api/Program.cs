using CountMeIn.Api;
using CountMeIn.Application;
using CountMeIn.Infrastructure;
using CountMeIn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("CountMeIn") ?? InfrastructureServices.DefaultConnectionString);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    // Convenient locally; in production run the migrations as a deployment step.
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CountMeInDbContext>().Database.Migrate();
}

app.Run();

public partial class Program;
