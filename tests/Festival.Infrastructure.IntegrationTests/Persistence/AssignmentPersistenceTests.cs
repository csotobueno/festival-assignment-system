using Festival.Domain.Attendees;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.IntegrationTests.Persistence;

public sealed class AssignmentPersistenceTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    [Fact]
    public async Task Assignment_ShouldRoundTripWithHistoricalSpotSnapshot()
    {
        await using var context = Fixture.CreateDbContext();
        var festivalDay = IntegrationTestData.CreateFestivalDay();
        var attendee = IntegrationTestData.CreateAttendee();
        var zone = IntegrationTestData.CreateZone();
        var spot = IntegrationTestData.CreateSpot();
        var requestRow = IntegrationTestData.CreateRequestRow(
            attendeeCodes: [AttendeeCode.Create("ATT-001")]);
        var assignmentId =
            Guid.Parse("50000000-0000-0000-0000-000000000001");
        var assignment =
            IntegrationTestData.CreateAssignment(assignmentId);

        context.AddRange(
            festivalDay,
            attendee,
            zone,
            spot,
            requestRow,
            assignment);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persisted = await context.Assignments.SingleAsync();

        persisted.Id.Value.Should().Be(assignmentId);
        persisted.AssignmentRequestId.Should()
            .Be(IntegrationTestData.RequestId);
        persisted.FestivalDayId.Should()
            .Be(IntegrationTestData.FestivalDayId);
        persisted.AttendeeId.Should()
            .Be(IntegrationTestData.FirstAttendeeId);
        persisted.SpotCode.Value.Should().Be("FR-A-001");
        persisted.ZoneId.Should().Be(IntegrationTestData.ZoneId);
        persisted.RowCode.Value.Should().Be("A");
        persisted.SpotNumber.Value.Should().Be(1);
        persisted.AssignedAt.Should().Be(IntegrationTestData.AssignedAt);
    }
}
