using CountMeIn.Application.Abstractions;
using CountMeIn.Application.Dtos;
using CountMeIn.Domain;
using CountMeIn.Domain.Checks;

namespace CountMeIn.Application.Services;

public class DoorService(IInterestCheckRepository checks, IUnitOfWork uow, IClock clock)
{
    public async Task CheckInAsync(string shareId, CheckInRequest r, CancellationToken ct = default)
    {
        var c = await Load(shareId, ct);
        var result = c.CheckIn(r.PersonId, r.At.ToUniversalTime());
        await uow.SaveChangesAsync(ct); // a refused attempt can still be remembered (waitlist attempts)
        if (!result.Succeeded) throw new DomainException(result.Refusal!);
    }

    public async Task<DoorListDto> GetDoorListAsync(string shareId, string? ownerKey, CancellationToken ct = default)
    {
        var c = await Load(shareId, ct);
        c.EnsureOwner(OwnerKeys.Hash(ownerKey), "Only the owner can see the door list");

        var all = c.Participants.Where(p => p.IsCountedIn).OrderBy(p => p.QueueNumber).ToList();
        static PersonDto Person(Participant p) => new(p.PersonId, p.Name);
        static TimedPersonDto Timed(Participant p, DateTimeOffset at) => new(p.PersonId, p.Name, at);

        return new DoorListDto(
            c.Venue, c.VenueRevealAt,
            all.Where(p => p.CheckedInAt is not null).OrderBy(p => p.CheckedInAt).Select(p => Timed(p, p.CheckedInAt!.Value)).ToList(),
            all.Where(p => p.HoldsASpot && p.CheckedInAt is null).Select(Person).ToList(),
            all.Where(p => p.IsWaitlisted).Select(Person).ToList(),
            all.Where(p => p.IsWaitlisted && p.SpotReleasedAt is null && p.FirstDoorAttemptAt is not null)
                .OrderBy(p => p.FirstDoorAttemptAt).Select(p => Timed(p, p.FirstDoorAttemptAt!.Value)).ToList(),
            all.Where(p => p.LetInAt is not null).OrderBy(p => p.LetInAt).Select(p => Timed(p, p.LetInAt!.Value)).ToList(),
            all.Where(p => p.SpotReleasedAt is not null).OrderBy(p => p.SpotReleasedAt).Select(p => Timed(p, p.SpotReleasedAt!.Value)).ToList(),
            all.Where(p => p.IsEarlyBird).Select(Person).ToList());
    }

    /// <summary>Called by the background service. Returns how many checks had their spots released.</summary>
    public async Task<int> ReleaseDueSpotsAsync(CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var due = await checks.GetShareIdsDueForReleaseAsync(now, ct);
        var released = 0;
        foreach (var shareId in due)
        {
            var c = await checks.GetByShareIdAsync(shareId, ct);
            if (c is null) continue;
            c.ReleaseUnclaimedSpots(now);
            await uow.SaveChangesAsync(ct);
            released++;
        }
        return released;
    }

    private async Task<InterestCheck> Load(string shareId, CancellationToken ct) =>
        await checks.GetByShareIdAsync(shareId, ct) ?? throw new NotFoundException("No such interest check");
}
