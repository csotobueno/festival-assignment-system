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
        IReadOnlyCollection<AttendeeCode> requestedAttendeeCodes,
        DateTimeOffset requestedAt,
        AssignmentRequestStatus status,
        DateTimeOffset? resolvedAt,
        AssignmentRequestRejection? rejection,
        AssignmentRequestFailure? failure)
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

        ArgumentNullException.ThrowIfNull(requestedAttendeeCodes);

        var codes = requestedAttendeeCodes.ToArray();

        if (codes.Any(code => code is null))
        {
            throw new ArgumentException(
                "Attendee codes cannot contain null values.",
                nameof(requestedAttendeeCodes));
        }

        if (codes.Length is < MinimumAttendeeCount or > MaximumAttendeeCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedAttendeeCodes),
                codes.Length,
                "An assignment request must contain between 1 and 10 attendee codes.");
        }

        if (codes.Distinct().Count() != codes.Length)
        {
            throw new ArgumentException(
                "An assignment request cannot contain duplicate attendee codes.",
                nameof(requestedAttendeeCodes));
        }

        ValidateOutcomeConsistency(
            requestedAt,
            status,
            resolvedAt,
            rejection,
            failure);

        Id = id;
        FestivalDayId = festivalDayId;
        RequestedAttendeeCodes = Array.AsReadOnly(codes);
        RequestedAt = requestedAt;
        Status = status;
        ResolvedAt = resolvedAt;
        Rejection = rejection;
        Failure = failure;
    }

    public static AssignmentRequest Create(
        AssignmentRequestId id,
        FestivalDayId festivalDayId,
        IReadOnlyCollection<AttendeeCode> attendeeCodes,
        DateTimeOffset requestedAt)
    {
        return new AssignmentRequest(
            id,
            festivalDayId,
            attendeeCodes,
            requestedAt,
            AssignmentRequestStatus.Received,
            null,
            null,
            null);
    }

    internal static AssignmentRequest Rehydrate(
        AssignmentRequestId id,
        FestivalDayId festivalDayId,
        IReadOnlyCollection<AttendeeCode> requestedAttendeeCodes,
        DateTimeOffset requestedAt,
        AssignmentRequestStatus status,
        DateTimeOffset? resolvedAt,
        AssignmentRequestRejection? rejection,
        AssignmentRequestFailure? failure)
    {
        return new AssignmentRequest(
            id,
            festivalDayId,
            requestedAttendeeCodes,
            requestedAt,
            status,
            resolvedAt,
            rejection,
            failure);
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

    private static void ValidateOutcomeConsistency(
        DateTimeOffset requestedAt,
        AssignmentRequestStatus status,
        DateTimeOffset? resolvedAt,
        AssignmentRequestRejection? rejection,
        AssignmentRequestFailure? failure)
    {
        if (resolvedAt is not null && resolvedAt.Value < requestedAt)
        {
            throw new ArgumentException(
                "Resolution time cannot be earlier than request time.",
                nameof(resolvedAt));
        }

        switch (status)
        {
            case AssignmentRequestStatus.Received:
                if (resolvedAt is not null || rejection is not null || failure is not null)
                {
                    throw new InvalidOperationException(
                        "A received assignment request cannot have an outcome.");
                }

                break;

            case AssignmentRequestStatus.Completed:
                if (resolvedAt is null || rejection is not null || failure is not null)
                {
                    throw new InvalidOperationException(
                        "A completed assignment request must have only a resolution time.");
                }

                break;

            case AssignmentRequestStatus.Rejected:
                if (resolvedAt is null || rejection is null || failure is not null)
                {
                    throw new InvalidOperationException(
                        "A rejected assignment request must have a resolution time and rejection data only.");
                }

                break;

            case AssignmentRequestStatus.Failed:
                if (resolvedAt is null || rejection is not null || failure is null)
                {
                    throw new InvalidOperationException(
                        "A failed assignment request must have a resolution time and failure data only.");
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Assignment request status is not supported.");
        }
    }
}
