using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;

namespace Festival.Domain.Tests.Assignments;

public sealed class AssignmentRequestTests
{
    private static readonly DateTimeOffset RequestedAt =
        new(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void Create_ShouldReturnReceivedRequest_WhenDataIsValid()
    {
        var id = AssignmentRequestId.New();
        var festivalDayId = FestivalDayId.New();

        var request = AssignmentRequest.Create(
            id,
            festivalDayId,
            [
                AttendeeCode.Create("ATT-001"),
                AttendeeCode.Create("ATT-002")
            ],
            RequestedAt);

        Assert.Equal(id, request.Id);
        Assert.Equal(festivalDayId, request.FestivalDayId);
        Assert.Equal(2, request.RequestedAttendeeCodes.Count);
        Assert.Equal(RequestedAt, request.RequestedAt);
        Assert.Equal(
            AssignmentRequestStatus.Received,
            request.Status);
        Assert.Null(request.ResolvedAt);
        Assert.Null(request.Rejection);
        Assert.Null(request.Failure);
    }

    [Fact]
    public void Create_ShouldThrow_WhenIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => AssignmentRequest.Create(
                default,
                FestivalDayId.New(),
                [AttendeeCode.Create("ATT-001")],
                RequestedAt));

        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenFestivalDayIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => AssignmentRequest.Create(
                AssignmentRequestId.New(),
                default,
                [AttendeeCode.Create("ATT-001")],
                RequestedAt));

        Assert.Equal("festivalDayId", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenCodesAreNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => AssignmentRequest.Create(
                AssignmentRequestId.New(),
                FestivalDayId.New(),
                null!,
                RequestedAt));

        Assert.Equal("attendeeCodes", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenNoCodesAreProvided()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => AssignmentRequest.Create(
                AssignmentRequestId.New(),
                FestivalDayId.New(),
                [],
                RequestedAt));

        Assert.Equal("attendeeCodes", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenMoreThanTenCodesAreProvided()
    {
        var codes = Enumerable
            .Range(1, 11)
            .Select(number =>
                AttendeeCode.Create($"ATT-{number:000}"))
            .ToArray();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => AssignmentRequest.Create(
                AssignmentRequestId.New(),
                FestivalDayId.New(),
                codes,
                RequestedAt));

        Assert.Equal("attendeeCodes", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenCodesAreDuplicated()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => AssignmentRequest.Create(
                AssignmentRequestId.New(),
                FestivalDayId.New(),
                [
                    AttendeeCode.Create("ATT-001"),
                    AttendeeCode.Create("att-001")
                ],
                RequestedAt));

        Assert.Equal("attendeeCodes", exception.ParamName);
    }

    [Fact]
    public void Complete_ShouldChangeStatusToCompleted()
    {
        var request = CreateRequest();
        var completedAt = request.RequestedAt.AddSeconds(1);

        request.Complete(completedAt);

        Assert.Equal(
            AssignmentRequestStatus.Completed,
            request.Status);
        Assert.Equal(completedAt, request.ResolvedAt);
        Assert.Null(request.Rejection);
        Assert.Null(request.Failure);
    }

    [Fact]
    public void Reject_ShouldStoreBusinessReason()
    {
        var request = CreateRequest();
        var rejection = AssignmentRequestRejection.Create(
            "CONTIGUOUS_SPOTS_NOT_AVAILABLE",
            "No contiguous spots were found for the requested group.");

        var rejectedAt = request.RequestedAt.AddSeconds(1);

        request.Reject(rejection, rejectedAt);

        Assert.Equal(
            AssignmentRequestStatus.Rejected,
            request.Status);
        Assert.Equal(rejection, request.Rejection);
        Assert.Equal(rejectedAt, request.ResolvedAt);
        Assert.Null(request.Failure);
    }

    [Fact]
    public void Reject_ShouldThrow_WhenReasonIsNull()
    {
        var request = CreateRequest();

        var exception = Assert.Throws<ArgumentNullException>(
            () => request.Reject(
                null!,
                request.RequestedAt.AddSeconds(1)));

        Assert.Equal("rejection", exception.ParamName);
    }

    [Fact]
    public void Fail_ShouldStoreTechnicalFailure()
    {
        var request = CreateRequest();
        var failure = AssignmentRequestFailure.Create(
            "PROCESSING_TIMEOUT",
            "The assignment process exceeded its time limit.");

        var failedAt = request.RequestedAt.AddSeconds(1);

        request.Fail(failure, failedAt);

        Assert.Equal(
            AssignmentRequestStatus.Failed,
            request.Status);
        Assert.Equal(failure, request.Failure);
        Assert.Equal(failedAt, request.ResolvedAt);
        Assert.Null(request.Rejection);
    }

    [Fact]
    public void Fail_ShouldThrow_WhenFailureIsNull()
    {
        var request = CreateRequest();

        var exception = Assert.Throws<ArgumentNullException>(
            () => request.Fail(
                null!,
                request.RequestedAt.AddSeconds(1)));

        Assert.Equal("failure", exception.ParamName);
    }

    [Theory]
    [InlineData(AssignmentRequestStatus.Completed)]
    [InlineData(AssignmentRequestStatus.Rejected)]
    [InlineData(AssignmentRequestStatus.Failed)]
    public void ResolvedRequest_ShouldNotAllowAnotherTransition(
    AssignmentRequestStatus finalStatus)
    {
        var request = CreateRequest();
        Resolve(request, finalStatus);

        Assert.Throws<InvalidOperationException>(
            () => request.Complete(
                request.RequestedAt.AddMinutes(1)));
    }

    [Fact]
    public void Complete_ShouldThrow_WhenResolutionTimeIsBeforeRequestTime()
    {
        var request = CreateRequest();

        var exception = Assert.Throws<ArgumentException>(
            () => request.Complete(
                request.RequestedAt.AddSeconds(-1)));

        Assert.Equal("resolvedAt", exception.ParamName);
    }

    [Fact]
    public void Rejection_ShouldNormalizeCode()
    {
        var rejection = AssignmentRequestRejection.Create(
            " contiguous_spots_not_available ",
            "No contiguous spots were found.");

        Assert.Equal(
            "CONTIGUOUS_SPOTS_NOT_AVAILABLE",
            rejection.Code);
    }

    [Fact]
    public void Failure_ShouldThrow_WhenMessageIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => AssignmentRequestFailure.Create(
                "PROCESSING_TIMEOUT",
                "   "));

        Assert.Equal("message", exception.ParamName);
    }

    private static AssignmentRequest CreateRequest()
    {
        return AssignmentRequest.Create(
            AssignmentRequestId.New(),
            FestivalDayId.New(),
            [
                AttendeeCode.Create("ATT-001"),
                AttendeeCode.Create("ATT-002")
            ],
            new DateTimeOffset(
                2026, 7, 10, 9, 0, 0,
                TimeSpan.FromHours(-5)));
    }

    private static void Resolve(
        AssignmentRequest request,
        AssignmentRequestStatus status)
    {
        var resolvedAt = request.RequestedAt.AddSeconds(1);

        switch (status)
        {
            case AssignmentRequestStatus.Completed:
                request.Complete(resolvedAt);
                break;

            case AssignmentRequestStatus.Rejected:
                request.Reject(
                    AssignmentRequestRejection.Create(
                        "ATTENDEE_ALREADY_ASSIGNED",
                        "An attendee already has an assignment."),
                    resolvedAt);
                break;

            case AssignmentRequestStatus.Failed:
                request.Fail(
                    AssignmentRequestFailure.Create(
                        "UNEXPECTED_ERROR",
                        "An unexpected error occurred."),
                    resolvedAt);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    null);
        }
    }
}
