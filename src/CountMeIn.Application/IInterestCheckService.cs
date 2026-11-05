namespace CountMeIn.Application;

public interface IInterestCheckService
{
    Task<StartedInterestCheckDto> StartAsync(StartInterestCheckRequest request, CancellationToken ct = default);

    Task<PitchDto> GetPitchAsync(string checkId, CancellationToken ct = default);

    Task CountMeInAsync(string checkId, CountMeInRequest request, CancellationToken ct = default);

    Task ChangeMyMindAsync(string checkId, string personId, CancellationToken ct = default);

    Task CheckInAsync(string checkId, CheckInRequest request, CancellationToken ct = default);

    Task<DoorListDto> GetDoorListAsync(string checkId, string? ownerKey, CancellationToken ct = default);

    Task<ProgressDto> GetProgressAsync(string checkId, string? ownerKey, CancellationToken ct = default);

    Task CloseAsync(string checkId, string? ownerKey, CancellationToken ct = default);
}
