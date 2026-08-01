using Festival.Application.Assignments.Ports;

namespace Festival.Infrastructure.Persistence;

public sealed class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly FestivalDbContext dbContext;

    public EfCoreUnitOfWork(FestivalDbContext dbContext)
    {
        this.dbContext = dbContext
            ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
