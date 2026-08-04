using Festival.Application.Assignments.Persistence;
using Festival.Application.Assignments.Ports;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Infrastructure.Assignments.PostgreSql;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using Festival.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Festival.Infrastructure.IntegrationTests.Persistence;

public sealed class AssignmentConflictRollbackTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    private const string UniqueViolation = "23505";

    [Fact]
    public async Task SaveChangesAsync_ShouldTranslateSpotConflictAndRollBackCompleteNewRequestGraph()
    {
        await using (var setupContext = Fixture.CreateDbContext())
        {
            await SeedMasterDataAsync(setupContext);
            await PersistSetupAssignmentAsync(setupContext);
        }

        await using (var failedContext = Fixture.CreateDbContext())
        {
            var request = CreateCompletedRequest(
                IntegrationTestData.SecondRequestId,
                "ATT-002");
            var conflictingAssignment = IntegrationTestData.CreateAssignment(
                Guid.Parse("50000000-0000-0000-0000-000000000502"),
                requestId: IntegrationTestData.SecondRequestId,
                attendeeId: IntegrationTestData.SecondAttendeeId);

            var exception = await StageAndSaveConflictAsync(
                failedContext,
                request,
                [conflictingAssignment]);

            AssertTranslatedConflict(
                exception,
                AssignmentPersistenceConflict.SpotAlreadyAssigned,
                PostgreSqlAssignmentConflictTranslator.SpotAlreadyAssignedConstraint);
        }

        await AssertOnlySetupAssignmentIsDurableAsync(
            IntegrationTestData.SecondRequestId);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldTranslateAttendeeConflictAndRollBackCompleteNewRequestGraph()
    {
        await using (var setupContext = Fixture.CreateDbContext())
        {
            await SeedMasterDataAsync(setupContext);
            await PersistSetupAssignmentAsync(setupContext);
        }

        await using (var failedContext = Fixture.CreateDbContext())
        {
            var request = CreateCompletedRequest(
                IntegrationTestData.SecondRequestId,
                "ATT-001");
            var conflictingAssignment = IntegrationTestData.CreateAssignment(
                Guid.Parse("50000000-0000-0000-0000-000000000503"),
                requestId: IntegrationTestData.SecondRequestId,
                spotCode: "FR-A-002",
                spotNumber: 2);

            var exception = await StageAndSaveConflictAsync(
                failedContext,
                request,
                [conflictingAssignment]);

            AssertTranslatedConflict(
                exception,
                AssignmentPersistenceConflict.AttendeeAlreadyAssigned,
                PostgreSqlAssignmentConflictTranslator.AttendeeAlreadyAssignedConstraint);
        }

        await AssertOnlySetupAssignmentIsDurableAsync(
            IntegrationTestData.SecondRequestId);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldTranslateDuplicateRequestAssignmentAndRollBackCompleteNewRequestGraph()
    {
        await using (var setupContext = Fixture.CreateDbContext())
        {
            await SeedMasterDataAsync(
                setupContext,
                includeSecondFestivalDay: true);
        }

        await using (var failedContext = Fixture.CreateDbContext())
        {
            var request = CreateCompletedRequest(
                IntegrationTestData.RequestId,
                "ATT-001");
            var assignments = new[]
            {
                IntegrationTestData.CreateAssignment(
                    Guid.Parse("50000000-0000-0000-0000-000000000504")),
                IntegrationTestData.CreateAssignment(
                    Guid.Parse("50000000-0000-0000-0000-000000000505"),
                    festivalDayId: IntegrationTestData.SecondFestivalDayId,
                    spotCode: "FR-A-002",
                    spotNumber: 2)
            };

            var exception = await StageAndSaveConflictAsync(
                failedContext,
                request,
                assignments);

            AssertTranslatedConflict(
                exception,
                AssignmentPersistenceConflict.DuplicateRequestAssignment,
                PostgreSqlAssignmentConflictTranslator.DuplicateRequestAssignmentConstraint);
        }

        await using var verificationContext = Fixture.CreateDbContext();
        await AssertNewRequestGraphIsAbsentAsync(
            verificationContext,
            IntegrationTestData.RequestId);
        (await verificationContext.Assignments.CountAsync()).Should().Be(0);
    }

    private static AssignmentRequest CreateCompletedRequest(
        AssignmentRequestId requestId,
        string attendeeCode)
    {
        return IntegrationTestData.CreateRequest(
            AssignmentRequestStatus.Completed,
            id: requestId,
            attendeeCodes: [AttendeeCode.Create(attendeeCode)]);
    }

    private static async Task<AssignmentPersistenceConflictException>
        StageAndSaveConflictAsync(
            FestivalDbContext context,
            AssignmentRequest request,
            IReadOnlyCollection<Assignment> assignments)
    {
        var requestRepository =
            new PostgreSqlAssignmentRequestRepository(context);
        var assignmentRepository =
            new PostgreSqlAssignmentRepository(context);
        IUnitOfWork unitOfWork = new EfCoreUnitOfWork(context);

        await requestRepository.AddAsync(request);
        await assignmentRepository.AddAsync(assignments);

        context.ChangeTracker.Entries().Should().HaveCount(
            assignments.Count + 2);
        context.ChangeTracker.Entries().Should().OnlyContain(
            entry => entry.State == EntityState.Added);

        var act = () => unitOfWork.SaveChangesAsync();

        return (await act.Should()
                .ThrowAsync<AssignmentPersistenceConflictException>())
            .Which;
    }

    private static void AssertTranslatedConflict(
        AssignmentPersistenceConflictException exception,
        AssignmentPersistenceConflict expectedConflict,
        string expectedConstraintName)
    {
        exception.Conflict.Should().Be(expectedConflict);
        exception.InnerException.Should().BeOfType<DbUpdateException>();

        var postgresException = FindPostgresException(exception);
        postgresException.Should().NotBeNull();
        postgresException!.SqlState.Should().Be(UniqueViolation);
        postgresException.ConstraintName.Should().Be(expectedConstraintName);
    }

    private static PostgresException? FindPostgresException(
        Exception exception)
    {
        for (Exception? current = exception;
             current is not null;
             current = current.InnerException)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }
        }

        return null;
    }

    private async Task AssertOnlySetupAssignmentIsDurableAsync(
        AssignmentRequestId failedRequestId)
    {
        await using var verificationContext = Fixture.CreateDbContext();

        await AssertNewRequestGraphIsAbsentAsync(
            verificationContext,
            failedRequestId);

        var persistedAssignment = await verificationContext.Assignments
            .SingleAsync();
        persistedAssignment.AssignmentRequestId.Should().Be(
            IntegrationTestData.RequestId);
        persistedAssignment.SpotCode.Value.Should().Be("FR-A-001");
        persistedAssignment.AttendeeId.Should().Be(
            IntegrationTestData.FirstAttendeeId);
    }

    private static async Task AssertNewRequestGraphIsAbsentAsync(
        FestivalDbContext verificationContext,
        AssignmentRequestId failedRequestId)
    {
        (await verificationContext.AssignmentRequests.CountAsync(row =>
            row.AssignmentRequestId == failedRequestId)).Should().Be(0);
        (await verificationContext.AssignmentRequestAttendees.CountAsync(row =>
            row.AssignmentRequestId == failedRequestId)).Should().Be(0);
        (await verificationContext.Assignments.CountAsync(assignment =>
            assignment.AssignmentRequestId == failedRequestId)).Should().Be(0);
    }

    private static async Task PersistSetupAssignmentAsync(
        FestivalDbContext context)
    {
        var requestRepository =
            new PostgreSqlAssignmentRequestRepository(context);
        var assignmentRepository =
            new PostgreSqlAssignmentRepository(context);
        IUnitOfWork unitOfWork = new EfCoreUnitOfWork(context);

        await requestRepository.AddAsync(
            CreateCompletedRequest(
                IntegrationTestData.RequestId,
                "ATT-001"));
        await assignmentRepository.AddAsync(
            [
                IntegrationTestData.CreateAssignment(
                    Guid.Parse("50000000-0000-0000-0000-000000000501"))
            ]);
        await unitOfWork.SaveChangesAsync();
    }

    private static async Task SeedMasterDataAsync(
        FestivalDbContext context,
        bool includeSecondFestivalDay = false)
    {
        context.AddRange(
            IntegrationTestData.CreateFestivalDay(),
            IntegrationTestData.CreateZone(),
            IntegrationTestData.CreateSpot(),
            IntegrationTestData.CreateSpot("FR-A-002", "A", 2),
            IntegrationTestData.CreateAttendee(),
            IntegrationTestData.CreateAttendee(
                IntegrationTestData.SecondAttendeeId,
                "ATT-002",
                "Grace Hopper"));

        if (includeSecondFestivalDay)
        {
            context.Add(
                IntegrationTestData.CreateFestivalDay(
                    IntegrationTestData.SecondFestivalDayId,
                    new DateOnly(2026, 8, 16)));
        }

        await context.SaveChangesAsync();
    }
}
