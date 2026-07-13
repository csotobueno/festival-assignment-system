using Festival.Application.Assignments.Ports;
using Festival.Domain.Assignments;

namespace Festival.Infrastructure.Assignments.InMemory;

public sealed class InMemoryAssignmentRepository : IAssignmentRepository
{
    private readonly List<Assignment> assignments = [];
    private readonly List<IReadOnlyList<Assignment>> savedBatches = [];

    public IReadOnlyList<Assignment> Assignments => assignments.AsReadOnly();

    public IReadOnlyList<IReadOnlyList<Assignment>> SavedBatches =>
        savedBatches.AsReadOnly();

    public Task SaveAsync(
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

        this.assignments.AddRange(materializedAssignments);
        savedBatches.Add(Array.AsReadOnly(materializedAssignments));

        return Task.CompletedTask;
    }
}
