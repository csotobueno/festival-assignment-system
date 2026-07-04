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

    public void EnsureValidResult(IEnumerable<Assignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(assignments);

        var results = assignments.ToArray();

        if (results.Any(assignment => assignment is null))
        {
            throw new ArgumentException(
                "Assignments cannot contain null values.",
                nameof(assignments));
        }

        if (results.Any(assignment => assignment.AssignmentRequestId != AssignmentRequestId))
        {
            throw new ArgumentException(
                "All assignments must belong to the assignment group's assignment request.",
                nameof(assignments));
        }

        if (results.Any(assignment => assignment.FestivalDayId != FestivalDayId))
        {
            throw new ArgumentException(
                "All assignments must belong to the assignment group's festival day.",
                nameof(assignments));
        }

        var resultAttendeeIds = results
            .Select(assignment => assignment.AttendeeId)
            .ToArray();

        if (resultAttendeeIds.Distinct().Count() != resultAttendeeIds.Length)
        {
            throw new ArgumentException(
                "Assignment result cannot contain duplicate attendees.",
                nameof(assignments));
        }

        if (!resultAttendeeIds.ToHashSet().SetEquals(AttendeeIds))
        {
            throw new ArgumentException(
                "Assignment result must contain exactly one assignment per attendee in the group.",
                nameof(assignments));
        }

        if (results.Select(assignment => assignment.ZoneId).Distinct().Count() != 1)
        {
            throw new ArgumentException(
                "Assignment result must belong to a single zone.",
                nameof(assignments));
        }

        if (results.Select(assignment => assignment.RowCode).Distinct().Count() != 1)
        {
            throw new ArgumentException(
                "Assignment result must belong to a single row.",
                nameof(assignments));
        }

        var orderedSpotNumbers = results
            .Select(assignment => assignment.SpotNumber.Value)
            .OrderBy(value => value)
            .ToArray();

        for (var index = 1; index < orderedSpotNumbers.Length; index++)
        {
            if (orderedSpotNumbers[index] != orderedSpotNumbers[index - 1] + 1)
            {
                throw new ArgumentException(
                    "Assignment result must contain consecutive spot numbers.",
                    nameof(assignments));
            }
        }
    }
}
