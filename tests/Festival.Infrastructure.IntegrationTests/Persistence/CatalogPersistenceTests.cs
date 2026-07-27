using Festival.Infrastructure.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.IntegrationTests.Persistence;

public sealed class CatalogPersistenceTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    [Fact]
    public async Task AttendeeZoneAndSpot_ShouldRoundTripThroughPostgreSql()
    {
        await using var context = Fixture.CreateDbContext();
        var attendee = IntegrationTestData.CreateAttendee();
        var zone = IntegrationTestData.CreateZone();
        var spot = IntegrationTestData.CreateSpot();

        context.AddRange(attendee, zone, spot);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persistedAttendee = await context.Attendees.SingleAsync();
        var persistedZone = await context.Zones.SingleAsync();
        var persistedSpot = await context.Spots.SingleAsync();

        persistedAttendee.Id.Should().Be(IntegrationTestData.FirstAttendeeId);
        persistedAttendee.Code.Value.Should().Be("ATT-001");
        persistedAttendee.Name.Should().Be("Ada Lovelace");

        persistedZone.Id.Should().Be(IntegrationTestData.ZoneId);
        persistedZone.Name.Should().Be("Front");

        persistedSpot.Code.Value.Should().Be("FR-A-001");
        persistedSpot.ZoneId.Should().Be(IntegrationTestData.ZoneId);
        persistedSpot.RowCode.Value.Should().Be("A");
        persistedSpot.Number.Value.Should().Be(1);
    }
}
