using Festival.Domain.Assignments;

namespace Festival.Application.Assignments.Ports;

public interface IAssignmentRepository
{
    Task SaveAsync(
        IEnumerable<Assignment> assignments,
        CancellationToken cancellationToken = default);
}
