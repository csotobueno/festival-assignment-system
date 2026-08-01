using Festival.Domain.Assignments;

namespace Festival.Application.Assignments.Ports;

public interface IAssignmentRequestRepository
{
    Task AddAsync(
        AssignmentRequest assignmentRequest,
        CancellationToken cancellationToken = default);
}
