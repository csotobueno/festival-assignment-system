using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using Festival.Simulation;
using FluentAssertions;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class ArrivalConcurrencyTests
{
    [Theory]
    [InlineData(ArrivalPattern.Moderate, new[] { 0, 25, 50, 75, 100, 125, 150, 175, 200 })]
    [InlineData(ArrivalPattern.BurstLike, new[] { 0, 5, 10, 15, 65, 70, 75, 80, 130 })]
    public void Schedule_UsesContractOffsets(ArrivalPattern pattern, int[] expected)
    {
        var schedule = new ArrivalSchedule(pattern);
        Enumerable.Range(0, expected.Length).Select(i => schedule.OffsetAt(i).TotalMilliseconds)
            .Should().Equal(expected.Select(i => (double)i));
        var invalid = () => schedule.OffsetAt(-1);
        invalid.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(ArrivalPattern.Moderate, 1)]
    [InlineData(ArrivalPattern.Moderate, 7)]
    [InlineData(ArrivalPattern.Moderate, 1464)]
    [InlineData(ArrivalPattern.BurstLike, 1)]
    [InlineData(ArrivalPattern.BurstLike, 7)]
    [InlineData(ArrivalPattern.BurstLike, 1464)]
    public void Schedule_IsDeterministicAndCoversEveryRequestWithoutPadding(ArrivalPattern pattern, int count)
    {
        var source = S8ConcurrentExecutionTests.SmallManifest(count);
        var day = source.DailyPlans[0] with { Requests = source.DailyPlans[0].Requests.Reverse().ToImmutableArray() };
        var schedule = new ArrivalSchedule(pattern);
        var planned = ArrivalSimulationExecutor.CreateSchedule(day, schedule);
        planned.Select(p => p.Request).Should().Equal(source.DailyPlans[0].Requests);
        planned.Select(p => p.Request.SimulationRequestId).Should().OnlyHaveUniqueItems().And.HaveCount(count);
        planned.Should().Equal(ArrivalSimulationExecutor.CreateSchedule(day, new(pattern)));
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(2001)]
    [InlineData(3001)]
    public void Create_PreservesExactBusinessInputsAndOnlyChangesExecutionMetadata(int seed)
    {
        var source = new ScenarioMaterializer().Materialize(S2ExpectedBaselineScenarioDefinition.Create(seed, new(2026, 8, 15)));
        var before = JsonSerializer.Serialize(source, SimulationEvidenceExporter.JsonOptions);
        foreach (var pattern in Enum.GetValues<ArrivalPattern>())
        {
            var manifest = ArrivalDrivenExecution.Create(source, pattern);
            manifest.Catalog.Should().BeSameAs(source.Catalog);
            manifest.Setup.Should().BeSameAs(source.Setup);
            manifest.DailyPlans.Should().Equal(source.DailyPlans);
            manifest.Metadata.ScenarioId.Should().Be(source.Metadata.ScenarioId);
            manifest.Metadata.Seed.Should().Be(seed);
            manifest.Metadata.ConcurrencyLevel.Should().Be(0);
            manifest.Metadata.Repetition.Should().BeNull();
            manifest.Metadata.ArrivalSchedule!.Pattern.Should().Be(pattern);
            var configuration = new SimulationExecutionConfiguration(manifest.Metadata.ExecutionMode,
                manifest.Metadata.ConcurrencyLevel, arrivalSchedule: manifest.Metadata.ArrivalSchedule);
            configuration.ExecutionMode.Should().Be(SimulationExecutionMode.ArrivalDriven);
        }
        JsonSerializer.Serialize(source, SimulationEvidenceExporter.JsonOptions).Should().Be(before);
    }

    [Fact]
    public void Create_RejectsTransformedInputsAndInvalidTimingConfiguration()
    {
        var source = S8ConcurrentExecutionTests.SmallManifest(3);
        var create = () => ArrivalDrivenExecution.Create(S5ReversedProcessingOrder.Create(source), ArrivalPattern.Moderate);
        create.Should().Throw<ArgumentException>();
        create = () => ArrivalDrivenExecution.Create(source with
        {
            DailyPlans = [source.DailyPlans[0] with { Requests = [source.DailyPlans[0].Requests[0] with { ExecutionSequence = 99 }] }]
        }, ArrivalPattern.Moderate);
        create.Should().Throw<ArgumentException>();
        var invalidPattern = () => new ArrivalSchedule((ArrivalPattern)99);
        invalidPattern.Should().Throw<ArgumentOutOfRangeException>();
        var noSchedule = () => new SimulationExecutionConfiguration(SimulationExecutionMode.ArrivalDriven, 0);
        noSchedule.Should().Throw<NotSupportedException>();
        var fixedLevel = () => new SimulationExecutionConfiguration(SimulationExecutionMode.ArrivalDriven, 4, arrivalSchedule: new(ArrivalPattern.Moderate));
        fixedLevel.Should().Throw<NotSupportedException>();
        var repeated = () => new SimulationExecutionConfiguration(SimulationExecutionMode.ArrivalDriven, 0, 1, new(ArrivalPattern.Moderate));
        repeated.Should().Throw<NotSupportedException>();
        var mixed = () => new SimulationExecutionConfiguration(SimulationExecutionMode.Concurrent, 4, arrivalSchedule: new(ArrivalPattern.Moderate));
        mixed.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(ArrivalPattern.Moderate)]
    [InlineData(ArrivalPattern.BurstLike)]
    public async Task ExecuteDayAsync_DoesNotWaitForEarlierRequestsOrClustersToDrain(ArrivalPattern pattern)
    {
        var day = S8ConcurrentExecutionTests.SmallManifest(9).DailyPlans[0];
        var allEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = 0;
        var run = ArrivalSimulationExecutor.ExecuteDayAsync(day, new(pattern), Stopwatch.GetTimestamp(),
            async (request, scheduled, token) =>
            {
                if (Interlocked.Increment(ref entered) == day.Requests.Length) allEntered.SetResult();
                await finish.Task.WaitAsync(token);
                return Attempt(0, 1) with { SimulationRequestId = request.SimulationRequestId,
                    AttemptSequence = request.Sequence, BaselineSequence = request.Sequence, ScheduledArrivalOffset = scheduled };
            });
        try
        {
            await allEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            run.IsCompleted.Should().BeFalse("every callback is still active, including the first cluster");
        }
        finally { finish.TrySetResult(); }
        var attempts = await run.WaitAsync(TimeSpan.FromSeconds(10));
        attempts.Select(a => a.SimulationRequestId).Should().Equal(day.Requests.Select(p => p.SimulationRequestId));
        for (var i = 0; i < attempts.Count; i++)
            (attempts[i].ScheduledArrivalOffset - attempts[0].ScheduledArrivalOffset).Should().Be(new ArrivalSchedule(pattern).OffsetAt(i));
    }

    [Fact]
    public async Task ExecuteDayAsync_CancellationDrainsStartedWorkWithoutLaunchingTheWholeDay()
    {
        var day = S8ConcurrentExecutionTests.SmallManifest(20).DailyPlans[0];
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var drained = 0;
        var run = ArrivalSimulationExecutor.ExecuteDayAsync(day, new(ArrivalPattern.Moderate), Stopwatch.GetTimestamp(),
            async (request, scheduled, token) =>
            {
                entered.TrySetResult();
                try { await finish.Task; return Attempt(0, 1); }
                finally { Interlocked.Increment(ref drained); }
            }, cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cancellation.Cancel();
        run.IsCompleted.Should().BeFalse("already-owned request work must drain");
        finish.SetResult();
        var action = async () => await run.WaitAsync(TimeSpan.FromSeconds(10));
        await action.Should().ThrowAsync<OperationCanceledException>();
        drained.Should().BeGreaterThan(0).And.BeLessThan(20);
    }

    [Theory]
    [InlineData(new[] { 0, 20, 40 }, new[] { 10, 10, 10 }, 1)]
    [InlineData(new[] { 0, 5, 15 }, new[] { 10, 15, 10 }, 2)]
    [InlineData(new[] { 0, 0, 0 }, new[] { 10, 10, 10 }, 3)]
    [InlineData(new[] { 0, 10 }, new[] { 10, 10 }, 1)]
    [InlineData(new[] { 0, 5 }, new[] { 0, 0 }, 0)]
    public void MaxActiveRequests_UsesActualHalfOpenIntervals(int[] starts, int[] durations, int expected)
    {
        var attempts = starts.Select((s, i) => Attempt(s, durations[i]) with { ScheduledArrivalOffset = TimeSpan.Zero });
        SimulationOverlapAnalyzer.MaxActiveRequests(attempts).Should().Be(expected);
    }

    [Fact]
    public void MaxActiveRequests_RequiresTimingAndExcludesInvalidInput()
    {
        SimulationOverlapAnalyzer.MaxActiveRequests([]).Should().Be(0);
        SimulationOverlapAnalyzer.MaxActiveRequests([Attempt(0, 10), Attempt(0, 10) with
            { Outcome = SimulationAttemptOutcome.ExcludedInvalidInput, ProcessingStartedOffset = null }]).Should().Be(1);
        var missing = () => SimulationOverlapAnalyzer.MaxActiveRequests([Attempt(0, 10) with { ProcessingStartedOffset = null }]);
        missing.Should().Throw<ArgumentException>();
        var negative = () => SimulationOverlapAnalyzer.MaxActiveRequests([Attempt(0, -1)]);
        negative.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(ArrivalPattern.Moderate)]
    [InlineData(ArrivalPattern.BurstLike)]
    public async Task RunAsync_ExportsScheduleAndActualTimingThroughRealPostgreSql(ArrivalPattern pattern)
    {
        var definition = S8ConcurrentExecutionTests.SmallDefinition(7, 6);
        definition = definition with { Days = [definition.Days[0], new(2, [], [])] };
        var source = new ScenarioMaterializer().Materialize(definition);
        var manifest = ArrivalDrivenExecution.Create(source, pattern);
        var started = DateTimeOffset.UtcNow;
        var result = await MinimalSimulationRunner.RunAsync(manifest);
        result.Attempts.Should().HaveCount(14).And.OnlyContain(a => a.ScheduledArrivalOffset.HasValue
            && a.ProcessingStartedOffset >= a.ScheduledArrivalOffset && a.WindowNumber == null && a.WindowPosition == null
            && a.Outcome != SimulationAttemptOutcome.UnexpectedTechnicalFailure);
        result.DailyCheckpoints.Should().HaveCount(2);
        result.Assignments.GroupBy(a => (a.FestivalDayId, a.SpotCode)).Should().OnlyContain(g => g.Count() == 1);
        result.Assignments.GroupBy(a => (a.FestivalDayId, a.AttendeeId)).Should().OnlyContain(g => g.Count() == 1);
        result.AttendeePaths.Sum(p => p.PathLength).Should().Be(result.Assignments.Count);
        // A failed observer read can follow a successfully committed request.
        // Preserve the unexpected outcome; correlate only a complete final graph.
        var committed = result.Attempts.First(a => a.Outcome == SimulationAttemptOutcome.Successful);
        var observationFailed = committed with { Outcome = SimulationAttemptOutcome.UnexpectedTechnicalFailure,
            DiagnosticCode = "RECONCILIATION_FAILED", PersistedRequestId = null, SelectedExperienceQuality = null,
            FeasibleContiguousBlockFound = true };
        var observedResult = result with { Attempts = result.Attempts.Select(a => a == committed ? observationFailed : a).ToArray() };
        var resolutions = PostCommitObservationResolver.Resolve(manifest, observedResult);
        resolutions[committed.SimulationRequestId].Should().Be(committed.PersistedRequestId!.Value);
        observedResult.Attempts.Single(a => a.SimulationRequestId == committed.SimulationRequestId).Outcome
            .Should().Be(SimulationAttemptOutcome.UnexpectedTechnicalFailure);
        PostCommitObservationResolver.Resolve(manifest, observedResult with
        {
            Requests = result.Requests.Select(r => r.AssignmentRequestId == committed.PersistedRequestId.Value.Value
                ? r with { Status = "Rejected" } : r).ToArray()
        }).Should().BeEmpty("a graph with inconsistent persisted request status cannot be accepted");
        PostCommitObservationResolver.Resolve(manifest, observedResult with
        {
            Assignments = result.Assignments.Where(a => a.Id != result.Assignments.First(a => a.AssignmentRequestId == committed.PersistedRequestId).Id).ToArray()
        }).Should().BeEmpty("a partial graph must remain an audit finding");
        PostCommitObservationResolver.Resolve(manifest, observedResult with
        {
            Attempts = observedResult.Attempts.Select(a => a == observationFailed ? a with
                { Outcome = SimulationAttemptOutcome.KnownConcurrencyConflict } : a).ToArray()
        }).Should().BeEmpty("known losing races cannot be correlated as committed observer failures");
        foreach (var day in manifest.DailyPlans)
        {
            var attempts = result.Attempts.Where(a => a.FestivalDayId == day.FestivalDayId).ToArray();
            for (var i = 0; i < attempts.Length; i++)
                (attempts[i].ScheduledArrivalOffset - attempts[0].ScheduledArrivalOffset).Should().Be(new ArrivalSchedule(pattern).OffsetAt(i));
        }
        var root = Path.Combine(Path.GetTempPath(), $"festival-arrival-test-{Guid.NewGuid():N}");
        try
        {
            var metadata = SimulationRunMetadata.Create("arrival-test", manifest, started, DateTimeOffset.UtcNow);
            var directory = await new SimulationEvidenceExporter().ExportAsync(new(definition, manifest, result, metadata, root));
            Directory.GetFiles(directory).Select(Path.GetFileName).Should().BeEquivalentTo(SimulationEvidenceExporterTests.FileNames);
            using var input = SimulationEvidenceExporterTests.Read(directory, "definition.json");
            input.RootElement.GetProperty("executionMode").GetString().Should().Be("Sequential");
            foreach (var file in new[] { "run-metadata.json", "manifest.json" })
            {
                using var document = SimulationEvidenceExporterTests.Read(directory, file);
                var data = file == "manifest.json" ? document.RootElement.GetProperty("metadata") : document.RootElement;
                data.GetProperty("arrivalSchedule").GetProperty("pattern").GetString().Should().Be(pattern.ToString());
                data.GetProperty("executionMode").GetString().Should().Be("ArrivalDriven");
            }
            var attempts = JsonSerializer.Deserialize<SimulationAttemptRecord[]>(
                await File.ReadAllTextAsync(Path.Combine(directory, "attempts.json")), SimulationEvidenceExporter.JsonOptions)!;
            attempts.Should().BeEquivalentTo(result.Attempts, options => options.WithStrictOrdering());
            SimulationOverlapAnalyzer.MaxActiveRequests(attempts).Should().BeGreaterThanOrEqualTo(1);
            var mismatch = metadata with { RunId = "mismatch", ArrivalSchedule = new(pattern == ArrivalPattern.Moderate ? ArrivalPattern.BurstLike : ArrivalPattern.Moderate) };
            var export = async () => await new SimulationEvidenceExporter().ExportAsync(new(definition, manifest, result, mismatch, root));
            await export.Should().ThrowAsync<ArgumentException>();
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static SimulationAttemptRecord Attempt(int startMs, int durationMs) => new("overlap", default,
        Guid.Empty, 1, 1, [], DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch,
        TimeSpan.FromMilliseconds(durationMs), SimulationAttemptOutcome.Successful,
        null, null, null, null, null, null, null) { ProcessingStartedOffset = TimeSpan.FromMilliseconds(startMs) };
}
