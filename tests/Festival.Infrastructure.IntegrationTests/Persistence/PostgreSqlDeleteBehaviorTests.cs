using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using Festival.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.IntegrationTests.Persistence;

public sealed class PostgreSqlDeleteBehaviorTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    private const string ForeignKeyViolation = "23503";

    [Fact]
    public async Task DeletingAssignmentRequest_ShouldCascadeToOwnedAttendeeRows()
    {
        await using var context = Fixture.CreateDbContext();
        var festivalDay = IntegrationTestData.CreateFestivalDay();
        var request = IntegrationTestData.CreateRequestRow();

        context.AddRange(festivalDay, request);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persistedRequest =
            await context.AssignmentRequests.SingleAsync();
        context.AssignmentRequests.Remove(persistedRequest);
        await context.SaveChangesAsync();

        (await context.AssignmentRequests.CountAsync()).Should().Be(0);
        (await context.AssignmentRequestAttendees.CountAsync())
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task DeletingFestivalDayReferencedByRequest_ShouldBeRejected()
    {
        await using var context = Fixture.CreateDbContext();
        var festivalDay = IntegrationTestData.CreateFestivalDay();
        var request = IntegrationTestData.CreateRequestRow();

        context.AddRange(festivalDay, request);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persistedFestivalDay =
            await context.FestivalDays.SingleAsync();
        context.FestivalDays.Remove(persistedFestivalDay);

        await AssertRestrictiveDeleteAsync(
            context,
            "FK_AssignmentRequests_FestivalDays_FestivalDayId");
    }

    [Fact]
    public async Task DeletingFestivalDayReferencedByAssignment_ShouldBeRejected()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedAssignmentAsync(
            context,
            IntegrationTestData.SecondFestivalDayId);

        var festivalDay = await context.FestivalDays.SingleAsync(
            candidate =>
                candidate.Id == IntegrationTestData.FestivalDayId);
        context.FestivalDays.Remove(festivalDay);

        await AssertRestrictiveDeleteAsync(
            context,
            "FK_Assignments_FestivalDays_FestivalDayId");
    }

    [Fact]
    public async Task DeletingAttendeeReferencedByAssignment_ShouldBeRejected()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedAssignmentAsync(context);

        var attendee = await context.Attendees.SingleAsync();
        context.Attendees.Remove(attendee);

        await AssertRestrictiveDeleteAsync(
            context,
            "FK_Assignments_Attendees_AttendeeId");
    }

    [Fact]
    public async Task DeletingSpotReferencedByAssignment_ShouldBeRejected()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedAssignmentAsync(context);

        var spot = await context.Spots.SingleAsync();
        context.Spots.Remove(spot);

        await AssertRestrictiveDeleteAsync(
            context,
            "FK_Assignments_Spots_SpotCode");
    }

    [Fact]
    public async Task DeletingZoneReferencedBySpot_ShouldBeRejected()
    {
        await using var context = Fixture.CreateDbContext();
        var zone = IntegrationTestData.CreateZone();
        var spot = IntegrationTestData.CreateSpot();

        context.AddRange(zone, spot);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persistedZone = await context.Zones.SingleAsync();
        context.Zones.Remove(persistedZone);

        await AssertRestrictiveDeleteAsync(
            context,
            "FK_Spots_Zones_ZoneId");
    }

    private static async Task AssertRestrictiveDeleteAsync(
        FestivalDbContext context,
        string constraintName)
    {
        var act = () => context.SaveChangesAsync();

        await PostgreSqlExceptionAssertions.ShouldFailWithPostgresAsync(
            act,
            ForeignKeyViolation,
            constraintName);
    }

    private static async Task SeedAssignmentAsync(
        FestivalDbContext context,
        FestivalDayId? requestFestivalDayId = null)
    {
        var festivalDay = IntegrationTestData.CreateFestivalDay();
        var attendee = IntegrationTestData.CreateAttendee();
        var zone = IntegrationTestData.CreateZone();
        var spot = IntegrationTestData.CreateSpot();
        var request = IntegrationTestData.CreateRequestRow(
            festivalDayId: requestFestivalDayId,
            attendeeCodes: [AttendeeCode.Create("ATT-001")]);
        var assignment = IntegrationTestData.CreateAssignment(
            Guid.Parse("50000000-0000-0000-0000-000000000101"));

        context.AddRange(
            festivalDay,
            attendee,
            zone,
            spot,
            request,
            assignment);

        if (requestFestivalDayId == IntegrationTestData.SecondFestivalDayId)
        {
            context.FestivalDays.Add(
                IntegrationTestData.CreateFestivalDay(
                    IntegrationTestData.SecondFestivalDayId,
                    new DateOnly(2026, 8, 16)));
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }
}
