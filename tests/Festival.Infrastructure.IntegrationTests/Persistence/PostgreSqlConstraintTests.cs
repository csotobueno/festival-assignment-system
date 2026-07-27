using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using Festival.Infrastructure.Persistence;
using Festival.Infrastructure.Persistence.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.IntegrationTests.Persistence;

public sealed class PostgreSqlConstraintTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    private const string UniqueViolation = "23505";
    private const string CheckViolation = "23514";

    [Fact]
    public async Task AssignmentWindowCheck_ShouldRejectInvalidPhysicalRow()
    {
        await using var context = Fixture.CreateDbContext();
        var festivalDayId =
            Guid.Parse("10000000-0000-0000-0000-000000000099");
        var date = new DateOnly(2026, 8, 20);
        var start = new TimeOnly(18, 0);
        var end = new TimeOnly(9, 0);

        var act = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "FestivalDays"
                 ("FestivalDayId", "Date",
                  "AssignmentWindowStart", "AssignmentWindowEnd")
             VALUES ({festivalDayId}, {date}, {start}, {end});
             """);

        await PostgreSqlExceptionAssertions.ShouldFailWithPostgresAsync(
            act,
            CheckViolation,
            "CK_FestivalDays_AssignmentWindow_StartBeforeEnd");
    }

    [Fact]
    public async Task Assignment_ShouldRejectDuplicateSpotWithinFestivalDay()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedAssignmentCatalogAsync(context);
        var first = IntegrationTestData.CreateAssignment(
            Guid.Parse("50000000-0000-0000-0000-000000000011"));
        var conflicting = IntegrationTestData.CreateAssignment(
            Guid.Parse("50000000-0000-0000-0000-000000000012"),
            attendeeId: IntegrationTestData.SecondAttendeeId);

        await PersistFirstThenAttemptConflictAsync(
            context,
            first,
            conflicting,
            "IX_Assignments_FestivalDayId_SpotCode");
    }

    [Fact]
    public async Task Assignment_ShouldRejectDuplicateAttendeeWithinFestivalDay()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedAssignmentCatalogAsync(
            context,
            includeSecondRequest: true);
        var first = IntegrationTestData.CreateAssignment(
            Guid.Parse("50000000-0000-0000-0000-000000000021"));
        var conflicting = IntegrationTestData.CreateAssignment(
            Guid.Parse("50000000-0000-0000-0000-000000000022"),
            requestId: IntegrationTestData.SecondRequestId,
            spotCode: "FR-A-002",
            spotNumber: 2);

        await PersistFirstThenAttemptConflictAsync(
            context,
            first,
            conflicting,
            "IX_Assignments_FestivalDayId_AttendeeId");
    }

    [Fact]
    public async Task Assignment_ShouldRejectDuplicateAttendeeWithinRequest()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedAssignmentCatalogAsync(
            context,
            includeSecondFestivalDay: true);
        var first = IntegrationTestData.CreateAssignment(
            Guid.Parse("50000000-0000-0000-0000-000000000031"));
        var conflicting = IntegrationTestData.CreateAssignment(
            Guid.Parse("50000000-0000-0000-0000-000000000032"),
            festivalDayId: IntegrationTestData.SecondFestivalDayId,
            spotCode: "FR-A-002",
            spotNumber: 2);

        await PersistFirstThenAttemptConflictAsync(
            context,
            first,
            conflicting,
            "IX_Assignments_AssignmentRequestId_AttendeeId");
    }

    [Fact]
    public async Task AssignmentRequestAttendee_ShouldRejectDuplicateCodeWithinRequest()
    {
        await using var context = Fixture.CreateDbContext();
        var festivalDay = IntegrationTestData.CreateFestivalDay();
        var requestRow = IntegrationTestData.CreateRequestRow(
            attendeeCodes: [AttendeeCode.Create("ATT-001")]);

        context.AddRange(festivalDay, requestRow);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        context.AssignmentRequestAttendees.Add(
            new AssignmentRequestAttendeeRow
            {
                AssignmentRequestId = IntegrationTestData.RequestId,
                Position = 1,
                AttendeeCode = AttendeeCode.Create("ATT-001")
            });

        var act = () => context.SaveChangesAsync();

        await PostgreSqlExceptionAssertions.ShouldFailWithPostgresAsync(
            act,
            UniqueViolation,
            "IX_AssignmentRequestAttendees_AssignmentRequestId_AttendeeCode");
    }

    [Fact]
    public async Task Spot_ShouldRejectDuplicatePhysicalPosition()
    {
        await using var context = Fixture.CreateDbContext();
        var zone = IntegrationTestData.CreateZone();
        var first = IntegrationTestData.CreateSpot();

        context.AddRange(zone, first);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var conflicting = IntegrationTestData.CreateSpot(
            code: "FRONT-ALIAS-001");
        context.Spots.Add(conflicting);

        var act = () => context.SaveChangesAsync();

        await PostgreSqlExceptionAssertions.ShouldFailWithPostgresAsync(
            act,
            UniqueViolation,
            "IX_Spots_ZoneId_RowCode_SpotNumber");
    }

    private async Task PersistFirstThenAttemptConflictAsync(
        FestivalDbContext context,
        Assignment first,
        Assignment conflicting,
        string constraintName)
    {
        context.Assignments.Add(first);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        context.Assignments.Add(conflicting);
        var act = () => context.SaveChangesAsync();

        await PostgreSqlExceptionAssertions.ShouldFailWithPostgresAsync(
            act,
            UniqueViolation,
            constraintName);

        await using var verificationContext = Fixture.CreateDbContext();
        (await verificationContext.Assignments.CountAsync())
            .Should()
            .Be(1);
    }

    private static async Task SeedAssignmentCatalogAsync(
        FestivalDbContext context,
        bool includeSecondFestivalDay = false,
        bool includeSecondRequest = false)
    {
        var festivalDay = IntegrationTestData.CreateFestivalDay();
        var firstAttendee = IntegrationTestData.CreateAttendee();
        var secondAttendee = IntegrationTestData.CreateAttendee(
            IntegrationTestData.SecondAttendeeId,
            "ATT-002",
            "Grace Hopper");
        var zone = IntegrationTestData.CreateZone();
        var firstSpot = IntegrationTestData.CreateSpot();
        var secondSpot = IntegrationTestData.CreateSpot(
            code: "FR-A-002",
            number: 2);
        var request = IntegrationTestData.CreateRequestRow();

        context.AddRange(
            festivalDay,
            firstAttendee,
            secondAttendee,
            zone,
            firstSpot,
            secondSpot,
            request);

        if (includeSecondFestivalDay)
        {
            context.FestivalDays.Add(
                IntegrationTestData.CreateFestivalDay(
                    IntegrationTestData.SecondFestivalDayId,
                    new DateOnly(2026, 8, 16)));
        }

        if (includeSecondRequest)
        {
            context.AssignmentRequests.Add(
                IntegrationTestData.CreateRequestRow(
                    id: IntegrationTestData.SecondRequestId));
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }
}
