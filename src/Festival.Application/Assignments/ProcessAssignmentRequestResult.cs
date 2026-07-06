using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Application.Assignments;

public sealed class ProcessAssignmentRequestResult
{
    public AssignmentRequestId AssignmentRequestId { get; }

    public AssignmentRequestStatus Status { get; }

    public DateTimeOffset RequestedAt { get; }

    public DateTimeOffset? ResolvedAt { get; }

    public bool IsAssigned => Status == AssignmentRequestStatus.Completed;

    public bool IsRejected => Status == AssignmentRequestStatus.Rejected;

    public string? RejectionCode { get; }

    public string? RejectionMessage { get; }

    public IReadOnlyList<AssignmentOutput> Assignments { get; }

    private ProcessAssignmentRequestResult(
        AssignmentRequestId assignmentRequestId,
        AssignmentRequestStatus status,
        DateTimeOffset requestedAt,
        DateTimeOffset? resolvedAt,
        string? rejectionCode,
        string? rejectionMessage,
        IReadOnlyList<AssignmentOutput> assignments)
    {
        AssignmentRequestId = assignmentRequestId;
        Status = status;
        RequestedAt = requestedAt;
        ResolvedAt = resolvedAt;
        RejectionCode = rejectionCode;
        RejectionMessage = rejectionMessage;
        Assignments = assignments;
    }

    internal static ProcessAssignmentRequestResult Assigned(
        AssignmentRequest request,
        IEnumerable<Assignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(assignments);

        var assignmentOutputs = assignments
            .Select(AssignmentOutput.FromAssignment)
            .ToArray();

        return new ProcessAssignmentRequestResult(
            request.Id,
            request.Status,
            request.RequestedAt,
            request.ResolvedAt,
            null,
            null,
            Array.AsReadOnly(assignmentOutputs));
    }

    internal static ProcessAssignmentRequestResult Rejected(
        AssignmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ProcessAssignmentRequestResult(
            request.Id,
            request.Status,
            request.RequestedAt,
            request.ResolvedAt,
            request.Rejection?.Code,
            request.Rejection?.Message,
            Array.AsReadOnly(Array.Empty<AssignmentOutput>()));
    }
}

public sealed class AssignmentOutput
{
    public AssignmentId AssignmentId { get; }

    public AssignmentRequestId AssignmentRequestId { get; }

    public FestivalDayId FestivalDayId { get; }

    public AttendeeId AttendeeId { get; }

    public SpotCode SpotCode { get; }

    public ZoneId ZoneId { get; }

    public RowCode RowCode { get; }

    public SpotNumber SpotNumber { get; }

    public DateTimeOffset AssignedAt { get; }

    private AssignmentOutput(
        AssignmentId assignmentId,
        AssignmentRequestId assignmentRequestId,
        FestivalDayId festivalDayId,
        AttendeeId attendeeId,
        SpotCode spotCode,
        ZoneId zoneId,
        RowCode rowCode,
        SpotNumber spotNumber,
        DateTimeOffset assignedAt)
    {
        AssignmentId = assignmentId;
        AssignmentRequestId = assignmentRequestId;
        FestivalDayId = festivalDayId;
        AttendeeId = attendeeId;
        SpotCode = spotCode;
        ZoneId = zoneId;
        RowCode = rowCode;
        SpotNumber = spotNumber;
        AssignedAt = assignedAt;
    }

    internal static AssignmentOutput FromAssignment(Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        return new AssignmentOutput(
            assignment.Id,
            assignment.AssignmentRequestId,
            assignment.FestivalDayId,
            assignment.AttendeeId,
            assignment.SpotCode,
            assignment.ZoneId,
            assignment.RowCode,
            assignment.SpotNumber,
            assignment.AssignedAt);
    }
}
