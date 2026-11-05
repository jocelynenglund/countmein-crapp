using CountMeIn.Application.Dtos;
using CountMeIn.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CountMeIn.Api.Controllers;

[ApiController]
[Route("api/checks/{shareId}")]
public class DoorController(DoorService door, VenueService venue) : ControllerBase
{
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn(string shareId, CheckInRequest request, CancellationToken ct)
    {
        await door.CheckInAsync(shareId, request, ct);
        return NoContent();
    }

    [HttpGet("door-list")]
    public async Task<DoorListDto> DoorList(string shareId, [FromHeader(Name = ChecksController.OwnerKeyHeader)] string? ownerKey, CancellationToken ct) =>
        await door.GetDoorListAsync(shareId, ownerKey, ct);

    [HttpPut("venue")]
    public async Task<IActionResult> SetVenue(string shareId, SetVenueRequest request,
        [FromHeader(Name = ChecksController.OwnerKeyHeader)] string? ownerKey, CancellationToken ct)
    {
        await venue.SetVenueAsync(shareId, ownerKey, request, ct);
        return NoContent();
    }

    [HttpGet("venue")]
    public async Task<VenueDto> Venue(string shareId, [FromQuery] string personId, CancellationToken ct) =>
        await venue.GetVenueAsync(shareId, personId, ct);
}
