using CountMeIn.Application;
using Microsoft.AspNetCore.Mvc;

namespace CountMeIn.Api.Controllers;

[ApiController]
[Route("api/checks")]
public class InterestChecksController(IInterestCheckService service) : ControllerBase
{
    public const string OwnerKeyHeader = "X-Owner-Key";

    /// <summary>Start an interest check. The owner key in the response is shown only this once.</summary>
    [HttpPost]
    public async Task<ActionResult<StartedInterestCheckDto>> Start(StartInterestCheckRequest request, CancellationToken ct)
    {
        var started = await service.StartAsync(request, ct);
        return CreatedAtAction(nameof(Pitch), new { checkId = started.CheckId }, started);
    }

    /// <summary>The public pitch page.</summary>
    [HttpGet("{checkId}")]
    public async Task<PitchDto> Pitch(string checkId, CancellationToken ct) =>
        await service.GetPitchAsync(checkId, ct);

    [HttpPost("{checkId}/interest")]
    public async Task<IActionResult> CountMeIn(string checkId, CountMeInRequest request, CancellationToken ct)
    {
        await service.CountMeInAsync(checkId, request, ct);
        return NoContent();
    }

    [HttpDelete("{checkId}/interest/{personId}")]
    public async Task<IActionResult> ChangeMyMind(string checkId, string personId, CancellationToken ct)
    {
        await service.ChangeMyMindAsync(checkId, personId, ct);
        return NoContent();
    }

    /// <summary>Owner only: the count, sign-ups per day and who is in.</summary>
    [HttpGet("{checkId}/progress")]
    public async Task<ProgressDto> Progress(
        string checkId, [FromHeader(Name = OwnerKeyHeader)] string? ownerKey, CancellationToken ct) =>
        await service.GetProgressAsync(checkId, ownerKey, ct);

    [HttpPost("{checkId}/close")]
    public async Task<IActionResult> Close(
        string checkId, [FromHeader(Name = OwnerKeyHeader)] string? ownerKey, CancellationToken ct)
    {
        await service.CloseAsync(checkId, ownerKey, ct);
        return NoContent();
    }
}
