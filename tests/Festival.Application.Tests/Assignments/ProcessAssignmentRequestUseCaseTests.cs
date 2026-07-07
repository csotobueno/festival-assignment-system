using Festival.Application.Assignments.Ports;
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
    public async Task ExecuteAsync_ShouldLeaveContiguityZoneAndRowSelectionToAssignmentEngine()
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

    private static TestContext CreateContext(
        int attendeeCount,
        IReadOnlyList<Spot> availableSpots)
    {
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
        var assignmentRequestRepository =
            new FakeAssignmentRequestRepository();
        var assignmentRepository = new FakeAssignmentRepository();

        var useCase = new ProcessAssignmentRequestUseCase(
            attendeeResolver,
            availableSpotProvider,
            assignmentRequestRepository,
            assignmentRepository);

        var command = new ProcessAssignmentRequestCommand(
            FestivalDayId.Create(
                Guid.Parse("10000000-0000-0000-0000-000000000001")),
            attendeeCodes,
            RequestedAt,
            AssignedAt);

        return new TestContext(
            useCase,
            command,
            attendeeResolver,
            assignmentRequestRepository,
            assignmentRepository);
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
        FakeAssignmentRequestRepository AssignmentRequestRepository,
        FakeAssignmentRepository AssignmentRepository);

    private sealed class FakeAttendeeCodeResolver : IAttendeeCodeResolver
    {
        public FakeAttendeeCodeResolver(
            IReadOnlyList<AttendeeId> attendeeIds)
        {
            AttendeeIds = attendeeIds;
        }

        public IReadOnlyList<AttendeeId> AttendeeIds { get; }

        public Task<IReadOnlyList<AttendeeId>> ResolveAttendeeIdsAsync(
            IEnumerable<AttendeeCode> attendeeCodes,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AttendeeIds);
        }
    }

    private sealed class FakeAvailableSpotProvider : IAvailableSpotProvider
    {
        private readonly IReadOnlyList<Spot> availableSpots;

        public FakeAvailableSpotProvider(
            IReadOnlyList<Spot> availableSpots)
        {
            this.availableSpots = availableSpots;
        }

        public Task<IReadOnlyList<Spot>> GetAvailableSpotsAsync(
            FestivalDayId festivalDayId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(availableSpots);
        }
    }

    private sealed class FakeAssignmentRequestRepository
        : IAssignmentRequestRepository
    {
        public List<AssignmentRequest> SavedRequests { get; } = [];

        public Task SaveAsync(
            AssignmentRequest assignmentRequest,
            CancellationToken cancellationToken = default)
        {
            SavedRequests.Add(assignmentRequest);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeAssignmentRepository : IAssignmentRepository
    {
        public List<IReadOnlyList<Assignment>> SavedBatches { get; } = [];

        public Task SaveAsync(
            IEnumerable<Assignment> assignments,
            CancellationToken cancellationToken = default)
        {
            SavedBatches.Add(Array.AsReadOnly(assignments.ToArray()));

            return Task.CompletedTask;
        }
    }
}
