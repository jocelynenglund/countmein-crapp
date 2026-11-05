using CountMeIn.Domain;

namespace CountMeIn.Application;

public class InterestCheckService(
    IInterestCheckRepository checks, IUnitOfWork unitOfWork, TimeProvider clock) : IInterestCheckService
{
    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    public async Task<StartedInterestCheckDto> StartAsync(StartInterestCheckRequest request, CancellationToken ct = default)
    {
        var checkId = Identifiers.NewShareId();
        var ownerKey = OwnerKey.Generate();

        var check = InterestCheck.Start(
            checkId, request.Title, request.Pitch, request.Goal, request.ClosesOn, ownerKey, Today,
            request.Capacity, request.DoorsOpen, request.DoorsClose);

        await checks.AddAsync(check, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new StartedInterestCheckDto(checkId, ownerKey);
    }

    public async Task<PitchDto> GetPitchAsync(string checkId, CancellationToken ct = default)
    {
        var check = await Load(checkId, ct);
        return new PitchDto(check.Title, check.Pitch, check.Goal, check.Count, check.GoalReached, !check.IsClosed, check.ClosesOn,
            check.SpotsLeft, check.WaitlistCount);
    }

    public async Task CountMeInAsync(string checkId, CountMeInRequest request, CancellationToken ct = default)
    {
        var check = await Load(checkId, ct);
        check.CountIn(request.PersonId ?? "", request.Name, request.Email, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ChangeMyMindAsync(string checkId, string personId, CancellationToken ct = default)
    {
        var check = await Load(checkId, ct);
        check.Withdraw(personId);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task CheckInAsync(string checkId, CheckInRequest request, CancellationToken ct = default)
    {
        var check = await Load(checkId, ct);
        try
        {
            check.CheckIn(request.PersonId ?? "", clock.GetUtcNow());
        }
        catch (OnTheWaitlistException)
        {
            // The refusal is still news: the attempt (and when it happened) was recorded and must be kept.
            await unitOfWork.SaveChangesAsync(ct);
            throw;
        }
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ReleaseDueSpotsAsync(CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        foreach (var check in await checks.GetDueForSpotReleaseAsync(now, ct))
            check.ReleaseUnclaimedSpots(now);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task SetVenueAsync(string checkId, string? ownerKey, SetVenueRequest request, CancellationToken ct = default)
    {
        var check = await Load(checkId, ct);
        check.SetVenue(ownerKey, request.Venue, request.RevealAt, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<VenueDto> GetVenueAsync(string checkId, string? personId, CancellationToken ct = default)
    {
        var check = await Load(checkId, ct);
        return new VenueDto(check.VenueRevealAt, check.VenueVisibleTo(personId ?? "", clock.GetUtcNow()));
    }

    public async Task<DoorListDto> GetDoorListAsync(string checkId, string? ownerKey, CancellationToken ct = default)
    {
        var check = await Load(checkId, ct);
        if (!check.IsOwnedBy(ownerKey)) throw new NotOwnerException("Only the owner can see this");

        var checkedIn = check.Participants.Where(p => p.CheckedInAt is not null)
            .OrderBy(p => p.CheckedInAt)
            .Select(p => new CheckedInDto(p.Name, p.CheckedInAt!.Value))
            .ToList();
        var expected = check.SpotHolders.Where(p => p.CheckedInAt is null).Select(p => p.Name).ToList();
        var waitlist = check.Waitlist.Select(p => p.Name).ToList();

        var letIn = check.LetInFromWaitlist.Select(p => p.Name).ToList();
        var lost = check.LostTheirSpot.Select(p => p.Name).ToList();

        return new DoorListDto(checkedIn, expected, waitlist, letIn, lost, check.Venue, check.VenueRevealAt,
            check.EarlyBirds.Select(p => p.Name).ToList());
    }

    public async Task<ProgressDto> GetProgressAsync(string checkId, string? ownerKey, CancellationToken ct = default)
    {
        var check = await Load(checkId, ct);
        if (!check.IsOwnedBy(ownerKey)) throw new NotOwnerException("Only the owner can see this");

        var perDay = check.Participants
            .GroupBy(p => DateOnly.FromDateTime(p.SignedUpAt.UtcDateTime))
            .OrderBy(g => g.Key)
            .Select(g => new SignUpsPerDayDto(g.Key, g.Count()))
            .ToList();
        var people = check.Participants.Select(p => new PersonDto(p.Name, p.Email)).ToList();

        return new ProgressDto(check.Count, check.Goal, check.GoalReached, perDay, people);
    }

    public async Task CloseAsync(string checkId, string? ownerKey, CancellationToken ct = default)
    {
        var check = await Load(checkId, ct);
        check.Close(ownerKey);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<InterestCheck> Load(string checkId, CancellationToken ct) =>
        await checks.GetByIdAsync(checkId, ct) ?? throw new InterestCheckNotFoundException();
}
