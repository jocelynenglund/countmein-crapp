using CountMeIn.Application;
using CountMeIn.Domain;
using Microsoft.EntityFrameworkCore;

namespace CountMeIn.Api;

/// <summary>Turns domain and application exceptions into HTTP responses with a plain { error } body.</summary>
public class ErrorHandlingMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (ex is NotFoundException or NotOwnerException or DomainException or DbUpdateConcurrencyException)
        {
            context.Response.StatusCode = ex switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                NotOwnerException => StatusCodes.Status403Forbidden,
                DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest,
            };
            var message = ex is DbUpdateConcurrencyException ? "Someone else changed this at the same time, please try again" : ex.Message;
            await context.Response.WriteAsJsonAsync(new { error = message });
        }
    }
}
