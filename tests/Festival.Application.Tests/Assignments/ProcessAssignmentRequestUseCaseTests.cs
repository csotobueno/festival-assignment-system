using Festival.Application.Assignments.Ports;
using Festival.Application.Zones.Ports;
using Festival.Application.Assignments.ProcessAssignmentRequest;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Application.Tests.Assignments;

public sealed class ProcessAssignmentRequestUseCaseTests
{
    private static readonly DateTimeOffset RequestedAt =
        new(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(-5));

    private static readonly DateTimeOffset AssignedAt =
        new(2026, 7, 10, 9, 1, 0, TimeSpan.FromHours(-5));

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task ExecuteAsync_ShouldStoreCommandEligibilityForEitherOutcome(
        bool allowsFrontStanding,
        bool hasAvailableSpot)
    {
        var context = CreateContext(1,
            hasAvailableSpot ? [CreateSpot(CreateZoneId(1), "A", 10)] : [],
            allowsFrontStanding);

        await context.UseCase.ExecuteAsync(context.Command);

        var saved = Assert.Single(context.AssignmentRequestRepository.SavedRequests);
        Assert.Same(context.Command.Eligibility, saved.Eligibility);
        Assert.Equal(allowsFrontStanding, saved.Eligibility.AllowsFrontStanding);
        Assert.Equal(hasAvailableSpot
            ? AssignmentRequestStatus.Completed : AssignmentRequestStatus.Rejected,
            saved.Status);
    }

