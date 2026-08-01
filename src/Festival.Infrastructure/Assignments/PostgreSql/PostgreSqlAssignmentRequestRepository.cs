using Festival.Application.Assignments.Ports;
using Festival.Domain.Assignments;
using Festival.Infrastructure.Persistence;
using Festival.Infrastructure.Persistence.Mappers;

namespace Festival.Infrastructure.Assignments.PostgreSql;

public sealed class PostgreSqlAssignmentRequestRepository
    : IAssignmentRequestRepository
{
    private readonly FestivalDbContext dbContext;

    public PostgreSqlAssignmentRequestRepository(
        FestivalDbContext dbContext)
    {
        this.dbContext = dbContext
            ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(
        AssignmentRequest assignmentRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignmentRequest);

        var row = AssignmentRequestMapper.ToRow(assignmentRequest);

        await dbContext.AssignmentRequests.AddAsync(
            row,
            cancellationToken);
    }
}
