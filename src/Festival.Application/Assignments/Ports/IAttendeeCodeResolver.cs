using Festival.Domain.Attendees;

namespace Festival.Application.Assignments.Ports;

public interface IAttendeeCodeResolver
{
    Task<IReadOnlyList<AttendeeId>> ResolveAttendeeIdsAsync(
        IEnumerable<AttendeeCode> attendeeCodes,
        CancellationToken cancellationToken = default);
}
