using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Infrastructure.Assignments.InMemory;
using FluentAssertions;

namespace Festival.Infrastructure.Tests.Assignments.InMemory;

public sealed class InMemoryAssignmentRequestRepositoryTests
{
    private static readonly DateTimeOffset RequestedAt =
        new(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(-5));

    private static readonly DateTimeOffset ResolvedAt =
        new(2026, 7, 10, 9, 1, 0, TimeSpan.FromHours(-5));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddAsync_ShouldPreserveEligibility(bool allowsFrontStanding)
    {
        var request = AssignmentRequest.Create(
            AssignmentRequestId.New(), FestivalDayId.New(),
            [AttendeeCode.Create("ATT-001")], RequestedAt,
            new RequestEligibility(allowsFrontStanding));
        var repository = new InMemoryAssignmentRequestRepository();

        await repository.AddAsync(request);

        repository.AssignmentRequests.Should().ContainSingle()
            .Which.Eligibility.Should().Be(request.Eligibility);
    }

    [Fact]
    public async Task AddAsync_ShouldAddAssignmentRequestInMemory()
    {
        var assignmentRequest = CreateAssignmentRequest();
        var repository = new InMemoryAssignmentRequestRepository();

        await repository.AddAsync(assignmentRequest);

        var savedRequest = Assert.Single(repository.AssignmentRequests);

        Assert.Equal(assignmentRequest, savedRequest);
    }

    [Fact]
    public async Task AddAsync_ShouldPreserveAddedAssignmentRequestStatus()
    {
        var assignmentRequest = CreateAssignmentRequest();
        assignmentRequest.Complete(ResolvedAt);
        var repository = new InMemoryAssignmentRequestRepository();

        await repository.AddAsync(assignmentRequest);

        var savedRequest = Assert.Single(repository.AssignmentRequests);

        Assert.Equal(AssignmentRequestStatus.Completed, savedRequest.Status);
        Assert.Equal(ResolvedAt, savedRequest.ResolvedAt);
    }

    private static AssignmentRequest CreateAssignmentRequest()
    {
        return AssignmentRequest.Create(
            AssignmentRequestId.Create(
                Guid.Parse("40000000-0000-0000-0000-000000000001")),
            FestivalDayId.Create(
                Guid.Parse("50000000-0000-0000-0000-000000000001")),
            [AttendeeCode.Create("ATT-001")],
            RequestedAt,
            new RequestEligibility(true));
    }
}
