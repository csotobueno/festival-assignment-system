using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;

namespace Festival.Domain.Assignments;

public sealed class AssignmentRequest
{
    private const int MinimumAttendeeCount = 1;
    private const int MaximumAttendeeCount = 10;

    public AssignmentRequestId Id { get; }

    public FestivalDayId FestivalDayId { get; }

    public IReadOnlyList<AttendeeCode> RequestedAttendeeCodes { get; }

    public DateTimeOffset RequestedAt { get; }

    public AssignmentRequestStatus Status { get; private set; }

    public AssignmentRequestRejection? Rejection { get; private set; }

    public AssignmentRequestFailure? Failure { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    private AssignmentRequest(
        AssignmentRequestId id,
        FestivalDayId festivalDayId,
        IReadOnlyList<AttendeeCode> requestedAttendeeCodes,
        DateTimeOffset requestedAt)
    {
        Id = id;
        FestivalDayId = festivalDayId;
        RequestedAttendeeCodes = requestedAttendeeCodes;
        RequestedAt = requestedAt;
        Status = AssignmentRequestStatus.Received;
    }

    public static AssignmentRequest Create(
        AssignmentRequestId id,
        FestivalDayId festivalDayId,
        IEnumerable<AttendeeCode> attendeeCodes,
        DateTimeOffset requestedAt)
    {
        if (id == default)
        {
            throw new ArgumentException(
                "Assignment request id is required.",
                nameof(id));
        }

        if (festivalDayId == default)
        {
            throw new ArgumentException(
                "Festival day id is required.",
                nameof(festivalDayId));
        }

        ArgumentNullException.ThrowIfNull(attendeeCodes);

        var codes = attendeeCodes.ToArray();

        if (codes.Any(code => code is null))
        {
            throw new ArgumentException(
                "Attendee codes cannot contain null values.",
                nameof(attendeeCodes));
        }

        if (codes.Length is < MinimumAttendeeCount or > MaximumAttendeeCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attendeeCodes),
                codes.Length,
                "An assignment request must contain between 1 and 10 attendee codes.");
        }

        if (codes.Distinct().Count() != codes.Length)
        {
            throw new ArgumentException(
                "An assignment request cannot contain duplicate attendee codes.",
                nameof(attendeeCodes));
        }

        return new AssignmentRequest(
            id,
            festivalDayId,
            Array.AsReadOnly(codes),
            requestedAt);
    }

    public void Complete(DateTimeOffset completedAt)
    {
        EnsureCanResolve(completedAt);

        Status = AssignmentRequestStatus.Completed;
        ResolvedAt = completedAt;
    }

    public void Reject(
        AssignmentRequestRejection rejection,
        DateTimeOffset rejectedAt)
    {
        ArgumentNullException.ThrowIfNull(rejection);
        EnsureCanResolve(rejectedAt);

        Status = AssignmentRequestStatus.Rejected;
        Rejection = rejection;
        ResolvedAt = rejectedAt;
    }

    public void Fail(
        AssignmentRequestFailure failure,
        DateTimeOffset failedAt)
    {
        ArgumentNullException.ThrowIfNull(failure);
        EnsureCanResolve(failedAt);

        Status = AssignmentRequestStatus.Failed;
        Failure = failure;
        ResolvedAt = failedAt;
    }

    private void EnsureCanResolve(DateTimeOffset resolvedAt)
    {
        if (Status != AssignmentRequestStatus.Received)
        {
            throw new InvalidOperationException(
                $"Assignment request in status '{Status}' cannot be resolved again.");
        }

        if (resolvedAt < RequestedAt)
        {
            throw new ArgumentException(
                "Resolution time cannot be earlier than request time.",
                nameof(resolvedAt));
        }
    }
}