using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;

namespace Festival.Domain.Assignments;

public sealed class AssignmentGroup
{
    public AssignmentRequestId AssignmentRequestId { get; }

    public FestivalDayId FestivalDayId { get; }

    public IReadOnlyList<AttendeeId> AttendeeIds { get; }

    public GroupSize GroupSize { get; }

    private AssignmentGroup(
        AssignmentRequestId assignmentRequestId,
        FestivalDayId festivalDayId,
        IReadOnlyList<AttendeeId> attendeeIds,
        GroupSize groupSize)
    {
        AssignmentRequestId = assignmentRequestId;
        FestivalDayId = festivalDayId;
        AttendeeIds = attendeeIds;
        GroupSize = groupSize;
    }

    public static AssignmentGroup Create(
        AssignmentRequestId assignmentRequestId,
        FestivalDayId festivalDayId,
        IEnumerable<AttendeeId> attendeeIds)
    {
        if (assignmentRequestId == default)
        {
            throw new ArgumentException(
                "Assignment request id is required.",
                nameof(assignmentRequestId));
        }

        if (festivalDayId == default)
        {
            throw new ArgumentException(
                "Festival day id is required.",
                nameof(festivalDayId));
        }

        ArgumentNullException.ThrowIfNull(attendeeIds);

        var ids = attendeeIds.ToArray();

        if (ids.Any(attendeeId => attendeeId == default))
        {
            throw new ArgumentException(
                "Attendee ids cannot contain empty values.",
                nameof(attendeeIds));
        }

        if (ids.Distinct().Count() != ids.Length)
        {
            throw new ArgumentException(
                "An assignment group cannot contain duplicate attendee ids.",
                nameof(attendeeIds));
        }

        var groupSize = GroupSize.Create(ids.Length);

        return new AssignmentGroup(
            assignmentRequestId,
            festivalDayId,
            Array.AsReadOnly(ids),
            groupSize);
    }
}
