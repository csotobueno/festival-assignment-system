using Festival.Domain.Assignments;

namespace Festival.Application.Assignments;

public interface IAssignmentRequestStore
{
    Task SaveAsync(
        AssignmentRequest assignmentRequest,
        CancellationToken cancellationToken = default);
}
