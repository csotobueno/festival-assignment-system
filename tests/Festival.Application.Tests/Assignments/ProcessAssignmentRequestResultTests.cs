using Festival.Application.Assignments.ProcessAssignmentRequest;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Application.Tests.Assignments;

public sealed class ProcessAssignmentRequestResultTests
{
    private static readonly DateTimeOffset RequestedAt =
        new(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(-5));

    private static readonly DateTimeOffset ResolvedAt =
        new(2026, 7, 10, 9, 1, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void Assigned_ShouldReturnAssignedResult_WhenRequestIsCompletedAndAssignmentsAreValid()
    {
        var request = CreateCompletedRequest();
        var assignment = CreateAssignment(request);

        var result = ProcessAssignmentRequestResult.Assigned(
            request,
            [assignment]);

        var output = Assert.Single(result.Assignments);

        Assert.True(result.IsAssigned);
        Assert.False(result.IsRejected);
        Assert.Equal(AssignmentRequestStatus.Completed, result.Status);
        Assert.Equal(request.Id, result.AssignmentRequestId);
        Assert.Equal(request.RequestedAt, result.RequestedAt);
        Assert.Equal(request.ResolvedAt, result.ResolvedAt);
        Assert.Null(result.RejectionCode);
        Assert.Null(result.RejectionMessage);
        Assert.Equal(assignment.Id, output.AssignmentId);
        Assert.Equal(assignment.AttendeeId, output.AttendeeId);
        Assert.Equal(assignment.SpotCode, output.SpotCode);
    }

    [Fact]
    public void Assigned_ShouldThrow_WhenRequestIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ProcessAssignmentRequestResult.Assigned(
                null!,
                [CreateAssignment(CreateCompletedRequest())]));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public void Assigned_ShouldThrow_WhenRequestIsNotCompleted()
    {
        var request = CreateRequest();

        var exception = Assert.Throws<ArgumentException>(
            () => ProcessAssignmentRequestResult.Assigned(
                request,
                [CreateAssignment(request)]));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public void Assigned_ShouldThrow_WhenAssignmentsCollectionIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ProcessAssignmentRequestResult.Assigned(
                CreateCompletedRequest(),
                null!));

        Assert.Equal("assignments", exception.ParamName);
    }

    [Fact]
    public void Assigned_ShouldThrow_WhenAssignmentsContainNull()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ProcessAssignmentRequestResult.Assigned(
                CreateCompletedRequest(),
                [null!]));

        Assert.Equal("assignments", exception.ParamName);
    }

    [Fact]
    public void Assigned_ShouldThrow_WhenAssignmentsAreEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ProcessAssignmentRequestResult.Assigned(
                CreateCompletedRequest(),
                []));

        Assert.Equal("assignments", exception.ParamName);
    }

    [Fact]
    public void Rejected_ShouldReturnRejectedResult_WhenRequestIsRejectedWithReason()
    {
        var request = CreateRejectedRequest();

        var result = ProcessAssignmentRequestResult.Rejected(request);

        Assert.False(result.IsAssigned);
        Assert.True(result.IsRejected);
        Assert.Equal(AssignmentRequestStatus.Rejected, result.Status);
        Assert.Equal(request.Id, result.AssignmentRequestId);
        Assert.Equal(request.RequestedAt, result.RequestedAt);
        Assert.Equal(request.ResolvedAt, result.ResolvedAt);
        Assert.Equal(request.Rejection?.Code, result.RejectionCode);
        Assert.Equal(request.Rejection?.Message, result.RejectionMessage);
    }

    [Fact]
    public void Rejected_ShouldThrow_WhenRequestIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ProcessAssignmentRequestResult.Rejected(null!));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public void Rejected_ShouldThrow_WhenRequestIsNotRejected()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ProcessAssignmentRequestResult.Rejected(CreateRequest()));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public void Rejected_ShouldThrow_WhenRejectionReasonIsMissing()
    {
        var request = CreateRejectedRequestWithoutReason();

        var exception = Assert.Throws<ArgumentException>(
            () => ProcessAssignmentRequestResult.Rejected(request));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public void Rejected_ShouldReturnNoAssignments()
    {
        var result = ProcessAssignmentRequestResult.Rejected(
            CreateRejectedRequest());

        Assert.Empty(result.Assignments);
    }

    private static AssignmentRequest CreateCompletedRequest()
    {
        var request = CreateRequest();

        request.Complete(ResolvedAt);

        return request;
    }

    private static AssignmentRequest CreateRejectedRequest()
    {
        var request = CreateRequest();

        request.Reject(
            AssignmentRequestRejection.Create(
                "NO_CONTIGUOUS_SPOTS_AVAILABLE",
                "No contiguous spots are available."),
            ResolvedAt);

        return request;
    }

    private static AssignmentRequest CreateRejectedRequestWithoutReason()
    {
        var request = CreateRequest();
        var statusField = typeof(AssignmentRequest).GetField(
            "<Status>k__BackingField",
            System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.NonPublic);
        var resolvedAtField = typeof(AssignmentRequest).GetField(
            "<ResolvedAt>k__BackingField",
            System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.NonPublic);

        statusField!.SetValue(request, AssignmentRequestStatus.Rejected);
        resolvedAtField!.SetValue(request, ResolvedAt);

        return request;
    }

    private static AssignmentRequest CreateRequest()
    {
        return AssignmentRequest.Create(
            AssignmentRequestId.Create(
                Guid.Parse("30000000-0000-0000-0000-000000000001")),
            FestivalDayId.Create(
                Guid.Parse("10000000-0000-0000-0000-000000000001")),
            [AttendeeCode.Create("ATT-001")],
            RequestedAt);
    }

    private static Assignment CreateAssignment(
        AssignmentRequest request)
    {
        return Assignment.Create(
            AssignmentId.Create(
                Guid.Parse("40000000-0000-0000-0000-000000000001")),
            request.Id,
            request.FestivalDayId,
            AttendeeId.Create(
                Guid.Parse("50000000-0000-0000-0000-000000000001")),
            SpotCode.Create("A-001"),
            ZoneId.Create(
                Guid.Parse("20000000-0000-0000-0000-000000000001")),
            RowCode.Create("A"),
            SpotNumber.Create(1),
            ResolvedAt);
    }
}
