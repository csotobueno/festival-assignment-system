using Festival.Application.Assignments.Ports;
using Festival.Domain.Assignments;
using Festival.Infrastructure.Persistence;

namespace Festival.Infrastructure.Assignments.PostgreSql;

public sealed class PostgreSqlAssignmentRepository
    : IAssignmentRepository
{
    private readonly FestivalDbContext dbContext;

    public PostgreSqlAssignmentRepository(FestivalDbContext dbContext)
    {
        this.dbContext = dbContext
            ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(
        IEnumerable<Assignment> assignments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        cancellationToken.ThrowIfCancellationRequested();

        var materializedAssignments = assignments.ToArray();

        if (materializedAssignments.Any(assignment => assignment is null))
        {
            throw new ArgumentException(
                "Assignments cannot contain null values.",
                nameof(assignments));
        }

        if (materializedAssignments.Length == 0)
        {
            return;
        }

        await dbContext.Assignments.AddRangeAsync(
            materializedAssignments,
            cancellationToken);
    }
}
