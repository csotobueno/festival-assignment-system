using Festival.Domain.Assignments;
using Festival.Domain.Attendees;

namespace Festival.Application.Assignments.Ports;

public interface IFairnessHistoryProvider
{
    /// <summary>
    /// Returns recorded outcomes oldest first, including empty histories for
    /// requested attendees without outcomes.
    /// </summary>
    Task<IReadOnlyDictionary<AttendeeId, FairnessHistory>> GetHistoriesAsync(
        IReadOnlyCollection<AttendeeId> attendeeIds,
        CancellationToken cancellationToken = default);
}
