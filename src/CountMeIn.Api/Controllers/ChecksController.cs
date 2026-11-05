using CountMeIn.Application.Dtos;
using CountMeIn.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CountMeIn.Api.Controllers;

[ApiController]
[Route("api/checks")]
public class ChecksController(InterestCheckService service) : ControllerBase
{
    public const string OwnerKeyHeader = "X-Owner-Key";

    [HttpPost]
    public async Task<IActionResult> Start(StartCheckRequest request, CancellationToken ct)
    {
        var created = await service.StartAsync(request, ct);
        return Created($"/api/checks/{created.ShareId}", created);
    }

    [HttpGet("{shareId}")]
    public async Task<PitchDto> Pitch(string shareId, CancellationToken ct) => await service.GetPitchAsync(shareId, ct);

    [HttpPost("{shareId}/join")]
    public async Task<IActionResult> Join(string shareId, CountMeInRequest request, CancellationToken ct)
    {
        await service.CountMeInAsync(shareId, request, ct);
        return NoContent();
    }

    [HttpPost("{shareId}/leave")]
    public async Task<IActionResult> Leave(string shareId, ChangeMyMindRequest request, CancellationToken ct)
    {
        await service.ChangeMyMindAsync(shareId, request, ct);
        return NoContent();
    }

    [HttpGet("{shareId}/progress")]
    public async Task<ProgressDto> Progress(string shareId, [FromHeader(Name = OwnerKeyHeader)] string? ownerKey, CancellationToken ct) =>
        await service.GetProgressAsync(shareId, ownerKey, ct);

    [HttpPost("{shareId}/close")]
    public async Task<IActionResult> Close(string shareId, [FromHeader(Name = OwnerKeyHeader)] string? ownerKey, CancellationToken ct)
    {
        await service.CloseAsync(shareId, ownerKey, ct);
        return NoContent();
    }
}
