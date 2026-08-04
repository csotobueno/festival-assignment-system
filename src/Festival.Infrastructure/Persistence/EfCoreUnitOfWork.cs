using Festival.Application.Assignments.Ports;
using Festival.Application.Assignments.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.Persistence;

public sealed class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly FestivalDbContext dbContext;

    public EfCoreUnitOfWork(FestivalDbContext dbContext)
    {
        this.dbContext = dbContext
            ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (PostgreSqlAssignmentConflictTranslator.TryTranslate(
                exception,
                out var conflict))
        {
            throw new AssignmentPersistenceConflictException(
                conflict,
                exception);
        }
    }
}
