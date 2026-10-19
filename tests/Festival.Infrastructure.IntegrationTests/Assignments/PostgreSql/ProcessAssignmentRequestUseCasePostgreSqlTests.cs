using Festival.Application.Assignments.ProcessAssignmentRequest;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.Spots;
using Festival.Domain.Zones;
using Festival.Infrastructure.Assignments.PostgreSql;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using Festival.Infrastructure.Persistence;
using Festival.Infrastructure.Zones.PostgreSql;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.IntegrationTests.Assignments.PostgreSql;

public sealed class ProcessAssignmentRequestUseCasePostgreSqlTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    [Fact]
    public async Task ExecuteAsync_ShouldPersistCompletedOutcomeThroughOneSharedContext()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedReferenceDataAsync(context, includeSecondSpot: true);
        var useCase = CreateUseCase(context);
        var command = CreateCommand();

        var result = await useCase.ExecuteAsync(command);

        result.Status.Should().Be(AssignmentRequestStatus.Completed);
        result.IsAssigned.Should().BeTrue();
        result.RequestedAt.Should().Be(IntegrationTestData.RequestedAt);
        result.ResolvedAt.Should().Be(IntegrationTestData.AssignedAt);
        result.Assignments
            .Select(assignment => assignment.AttendeeId)
            .Should()
            .Equal(
                IntegrationTestData.FirstAttendeeId,
                IntegrationTestData.SecondAttendeeId);
        result.Assignments
            .Select(assignment => new
            {
                SpotCode = assignment.SpotCode.Value,
                ZoneId = assignment.ZoneId.Value,
                RowCode = assignment.RowCode.Value,
                SpotNumber = assignment.SpotNumber.Value
            })
            .Should()
            .Equal(
                new
                {
                    SpotCode = "FR-A-001",
                    ZoneId = IntegrationTestData.ZoneId.Value,
                    RowCode = "A",
                    SpotNumber = 1
                },
                new
                {
                    SpotCode = "FR-A-002",
                    ZoneId = IntegrationTestData.ZoneId.Value,
                    RowCode = "A",
                    SpotNumber = 2
                });

        await using var verificationContext = Fixture.CreateDbContext();
        var persistedRequest = await verificationContext.AssignmentRequests
            .Include(request => request.Attendees)
            .SingleAsync(request =>
                request.AssignmentRequestId == result.AssignmentRequestId);
        persistedRequest.Status.Should().Be(AssignmentRequestStatus.Completed);
        persistedRequest.RequestedAt.Should().Be(IntegrationTestData.RequestedAt);
        persistedRequest.ResolvedAt.Should().Be(IntegrationTestData.AssignedAt);
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
                assignment.AssignmentRequestId == result.AssignmentRequestId)
            .OrderBy(assignment => assignment.SpotNumber)
            .ToArrayAsync();
        persistedAssignments.Should().HaveCount(result.Assignments.Count);
        persistedAssignments.Should().OnlyContain(assignment =>
            assignment.AssignmentRequestId == result.AssignmentRequestId);
        persistedAssignments
            .Select(assignment => new
            {
                SpotCode = assignment.SpotCode.Value,
                ZoneId = assignment.ZoneId.Value,
                RowCode = assignment.RowCode.Value,
                SpotNumber = assignment.SpotNumber.Value
            })
            .Should()
            .Equal(
                new
                {
                    SpotCode = "FR-A-001",
                    ZoneId = IntegrationTestData.ZoneId.Value,
                    RowCode = "A",
                    SpotNumber = 1
                },
                new
                {
                    SpotCode = "FR-A-002",
                    ZoneId = IntegrationTestData.ZoneId.Value,
                    RowCode = "A",
                    SpotNumber = 2
                });
        (await verificationContext.Assignments.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPersistRejectedOutcomeWithoutAssignments()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedReferenceDataAsync(context, includeSecondSpot: false);
        var useCase = CreateUseCase(context);
        var command = CreateCommand();

        var result = await useCase.ExecuteAsync(command);

        result.Status.Should().Be(AssignmentRequestStatus.Rejected);
        result.IsRejected.Should().BeTrue();
        result.RejectionCode.Should().Be(
            ProcessAssignmentRequestUseCase.NoContiguousSpotsAvailableCode);
        result.RejectionMessage.Should().Be(
            "No contiguous spots are available for the requested assignment group.");
        result.Assignments.Should().BeEmpty();

        await using var verificationContext = Fixture.CreateDbContext();
        var persistedRequest = await verificationContext.AssignmentRequests
            .Include(request => request.Attendees)
            .SingleAsync(request =>
                request.AssignmentRequestId == result.AssignmentRequestId);
        persistedRequest.Status.Should().Be(AssignmentRequestStatus.Rejected);
        persistedRequest.ResolvedAt.Should().Be(IntegrationTestData.AssignedAt);
        persistedRequest.RejectionCode.Should().Be(result.RejectionCode);
        persistedRequest.RejectionMessage.Should().Be(result.RejectionMessage);
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
        (await verificationContext.Assignments.AnyAsync(assignment =>
            assignment.AssignmentRequestId == result.AssignmentRequestId))
            .Should()
            .BeFalse();
        (await verificationContext.Assignments.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("ATT-001", "ATT-999")]
    [InlineData("ATT-998", "ATT-999")]
    public async Task ExecuteAsync_ShouldPersistWholeRequestAsRejected_WhenAttendeeResolutionIsIncomplete(
        string firstCode,
        string secondCode)
    {
        await using var context = Fixture.CreateDbContext();
        await SeedReferenceDataAsync(context, includeSecondSpot: true);
        var useCase = CreateUseCase(context);
        var command = new ProcessAssignmentRequestCommand(
            IntegrationTestData.FestivalDayId,
            [AttendeeCode.Create(firstCode), AttendeeCode.Create(secondCode)],
            IntegrationTestData.RequestedAt,
            IntegrationTestData.AssignedAt,
            new RequestEligibility(true));

        var result = await useCase.ExecuteAsync(command);

        result.IsRejected.Should().BeTrue();
        result.RejectionCode.Should().Be("ATTENDEE_NOT_FOUND");
        result.RejectionMessage.Should().Be(
            "One or more requested attendees could not be resolved.");
        result.Assignments.Should().BeEmpty();

        await using var verificationContext = Fixture.CreateDbContext();
        var persistedRequest = await verificationContext.AssignmentRequests
            .Include(request => request.Attendees)
            .SingleAsync(request =>
                request.AssignmentRequestId == result.AssignmentRequestId);
        persistedRequest.Status.Should().Be(AssignmentRequestStatus.Rejected);
        persistedRequest.ResolvedAt.Should().Be(IntegrationTestData.AssignedAt);
        persistedRequest.RejectionCode.Should().Be(result.RejectionCode);
        persistedRequest.RejectionMessage.Should().Be(result.RejectionMessage);
        persistedRequest.Attendees
            .OrderBy(attendee => attendee.Position)
            .Select(attendee => attendee.AttendeeCode)
            .Should().Equal(command.AttendeeCodes);
        (await verificationContext.Assignments.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(false, ExperienceQuality.Medium)]
    [InlineData(true, ExperienceQuality.Good)]
    public async Task ExecuteAsync_ShouldSelectAndPersistQualityFromRecordedHistoryOrNeutralBootstrap(
        bool hasUnfavorableHistory, ExperienceQuality expectedQuality)
    {
        await using var context = Fixture.CreateDbContext();
        await SeedReferenceDataAsync(context, includeSecondSpot: true);
        var goodZone = Zone.Create(ZoneId.Create(
            Guid.Parse("20000000-0000-0000-0000-000000000002")), "Good", ZoneType.MiddleCenter);
        context.Zones.Add(goodZone);
        context.Spots.AddRange(Enumerable.Range(1, 2).Select(i => Spot.Create(
            SpotCode.Create($"GOOD-A-{i:000}"), goodZone.Id, RowCode.Create("A"), SpotNumber.Create(i))));
        if (hasUnfavorableHistory)
        {
            var priorDay = IntegrationTestData.CreateFestivalDay(
                IntegrationTestData.SecondFestivalDayId, new DateOnly(2026, 8, 14));
            var requestId = IntegrationTestData.SecondRequestId;
            var priorRequest = IntegrationTestData.CreateRequestRow(AssignmentRequestStatus.Completed,
                requestId, priorDay.Id, [AttendeeCode.Create("ATT-001"), AttendeeCode.Create("ATT-002")]);
            context.AddRange(priorDay, priorRequest);
            var attendeeIds = new[] { IntegrationTestData.FirstAttendeeId, IntegrationTestData.SecondAttendeeId };
            for (var i = 0; i < 2; i++)
            {
                var spot = IntegrationTestData.CreateSpot($"FR-A-{i + 1:000}", "A", i + 1);
                context.Assignments.Add(Assignment.Create(
                    AssignmentId.Create(Guid.Parse($"50000000-0000-0000-0000-{i + 1:000000000000}")),
                    requestId, priorDay.Id, attendeeIds[i], spot.Code, spot.ZoneId, spot.RowCode,
                    spot.Number, ExperienceQuality.Bad, IntegrationTestData.AssignedAt.AddDays(-1)));
            }
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await CreateUseCase(context).ExecuteAsync(CreateCommand());

        result.IsAssigned.Should().BeTrue();
        result.Assignments.Should().HaveCount(2);
        result.Assignments.Should().OnlyContain(assignment =>
            assignment.ZoneId == (hasUnfavorableHistory ? goodZone.Id : IntegrationTestData.ZoneId));
        await using var verificationContext = Fixture.CreateDbContext();
        var assignments = await verificationContext.Assignments.AsNoTracking()
            .Where(assignment => assignment.AssignmentRequestId == result.AssignmentRequestId).ToArrayAsync();
        assignments.Should().HaveCount(2);
        assignments.Should().OnlyContain(assignment => assignment.ExperienceQuality == expectedQuality);
        var histories = await new PostgreSqlFairnessHistoryProvider(verificationContext).GetHistoriesAsync(
            [IntegrationTestData.FirstAttendeeId, IntegrationTestData.SecondAttendeeId]);
        var expectedHistory = hasUnfavorableHistory
            ? new[] { ExperienceQuality.Bad, ExperienceQuality.Good }
            : new[] { ExperienceQuality.Medium };
        histories.Values.Should().OnlyContain(history => history.Experiences.SequenceEqual(expectedHistory));
    }

    private static ProcessAssignmentRequestUseCase CreateUseCase(
        FestivalDbContext context)
    {
        return new ProcessAssignmentRequestUseCase(
            new PostgreSqlAttendeeCodeResolver(context),
            new PostgreSqlAvailableSpotProvider(context),
            new PostgreSqlZoneCatalogProvider(context),
            new PostgreSqlFairnessHistoryProvider(context),
            new PostgreSqlAssignmentRequestRepository(context),
            new PostgreSqlAssignmentRepository(context),
            new EfCoreUnitOfWork(context),
            new AssignmentEngine());
    }

    private static ProcessAssignmentRequestCommand CreateCommand()
    {
        return new ProcessAssignmentRequestCommand(
            IntegrationTestData.FestivalDayId,
            [
                AttendeeCode.Create("ATT-001"),
                AttendeeCode.Create("ATT-002")
            ],
            IntegrationTestData.RequestedAt,
            IntegrationTestData.AssignedAt,
            new RequestEligibility(true));
    }

    private static async Task SeedReferenceDataAsync(
        FestivalDbContext context,
        bool includeSecondSpot)
    {
        context.AddRange(
            IntegrationTestData.CreateFestivalDay(),
            IntegrationTestData.CreateZone(),
            IntegrationTestData.CreateSpot(),
            IntegrationTestData.CreateAttendee(),
            IntegrationTestData.CreateAttendee(
                IntegrationTestData.SecondAttendeeId,
                "ATT-002",
                "Grace Hopper"));

        if (includeSecondSpot)
        {
            context.Add(
                IntegrationTestData.CreateSpot(
                    "FR-A-002",
                    "A",
                    2));
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }
}