    [Fact]
    public void Command_ShouldRequireEligibility()
    {
        var act = () => new ProcessAssignmentRequestCommand(
            FestivalDayId.New(), [AttendeeCode.Create("ATT-001")],
            RequestedAt, AssignedAt, null!);

        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("eligibility", exception.ParamName);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAssignedResult_WhenContiguousSpotsAreAvailable()
    {
        var context = CreateContext(
            attendeeCount: 1,
            availableSpots:
            [
                CreateSpot(CreateZoneId(1), "A", 10)
            ]);

        var result = await context.UseCase.ExecuteAsync(context.Command);

        Assert.True(result.IsAssigned);
        Assert.False(result.IsRejected);
        Assert.Equal(AssignmentRequestStatus.Completed, result.Status);
        Assert.Single(result.Assignments);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnRejectedResult_WhenNoContiguousSpotsAreAvailable()
    {
        var zoneId = CreateZoneId(1);
        var context = CreateContext(
            attendeeCount: 2,
            availableSpots:
            [
                CreateSpot(zoneId, "A", 10),
                CreateSpot(zoneId, "A", 12)
            ]);

        var result = await context.UseCase.ExecuteAsync(context.Command);

        Assert.False(result.IsAssigned);
        Assert.True(result.IsRejected);
        Assert.Equal(AssignmentRequestStatus.Rejected, result.Status);
        Assert.Equal(
            ProcessAssignmentRequestUseCase.NoContiguousSpotsAvailableCode,
            result.RejectionCode);
        Assert.Empty(result.Assignments);
    }

    [Theory]
    [InlineData(3, 2)]
    [InlineData(2, 0)]
    public async Task ExecuteAsync_ShouldRejectWholeRequestBeforePlacement_WhenResolutionIsIncomplete(
        int requestedCount,
        int resolvedCount)
    {
        var zoneId = CreateZoneId(1);
        var context = CreateContext(
            attendeeCount: requestedCount,
            availableSpots:
            [
                CreateSpot(zoneId, "A", 10),
                CreateSpot(zoneId, "A", 11),
                CreateSpot(zoneId, "A", 12)
            ]);
        context.AttendeeResolver.AttendeeIds = context.AttendeeResolver.AttendeeIds
            .Take(resolvedCount)
            .ToArray();
        using var cancellation = new CancellationTokenSource();

        var result = await context.UseCase.ExecuteAsync(
            context.Command,
            cancellation.Token);

        var savedRequest = Assert.Single(
            context.AssignmentRequestRepository.SavedRequests);
        var rejection = Assert.IsType<AssignmentRequestRejection>(savedRequest.Rejection);
        Assert.Equal(AssignmentRequestStatus.Rejected, savedRequest.Status);
        Assert.Equal(context.Command.AttendeeCodes, savedRequest.RequestedAttendeeCodes);
        Assert.Equal(requestedCount, savedRequest.RequestedAttendeeCodes.Count);
        Assert.Equal(AssignedAt, savedRequest.ResolvedAt);
        Assert.Equal("ATTENDEE_NOT_FOUND", rejection.Code);
        Assert.Equal(
            "One or more requested attendees could not be resolved.",
            rejection.Message);
        Assert.Null(context.AvailableSpotProvider.ReceivedCancellationToken);
        Assert.Null(context.ZoneCatalogProvider.ReceivedCancellationToken);
        Assert.Null(context.HistoryProvider.ReceivedCancellationToken);
        Assert.Empty(context.AssignmentRepository.SavedBatches);
        Assert.Equal(1, context.UnitOfWork.InvocationCount);
        Assert.Equal(
            ["RequestRepository.AddAsync", "UnitOfWork.SaveChangesAsync"],
            context.PersistenceEvents);
        Assert.Equal(cancellation.Token, context.AttendeeResolver.ReceivedCancellationToken);
        Assert.Equal(cancellation.Token, context.AssignmentRequestRepository.ReceivedCancellationToken);
        Assert.Equal(cancellation.Token, context.UnitOfWork.ReceivedCancellationToken);
        Assert.True(result.IsRejected);
        Assert.False(result.IsAssigned);
        Assert.Equal(savedRequest.Id, result.AssignmentRequestId);
        Assert.Equal(AssignmentRequestStatus.Rejected, result.Status);
        Assert.Equal(rejection.Code, result.RejectionCode);
        Assert.Equal(rejection.Message, result.RejectionMessage);
        Assert.Equal(RequestedAt, result.RequestedAt);
        Assert.Equal(AssignedAt, result.ResolvedAt);
        Assert.Empty(result.Assignments);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_ShouldAssignEntireGroup_WhenResolutionIsCompleteInEitherOrder(
        bool reverseResolutionOrder)
    {
        var zoneId = CreateZoneId(1);
        var context = CreateContext(
            attendeeCount: 3,
            availableSpots:
            [
                CreateSpot(zoneId, "A", 10),
                CreateSpot(zoneId, "A", 11),
                CreateSpot(zoneId, "A", 12)
            ]);
        var expectedAttendeeIds = context.AttendeeResolver.AttendeeIds.ToArray();
        if (reverseResolutionOrder)
        {
            context.AttendeeResolver.AttendeeIds = expectedAttendeeIds.Reverse().ToArray();
        }

        var result = await context.UseCase.ExecuteAsync(context.Command);

        Assert.True(result.IsAssigned);
        Assert.Equal(3, result.Assignments.Count);
        Assert.Equal(
            expectedAttendeeIds.OrderBy(id => id.Value),
            result.Assignments.Select(assignment => assignment.AttendeeId).OrderBy(id => id.Value));
        Assert.NotNull(context.AvailableSpotProvider.ReceivedCancellationToken);
        var savedRequest = Assert.Single(context.AssignmentRequestRepository.SavedRequests);
        Assert.Equal(AssignmentRequestStatus.Completed, savedRequest.Status);
        Assert.Equal(3, Assert.Single(context.AssignmentRepository.SavedBatches).Count);
        Assert.Equal(1, context.UnitOfWork.InvocationCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPersistAssignmentRequestAsCompleted_OnSuccess()
    {
        var context = CreateContext(
            attendeeCount: 1,
            availableSpots:
            [
                CreateSpot(CreateZoneId(1), "A", 10)
            ]);

        await context.UseCase.ExecuteAsync(context.Command);

        var savedRequest = Assert.Single(
            context.AssignmentRequestRepository.SavedRequests);

        Assert.Equal(
            AssignmentRequestStatus.Completed,
            savedRequest.Status);
        Assert.Equal(AssignedAt, savedRequest.ResolvedAt);
        Assert.Null(savedRequest.Rejection);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPersistAssignmentRequestAsRejected_WhenNoSolutionExists()
    {
        var context = CreateContext(
            attendeeCount: 2,
            availableSpots:
            [
                CreateSpot(CreateZoneId(1), "A", 10)
            ]);

        await context.UseCase.ExecuteAsync(context.Command);

        var savedRequest = Assert.Single(
            context.AssignmentRequestRepository.SavedRequests);

        Assert.Equal(
            AssignmentRequestStatus.Rejected,
            savedRequest.Status);
        Assert.Equal(AssignedAt, savedRequest.ResolvedAt);
        Assert.Equal(
            ProcessAssignmentRequestUseCase.NoContiguousSpotsAvailableCode,
            savedRequest.Rejection?.Code);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPersistAssignmentsOnlyOnSuccess()
    {
        var successfulContext = CreateContext(
            attendeeCount: 1,
            availableSpots:
            [
                CreateSpot(CreateZoneId(1), "A", 10)
            ]);

        await successfulContext.UseCase.ExecuteAsync(
            successfulContext.Command);

        Assert.Single(successfulContext.AssignmentRepository.SavedBatches);
        Assert.Single(successfulContext.AssignmentRepository.SavedBatches[0]);

        var rejectedContext = CreateContext(
            attendeeCount: 2,
            availableSpots:
            [
                CreateSpot(CreateZoneId(1), "A", 10)
            ]);

        await rejectedContext.UseCase.ExecuteAsync(
            rejectedContext.Command);

        Assert.Empty(rejectedContext.AssignmentRepository.SavedBatches);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStageCompletedOutcomeBeforeSavingOnce()
    {
        var context = CreateContext(
            attendeeCount: 1,
            availableSpots:
            [
                CreateSpot(CreateZoneId(1), "A", 10)
            ]);
        using var cancellation = new CancellationTokenSource();

        var result = await context.UseCase.ExecuteAsync(
            context.Command,
            cancellation.Token);

        var savedRequest = Assert.Single(
            context.AssignmentRequestRepository.SavedRequests);
        Assert.Equal(AssignmentRequestStatus.Completed, savedRequest.Status);
        Assert.Single(context.AssignmentRepository.SavedBatches);
        Assert.Equal(1, context.UnitOfWork.InvocationCount);
        Assert.Equal(
            ["RequestRepository.AddAsync", "AssignmentRepository.AddAsync", "UnitOfWork.SaveChangesAsync"],
            context.PersistenceEvents);
        Assert.Equal(
            cancellation.Token,
            context.AttendeeResolver.ReceivedCancellationToken);
        Assert.Equal(
            cancellation.Token,
            context.AvailableSpotProvider.ReceivedCancellationToken);
        Assert.Equal(
            cancellation.Token,
            context.AssignmentRequestRepository.ReceivedCancellationToken);
        Assert.Equal(
            cancellation.Token,
            context.AssignmentRepository.ReceivedCancellationToken);
        Assert.Equal(
            cancellation.Token,
            context.UnitOfWork.ReceivedCancellationToken);
        Assert.True(result.IsAssigned);
        Assert.Single(result.Assignments);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStageRejectedOutcomeBeforeSavingOnce()
    {
        var context = CreateContext(
            attendeeCount: 2,
            availableSpots:
            [
                CreateSpot(CreateZoneId(1), "A", 10)
            ]);
        using var cancellation = new CancellationTokenSource();

        var result = await context.UseCase.ExecuteAsync(
            context.Command,
            cancellation.Token);

        var savedRequest = Assert.Single(
            context.AssignmentRequestRepository.SavedRequests);
        var rejection = Assert.IsType<AssignmentRequestRejection>(
            savedRequest.Rejection);
        Assert.Equal(AssignmentRequestStatus.Rejected, savedRequest.Status);
        Assert.Equal(
            ProcessAssignmentRequestUseCase.NoContiguousSpotsAvailableCode,
            rejection.Code);
        Assert.Equal(
            "No contiguous spots are available for the requested assignment group.",
            rejection.Message);
        Assert.Empty(context.AssignmentRepository.SavedBatches);
        Assert.Equal(1, context.UnitOfWork.InvocationCount);
        Assert.Equal(
            ["RequestRepository.AddAsync", "UnitOfWork.SaveChangesAsync"],
            context.PersistenceEvents);
        Assert.Equal(
            cancellation.Token,
            context.AttendeeResolver.ReceivedCancellationToken);
        Assert.Equal(
            cancellation.Token,
            context.AvailableSpotProvider.ReceivedCancellationToken);
        Assert.Equal(
            cancellation.Token,
            context.AssignmentRequestRepository.ReceivedCancellationToken);
        Assert.Null(
            context.AssignmentRepository.ReceivedCancellationToken);
        Assert.Equal(
            cancellation.Token,
            context.UnitOfWork.ReceivedCancellationToken);
        Assert.True(result.IsRejected);
        Assert.Equal(rejection.Code, result.RejectionCode);
        Assert.Equal(rejection.Message, result.RejectionMessage);
        Assert.Empty(result.Assignments);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateUnitOfWorkException()
    {
        var context = CreateContext(
            attendeeCount: 1,
            availableSpots:
            [
                CreateSpot(CreateZoneId(1), "A", 10)
            ]);
        var expectedException = new InvalidOperationException(
            "Persistence failed.");
        context.UnitOfWork.Exception = expectedException;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.UseCase.ExecuteAsync(context.Command));

        Assert.Same(expectedException, exception);
        Assert.Equal(1, context.UnitOfWork.InvocationCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseStage4SelectionForCompleteBlocks()
    {
        var firstZone = CreateZoneId(1);
        var secondZone = CreateZoneId(2);

        var context = CreateContext(
            attendeeCount: 2,
            availableSpots:
            [
                CreateSpot(firstZone, "A", 10),
                CreateSpot(firstZone, "B", 11),
                CreateSpot(secondZone, "C", 20),
                CreateSpot(secondZone, "C", 21)
            ]);

        var result = await context.UseCase.ExecuteAsync(context.Command);

        Assert.True(result.IsAssigned);
        Assert.Equal(
            [20, 21],
            result.Assignments
                .Select(assignment => assignment.SpotNumber.Value)
                .ToArray());
        Assert.All(
            result.Assignments,
            assignment => Assert.Equal(secondZone, assignment.ZoneId));
        Assert.All(
            result.Assignments,
            assignment => Assert.Equal(
                RowCode.Create("C"),
                assignment.RowCode));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAssignmentOutputData()
    {
        var zoneId = CreateZoneId(1);
        var spot = CreateSpot(zoneId, "A", 10);
        var context = CreateContext(
            attendeeCount: 1,
            availableSpots: [spot]);

        var result = await context.UseCase.ExecuteAsync(context.Command);

        var output = Assert.Single(result.Assignments);

        Assert.NotEqual(default, output.AssignmentId);
        Assert.Equal(result.AssignmentRequestId, output.AssignmentRequestId);
        Assert.Equal(context.Command.FestivalDayId, output.FestivalDayId);
        Assert.Equal(context.AttendeeResolver.AttendeeIds[0], output.AttendeeId);
        Assert.Equal(spot.Code, output.SpotCode);
        Assert.Equal(spot.ZoneId, output.ZoneId);
        Assert.Equal(spot.RowCode, output.RowCode);
        Assert.Equal(spot.Number, output.SpotNumber);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPreserveRequestedAndAssignedTimestamps()
    {
        var context = CreateContext(
            attendeeCount: 1,
            availableSpots:
            [
                CreateSpot(CreateZoneId(1), "A", 10)
            ]);

        var result = await context.UseCase.ExecuteAsync(context.Command);

        var savedRequest = Assert.Single(
            context.AssignmentRequestRepository.SavedRequests);
        var savedAssignment = Assert.Single(
            context.AssignmentRepository.SavedBatches.Single());
        var output = Assert.Single(result.Assignments);

        Assert.Equal(RequestedAt, savedRequest.RequestedAt);
        Assert.Equal(AssignedAt, savedRequest.ResolvedAt);
        Assert.Equal(RequestedAt, result.RequestedAt);
        Assert.Equal(AssignedAt, result.ResolvedAt);
        Assert.Equal(AssignedAt, savedAssignment.AssignedAt);
        Assert.Equal(AssignedAt, output.AssignedAt);
    }

    [Theory]
    [InlineData(0, 2, ExperienceQuality.Medium)]
    [InlineData(1, 2, ExperienceQuality.Medium)]
    [InlineData(2, 1, ExperienceQuality.Good)]
    public async Task ExecuteAsync_ShouldUseMemberHistoriesToSelectAndRecordActualQuality(
        int membersWithBadHistory, int expectedZone, ExperienceQuality expectedQuality)
    {
        var zones = new[]
        {
            Zone.Create(CreateZoneId(1), "Good", ZoneType.FrontStanding),
            Zone.Create(CreateZoneId(2), "Medium", ZoneType.MiddleLeft)
        };
        var spots = zones.SelectMany(zone => new[]
        {
            CreateSpot(zone.Id, "A", 1), CreateSpot(zone.Id, "A", 2)
        }).ToArray();
        var context = CreateContext(2, spots, zones: zones);
        foreach (var id in context.AttendeeResolver.AttendeeIds.Take(membersWithBadHistory))
        {
            context.HistoryProvider.Histories[id] = FairnessHistory.Create([ExperienceQuality.Bad]);
        }
        using var cancellation = new CancellationTokenSource();

        var result = await context.UseCase.ExecuteAsync(context.Command, cancellation.Token);

        Assert.True(result.IsAssigned);
        Assert.Equal(2, result.Assignments.Count);
        Assert.All(result.Assignments, assignment => Assert.Equal(CreateZoneId(expectedZone), assignment.ZoneId));
        Assert.All(Assert.Single(context.AssignmentRepository.SavedBatches),
            assignment => Assert.Equal(expectedQuality, assignment.ExperienceQuality));
        Assert.Equal(1, context.HistoryProvider.InvocationCount);
        Assert.Equal(context.AttendeeResolver.AttendeeIds, context.HistoryProvider.RequestedIds);
        Assert.Equal(cancellation.Token, context.HistoryProvider.ReceivedCancellationToken);
        Assert.Equal(cancellation.Token, context.ZoneCatalogProvider.ReceivedCancellationToken);
        Assert.Equal(1, context.UnitOfWork.InvocationCount);
    }

    [Theory]
    [InlineData(true, ExperienceQuality.Good)]
    [InlineData(false, ExperienceQuality.Medium)]
    public async Task ExecuteAsync_ShouldApplyEligibilityBeforeBetterThanTargetSelection(
        bool allowsFrontStanding, ExperienceQuality expectedQuality)
    {
        var front = Zone.Create(CreateZoneId(1), "Front", ZoneType.FrontStanding);
        var middle = Zone.Create(CreateZoneId(2), "Middle", ZoneType.MiddleLeft);
        var spots = Enumerable.Range(1, 3).Select(i => CreateSpot(front.Id, "A", i))
            .Append(CreateSpot(middle.Id, "A", 1)).ToArray();
        var context = CreateContext(1, spots, allowsFrontStanding, [front, middle]);

        var result = await context.UseCase.ExecuteAsync(context.Command);

        Assert.True(result.IsAssigned);
        var assignment = Assert.Single(Assert.Single(context.AssignmentRepository.SavedBatches));
        Assert.Equal(allowsFrontStanding ? front.Id : middle.Id, assignment.ZoneId);
        Assert.Equal(expectedQuality, assignment.ExperienceQuality);
    }

    [Theory]
    [InlineData(ZoneType.MiddleLeft, ExperienceQuality.Medium)]
    [InlineData(ZoneType.UpperLeft, ExperienceQuality.Bad)]
    public async Task ExecuteAsync_ShouldRecordDegradedActualQuality_WhenGoodTargetIsUnavailable(
        ZoneType type, ExperienceQuality expectedQuality)
    {
        var zone = Zone.Create(CreateZoneId(1), "Available", type);
        var context = CreateContext(1, [CreateSpot(zone.Id, "A", 1)], zones: [zone]);
        context.HistoryProvider.Histories[context.AttendeeResolver.AttendeeIds[0]] =
            FairnessHistory.Create([ExperienceQuality.Bad]);

        var result = await context.UseCase.ExecuteAsync(context.Command);

        Assert.True(result.IsAssigned);
        Assert.Equal(expectedQuality,
            Assert.Single(Assert.Single(context.AssignmentRepository.SavedBatches)).ExperienceQuality);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectWhenNoZoneIsEligible()
    {
        var zone = Zone.Create(CreateZoneId(1), "Front", ZoneType.FrontStanding);
        var context = CreateContext(1, [CreateSpot(zone.Id, "A", 1)], false, [zone]);

        var result = await context.UseCase.ExecuteAsync(context.Command);

        Assert.True(result.IsRejected);
        Assert.Equal(ProcessAssignmentRequestUseCase.NoContiguousSpotsAvailableCode, result.RejectionCode);
        Assert.Empty(context.AssignmentRepository.SavedBatches);
        Assert.Equal(AssignmentRequestStatus.Rejected,
            Assert.Single(context.AssignmentRequestRepository.SavedRequests).Status);
        Assert.Equal(1, context.UnitOfWork.InvocationCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseBlockSelectionInsteadOfFirstAvailableRow()
    {
        var zoneId = CreateZoneId(1);
        var spots = Enumerable.Range(1, 3).Select(i => CreateSpot(zoneId, "A", i))
            .Concat(Enumerable.Range(1, 6).Select(i => CreateSpot(zoneId, "B", i))).ToArray();
        var context = CreateContext(2, spots);

        var result = await context.UseCase.ExecuteAsync(context.Command);

        Assert.All(result.Assignments, assignment => Assert.Equal(RowCode.Create("B"), assignment.RowCode));
        Assert.Equal([1, 2], result.Assignments.Select(assignment => assignment.SpotNumber.Value));
    }

    private static TestContext CreateContext(
        int attendeeCount,
        IReadOnlyList<Spot> availableSpots,
        bool allowsFrontStanding = true,
        IReadOnlyList<Zone>? zones = null)
    {
        // Compatibility baseline for tests unrelated to eligibility.
        var attendeeCodes = Enumerable
            .Range(1, attendeeCount)
            .Select(number => AttendeeCode.Create($"ATT-{number:000}"))
            .ToArray();

        var attendeeIds = Enumerable
            .Range(1, attendeeCount)
            .Select(number => AttendeeId.Create(
                Guid.Parse(
                    $"00000000-0000-0000-0000-{number:000000000000}")))
            .ToArray();

        var attendeeResolver = new FakeAttendeeCodeResolver(attendeeIds);
        var availableSpotProvider = new FakeAvailableSpotProvider(
            availableSpots);
        var zoneCatalogProvider = new FakeZoneCatalogProvider(zones ?? availableSpots
            .Select(spot => spot.ZoneId).Distinct()
            .Select(id => Zone.Create(id, "Test Zone", ZoneType.MiddleLeft)).ToArray());
        var historyProvider = new FakeFairnessHistoryProvider();
        var persistenceEvents = new List<string>();
        var assignmentRequestRepository =
            new FakeAssignmentRequestRepository(persistenceEvents);
        var assignmentRepository = new FakeAssignmentRepository(
            persistenceEvents);
        var unitOfWork = new FakeUnitOfWork(persistenceEvents);

        var useCase = new ProcessAssignmentRequestUseCase(
            attendeeResolver,
            availableSpotProvider,
            zoneCatalogProvider,
            historyProvider,
            assignmentRequestRepository,
            assignmentRepository,
            unitOfWork,
            new AssignmentEngine());

        var command = new ProcessAssignmentRequestCommand(
            FestivalDayId.Create(
                Guid.Parse("10000000-0000-0000-0000-000000000001")),
            attendeeCodes,
            RequestedAt,
            AssignedAt,
            new RequestEligibility(allowsFrontStanding));

        return new TestContext(
            useCase,
            command,
            attendeeResolver,
            availableSpotProvider,
            zoneCatalogProvider,
            historyProvider,
            assignmentRequestRepository,
            assignmentRepository,
            unitOfWork,
            persistenceEvents);
    }

    private static Spot CreateSpot(
        ZoneId zoneId,
        string rowCode,
        int spotNumber)
    {
        return Spot.Create(
            SpotCode.Create($"{zoneId.Value:N}-{rowCode}-{spotNumber:000}"),
            zoneId,
            RowCode.Create(rowCode),
            SpotNumber.Create(spotNumber));
    }

    private static ZoneId CreateZoneId(int number)
    {
        return ZoneId.Create(
            Guid.Parse($"20000000-0000-0000-0000-{number:000000000000}"));
    }

    private sealed record TestContext(
        ProcessAssignmentRequestUseCase UseCase,
        ProcessAssignmentRequestCommand Command,
        FakeAttendeeCodeResolver AttendeeResolver,
        FakeAvailableSpotProvider AvailableSpotProvider,
        FakeZoneCatalogProvider ZoneCatalogProvider,
        FakeFairnessHistoryProvider HistoryProvider,
        FakeAssignmentRequestRepository AssignmentRequestRepository,
        FakeAssignmentRepository AssignmentRepository,
        FakeUnitOfWork UnitOfWork,
        IReadOnlyList<string> PersistenceEvents);

    private sealed class FakeAttendeeCodeResolver : IAttendeeCodeResolver
    {
        public FakeAttendeeCodeResolver(
            IReadOnlyList<AttendeeId> attendeeIds)
        {
            AttendeeIds = attendeeIds;
        }

        public IReadOnlyList<AttendeeId> AttendeeIds { get; set; }

        public CancellationToken? ReceivedCancellationToken { get; private set; }

        public Task<IReadOnlyList<AttendeeId>> ResolveAttendeeIdsAsync(
            IEnumerable<AttendeeCode> attendeeCodes,
            CancellationToken cancellationToken = default)
        {
            ReceivedCancellationToken = cancellationToken;

            return Task.FromResult(AttendeeIds);
        }
    }

    private sealed class FakeZoneCatalogProvider(IReadOnlyList<Zone> zones) : IZoneCatalogProvider
    {
        public CancellationToken? ReceivedCancellationToken { get; private set; }
        public Task<IReadOnlyList<Zone>> GetZonesAsync(CancellationToken cancellationToken = default)
        {
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(zones);
        }
    }

    private sealed class FakeFairnessHistoryProvider : IFairnessHistoryProvider
    {
        public Dictionary<AttendeeId, FairnessHistory> Histories { get; } = [];
        public IReadOnlyCollection<AttendeeId>? RequestedIds { get; private set; }
        public int InvocationCount { get; private set; }
        public CancellationToken? ReceivedCancellationToken { get; private set; }
        public Task<IReadOnlyDictionary<AttendeeId, FairnessHistory>> GetHistoriesAsync(
            IReadOnlyCollection<AttendeeId> attendeeIds, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            RequestedIds = attendeeIds;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult<IReadOnlyDictionary<AttendeeId, FairnessHistory>>(
                attendeeIds.ToDictionary(id => id,
                    id => Histories.TryGetValue(id, out var history) ? history : FairnessHistory.Create([])));
        }
    }

    private sealed class FakeAvailableSpotProvider : IAvailableSpotProvider
    {
        private readonly IReadOnlyList<Spot> availableSpots;

        public CancellationToken? ReceivedCancellationToken { get; private set; }

        public FakeAvailableSpotProvider(
            IReadOnlyList<Spot> availableSpots)
        {
            this.availableSpots = availableSpots;
        }

        public Task<IReadOnlyList<Spot>> GetAvailableSpotsAsync(
            FestivalDayId festivalDayId,
            CancellationToken cancellationToken = default)
        {
            ReceivedCancellationToken = cancellationToken;

            return Task.FromResult(availableSpots);
        }
    }

    private sealed class FakeAssignmentRequestRepository
        : IAssignmentRequestRepository
    {
        private readonly List<string> persistenceEvents;

        public FakeAssignmentRequestRepository(
            List<string> persistenceEvents)
        {
            this.persistenceEvents = persistenceEvents;
        }

        public List<AssignmentRequest> SavedRequests { get; } = [];

        public CancellationToken? ReceivedCancellationToken { get; private set; }

        public Task AddAsync(
            AssignmentRequest assignmentRequest,
            CancellationToken cancellationToken = default)
        {
            ReceivedCancellationToken = cancellationToken;
            SavedRequests.Add(assignmentRequest);
            persistenceEvents.Add("RequestRepository.AddAsync");

            return Task.CompletedTask;
        }
    }

    private sealed class FakeAssignmentRepository : IAssignmentRepository
    {
        private readonly List<string> persistenceEvents;

        public FakeAssignmentRepository(List<string> persistenceEvents)
        {
            this.persistenceEvents = persistenceEvents;
        }

        public List<IReadOnlyList<Assignment>> SavedBatches { get; } = [];

        public CancellationToken? ReceivedCancellationToken { get; private set; }

        public Task AddAsync(
            IEnumerable<Assignment> assignments,
            CancellationToken cancellationToken = default)
        {
            ReceivedCancellationToken = cancellationToken;
            SavedBatches.Add(Array.AsReadOnly(assignments.ToArray()));
            persistenceEvents.Add("AssignmentRepository.AddAsync");

            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly List<string> persistenceEvents;

        public FakeUnitOfWork(List<string> persistenceEvents)
        {
            this.persistenceEvents = persistenceEvents;
        }

        public int InvocationCount { get; private set; }

        public CancellationToken? ReceivedCancellationToken { get; private set; }

        public int ReturnValue { get; init; }

        public Exception? Exception { get; set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            ReceivedCancellationToken = cancellationToken;
            persistenceEvents.Add("UnitOfWork.SaveChangesAsync");

            return Exception is null
                ? Task.FromResult(ReturnValue)
                : Task.FromException<int>(Exception);
        }
    }
}
