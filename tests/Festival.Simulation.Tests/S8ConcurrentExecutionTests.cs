using System.Collections.Immutable;
using System.Text.Json;
using Festival.Domain.Assignments;
using Festival.Domain.Zones;
using Festival.Simulation;
using FluentAssertions;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class S8ConcurrentExecutionTests
{
    [Theory]
    [InlineData(1001)]
    [InlineData(2001)]
    [InlineData(3001)]
    public void Create_ReusesExactS2InputsAcrossLevelsAndRepetitions(int seed)
    {
        var source = new ScenarioMaterializer().Materialize(
            S2ExpectedBaselineScenarioDefinition.Create(seed, new(2026, 8, 15)));
        var before = JsonSerializer.Serialize(source);
        foreach (var level in new[] { 1, 4, 16 })
        foreach (var repetition in new[] { 1, 2, 3 })
        {
            var manifest = S8ConcurrentExecution.Create(source, level, repetition);
            manifest.Catalog.Should().BeSameAs(source.Catalog);
            manifest.Setup.Should().BeSameAs(source.Setup);
            manifest.DailyPlans.Should().Equal(source.DailyPlans);
            manifest.DailyPlans.SelectMany(day => day.Requests).Should()
                .Equal(source.DailyPlans.SelectMany(day => day.Requests));
            manifest.Metadata.Seed.Should().Be(seed);
            manifest.Metadata.GeneratorVersion.Should().Be("v2");
            manifest.Metadata.Repetition.Should().Be(repetition);
            manifest.Metadata.ConcurrencyLevel.Should().Be(level);
            manifest.Metadata.ExecutionMode.Should().Be(level == 1
                ? SimulationExecutionMode.Sequential : SimulationExecutionMode.Concurrent);
            manifest.Metadata.WorkloadSourceScenarioId.Should().Be(level == 1 ? null : source.Metadata.ScenarioId);
        }
        JsonSerializer.Serialize(source).Should().Be(before);
    }

    [Theory]
    [InlineData(4, 11, 3)]
    [InlineData(16, 35, 3)]
    [InlineData(16, 7, 7)]
    public void CreateWindows_UsesStableBaselineOrderAndActualPartialWindow(int level, int count, int finalSize)
    {
        var source = SmallManifest(count);
        var day = source.DailyPlans[0];
        var shuffled = day with { Requests = day.Requests.Reverse().ToImmutableArray() };
        var config = new SimulationExecutionConfiguration(SimulationExecutionMode.Concurrent, level);
        var windows = ConcurrentSimulationExecutor.CreateWindows(shuffled, config);

        windows.SelectMany(window => window.Requests).Should().Equal(day.Requests);
        windows.Select(window => window.Number).Should().Equal(Enumerable.Range(1, windows.Length));
        windows[^1].Requests.Length.Should().Be(finalSize);
        windows.Take(windows.Length - 1).Select(window => window.Requests.Length)
            .Should().Equal(Enumerable.Repeat(level, windows.Length - 1));
        ConcurrentSimulationExecutor.CreateWindows(shuffled, config).Should().BeEquivalentTo(windows,
            options => options.WithStrictOrdering());
    }

    [Theory]
    [InlineData(4)]
    [InlineData(16)]
    public async Task ExecuteDayAsync_WaitsForAllPreparedRequestsAndDrainsBeforePreparingNextWindow(int level)
    {
        var day = SmallManifest(level + 1).DailyPlans[0];
        var lastPreparing = Signal();
        var allowLastReady = Signal();
        var allEntered = Signal();
        var finishBusiness = Signal();
        var prepared = 0;
        var entered = 0;
        var finished = 0;
        var results = ConcurrentSimulationExecutor.ExecuteDayAsync(day,
            new(SimulationExecutionMode.Concurrent, level), async (request, position, ready, token) =>
            {
                Interlocked.Increment(ref prepared);
                if (position.Number == 1 && position.Position == level)
                {
                    lastPreparing.SetResult();
                    await allowLastReady.Task;
                }
                if (position.Number == 2) finished.Should().Be(level);
                await ready();
                if (Interlocked.Increment(ref entered) == level) allEntered.SetResult();
                if (position.Number == 1) await finishBusiness.Task;
                Interlocked.Increment(ref finished);
                return Attempt(day, request) with { WindowNumber = position.Number, WindowPosition = position.Position };
            });

        await lastPreparing.Task.WaitAsync(TimeSpan.FromSeconds(10));
        entered.Should().Be(0, "no business execution may precede readiness of every member");
        allowLastReady.SetResult();
        await allEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        prepared.Should().Be(level, "the next window cannot prepare while this one is active");
        finishBusiness.SetResult();
        var attempts = await results.WaitAsync(TimeSpan.FromSeconds(10));
        attempts.Select(attempt => attempt.BaselineSequence).Should().Equal(day.Requests.Select(request => request.Sequence));
        attempts.Select(attempt => attempt.WindowNumber).Should().Equal(Enumerable.Repeat<int?>(1, level).Append(2));
        attempts.Select(attempt => attempt.WindowPosition).Should().Equal(Enumerable.Range(1, level).Select(i => (int?)i).Append(1));
    }

    [Fact]
    public async Task ExecuteDayAsync_PreparationFailureReleasesWaitersAndDoesNotAdvance()
    {
        var day = SmallManifest(5).DailyPlans[0];
        var finished = 0;
        var action = async () => await ConcurrentSimulationExecutor.ExecuteDayAsync(day,
            new(SimulationExecutionMode.Concurrent, 4), async (request, position, ready, token) =>
            {
                position.Number.Should().Be(1);
                try
                {
                    if (position.Position == 4) throw new InvalidOperationException("Preparation failed");
                    await ready();
                    throw new Xunit.Sdk.XunitException("A failed preparation must not release business execution");
                }
                finally { Interlocked.Increment(ref finished); }
            }).WaitAsync(TimeSpan.FromSeconds(10));
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Preparation failed");
        finished.Should().Be(4);
    }

    [Fact]
    public async Task ExecuteDayAsync_CancellationDrainsCurrentWindowWithoutStartingNext()
    {
        var day = SmallManifest(5).DailyPlans[0];
        using var cancellation = new CancellationTokenSource();
        var entered = Signal();
        var drained = 0;
        var run = ConcurrentSimulationExecutor.ExecuteDayAsync(day, new(SimulationExecutionMode.Concurrent, 4),
            async (request, position, ready, token) =>
            {
                position.Number.Should().Be(1);
                try
                {
                    await ready();
                    entered.TrySetResult();
                    await new TaskCompletionSource().Task.WaitAsync(token);
                    return Attempt(day, request);
                }
                finally { Interlocked.Increment(ref drained); }
            }, cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cancellation.Cancel();
        var action = async () => await run.WaitAsync(TimeSpan.FromSeconds(10));
        await action.Should().ThrowAsync<OperationCanceledException>();
        drained.Should().Be(4);
    }

    [Theory]
    [InlineData(SimulationExecutionMode.Sequential, 4, 1)]
    [InlineData(SimulationExecutionMode.Concurrent, 1, 1)]
    [InlineData(SimulationExecutionMode.Concurrent, 8, 1)]
    [InlineData((SimulationExecutionMode)99, 4, 1)]
    public void Configuration_RejectsUnsupportedModeLevelPairs(SimulationExecutionMode mode, int level, int repetition)
    {
        var action = () => new SimulationExecutionConfiguration(mode, level, repetition);
        action.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Configuration_RejectsUnsupportedRepetitions(int repetition)
    {
        var action = () => new SimulationExecutionConfiguration(SimulationExecutionMode.Concurrent, 4, repetition);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_RejectsTransformedOrOccupiedSources()
    {
        var source = SmallManifest(5);
        var transformed = S5ReversedProcessingOrder.Create(source);
        var action = () => S8ConcurrentExecution.Create(transformed, 4, 1);
        action.Should().Throw<ArgumentException>();
        action = () => S8ConcurrentExecution.Create(source with
        {
            DailyPlans = [source.DailyPlans[0] with { Requests = [source.DailyPlans[0].Requests[0] with { ExecutionSequence = 99 }] }]
        }, 4, 1);
        action.Should().Throw<ArgumentException>();
        var occupied = new ScenarioMaterializer().Materialize(MinimalRecoveryScenarioDefinition.Create());
        action = () => S8ConcurrentExecution.Create(occupied with { Metadata = source.Metadata }, 4, 1);
        action.Should().Throw<ArgumentException>();
    }

    internal static ScenarioDefinition SmallDefinition(int requests, int spots = 2) =>
        S2ExpectedBaselineScenarioDefinition.Create(1001, new(2026, 8, 15)) with
        {
            AttendeeCount = requests * 3,
            Workload = new RepresentativeWorkloadDefinition(requests * 2, .90m,
                [new(2, 2, 1m)], [new(new(7, 0), new(23, 0), 1m)], .50m),
            Topology = [new("medium", ZoneType.MiddleCenter, spots)],
            Days = [new(1, [], [])]
        };

    internal static ScenarioManifest SmallManifest(int requests, int spots = 2) =>
        new ScenarioMaterializer().Materialize(SmallDefinition(requests, spots));

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static SimulationAttemptRecord Attempt(DailyPlan day, DailyRequestPlan request) =>
        new("scheduler-test", day.FestivalDayId, request.SimulationRequestId, request.Sequence,
            request.Members.Length, request.Members.Select(member => member.Id).ToImmutableArray(),
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, TimeSpan.Zero,
            SimulationAttemptOutcome.Successful, null, null, null, null, null, null, null);
}
