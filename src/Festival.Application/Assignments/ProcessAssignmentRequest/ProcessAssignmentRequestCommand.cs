using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;

namespace Festival.Application.Assignments.ProcessAssignmentRequest;

public sealed class ProcessAssignmentRequestCommand
{
    public FestivalDayId FestivalDayId { get; }

    public IReadOnlyList<AttendeeCode> AttendeeCodes { get; }

    public DateTimeOffset RequestedAt { get; }

    public DateTimeOffset AssignedAt { get; }

    public ProcessAssignmentRequestCommand(
        FestivalDayId festivalDayId,
        IEnumerable<AttendeeCode> attendeeCodes,
        DateTimeOffset requestedAt,
        DateTimeOffset assignedAt)
    {
        ArgumentNullException.ThrowIfNull(attendeeCodes);

        FestivalDayId = festivalDayId;
        AttendeeCodes = Array.AsReadOnly(attendeeCodes.ToArray());
        RequestedAt = requestedAt;
        AssignedAt = assignedAt;
    }
}
