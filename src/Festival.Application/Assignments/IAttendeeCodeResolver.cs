using Festival.Domain.Attendees;

namespace Festival.Application.Assignments;

public interface IAttendeeCodeResolver
{
    Task<IReadOnlyList<AttendeeId>> ResolveAttendeeIdsAsync(
        IEnumerable<AttendeeCode> attendeeCodes,
        CancellationToken cancellationToken = default);
}
