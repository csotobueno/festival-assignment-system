using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Infrastructure.Assignments.PostgreSql;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using Festival.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.IntegrationTests.Assignments.PostgreSql;

public sealed class PostgreSqlAssignmentRepositoryTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    [Fact]
    public async Task AddAsync_ShouldStageOneAssignmentWithoutMakingItDurable()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedRequiredRowsAsync(context, includeSecondAssignment: false);
        var repository = new PostgreSqlAssignmentRepository(context);
        var assignment = IntegrationTestData.CreateAssignment(
            Guid.Parse("50000000-0000-0000-0000-000000000301"));

        await repository.AddAsync([assignment]);

        context.ChangeTracker.Entries<Assignment>()
            .Should()
            .ContainSingle()
            .Which.State.Should().Be(EntityState.Added);

        await using (var beforeCommit = Fixture.CreateDbContext())
        {
            (await beforeCommit.Assignments.CountAsync()).Should().Be(0);
        }

        await context.SaveChangesAsync();

        await using var afterCommit = Fixture.CreateDbContext();
        var persisted = await afterCommit.Assignments.SingleAsync();
        persisted.Should().BeEquivalentTo(assignment);
    }

    [Fact]
    public async Task AddAsync_ShouldStageMultipleAssignmentsAndPreserveSpotSnapshots()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedRequiredRowsAsync(context, includeSecondAssignment: true);
        var repository = new PostgreSqlAssignmentRepository(context);
        var assignments = new[]
        {
            IntegrationTestData.CreateAssignment(
                Guid.Parse("50000000-0000-0000-0000-000000000311")),
            IntegrationTestData.CreateAssignment(
                Guid.Parse("50000000-0000-0000-0000-000000000312"),
                attendeeId: IntegrationTestData.SecondAttendeeId,
                spotCode: "FR-A-002",
                rowCode: "A",
                spotNumber: 2)
        };

        await repository.AddAsync(assignments);

        context.ChangeTracker.Entries<Assignment>()
            .Should()
            .HaveCount(2)
            .And.OnlyContain(entry => entry.State == EntityState.Added);

        await context.SaveChangesAsync();

        await using var verificationContext = Fixture.CreateDbContext();
        var persisted = await verificationContext.Assignments
            .OrderBy(assignment => assignment.SpotNumber)
            .ToArrayAsync();
        persisted.Should().BeEquivalentTo(
            assignments,
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task AddAsync_ShouldStageNoChangesForEmptyInput()
    {
        await using var context = Fixture.CreateDbContext();
        var repository = new PostgreSqlAssignmentRepository(context);

        await repository.AddAsync([]);

        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    private static async Task SeedRequiredRowsAsync(
        FestivalDbContext context,
        bool includeSecondAssignment)
    {
        context.AddRange(
            IntegrationTestData.CreateFestivalDay(),
            IntegrationTestData.CreateZone(),
            IntegrationTestData.CreateSpot(),
            IntegrationTestData.CreateRequestRow(
                attendeeCodes: includeSecondAssignment
                    ?
                    [
                        AttendeeCode.Create("ATT-001"),
                        AttendeeCode.Create("ATT-002")
                    ]
                    : [AttendeeCode.Create("ATT-001")]),
            IntegrationTestData.CreateAttendee());

        if (includeSecondAssignment)
        {
            context.AddRange(
                IntegrationTestData.CreateAttendee(
                    IntegrationTestData.SecondAttendeeId,
                    "ATT-002",
                    "Grace Hopper"),
                IntegrationTestData.CreateSpot(
                    "FR-A-002",
                    "A",
                    2));
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }
}
