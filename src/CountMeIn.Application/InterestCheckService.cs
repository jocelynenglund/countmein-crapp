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
            checkId, request.Title, request.Pitch, request.Goal, request.ClosesOn, ownerKey, Today);

        await checks.AddAsync(check, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new StartedInterestCheckDto(checkId, ownerKey);
    }

    public async Task<PitchDto> GetPitchAsync(string checkId, CancellationToken ct = default)
    {
        var check = await Load(checkId, ct);
        return new PitchDto(check.Title, check.Pitch, check.Goal, check.Count, check.GoalReached, !check.IsClosed, check.ClosesOn);
    }

    public async Task CountMeInAsync(string checkId, CountMeInRequest request, CancellationToken ct = default)
    {
        var check = await Load(checkId, ct);
        check.CountIn(request.PersonId ?? "", request.Name, request.Email, Today);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ChangeMyMindAsync(string checkId, string personId, CancellationToken ct = default)
    {
        var check = await Load(checkId, ct);
        check.Withdraw(personId);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<ProgressDto> GetProgressAsync(string checkId, string? ownerKey, CancellationToken ct = default)
    {
        var check = await Load(checkId, ct);
        if (!check.IsOwnedBy(ownerKey)) throw new NotOwnerException("Only the owner can see this");

        var perDay = check.Participants
            .GroupBy(p => p.JoinedOn)
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
