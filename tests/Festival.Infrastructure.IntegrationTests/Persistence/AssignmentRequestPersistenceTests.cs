using Festival.Domain.Assignments;
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
    [InlineData(AssignmentRequestStatus.Received)]
    [InlineData(AssignmentRequestStatus.Completed)]
    [InlineData(AssignmentRequestStatus.Rejected)]
    [InlineData(AssignmentRequestStatus.Failed)]
    public async Task AssignmentRequest_ShouldRoundTripThroughPersistenceRows(
        AssignmentRequestStatus status)
    {
        await using var context = Fixture.CreateDbContext();
        var festivalDay = IntegrationTestData.CreateFestivalDay();
        var request = IntegrationTestData.CreateRequest(status);
        var row = AssignmentRequestMapper.ToRow(request);

        context.FestivalDays.Add(festivalDay);
        context.AssignmentRequests.Add(row);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persistedRow = await context.AssignmentRequests
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
        persisted.ResolvedAt.Should().Be(request.ResolvedAt);
        persisted.RequestedAttendeeCodes
            .Select(code => code.Value)
            .Should()
            .Equal("ATT-001", "ATT-002", "ATT-003");
        persisted.Rejection.Should().Be(request.Rejection);
        persisted.Failure.Should().Be(request.Failure);
    }
}
