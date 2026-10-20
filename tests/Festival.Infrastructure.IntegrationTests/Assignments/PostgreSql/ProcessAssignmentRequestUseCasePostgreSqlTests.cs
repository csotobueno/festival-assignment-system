using Festival.Application.Assignments.ProcessAssignmentRequest;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.Spots;
using Festival.Domain.Zones;
using Festival.Infrastructure.Assignments.PostgreSql;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using Festival.Infrastructure.Persistence;
using Festival.Infrastructure.Persistence.Mappers;
using Festival.Infrastructure.Zones.PostgreSql;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.IntegrationTests.Assignments.PostgreSql;

public sealed class ProcessAssignmentRequestUseCasePostgreSqlTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    private static readonly AttendeeId ThirdAttendeeId = AttendeeId.Create(
        Guid.Parse("30000000-0000-0000-0000-000000000003"));

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

    [Fact]
    [Trait("Category", "Stage4Acceptance")]
    public async Task ExecuteAsync_ShouldAssignNeutralGroupWithoutHistory()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedReferenceDataAsync(context, includeSecondSpot: true);
        AddZoneWithSpots(context, ZoneType.MiddleCenter, "GOOD", 1, 2);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var command = CreateCommand();

        var result = await CreateUseCase(context).ExecuteAsync(command);

        // Equal Good/Medium capacity lets the neutral target select Medium.
        await AssertCompletedOutcomeAsync(result, command, ExperienceQuality.Medium,
            [IntegrationTestData.FirstAttendeeId, IntegrationTestData.SecondAttendeeId],
            [IntegrationTestData.CreateSpot(), IntegrationTestData.CreateSpot("FR-A-002", "A", 2)]);
    }

    [Fact]
    [Trait("Category", "Stage4Acceptance")]
    public async Task ExecuteAsync_ShouldRecoverPersistedBadHistoryWithGoodPlacement()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedReferenceDataAsync(context, includeSecondSpot: true);
        var goodBlock = AddZoneWithSpots(context, ZoneType.MiddleCenter, "GOOD", 1, 2);
        AddUnfavorableHistory(context);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var command = CreateCommand();

        var result = await CreateUseCase(context).ExecuteAsync(command);

        // Same current Good/Medium capacity as bootstrap; persisted Bad changes the outcome.
        await AssertCompletedOutcomeAsync(result, command, ExperienceQuality.Good,
            [IntegrationTestData.FirstAttendeeId, IntegrationTestData.SecondAttendeeId],
            goodBlock, ExperienceQuality.Bad);
    }

    [Fact]
    [Trait("Category", "Stage4Acceptance")]
    public async Task ExecuteAsync_ShouldExcludeFrontStandingAndPersistEligibleZoneQuality()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedReferenceDataAsync(context, includeSecondSpot: true);
        var frontSpots = AddZoneWithSpots(context, ZoneType.FrontStanding, "FRONT", 1, 2, 3);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var command = CreateCommand(allowsFrontStanding: false);

        var result = await CreateUseCase(context).ExecuteAsync(command);

        result.Assignments.Should().NotContain(assignment => assignment.ZoneId == frontSpots[0].ZoneId);
        await AssertCompletedOutcomeAsync(result, command, ExperienceQuality.Medium,
            [IntegrationTestData.FirstAttendeeId, IntegrationTestData.SecondAttendeeId],
            [IntegrationTestData.CreateSpot(), IntegrationTestData.CreateSpot("FR-A-002", "A", 2)]);
    }

    [Fact]
    [Trait("Category", "Stage4Acceptance")]
    public async Task ExecuteAsync_ShouldRecordGoodForNeutralMediumTargetWhenGoodInventoryIsAbundant()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedReferenceDataAsync(context, includeSecondSpot: true);
        var goodSpots = AddZoneWithSpots(context, ZoneType.MiddleCenter, "GOOD", 1, 2, 3);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var command = CreateCommand([AttendeeCode.Create("ATT-001")]);

        var result = await CreateUseCase(context).ExecuteAsync(command);

        // Empty history targets Medium; three Good versus two Medium Spots puts Good first.
        await AssertCompletedOutcomeAsync(result, command, ExperienceQuality.Good,
            [IntegrationTestData.FirstAttendeeId], [goodSpots[0]]);
    }

    [Fact]
    [Trait("Category", "Stage4Acceptance")]
    public async Task ExecuteAsync_ShouldDegradeGoodTargetToCompleteMediumBlockWhenGoodIsFragmented()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedReferenceDataAsync(context, includeSecondSpot: true);
        AddZoneWithSpots(context, ZoneType.MiddleCenter, "GOOD", 1, 3);
        // Both members have Bad history and therefore target Good. The historical
        // UpperLeft Zone also offers a feasible Bad block today, after Medium.
        AddUnfavorableHistory(context);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var command = CreateCommand();

        var result = await CreateUseCase(context).ExecuteAsync(command);

        await AssertCompletedOutcomeAsync(result, command, ExperienceQuality.Medium,
            [IntegrationTestData.FirstAttendeeId, IntegrationTestData.SecondAttendeeId],
            [IntegrationTestData.CreateSpot(), IntegrationTestData.CreateSpot("FR-A-002", "A", 2)],
            ExperienceQuality.Bad);
    }

    [Fact]
    [Trait("Category", "Stage4Acceptance")]
    public async Task ExecuteAsync_ShouldRejectWholeGroupWhenRawCapacityHasNoContiguousBlock()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedReferenceDataAsync(context, includeSecondSpot: false);
        context.Attendees.Add(IntegrationTestData.CreateAttendee(ThirdAttendeeId, "ATT-003", "Katherine Johnson"));
        context.Spots.AddRange(
            IntegrationTestData.CreateSpot("FR-A-003", "A", 3),
            IntegrationTestData.CreateSpot("FR-B-001", "B", 1),
            IntegrationTestData.CreateSpot("FR-B-003", "B", 3));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var command = CreateCommand([
            AttendeeCode.Create("ATT-001"), AttendeeCode.Create("ATT-002"), AttendeeCode.Create("ATT-003")]);

        // Four available Spots for three attendees, but Rows A and B each contain only 1,3.
        var available = await new PostgreSqlAvailableSpotProvider(context).GetAvailableSpotsAsync(command.FestivalDayId);
        available.Should().HaveCount(4);
        var result = await CreateUseCase(context).ExecuteAsync(command);

        result.Status.Should().Be(AssignmentRequestStatus.Rejected);
        result.IsRejected.Should().BeTrue();
        result.RejectionCode.Should().Be(ProcessAssignmentRequestUseCase.NoContiguousSpotsAvailableCode);
        result.RejectionMessage.Should().Be("No contiguous spots are available for the requested assignment group.");
        result.Assignments.Should().BeEmpty();

        await using var verificationContext = Fixture.CreateDbContext();
        var request = await verificationContext.AssignmentRequests.Include(request => request.Attendees)
            .SingleAsync(request => request.AssignmentRequestId == result.AssignmentRequestId);
        request.Status.Should().Be(AssignmentRequestStatus.Rejected);
        request.RejectionCode.Should().Be(result.RejectionCode);
        request.RejectionMessage.Should().Be(result.RejectionMessage);
        request.ResolvedAt.Should().Be(command.AssignedAt);
        request.Attendees.OrderBy(attendee => attendee.Position).Select(attendee => attendee.AttendeeCode)
            .Should().Equal(command.AttendeeCodes);
        (await verificationContext.Assignments.CountAsync()).Should().Be(0);
        var histories = await new PostgreSqlFairnessHistoryProvider(verificationContext).GetHistoriesAsync(
            [IntegrationTestData.FirstAttendeeId, IntegrationTestData.SecondAttendeeId, ThirdAttendeeId]);
        foreach (var history in histories.Values)
        {
            history.Experiences.Should().BeEmpty();
        }
    }

    [Fact]
    [Trait("Category", "Stage4Acceptance")]
    public async Task ExecuteAsync_ShouldPersistEveryGroupMemberInSelectedBlockAndRecordEachHistory()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedReferenceDataAsync(context, includeSecondSpot: false);
        context.Attendees.Add(IntegrationTestData.CreateAttendee(ThirdAttendeeId, "ATT-003", "Katherine Johnson"));
        context.Spots.Add(IntegrationTestData.CreateSpot("FR-A-003", "A", 3));
        Spot[] completeBlock = [
            IntegrationTestData.CreateSpot("FR-B-004", "B", 4),
            IntegrationTestData.CreateSpot("FR-B-005", "B", 5),
            IntegrationTestData.CreateSpot("FR-B-006", "B", 6)];
        context.Spots.AddRange(completeBlock);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var command = CreateCommand([
            AttendeeCode.Create("ATT-003"), AttendeeCode.Create("ATT-001"), AttendeeCode.Create("ATT-002")]);

        var result = await CreateUseCase(context).ExecuteAsync(command);

        // The only complete block is B:4,5,6; request member order deliberately differs from ID order.
        await AssertCompletedOutcomeAsync(result, command, ExperienceQuality.Medium,
            [ThirdAttendeeId, IntegrationTestData.FirstAttendeeId, IntegrationTestData.SecondAttendeeId], completeBlock);
    }

    private async Task AssertCompletedOutcomeAsync(
        ProcessAssignmentRequestResult result,
        ProcessAssignmentRequestCommand command,
        ExperienceQuality expectedQuality,
        IReadOnlyList<AttendeeId> expectedAttendeeIds,
        IReadOnlyList<Spot> expectedBlock,
        params ExperienceQuality[] previousExperiences)
    {
        result.Status.Should().Be(AssignmentRequestStatus.Completed);
        result.IsAssigned.Should().BeTrue();
        result.Assignments.Select(assignment => assignment.AttendeeId).Should().Equal(expectedAttendeeIds);
        result.Assignments.Select(assignment => assignment.SpotCode).Should().Equal(expectedBlock.Select(spot => spot.Code));

        // A new context verifies the committed outcome rather than tracked entities.
        await using var verificationContext = Fixture.CreateDbContext();
        var request = await verificationContext.AssignmentRequests.Include(request => request.Attendees)
            .SingleAsync(request => request.AssignmentRequestId == result.AssignmentRequestId);
        request.Status.Should().Be(AssignmentRequestStatus.Completed);
        request.FestivalDayId.Should().Be(command.FestivalDayId);
        request.RequestedAt.Should().Be(command.RequestedAt);
        request.ResolvedAt.Should().Be(command.AssignedAt);
        request.AllowsFrontStanding.Should().Be(command.Eligibility.AllowsFrontStanding);
        request.Attendees.OrderBy(attendee => attendee.Position).Select(attendee => attendee.AttendeeCode)
            .Should().Equal(command.AttendeeCodes);

        var assignments = await verificationContext.Assignments.AsNoTracking()
            .Where(assignment => assignment.AssignmentRequestId == result.AssignmentRequestId).ToArrayAsync();
        assignments.Should().HaveCount(expectedAttendeeIds.Count);
        assignments.Select(assignment => assignment.AttendeeId).Should().BeEquivalentTo(expectedAttendeeIds);
        assignments.Select(assignment => assignment.AttendeeId).Should().OnlyHaveUniqueItems();
        assignments.Select(assignment => assignment.SpotCode).Should().OnlyHaveUniqueItems();
        assignments.Should().OnlyContain(assignment =>
            assignment.ExperienceQuality == expectedQuality &&
            assignment.FestivalDayId == command.FestivalDayId && assignment.AssignedAt == command.AssignedAt);
        foreach (var (attendeeId, spot) in expectedAttendeeIds.Zip(expectedBlock))
        {
            var assignment = assignments.Single(assignment => assignment.AttendeeId == attendeeId);
            assignment.SpotCode.Should().Be(spot.Code);
            assignment.ZoneId.Should().Be(spot.ZoneId);
            assignment.RowCode.Should().Be(spot.RowCode);
            assignment.SpotNumber.Should().Be(spot.Number);
        }

        var histories = await new PostgreSqlFairnessHistoryProvider(verificationContext).GetHistoriesAsync(expectedAttendeeIds);
        histories.Keys.Should().BeEquivalentTo(expectedAttendeeIds);
        foreach (var attendeeId in expectedAttendeeIds)
        {
            histories[attendeeId].Experiences.Should().Equal([.. previousExperiences, expectedQuality]);
        }
    }

    private static Spot[] AddZoneWithSpots(
        FestivalDbContext context, ZoneType type, string codePrefix, params int[] spotNumbers)
    {
        var zone = Zone.Create(ZoneId.Create(
            Guid.Parse($"20000000-0000-0000-0000-{(int)type + 10:000000000000}")), codePrefix, type);
        var spots = spotNumbers.Select(number => Spot.Create(
            SpotCode.Create($"{codePrefix}-A-{number:000}"), zone.Id, RowCode.Create("A"), SpotNumber.Create(number))).ToArray();
        context.Zones.Add(zone);
        context.Spots.AddRange(spots);
        return spots;
    }

    private static void AddUnfavorableHistory(FestivalDbContext context)
    {
        var priorDay = IntegrationTestData.CreateFestivalDay(
            IntegrationTestData.SecondFestivalDayId, new DateOnly(2026, 8, 14));
        var request = AssignmentRequest.Create(IntegrationTestData.SecondRequestId, priorDay.Id,
            [AttendeeCode.Create("ATT-001"), AttendeeCode.Create("ATT-002")],
            IntegrationTestData.RequestedAt.AddDays(-1), new RequestEligibility(true));
        request.Complete(IntegrationTestData.AssignedAt.AddDays(-1));
        context.AddRange(priorDay, AssignmentRequestMapper.ToRow(request));
        var spots = AddZoneWithSpots(context, ZoneType.UpperLeft, "BAD", 1, 2);
        AttendeeId[] attendeeIds = [IntegrationTestData.FirstAttendeeId, IntegrationTestData.SecondAttendeeId];
        for (var i = 0; i < attendeeIds.Length; i++)
        {
            context.Assignments.Add(Assignment.Create(
                AssignmentId.Create(Guid.Parse($"50000000-0000-0000-0000-{i + 1:000000000000}")),
                request.Id, priorDay.Id, attendeeIds[i], spots[i].Code, spots[i].ZoneId, spots[i].RowCode,
                spots[i].Number, ExperienceQuality.Bad, IntegrationTestData.AssignedAt.AddDays(-1)));
        }
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

    private static ProcessAssignmentRequestCommand CreateCommand(
        IReadOnlyCollection<AttendeeCode>? attendeeCodes = null,
        bool allowsFrontStanding = true)
    {
        return new ProcessAssignmentRequestCommand(
            IntegrationTestData.FestivalDayId,
            attendeeCodes ??
            [
                AttendeeCode.Create("ATT-001"),
                AttendeeCode.Create("ATT-002")
            ],
            IntegrationTestData.RequestedAt,
            IntegrationTestData.AssignedAt,
            new RequestEligibility(allowsFrontStanding));
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
