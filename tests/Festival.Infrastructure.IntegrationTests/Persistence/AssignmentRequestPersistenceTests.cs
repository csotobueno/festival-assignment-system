using Festival.Domain.Assignments;
using Festival.Infrastructure.Assignments.PostgreSql;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using Festival.Infrastructure.Persistence.Mappers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.IntegrationTests.Persistence;

public sealed class AssignmentRequestPersistenceTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    [Theory]
    [InlineData(AssignmentRequestStatus.Received, true)]
    [InlineData(AssignmentRequestStatus.Received, false)]
    [InlineData(AssignmentRequestStatus.Completed, true)]
    [InlineData(AssignmentRequestStatus.Completed, false)]
    [InlineData(AssignmentRequestStatus.Rejected, true)]
    [InlineData(AssignmentRequestStatus.Rejected, false)]
    [InlineData(AssignmentRequestStatus.Failed, true)]
    [InlineData(AssignmentRequestStatus.Failed, false)]
    public async Task AssignmentRequest_ShouldRoundTripThroughPersistenceRows(
        AssignmentRequestStatus status,
        bool allowsFrontStanding)
    {
        await using var context = Fixture.CreateDbContext();
        var festivalDay = IntegrationTestData.CreateFestivalDay();
        var request = IntegrationTestData.CreateRequest(
            status, allowsFrontStanding: allowsFrontStanding);
        var repository = new PostgreSqlAssignmentRequestRepository(context);

        context.FestivalDays.Add(festivalDay);
        await repository.AddAsync(request);
        await context.SaveChangesAsync();

        await using var reloadContext = Fixture.CreateDbContext();
        var persistedRow = await reloadContext.AssignmentRequests
            .Include(candidate => candidate.Attendees)
            .SingleAsync();

        // Deliberately disturb navigation order. Reconstruction must use the
        // persisted Position rather than the collection's materialized order.
        persistedRow.Attendees = persistedRow.Attendees
            .OrderByDescending(attendee => attendee.Position)
            .ToList();

        var persisted = AssignmentRequestMapper.ToDomain(persistedRow);

        persisted.Id.Should().Be(IntegrationTestData.RequestId);
        persisted.FestivalDayId.Should()
            .Be(IntegrationTestData.FestivalDayId);
        persisted.RequestedAt.Should().Be(IntegrationTestData.RequestedAt);
        persisted.Status.Should().Be(status);
        persistedRow.AllowsFrontStanding.Should().Be(allowsFrontStanding);
        persisted.Eligibility.Should().Be(request.Eligibility);
        persisted.ResolvedAt.Should().Be(request.ResolvedAt);
        persisted.RequestedAttendeeCodes
            .Select(code => code.Value)
            .Should()
            .Equal("ATT-001", "ATT-002", "ATT-003");
        persisted.Rejection.Should().Be(request.Rejection);
        persisted.Failure.Should().Be(request.Failure);
    }
}
