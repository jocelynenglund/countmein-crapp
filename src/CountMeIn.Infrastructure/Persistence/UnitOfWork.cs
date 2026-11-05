using CountMeIn.Application;
using CountMeIn.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CountMeIn.Infrastructure.Persistence;

public class UnitOfWork(CountMeInDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two requests for the same person raced past the domain check; the unique index caught the second.
            throw new AlreadyCountedInException();
        }
    }
}
