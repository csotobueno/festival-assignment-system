using Festival.Application.Assignments.Ports;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Infrastructure.Assignments.PostgreSql;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using Festival.Infrastructure.Persistence;
using Festival.Infrastructure.Persistence.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.IntegrationTests.Persistence;

public sealed class EfCoreUnitOfWorkTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    [Fact]
    public async Task SaveChangesAsync_ShouldConfirmRequestGraphAndAssignmentsStagedInSharedContext()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedReferenceDataAsync(context);

        var assignmentRequestRepository =
            new PostgreSqlAssignmentRequestRepository(context);
        var assignmentRepository =
            new PostgreSqlAssignmentRepository(context);
        IUnitOfWork unitOfWork = new EfCoreUnitOfWork(context);
        var attendeeCodes = new[]
        {
            AttendeeCode.Create("ATT-001"),
            AttendeeCode.Create("ATT-002")
        };
        var request = IntegrationTestData.CreateRequest(
            AssignmentRequestStatus.Completed,
            attendeeCodes: attendeeCodes);
        var assignments = new[]
        {
            IntegrationTestData.CreateAssignment(
                Guid.Parse("50000000-0000-0000-0000-000000000401")),
            IntegrationTestData.CreateAssignment(
                Guid.Parse("50000000-0000-0000-0000-000000000402"),
                attendeeId: IntegrationTestData.SecondAttendeeId,
                spotCode: "FR-A-002",
                rowCode: "A",
                spotNumber: 2)
        };

        await assignmentRequestRepository.AddAsync(request);
        await assignmentRepository.AddAsync(assignments);

        context.ChangeTracker.Entries<AssignmentRequestRow>()
            .Should()
            .ContainSingle()
            .Which.State.Should().Be(EntityState.Added);
        context.ChangeTracker.Entries<AssignmentRequestAttendeeRow>()
            .Should()
            .HaveCount(2)
            .And.OnlyContain(entry => entry.State == EntityState.Added);
        context.ChangeTracker.Entries<Assignment>()
            .Should()
            .HaveCount(2)
            .And.OnlyContain(entry => entry.State == EntityState.Added);
        context.ChangeTracker.Entries()
            .Should()
            .HaveCount(5)
            .And.OnlyContain(entry => entry.State == EntityState.Added);

        await AssertRequestOutcomeIsNotDurableAsync();

        var saveResult = await unitOfWork.SaveChangesAsync();

        saveResult.Should().BeGreaterThan(0);

        await using (var verificationContext = Fixture.CreateDbContext())
        {
            var persistedRequest = await verificationContext.AssignmentRequests
                .Include(row => row.Attendees)
                .SingleAsync(
                    row => row.AssignmentRequestId ==
                        IntegrationTestData.RequestId);
            persistedRequest.Status.Should().Be(
                AssignmentRequestStatus.Completed);
            persistedRequest.Attendees
                .OrderBy(attendee => attendee.Position)
                .Select(attendee => new
                {
                    attendee.Position,
                    Code = attendee.AttendeeCode.Value
                })
                .Should()
                .Equal(
                    new { Position = 0, Code = "ATT-001" },
                    new { Position = 1, Code = "ATT-002" });

            var persistedAssignments = await verificationContext.Assignments
                .Where(assignment =>
                    assignment.AssignmentRequestId ==
                    IntegrationTestData.RequestId)
                .OrderBy(assignment => assignment.SpotNumber)
                .ToArrayAsync();
            persistedAssignments.Should().BeEquivalentTo(
                assignments,
                options => options.WithStrictOrdering());
        }

        context.ChangeTracker.Entries<AssignmentRequestRow>()
            .Should()
            .OnlyContain(entry => entry.State == EntityState.Unchanged);
        context.ChangeTracker.Entries<AssignmentRequestAttendeeRow>()
            .Should()
            .OnlyContain(entry => entry.State == EntityState.Unchanged);
        context.ChangeTracker.Entries<Assignment>()
            .Should()
            .OnlyContain(entry => entry.State == EntityState.Unchanged);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnZeroForEmptyChangeTrackerWithoutCreatingRows()
    {
        await using var context = Fixture.CreateDbContext();
        IUnitOfWork unitOfWork = new EfCoreUnitOfWork(context);

        var saveResult = await unitOfWork.SaveChangesAsync();

        saveResult.Should().Be(0);
        context.ChangeTracker.Entries().Should().BeEmpty();

        await using var verificationContext = Fixture.CreateDbContext();
        (await verificationContext.Attendees.CountAsync()).Should().Be(0);
        (await verificationContext.FestivalDays.CountAsync()).Should().Be(0);
        (await verificationContext.Zones.CountAsync()).Should().Be(0);
        (await verificationContext.Spots.CountAsync()).Should().Be(0);
        (await verificationContext.AssignmentRequests.CountAsync())
            .Should()
            .Be(0);
        (await verificationContext.AssignmentRequestAttendees.CountAsync())
            .Should()
            .Be(0);
        (await verificationContext.Assignments.CountAsync()).Should().Be(0);
    }

    private async Task AssertRequestOutcomeIsNotDurableAsync()
    {
        await using var beforeSaveContext = Fixture.CreateDbContext();

        (await beforeSaveContext.AssignmentRequests.AnyAsync(
            row => row.AssignmentRequestId == IntegrationTestData.RequestId))
            .Should()
            .BeFalse();
        (await beforeSaveContext.AssignmentRequestAttendees.AnyAsync(
            row => row.AssignmentRequestId == IntegrationTestData.RequestId))
            .Should()
            .BeFalse();
        (await beforeSaveContext.Assignments.AnyAsync(
            assignment => assignment.AssignmentRequestId ==
                IntegrationTestData.RequestId))
            .Should()
            .BeFalse();
    }

    private static async Task SeedReferenceDataAsync(
        FestivalDbContext context)
    {
        context.AddRange(
            IntegrationTestData.CreateFestivalDay(),
            IntegrationTestData.CreateZone(),
            IntegrationTestData.CreateSpot(),
            IntegrationTestData.CreateSpot(
                "FR-A-002",
                "A",
                2),
            IntegrationTestData.CreateAttendee(),
            IntegrationTestData.CreateAttendee(
                IntegrationTestData.SecondAttendeeId,
                "ATT-002",
                "Grace Hopper"));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }
}
