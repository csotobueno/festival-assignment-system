using Festival.Application.Assignments.Ports;
using Festival.Domain.Attendees;

namespace Festival.Infrastructure.Assignments.InMemory;

public sealed class InMemoryAttendeeCodeResolver : IAttendeeCodeResolver
{
    private readonly IReadOnlyDictionary<AttendeeCode, AttendeeId> attendeeIdsByCode;

    public InMemoryAttendeeCodeResolver(IEnumerable<Attendee> attendees)
    {
        ArgumentNullException.ThrowIfNull(attendees);

        var materializedAttendees = attendees.ToArray();

        if (materializedAttendees.Any(attendee => attendee is null))
        {
            throw new ArgumentException(
                "Attendees cannot contain null values.",
                nameof(attendees));
        }

        attendeeIdsByCode = materializedAttendees.ToDictionary(
            attendee => attendee.Code,
            attendee => attendee.Id);
    }

    public Task<IReadOnlyList<AttendeeId>> ResolveAttendeeIdsAsync(
        IEnumerable<AttendeeCode> attendeeCodes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attendeeCodes);
        cancellationToken.ThrowIfCancellationRequested();

        var requestedCodes = attendeeCodes.ToArray();

        if (requestedCodes.Any(code => code is null))
        {
            throw new ArgumentException(
                "Attendee codes cannot contain null values.",
                nameof(attendeeCodes));
        }

        var attendeeIds = requestedCodes
            .Where(attendeeIdsByCode.ContainsKey)
            .Select(code => attendeeIdsByCode[code])
            .ToArray();

        return Task.FromResult<IReadOnlyList<AttendeeId>>(
            Array.AsReadOnly(attendeeIds));
    }
}
