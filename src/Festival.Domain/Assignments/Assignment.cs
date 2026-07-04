using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Assignments;

public sealed class Assignment
{
    public AssignmentId Id { get; }

    public AssignmentRequestId AssignmentRequestId { get; }

    public FestivalDayId FestivalDayId { get; }

    public AttendeeId AttendeeId { get; }

    public SpotCode SpotCode { get; }

    public ZoneId ZoneId { get; }

    public RowCode RowCode { get; }

    public SpotNumber SpotNumber { get; }

    public DateTimeOffset AssignedAt { get; }

    private Assignment(
        AssignmentId id,
        AssignmentRequestId assignmentRequestId,
        FestivalDayId festivalDayId,
        AttendeeId attendeeId,
        SpotCode spotCode,
        ZoneId zoneId,
        RowCode rowCode,
        SpotNumber spotNumber,
        DateTimeOffset assignedAt)
    {
        Id = id;
        AssignmentRequestId = assignmentRequestId;
        FestivalDayId = festivalDayId;
        AttendeeId = attendeeId;
        SpotCode = spotCode;
        ZoneId = zoneId;
        RowCode = rowCode;
        SpotNumber = spotNumber;
        AssignedAt = assignedAt;
    }

    public static Assignment Create(
        AssignmentId id,
        AssignmentRequestId assignmentRequestId,
        FestivalDayId festivalDayId,
        AttendeeId attendeeId,
        SpotCode spotCode,
        ZoneId zoneId,
        RowCode rowCode,
        SpotNumber spotNumber,
        DateTimeOffset assignedAt)
    {
        if (id == default)
        {
            throw new ArgumentException(
                "Assignment id is required.",
                nameof(id));
        }

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

        if (attendeeId == default)
        {
            throw new ArgumentException(
                "Attendee id is required.",
                nameof(attendeeId));
        }

        ArgumentNullException.ThrowIfNull(spotCode);

        if (zoneId == default)
        {
            throw new ArgumentException(
                "Zone id is required.",
                nameof(zoneId));
        }

        ArgumentNullException.ThrowIfNull(rowCode);

        if (spotNumber == default)
        {
            throw new ArgumentException(
                "Spot number is required.",
                nameof(spotNumber));
        }

        return new Assignment(
            id,
            assignmentRequestId,
            festivalDayId,
            attendeeId,
            spotCode,
            zoneId,
            rowCode,
            spotNumber,
            assignedAt);
    }
}
