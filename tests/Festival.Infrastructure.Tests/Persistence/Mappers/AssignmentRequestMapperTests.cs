using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Infrastructure.Persistence.Mappers;
using Festival.Infrastructure.Persistence.Models;
using FluentAssertions;

namespace Festival.Infrastructure.Tests.Persistence.Mappers;

public sealed class AssignmentRequestMapperTests
{
    private static readonly AssignmentRequestId RequestId =
        AssignmentRequestId.Create(
            Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static readonly FestivalDayId FestivalDayId =
        FestivalDayId.Create(
            Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private static readonly DateTimeOffset RequestedAt =
        new(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(-5));

    private static readonly DateTimeOffset ResolvedAt =
        new(2026, 7, 10, 9, 5, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void ToRow_ShouldMapReceivedRequest()
    {
        var request = CreateRequest();

        var row = AssignmentRequestMapper.ToRow(request);

        row.AssignmentRequestId.Should().Be(RequestId);
        row.FestivalDayId.Should().Be(FestivalDayId);
        row.RequestedAt.Should().Be(RequestedAt);
        row.Status.Should().Be(AssignmentRequestStatus.Received);
        row.ResolvedAt.Should().BeNull();
        row.RejectionCode.Should().BeNull();
        row.RejectionMessage.Should().BeNull();
        row.FailureCode.Should().BeNull();
        row.FailureMessage.Should().BeNull();
        AssertAttendeeRows(row, "ATT-001", "ATT-002", "ATT-003");
    }

    [Fact]
    public void ToRow_ShouldMapCompletedRequest()
    {
        var request = CreateRequest();
        request.Complete(ResolvedAt);

        var row = AssignmentRequestMapper.ToRow(request);

        row.Status.Should().Be(AssignmentRequestStatus.Completed);
        row.ResolvedAt.Should().Be(ResolvedAt);
        row.RejectionCode.Should().BeNull();
        row.RejectionMessage.Should().BeNull();
        row.FailureCode.Should().BeNull();
        row.FailureMessage.Should().BeNull();
        AssertAttendeeRows(row, "ATT-001", "ATT-002", "ATT-003");
    }

    [Fact]
    public void ToRow_ShouldMapRejectedRequest()
    {
        var request = CreateRequest();
        var rejection = AssignmentRequestRejection.Create(
            "contiguous_spots_not_available",
            "No contiguous spots were found.");
        request.Reject(rejection, ResolvedAt);

        var row = AssignmentRequestMapper.ToRow(request);

        row.Status.Should().Be(AssignmentRequestStatus.Rejected);
        row.ResolvedAt.Should().Be(ResolvedAt);
        row.RejectionCode.Should().Be(rejection.Code);
        row.RejectionMessage.Should().Be(rejection.Message);
        row.FailureCode.Should().BeNull();
        row.FailureMessage.Should().BeNull();
        AssertAttendeeRows(row, "ATT-001", "ATT-002", "ATT-003");
    }

    [Fact]
    public void ToRow_ShouldMapFailedRequest()
    {
        var request = CreateRequest();
        var failure = AssignmentRequestFailure.Create(
            "processing_timeout",
            "The assignment process exceeded its time limit.");
        request.Fail(failure, ResolvedAt);

        var row = AssignmentRequestMapper.ToRow(request);

        row.Status.Should().Be(AssignmentRequestStatus.Failed);
        row.ResolvedAt.Should().Be(ResolvedAt);
        row.FailureCode.Should().Be(failure.Code);
        row.FailureMessage.Should().Be(failure.Message);
        row.RejectionCode.Should().BeNull();
        row.RejectionMessage.Should().BeNull();
        AssertAttendeeRows(row, "ATT-001", "ATT-002", "ATT-003");
    }

    [Fact]
    public void ToDomain_ShouldRehydrateReceivedRequest()
    {
        var row = CreateRow(AssignmentRequestStatus.Received);

        var request = AssignmentRequestMapper.ToDomain(row);

        AssertCoreRequest(request);
        request.Status.Should().Be(AssignmentRequestStatus.Received);
        request.ResolvedAt.Should().BeNull();
        request.Rejection.Should().BeNull();
        request.Failure.Should().BeNull();
    }

    [Fact]
    public void ToDomain_ShouldRehydrateCompletedRequest()
    {
        var row = CreateRow(
            AssignmentRequestStatus.Completed,
            resolvedAt: ResolvedAt);

        var request = AssignmentRequestMapper.ToDomain(row);

        AssertCoreRequest(request);
        request.Status.Should().Be(AssignmentRequestStatus.Completed);
        request.ResolvedAt.Should().Be(ResolvedAt);
        request.Rejection.Should().BeNull();
        request.Failure.Should().BeNull();
    }

    [Fact]
    public void ToDomain_ShouldRehydrateRejectedRequest()
    {
        var row = CreateRow(
            AssignmentRequestStatus.Rejected,
            resolvedAt: ResolvedAt,
            rejectionCode: "ATTENDEE_ALREADY_ASSIGNED",
            rejectionMessage: "An attendee already has an assignment.");

        var request = AssignmentRequestMapper.ToDomain(row);

        AssertCoreRequest(request);
        request.Status.Should().Be(AssignmentRequestStatus.Rejected);
        request.ResolvedAt.Should().Be(ResolvedAt);
        request.Rejection.Should().NotBeNull();
        request.Rejection!.Code.Should().Be("ATTENDEE_ALREADY_ASSIGNED");
        request.Rejection.Message.Should()
            .Be("An attendee already has an assignment.");
        request.Failure.Should().BeNull();
    }

    [Fact]
    public void ToDomain_ShouldRehydrateFailedRequest()
    {
        var row = CreateRow(
            AssignmentRequestStatus.Failed,
            resolvedAt: ResolvedAt,
            failureCode: "UNEXPECTED_ERROR",
            failureMessage: "An unexpected error occurred.");

        var request = AssignmentRequestMapper.ToDomain(row);

        AssertCoreRequest(request);
        request.Status.Should().Be(AssignmentRequestStatus.Failed);
        request.ResolvedAt.Should().Be(ResolvedAt);
        request.Failure.Should().NotBeNull();
        request.Failure!.Code.Should().Be("UNEXPECTED_ERROR");
        request.Failure.Message.Should().Be("An unexpected error occurred.");
        request.Rejection.Should().BeNull();
    }

    [Fact]
    public void ToDomain_ShouldThrow_WhenNoAttendeeRowsExist()
    {
        var row = CreateRow(AssignmentRequestStatus.Received);
        row.Attendees.Clear();

        var act = () => AssignmentRequestMapper.ToDomain(row);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToDomain_ShouldThrow_WhenAttendeeRowsContainDuplicateCodes()
    {
        var row = CreateRow(AssignmentRequestStatus.Received);
        row.Attendees[0].AttendeeCode = AttendeeCode.Create("ATT-002");

        var act = () => AssignmentRequestMapper.ToDomain(row);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToDomain_ShouldThrow_WhenAttendeePositionIsNegative()
    {
        var row = CreateRow(AssignmentRequestStatus.Received);
        row.Attendees[0].Position = -1;

        var act = () => AssignmentRequestMapper.ToDomain(row);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToDomain_ShouldThrow_WhenAttendeePositionsAreDuplicated()
    {
        var row = CreateRow(AssignmentRequestStatus.Received);
        row.Attendees[0].Position = row.Attendees[1].Position;

        var act = () => AssignmentRequestMapper.ToDomain(row);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("REJECTED", null)]
    [InlineData(null, "Rejected message.")]
    public void ToDomain_ShouldThrow_WhenRejectionDataIsIncomplete(
        string? code,
        string? message)
    {
        var row = CreateRow(
            AssignmentRequestStatus.Rejected,
            resolvedAt: ResolvedAt,
            rejectionCode: code,
            rejectionMessage: message);

        var act = () => AssignmentRequestMapper.ToDomain(row);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("FAILED", null)]
    [InlineData(null, "Failure message.")]
    public void ToDomain_ShouldThrow_WhenFailureDataIsIncomplete(
        string? code,
        string? message)
    {
        var row = CreateRow(
            AssignmentRequestStatus.Failed,
            resolvedAt: ResolvedAt,
            failureCode: code,
            failureMessage: message);

        var act = () => AssignmentRequestMapper.ToDomain(row);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToDomain_ShouldThrow_WhenRejectedStatusHasNoRejectionData()
    {
        var row = CreateRow(
            AssignmentRequestStatus.Rejected,
            resolvedAt: ResolvedAt);

        var act = () => AssignmentRequestMapper.ToDomain(row);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToDomain_ShouldThrow_WhenFailedStatusHasNoFailureData()
    {
        var row = CreateRow(
            AssignmentRequestStatus.Failed,
            resolvedAt: ResolvedAt);

        var act = () => AssignmentRequestMapper.ToDomain(row);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToDomain_ShouldThrow_WhenCompletedStatusHasRejectionData()
    {
        var row = CreateRow(
            AssignmentRequestStatus.Completed,
            resolvedAt: ResolvedAt,
            rejectionCode: "REJECTED",
            rejectionMessage: "Rejected message.");

        var act = () => AssignmentRequestMapper.ToDomain(row);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToDomain_ShouldThrow_WhenCompletedStatusHasFailureData()
    {
        var row = CreateRow(
            AssignmentRequestStatus.Completed,
            resolvedAt: ResolvedAt,
            failureCode: "FAILED",
            failureMessage: "Failure message.");

        var act = () => AssignmentRequestMapper.ToDomain(row);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToDomain_ShouldThrow_WhenReceivedStatusHasResolvedAt()
    {
        var row = CreateRow(
            AssignmentRequestStatus.Received,
            resolvedAt: ResolvedAt);

        var act = () => AssignmentRequestMapper.ToDomain(row);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(AssignmentRequestStatus.Completed)]
    [InlineData(AssignmentRequestStatus.Rejected)]
    [InlineData(AssignmentRequestStatus.Failed)]
    public void ToDomain_ShouldThrow_WhenFinalStatusHasNoResolvedAt(
        AssignmentRequestStatus status)
    {
        var row = status switch
        {
            AssignmentRequestStatus.Rejected => CreateRow(
                status,
                rejectionCode: "REJECTED",
                rejectionMessage: "Rejected message."),
            AssignmentRequestStatus.Failed => CreateRow(
                status,
                failureCode: "FAILED",
                failureMessage: "Failure message."),
            _ => CreateRow(status)
        };

        var act = () => AssignmentRequestMapper.ToDomain(row);

        act.Should().Throw<InvalidOperationException>();
    }

    private static AssignmentRequest CreateRequest()
    {
        return AssignmentRequest.Create(
            RequestId,
            FestivalDayId,
            [
                AttendeeCode.Create("ATT-001"),
                AttendeeCode.Create("ATT-002"),
                AttendeeCode.Create("ATT-003")
            ],
            RequestedAt);
    }

    private static AssignmentRequestRow CreateRow(
        AssignmentRequestStatus status,
        DateTimeOffset? resolvedAt = null,
        string? rejectionCode = null,
        string? rejectionMessage = null,
        string? failureCode = null,
        string? failureMessage = null)
    {
        var row = new AssignmentRequestRow
        {
            AssignmentRequestId = RequestId,
            FestivalDayId = FestivalDayId,
            RequestedAt = RequestedAt,
            Status = status,
            ResolvedAt = resolvedAt,
            RejectionCode = rejectionCode,
            RejectionMessage = rejectionMessage,
            FailureCode = failureCode,
            FailureMessage = failureMessage
        };

        row.Attendees =
        [
            CreateAttendeeRow(row, 2, "ATT-003"),
            CreateAttendeeRow(row, 0, "ATT-001"),
            CreateAttendeeRow(row, 1, "ATT-002")
        ];

        return row;
    }

    private static AssignmentRequestAttendeeRow CreateAttendeeRow(
        AssignmentRequestRow row,
        int position,
        string attendeeCode)
    {
        return new AssignmentRequestAttendeeRow
        {
            AssignmentRequestId = row.AssignmentRequestId,
            Position = position,
            AttendeeCode = AttendeeCode.Create(attendeeCode),
            AssignmentRequest = row
        };
    }

    private static void AssertAttendeeRows(
        AssignmentRequestRow row,
        params string[] expectedAttendeeCodes)
    {
        row.Attendees.Should().HaveCount(expectedAttendeeCodes.Length);
        row.Attendees.Select(attendee => attendee.Position)
            .Should()
            .Equal(Enumerable.Range(0, expectedAttendeeCodes.Length));
        row.Attendees.Select(attendee => attendee.AttendeeCode.Value)
            .Should()
            .Equal(expectedAttendeeCodes);
        row.Attendees.Should().OnlyContain(
            attendee => ReferenceEquals(attendee.AssignmentRequest, row));
        row.Attendees.Select(attendee => attendee.AssignmentRequestId)
            .Should()
            .OnlyContain(id => id == row.AssignmentRequestId);
    }

    private static void AssertCoreRequest(
        AssignmentRequest request)
    {
        request.Id.Should().Be(RequestId);
        request.FestivalDayId.Should().Be(FestivalDayId);
        request.RequestedAt.Should().Be(RequestedAt);
        request.RequestedAttendeeCodes.Select(code => code.Value)
            .Should()
            .Equal("ATT-001", "ATT-002", "ATT-003");
    }
}
