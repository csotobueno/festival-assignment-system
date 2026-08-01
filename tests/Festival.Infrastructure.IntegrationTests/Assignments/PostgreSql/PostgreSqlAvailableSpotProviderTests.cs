using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.Spots;
using Festival.Domain.Zones;
using Festival.Infrastructure.Assignments.PostgreSql;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using Festival.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.IntegrationTests.Assignments.PostgreSql;

public sealed class PostgreSqlAvailableSpotProviderTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    private static readonly ZoneId SecondZoneId = ZoneId.Create(
        Guid.Parse("20000000-0000-0000-0000-000000000002"));

    [Fact]
    public async Task GetAvailableSpotsAsync_ShouldReturnAllSpotsWhenNoneIsAssigned()
    {
        await using var context = Fixture.CreateDbContext();
        var spots = await SeedSpotCatalogAsync(context);
        var provider = new PostgreSqlAvailableSpotProvider(context);

        var available = await provider.GetAvailableSpotsAsync(
            IntegrationTestData.FestivalDayId);

        available.Should().BeEquivalentTo(spots);
    }

    [Fact]
    public async Task GetAvailableSpotsAsync_ShouldExcludeSpotAssignedForRequestedDay()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedSpotCatalogAsync(context);
        await SeedAssignmentAsync(
            context,
            IntegrationTestData.FestivalDayId,
            IntegrationTestData.RequestId);
        var provider = new PostgreSqlAvailableSpotProvider(context);

        var available = await provider.GetAvailableSpotsAsync(
            IntegrationTestData.FestivalDayId);

        available.Select(spot => spot.Code.Value)
            .Should()
            .NotContain("FR-A-001");
    }

    [Fact]
    public async Task GetAvailableSpotsAsync_ShouldKeepSpotAssignedOnlyForAnotherDay()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedSpotCatalogAsync(context);
        await SeedAssignmentAsync(
            context,
            IntegrationTestData.SecondFestivalDayId,
            IntegrationTestData.SecondRequestId);
        var provider = new PostgreSqlAvailableSpotProvider(context);

        var available = await provider.GetAvailableSpotsAsync(
            IntegrationTestData.FestivalDayId);

        available.Select(spot => spot.Code.Value)
            .Should()
            .Contain("FR-A-001");
    }

    [Fact]
    public async Task GetAvailableSpotsAsync_ShouldUseDeterministicPhysicalOrderingWithoutTrackingOrWrites()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedSpotCatalogAsync(context);
        var provider = new PostgreSqlAvailableSpotProvider(context);

        var available = await provider.GetAvailableSpotsAsync(
            IntegrationTestData.FestivalDayId);

        available.Select(spot => spot.Code.Value).Should().Equal(
            "FR-A-001",
            "FR-A-002",
            "FR-B-001",
            "BK-A-001");
        context.ChangeTracker.Entries().Should().BeEmpty();

        await using var verificationContext = Fixture.CreateDbContext();
        (await verificationContext.Spots.CountAsync()).Should().Be(4);
        (await verificationContext.Assignments.CountAsync()).Should().Be(0);
    }

    private static async Task<Spot[]> SeedSpotCatalogAsync(
        FestivalDbContext context)
    {
        var zones = new[]
        {
            IntegrationTestData.CreateZone(),
            Zone.Create(SecondZoneId, "Back")
        };
        var spots = new[]
        {
            IntegrationTestData.CreateSpot("BK-A-001", "A", 1),
            Spot.Create(
                SpotCode.Create("FR-B-001"),
                IntegrationTestData.ZoneId,
                RowCode.Create("B"),
                SpotNumber.Create(1)),
            IntegrationTestData.CreateSpot("FR-A-002", "A", 2),
            IntegrationTestData.CreateSpot("FR-A-001", "A", 1)
        };

        spots[0] = Spot.Create(
            spots[0].Code,
            SecondZoneId,
            spots[0].RowCode,
            spots[0].Number);

        context.AddRange(
            IntegrationTestData.CreateFestivalDay(),
            IntegrationTestData.CreateFestivalDay(
                IntegrationTestData.SecondFestivalDayId,
                new DateOnly(2026, 8, 16)));
        context.Zones.AddRange(zones);
        context.Spots.AddRange(spots);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return spots;
    }

    private static async Task SeedAssignmentAsync(
        FestivalDbContext context,
        Festival.Domain.FestivalDays.FestivalDayId festivalDayId,
        AssignmentRequestId requestId)
    {
        var attendee = IntegrationTestData.CreateAttendee();
        var request = IntegrationTestData.CreateRequestRow(
            id: requestId,
            festivalDayId: festivalDayId,
            attendeeCodes: [AttendeeCode.Create("ATT-001")]);
        var assignment = IntegrationTestData.CreateAssignment(
            Guid.Parse("50000000-0000-0000-0000-000000000201"),
            requestId,
            festivalDayId);

        context.AddRange(attendee, request, assignment);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }
}
