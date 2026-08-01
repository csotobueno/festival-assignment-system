using Festival.Domain.Assignments;

namespace Festival.Application.Assignments.Ports;

public interface IAssignmentRepository
{
    Task AddAsync(
        IEnumerable<Assignment> assignments,
        CancellationToken cancellationToken = default);
}
