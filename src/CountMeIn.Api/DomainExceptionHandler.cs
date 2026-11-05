using CountMeIn.Application;
using CountMeIn.Domain;
using Microsoft.AspNetCore.Diagnostics;

namespace CountMeIn.Api;

/// <summary>Turns business-rule failures into HTTP responses shaped <c>{ "error": "..." }</c>.</summary>
public class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            InterestCheckNotFoundException => StatusCodes.Status404NotFound,
            NotOwnerException => StatusCodes.Status403Forbidden,
            InvalidInterestCheckException => StatusCodes.Status400BadRequest,
            DomainException => StatusCodes.Status409Conflict,
            _ => 0,
        };
        if (status == 0) return false;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { error = exception.Message }, ct);
        return true;
    }
}
