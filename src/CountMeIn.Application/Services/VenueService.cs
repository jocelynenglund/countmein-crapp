using CountMeIn.Application.Abstractions;
using CountMeIn.Application.Dtos;

namespace CountMeIn.Application.Services;

public class VenueService(IInterestCheckRepository checks, IUnitOfWork uow, IClock clock)
{
    public async Task SetVenueAsync(string shareId, string? ownerKey, SetVenueRequest r, CancellationToken ct = default)
    {
        var c = await checks.GetByShareIdAsync(shareId, ct) ?? throw new NotFoundException("No such interest check");
        c.SetVenue(OwnerKeys.Hash(ownerKey), r.Venue, r.RevealAt.ToUniversalTime(), clock.UtcNow);
        await uow.SaveChangesAsync(ct);
    }

    public async Task<VenueDto> GetVenueAsync(string shareId, string personId, CancellationToken ct = default)
    {
        var c = await checks.GetByShareIdAsync(shareId, ct) ?? throw new NotFoundException("No such interest check");
        var v = c.VenueFor(personId, clock.UtcNow);
        return new VenueDto(v.Visible, v.Venue, v.RevealAt, v.Message);
    }
}
