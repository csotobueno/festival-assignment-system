using Festival.Application.Assignments.Ports;

namespace Festival.Infrastructure.Assignments.InMemory;

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(0);
    }
}
