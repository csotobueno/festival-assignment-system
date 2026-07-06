using Festival.Domain.Assignments;

namespace Festival.Application.Assignments;

public interface IAssignmentStore
{
    Task SaveAsync(
        IEnumerable<Assignment> assignments,
        CancellationToken cancellationToken = default);
}
