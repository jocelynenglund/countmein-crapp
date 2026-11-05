namespace CountMeIn.Application;

public interface IUnitOfWork
{
    /// <summary>Saves everything changed in this request in one transaction.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
