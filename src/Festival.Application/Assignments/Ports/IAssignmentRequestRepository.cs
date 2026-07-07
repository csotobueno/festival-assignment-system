using Festival.Domain.Assignments;

namespace Festival.Application.Assignments.Ports;

public interface IAssignmentRequestRepository
{
    Task SaveAsync(
        AssignmentRequest assignmentRequest,
        CancellationToken cancellationToken = default);
}
