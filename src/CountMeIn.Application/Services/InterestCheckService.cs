using CountMeIn.Application.Abstractions;
using CountMeIn.Application.Dtos;
using CountMeIn.Domain.Checks;

namespace CountMeIn.Application.Services;

public class InterestCheckService(IInterestCheckRepository checks, IUnitOfWork uow, IClock clock)
{
    public async Task<StartCheckResponse> StartAsync(StartCheckRequest r, CancellationToken ct = default)
    {
        string shareId;
        do { shareId = OwnerKeys.NewShareId(); } while (await checks.ShareIdExistsAsync(shareId, ct));
        var ownerKey = OwnerKeys.NewOwnerKey();

        var check = InterestCheck.Start(shareId, OwnerKeys.Hash(ownerKey), r.Title, r.Pitch, r.Goal, r.ClosesOn,
            r.Capacity, r.DoorsOpen?.ToUniversalTime(), r.DoorsClose?.ToUniversalTime(), clock.UtcNow);
        checks.Add(check);
        await uow.SaveChangesAsync(ct);
        return new StartCheckResponse(shareId, ownerKey);
    }

    public async Task<PitchDto> GetPitchAsync(string shareId, CancellationToken ct = default)
    {
        var c = await Load(shareId, ct);
        return new PitchDto(c.Title, c.Pitch, c.Goal, c.Count, c.GoalReached, c.IsOpen(clock.UtcNow),
            c.ClosesOn, c.SpotsLeft, c.WaitlistCount);
    }

    public async Task CountMeInAsync(string shareId, CountMeInRequest r, CancellationToken ct = default)
    {
        var c = await Load(shareId, ct);
        c.CountMeIn(r.PersonId, r.Name, r.Email, clock.UtcNow);
        await uow.SaveChangesAsync(ct);
    }

    public async Task ChangeMyMindAsync(string shareId, ChangeMyMindRequest r, CancellationToken ct = default)
    {
        var c = await Load(shareId, ct);
        c.ChangeMyMind(r.PersonId, clock.UtcNow);
        await uow.SaveChangesAsync(ct);
    }

    public async Task<ProgressDto> GetProgressAsync(string shareId, string? ownerKey, CancellationToken ct = default)
    {
        var c = await Load(shareId, ct);
        c.EnsureOwner(OwnerKeys.Hash(ownerKey), "Only the owner can see how it's going");
        var days = await checks.GetDailyActivityAsync(c.Id, ct);
        var people = c.Participants.Where(p => p.IsCountedIn).OrderBy(p => p.QueueNumber)
            .Select(p => new PersonDto(p.PersonId, p.Name)).ToList();
        return new ProgressDto(c.Count, c.Goal, c.GoalReached,
            days.Select(d => new DayDto(d.Day, d.CountedIn, d.ChangedMind)).ToList(), people);
    }

    public async Task CloseAsync(string shareId, string? ownerKey, CancellationToken ct = default)
    {
        var c = await Load(shareId, ct);
        c.Close(OwnerKeys.Hash(ownerKey));
        await uow.SaveChangesAsync(ct);
    }

    private async Task<InterestCheck> Load(string shareId, CancellationToken ct) =>
        await checks.GetByShareIdAsync(shareId, ct) ?? throw new NotFoundException("No such interest check");
}
