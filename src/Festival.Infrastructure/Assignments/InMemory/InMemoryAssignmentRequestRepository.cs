using Festival.Application.Assignments.Ports;
using Festival.Domain.Assignments;

namespace Festival.Infrastructure.Assignments.InMemory;

public sealed class InMemoryAssignmentRequestRepository
    : IAssignmentRequestRepository
{
    private readonly List<AssignmentRequest> assignmentRequests = [];

    public IReadOnlyList<AssignmentRequest> AssignmentRequests =>
        assignmentRequests.AsReadOnly();

    public Task SaveAsync(
        AssignmentRequest assignmentRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignmentRequest);
        cancellationToken.ThrowIfCancellationRequested();

        assignmentRequests.Add(assignmentRequest);

        return Task.CompletedTask;
    }
}
