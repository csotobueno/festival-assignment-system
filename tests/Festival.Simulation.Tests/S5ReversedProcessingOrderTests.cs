using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using Festival.Simulation;
using FluentAssertions;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class S5ReversedProcessingOrderTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"festival-s5-test-{Guid.NewGuid():N}");

    [Theory]
    [InlineData(1001)]
    [InlineData(2001)]
    [InlineData(3001)]
    public void Create_PreservesExactS2WorkloadAndReversesEveryDailyExecutionOrder(int seed)
    {
        var definition = Definition(seed);
        var s2 = new ScenarioMaterializer().Materialize(definition);
        var originalHash = Hash(s2);

        var s5 = S5ReversedProcessingOrder.Create(s2);

        Hash(s2).Should().Equal(originalHash, "transformation must not mutate its S2 control");
        s5.Catalog.Should().BeSameAs(s2.Catalog, "attendees, days, Zones, rows, Spots and IDs are reused");
        s5.Setup.Should().BeSameAs(s2.Setup);
        s5.Metadata.Should().Be(s2.Metadata with
        {
            ScenarioId = S5ReversedProcessingOrder.ScenarioId,
            WorkloadSourceScenarioId = s2.Metadata.ScenarioId,
            OrderingTransformation = S5ReversedProcessingOrder.Transformation
        });
        s5.DailyPlans.Select(day => (day.DayNumber, day.FestivalDayId)).Should()
            .Equal(s2.DailyPlans.Select(day => (day.DayNumber, day.FestivalDayId)));

        foreach (var (baselineDay, transformedDay) in s2.DailyPlans.Zip(s5.DailyPlans))
        {
            transformedDay.Requests.Should().HaveCount(baselineDay.Requests.Length);
            transformedDay.Requests.Select(request => request.Sequence).Should()
                .Equal(baselineDay.Requests.Select(request => request.Sequence), "baseline order remains recoverable");
            var byId = transformedDay.Requests.ToDictionary(request => request.SimulationRequestId);
            foreach (var baseline in baselineDay.Requests)
            {
                var transformed = byId[baseline.SimulationRequestId];
                // Record equality checks every field, including any added later.
                (transformed with { ExecutionSequence = baseline.ExecutionSequence }).Should()
                    .Be(baseline with { ExecutionSequence = baseline.ExecutionSequence });
                transformed.Members.Select(member => (member.Id, member.Code)).Should()
                    .Equal(baseline.Members.Select(member => (member.Id, member.Code)));
                transformed.Eligibility.Should().Be(baseline.Eligibility);
                transformed.RequestedAt.Should().Be(baseline.RequestedAt);
                transformed.AssignedAt.Should().Be(baseline.AssignedAt).And.Be(transformed.RequestedAt);
                var bands = definition.Workload!.ArrivalProfile;
                var originalBand = bands.Single(band => TimeOnly.FromDateTime(baseline.RequestedAt.UtcDateTime) >= band.Start
                    && TimeOnly.FromDateTime(baseline.RequestedAt.UtcDateTime) < band.End);
                var actualBand = bands.Single(band => TimeOnly.FromDateTime(transformed.RequestedAt.UtcDateTime) >= band.Start
                    && TimeOnly.FromDateTime(transformed.RequestedAt.UtcDateTime) < band.End);
                actualBand.Should().Be(originalBand);
            }

            var executed = transformedDay.Requests.OrderBy(request => request.ExecutionSequence).ToArray();
            executed.Select(request => request.SimulationRequestId).Should()
                .Equal(baselineDay.Requests.Reverse().Select(request => request.SimulationRequestId));
            executed[0].SimulationRequestId.Should().Be(baselineDay.Requests[^1].SimulationRequestId);
            executed[^1].SimulationRequestId.Should().Be(baselineDay.Requests[0].SimulationRequestId);
            for (var index = 0; index < baselineDay.Requests.Length; index++)
                Array.FindIndex(executed, request => request.SimulationRequestId == baselineDay.Requests[index].SimulationRequestId)
                    .Should().Be(baselineDay.Requests.Length - 1 - index);
            transformedDay.Requests.SelectMany(request => request.Members).Select(member => member.Id)
                .Should().BeEquivalentTo(baselineDay.Requests.SelectMany(request => request.Members).Select(member => member.Id));
            transformedDay.Requests.SelectMany(request => request.Members).Select(member => member.Id)
                .Distinct().Should().HaveCount(4400);
        }
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(2001)]
    [InlineData(3001)]
    public void Create_RepeatedS2MaterializationReproducesExactS5InputsAndBothOrders(int seed)
    {
        var materializer = new ScenarioMaterializer();
        var first = materializer.Materialize(Definition(seed));
        var second = materializer.Materialize(Definition(seed));

        Hash(second).Should().Equal(Hash(first));
        Hash(S5ReversedProcessingOrder.Create(second)).Should().Equal(Hash(S5ReversedProcessingOrder.Create(first)));
        first.DailyPlans.SelectMany(day => day.Requests).Should()
            .OnlyContain(request => request.ExecutionSequence == request.Sequence);
    }

    [Fact]
    public void Create_RejectsNullInput()
    {
        var create = () => S5ReversedProcessingOrder.Create(null!);
        create.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_RejectsAnotherScenarioOrAlreadyTransformedWorkload()
    {
        var s2 = new ScenarioMaterializer().Materialize(Definition());
        var other = () => S5ReversedProcessingOrder.Create(s2 with
        {
            Metadata = s2.Metadata with { ScenarioId = "s1-best-plausible" }
        });
        var repeated = () => S5ReversedProcessingOrder.Create(S5ReversedProcessingOrder.Create(s2));

        other.Should().Throw<ArgumentException>();
        repeated.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_RejectsAmbiguousBaselineOrPreviouslyChangedExecutionOrder(bool duplicateBaseline)
    {
        var s2 = new ScenarioMaterializer().Materialize(Definition());
        var day = s2.DailyPlans[0];
        var altered = duplicateBaseline
            ? day.Requests[1] with { Sequence = day.Requests[0].Sequence, ExecutionSequence = day.Requests[0].Sequence }
            : day.Requests[1] with { ExecutionSequence = day.Requests[0].ExecutionSequence };
        var invalid = s2 with
        {
            DailyPlans = s2.DailyPlans.SetItem(0, day with { Requests = day.Requests.SetItem(1, altered) })
        };

        var create = () => S5ReversedProcessingOrder.Create(invalid);
        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_RejectsNonSequentialExecution()
    {
        var s2 = new ScenarioMaterializer().Materialize(Definition());
        var create = () => S5ReversedProcessingOrder.Create(s2 with
        {
            Metadata = s2.Metadata with { ConcurrencyLevel = 2 }
        });

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task RunAsync_S5RepresentativeSliceDispatchesByExecutionOrderAndExportsBothSequences()
    {
        var definition = Definition();
        var s2 = new ScenarioMaterializer().Materialize(definition);
        // Six real representative requests over two days; never an official S5 run.
        var slice = s2 with
        {
            DailyPlans = s2.DailyPlans.Take(2).Select(day => day with
            {
                Requests = [day.Requests[0], day.Requests[day.Requests.Length / 2], day.Requests[^1]]
            }).ToImmutableArray()
        };
        var s5 = S5ReversedProcessingOrder.Create(slice);

        var result = await MinimalSimulationRunner.RunAsync(s5);

        result.Attempts.Should().HaveCount(6).And.OnlyContain(attempt => attempt.Outcome == SimulationAttemptOutcome.Successful);
        foreach (var (baselineDay, executedDay) in slice.DailyPlans.Zip(s5.DailyPlans))
        {
            var attempts = result.Attempts.Where(attempt => attempt.FestivalDayId == executedDay.FestivalDayId).ToArray();
            attempts.Select(attempt => attempt.SimulationRequestId).Should()
                .Equal(baselineDay.Requests.Reverse().Select(request => request.SimulationRequestId));
            attempts.Select(attempt => attempt.AttemptSequence).Should()
                .Equal(baselineDay.Requests.Select(request => request.Sequence));
            attempts.Select(attempt => attempt.BaselineSequence).Should()
                .Equal(baselineDay.Requests.Reverse().Select(request => request.Sequence));
            attempts[0].SimulationRequestId.Should().Be(baselineDay.Requests[^1].SimulationRequestId);
            result.DailyCheckpoints.Single(checkpoint => checkpoint.FestivalDayId == executedDay.FestivalDayId)
                .EvaluatedRequestCount.Should().Be(3);
            foreach (var attempt in attempts)
            {
                var plan = baselineDay.Requests.Single(request => request.SimulationRequestId == attempt.SimulationRequestId);
                attempt.ScenarioId.Should().Be(S5ReversedProcessingOrder.ScenarioId);
                attempt.AttendeeIds.Should().Equal(plan.Members.Select(member => member.Id));
                var persisted = result.Assignments.Where(assignment => assignment.AssignmentRequestId == attempt.PersistedRequestId).ToArray();
                persisted.Select(assignment => assignment.AttendeeId).Should().BeEquivalentTo(plan.Members.Select(member => member.Id));
                foreach (var assignment in persisted)
                    assignment.AssignedAt.Should().BeCloseTo(plan.AssignedAt, TimeSpan.FromTicks(9));
            }
        }

        var start = new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero);
        var metadata = SimulationRunMetadata.Create("test-s5-slice", s5, start, start.AddMinutes(1));
        var export = new SimulationEvidenceExportRequest(definition, s5, result, metadata, root);
        var directory = await new SimulationEvidenceExporter().ExportAsync(export);
        var repeatedDirectory = await new SimulationEvidenceExporter().ExportAsync(export with { DestinationRoot = Path.Combine(root, "repeat") });

        directory.Should().Be(Path.Combine(root, S5ReversedProcessingOrder.ScenarioId, "test-s5-slice"));
        foreach (var file in SimulationEvidenceExporterTests.FileNames)
            (await File.ReadAllBytesAsync(Path.Combine(directory, file))).Should()
                .Equal(await File.ReadAllBytesAsync(Path.Combine(repeatedDirectory, file)));
        using var manifestJson = SimulationEvidenceExporterTests.Read(directory, "manifest.json");
        manifestJson.RootElement.GetRawText().Should().Be(JsonSerializer.Serialize(s5, SimulationEvidenceExporter.JsonOptions));
        using var runJson = SimulationEvidenceExporterTests.Read(directory, "run-metadata.json");
        runJson.RootElement.GetProperty("scenarioId").GetString().Should().Be(S5ReversedProcessingOrder.ScenarioId);
        runJson.RootElement.GetProperty("workloadSourceScenarioId").GetString().Should().Be(definition.ScenarioId);
        runJson.RootElement.GetProperty("orderingTransformation").GetString().Should().Be(S5ReversedProcessingOrder.Transformation);
        runJson.RootElement.GetProperty("generatorVersion").GetString().Should().Be("v2");
        using var definitionJson = SimulationEvidenceExporterTests.Read(directory, "definition.json");
        definitionJson.RootElement.GetProperty("scenarioId").GetString().Should().Be(definition.ScenarioId);
        using var attemptJson = SimulationEvidenceExporterTests.Read(directory, "attempts.json");
        attemptJson.RootElement.EnumerateArray().Select(item => item.GetProperty("simulationRequestId").GetGuid())
            .Should().Equal(result.Attempts.Select(attempt => attempt.SimulationRequestId));
        attemptJson.RootElement.EnumerateArray().Select(item => item.GetProperty("baselineSequence").GetInt32())
            .Should().Equal(result.Attempts.Select(attempt => attempt.BaselineSequence));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExportAsync_RejectsMismatchedS5SourceOrTransformation(bool changeSource)
    {
        var definition = Definition();
        var s5 = S5ReversedProcessingOrder.Create(new ScenarioMaterializer().Materialize(definition));
        var start = new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero);
        var metadata = SimulationRunMetadata.Create("test-invalid-source", s5, start, start);
        var manifest = s5 with
        {
            Metadata = changeSource
                ? s5.Metadata with { WorkloadSourceScenarioId = "s1-best-plausible" }
                : s5.Metadata with { OrderingTransformation = "other-order" }
        };
        var result = new SimulationResult([], [], [], [], SimulationMetricsCalculator.Calculate([], []), [], [], [], [], []);
        var export = () => new SimulationEvidenceExporter().ExportAsync(new(definition, manifest, result, metadata, root));

        await export.Should().ThrowAsync<ArgumentException>();
        Directory.Exists(root).Should().BeFalse();
    }

    private static ScenarioDefinition Definition(int seed = 1001) =>
        S2ExpectedBaselineScenarioDefinition.Create(seed, new DateOnly(2026, 8, 15));

    private static byte[] Hash(ScenarioManifest manifest) =>
        SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(manifest, SimulationEvidenceExporter.JsonOptions));

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
