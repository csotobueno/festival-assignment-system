using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Application.Assignments.ProcessAssignmentRequest;

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

    public IReadOnlyList<ProcessedAssignmentOutput> Assignments { get; }

    private ProcessAssignmentRequestResult(
        AssignmentRequestId assignmentRequestId,
        AssignmentRequestStatus status,
        DateTimeOffset requestedAt,
        DateTimeOffset? resolvedAt,
        string? rejectionCode,
        string? rejectionMessage,
        IReadOnlyList<ProcessedAssignmentOutput> assignments)
    {
        AssignmentRequestId = assignmentRequestId;
        Status = status;
        RequestedAt = requestedAt;
        ResolvedAt = resolvedAt;
        RejectionCode = rejectionCode;
        RejectionMessage = rejectionMessage;
        Assignments = assignments;
    }

    public static ProcessAssignmentRequestResult Assigned(
        AssignmentRequest request,
        IEnumerable<Assignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(assignments);

        if (request.Status != AssignmentRequestStatus.Completed)
        {
            throw new ArgumentException(
                "Assigned result requires a completed assignment request.",
                nameof(request));
        }

        var items = assignments.ToArray();

        if (items.Any(assignment => assignment is null))
        {
            throw new ArgumentException(
                "Assignments cannot contain null values.",
                nameof(assignments));
        }

        if (items.Length == 0)
        {
            throw new ArgumentException(
                "Assigned result must contain at least one assignment.",
                nameof(assignments));
        }

        var assignmentOutputs = items
            .Select(ProcessedAssignmentOutput.FromAssignment)
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

    public static ProcessAssignmentRequestResult Rejected(
        AssignmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Status != AssignmentRequestStatus.Rejected)
        {
            throw new ArgumentException(
                "Rejected result requires a rejected assignment request.",
                nameof(request));
        }

        if (request.Rejection is null)
        {
            throw new ArgumentException(
                "Rejected result requires a rejection reason.",
                nameof(request));
        }

        return new ProcessAssignmentRequestResult(
            request.Id,
            request.Status,
            request.RequestedAt,
            request.ResolvedAt,
            request.Rejection.Code,
            request.Rejection.Message,
            Array.AsReadOnly(Array.Empty<ProcessedAssignmentOutput>()));
    }
}

public sealed class ProcessedAssignmentOutput
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

    private ProcessedAssignmentOutput(
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

    internal static ProcessedAssignmentOutput FromAssignment(
        Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        return new ProcessedAssignmentOutput(
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
