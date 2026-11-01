using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Simulation;
using FluentAssertions;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class SimulationEvidenceExporterTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"festival-evidence-{Guid.NewGuid():N}");
    internal static readonly string[] FileNames =
    [
        "run-metadata.json", "definition.json", "manifest.json", "attempts.json",
        "checkpoints.json", "attendee-paths.json", "recovery-observations.json", "metrics.json"
    ];

    [Fact]
    public async Task ExportAsync_WritesEightReadableUtf8FilesWithExplicitNullsEnumsAndUtcTimestamps()
    {
        var request = Request();

        var directory = await new SimulationEvidenceExporter().ExportAsync(request);

        directory.Should().Be(Path.Combine(root, "minimal-recovery", "run-001"));
        Directory.GetFiles(directory).Select(Path.GetFileName).Should().BeEquivalentTo(FileNames);
        foreach (var file in Directory.GetFiles(directory))
        {
            var bytes = await File.ReadAllBytesAsync(file);
            bytes.Take(3).Should().NotEqual(new byte[] { 0xef, 0xbb, 0xbf });
            var text = new UTF8Encoding(false, true).GetString(bytes);
            text.Should().NotContain("\r").And.EndWith("\n");
            using var json = JsonDocument.Parse(text);
        }
        using var metadata = Read(directory, "run-metadata.json");
        var run = metadata.RootElement;
        run.GetProperty("scenarioDefinitionVersion").ValueKind.Should().Be(JsonValueKind.Null);
        run.GetProperty("metricsFrameworkVersion").ValueKind.Should().Be(JsonValueKind.Null);
        run.GetProperty("repositoryCommitSha").ValueKind.Should().Be(JsonValueKind.Null);
        run.GetProperty("engineVersion").GetString().Should().Be("v0.3.0");
        run.GetProperty("rubricVersion").GetString().Should().Be(PathClassificationRubric.Version);
        run.GetProperty("generatorVersion").GetString().Should().Be(request.Manifest.Metadata.GeneratorVersion);
        run.GetProperty("seed").GetInt32().Should().Be(request.Manifest.Metadata.Seed);
        run.GetProperty("executionMode").GetString().Should().Be("Sequential");
        run.GetProperty("concurrencyLevel").GetInt32().Should().Be(1);
        run.GetProperty("startedAt").GetString().Should().Be("2026-08-15T12:00:00.0000000Z");
        run.GetProperty("completedAt").GetString().Should().Be("2026-08-15T12:01:00.0000000Z");
        using var metrics = Read(directory, "metrics.json");
        var fairness = metrics.RootElement.GetProperty("fairness");
        fairness.GetProperty("balancedPathRate").ValueKind.Should().Be(JsonValueKind.Null);
        fairness.GetProperty("goodOutcomeRate").ValueKind.Should().Be(JsonValueKind.Null);
        var recovery = metrics.RootElement.GetProperty("recovery");
        recovery.GetProperty("recoveryRate").ValueKind.Should().Be(JsonValueKind.Null);
        recovery.GetProperty("averageOpportunitiesToRecovery").ValueKind.Should().Be(JsonValueKind.Null);
        recovery.GetProperty("medianOpportunitiesToRecovery").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ExportAsync_OrdersObservationsPreservesManifestOrderAndDoesNotMutateOrRecalculateEvidence()
    {
        var request = Request();
        var manifest = request.Manifest with { DailyPlans = request.Manifest.DailyPlans.Reverse().ToImmutableArray() };
        var members = manifest.Catalog.Attendees.OrderBy(attendee => attendee.Id.Value).ToArray();
        var days = manifest.Catalog.FestivalDays.OrderBy(day => day.Date).ToArray();
        var entries = Enumerable.Range(0, 4).Select(index => new AttendeePathEntry(
            AssignmentId.Create(Guid.Parse($"10000000-0000-0000-0000-{index + 1:D12}")),
            days[index % 2].Id, days[index % 2].Date,
            request.RunMetadata.StartedAt.AddMinutes(index / 2),
            index % 2 == 0 ? ExperienceQuality.Bad : ExperienceQuality.Good)).ToImmutableArray();
        var paths = members.Reverse().Select(member => new AttendeePathRecord(member.Id,
            entries.Reverse().ToImmutableArray(), 0.123m, 0.456m, OverallQualityBand.Low,
            PathDirection.Negative, PathClassification.NotBalanced)).ToImmutableArray();
        var observations = members.Reverse().Select(member => new AttendeeRecoveryRecord(member.Id,
            [new(member.Id, entries[2], entries[3], 1), new(member.Id, entries[0], entries[1], 1)]))
            .ToImmutableArray();
        var attempts = days.Select((day, index) => Attempt(manifest, day.Id, index + 1)).Reverse().ToArray();
        var checkpoints = days.Select(day => new DailySimulationCheckpoint(manifest.Metadata.ScenarioId,
            day.Id, 1, 1, 0, 2, 2, 2, 6)).Reverse().ToArray();
        var metrics = SimulationMetricsCalculator.Calculate(paths, observations);
        metrics = metrics with { Recovery = metrics.Recovery with
        {
            OpportunityDistribution = [new(3, 7, 0.7m), new(1, 3, 0.3m)]
        } };
        request = request with
        {
            Manifest = manifest,
            RunMetadata = request.RunMetadata with { RepositoryCommitSha = "explicit-source-revision" },
            Result = request.Result with
            {
                Attempts = attempts, DailyCheckpoints = checkpoints, AttendeePaths = paths,
                RecoveryObservations = observations, Metrics = metrics
            }
        };
        var before = JsonSerializer.Serialize(request);

        var exporter = new SimulationEvidenceExporter();
        var directory = await exporter.ExportAsync(request);
        var second = await exporter.ExportAsync(request with { DestinationRoot = Path.Combine(root, "repeat") });

        JsonSerializer.Serialize(request).Should().Be(before);
        foreach (var name in FileNames)
            (await File.ReadAllBytesAsync(Path.Combine(directory, name))).Should()
                .Equal(await File.ReadAllBytesAsync(Path.Combine(second, name)));
        using var manifestJson = Read(directory, "manifest.json");
        manifestJson.RootElement.GetRawText().Should().Be(JsonSerializer.Serialize(manifest, SimulationEvidenceExporter.JsonOptions));
        using var attemptJson = Read(directory, "attempts.json");
        attemptJson.RootElement.EnumerateArray().Select(item => item.GetProperty("attemptSequence").GetInt32())
            .Should().Equal(1, 2);
        attemptJson.RootElement[0].GetProperty("outcome").GetString().Should().Be("Successful");
        attemptJson.RootElement[0].GetProperty("selectedExperienceQuality").GetString().Should().Be("Good");
        using var checkpointJson = Read(directory, "checkpoints.json");
        checkpointJson.RootElement.EnumerateArray().Select(item => Id(item, "festivalDayId"))
            .Should().Equal(days.Select(day => day.Id.Value));
        using var pathJson = Read(directory, "attendee-paths.json");
        pathJson.RootElement.EnumerateArray().Select(item => Id(item, "attendeeId"))
            .Should().Equal(members.Select(member => member.Id.Value));
        foreach (var path in pathJson.RootElement.EnumerateArray())
        {
            path.GetProperty("entries").EnumerateArray().Select(item => Id(item, "assignmentId"))
                .Should().Equal(entries.Select(entry => entry.AssignmentId.Value));
            path.GetProperty("overallQuality").GetDecimal().Should().Be(0.123m);
            path.GetProperty("directionScore").GetDecimal().Should().Be(0.456m);
            path.GetProperty("classification").GetString().Should().Be("NotBalanced");
        }
        using var recoveryJson = Read(directory, "recovery-observations.json");
        recoveryJson.RootElement.EnumerateArray().Select(item => Id(item, "attendeeId"))
            .Should().Equal(members.Select(member => member.Id.Value));
        foreach (var record in recoveryJson.RootElement.EnumerateArray())
            record.GetProperty("episodes").EnumerateArray().Select(item => Id(item, "startAssignmentId"))
                .Should().Equal(entries[0].AssignmentId.Value, entries[2].AssignmentId.Value);
        using var metricsJson = Read(directory, "metrics.json");
        metricsJson.RootElement.GetProperty("recovery").GetProperty("opportunityDistribution").EnumerateArray()
            .Select(item => item.GetProperty("opportunities").GetInt32()).Should().Equal(1, 3);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../escape")]
    [InlineData("/absolute")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("with space")]
    [InlineData("a:b")]
    [InlineData("é")]
    public async Task ExportAsync_RejectsUnsafeScenarioAndRunIdentifiers(string identifier)
    {
        var request = Request();
        var exporter = new SimulationEvidenceExporter();
        var invalidRun = () => exporter.ExportAsync(request with
        {
            RunMetadata = request.RunMetadata with { RunId = identifier }
        });
        var invalidScenario = () => exporter.ExportAsync(request with
        {
            RunMetadata = request.RunMetadata with { ScenarioId = identifier }
        });

        await invalidRun.Should().ThrowAsync<ArgumentException>();
        await invalidScenario.Should().ThrowAsync<ArgumentException>();
        Directory.Exists(root).Should().BeFalse();
    }

    [Fact]
    public async Task ExportAsync_RejectsOverlongIdentifier()
    {
        var request = Request();
        var export = () => new SimulationEvidenceExporter().ExportAsync(request with
        {
            RunMetadata = request.RunMetadata with { RunId = new string('a', 129) }
        });

        await export.Should().ThrowAsync<ArgumentException>();
        Directory.Exists(root).Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExportAsync_RejectsExistingEmptyOrPopulatedRunWithoutChangingFiles(bool populated)
    {
        var request = Request();
        var directory = Path.Combine(root, "minimal-recovery", "run-001");
        Directory.CreateDirectory(directory);
        var sentinel = Path.Combine(directory, "metrics.json");
        if (populated) await File.WriteAllTextAsync(sentinel, "original evidence");

        var export = () => new SimulationEvidenceExporter().ExportAsync(request);

        await export.Should().ThrowAsync<IOException>();
        Directory.GetFiles(directory).Should().HaveCount(populated ? 1 : 0);
        if (populated) (await File.ReadAllTextAsync(sentinel)).Should().Be("original evidence");
        Directory.GetDirectories(Path.GetDirectoryName(directory)!).Should().Equal(directory);
    }

    [Fact]
    public async Task ExportAsync_SerializationFailureLeavesNoPartialRunAndAllowsRetry()
    {
        var request = Request();
        var attempt = Attempt(request.Manifest, request.Manifest.DailyPlans[0].FestivalDayId, 1)
            with { Outcome = (SimulationAttemptOutcome)999 };
        var export = () => new SimulationEvidenceExporter().ExportAsync(request with
        {
            Result = request.Result with { Attempts = new[] { attempt } }
        });

        await export.Should().ThrowAsync<JsonException>();

        Directory.GetFileSystemEntries(Path.Combine(root, "minimal-recovery")).Should().BeEmpty();
        var directory = await new SimulationEvidenceExporter().ExportAsync(request);
        Directory.GetFiles(directory).Should().HaveCount(8);
    }

    [Fact]
    public async Task ExportAsync_CancellationDoesNotPublishEvidence()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var export = () => new SimulationEvidenceExporter().ExportAsync(Request(), cancellation.Token);

        await export.Should().ThrowAsync<OperationCanceledException>();
        Directory.Exists(root).Should().BeFalse();
    }

    [Fact]
    public async Task ExportAsync_RejectsMismatchedProvenance()
    {
        var request = Request();
        var export = () => new SimulationEvidenceExporter().ExportAsync(request with
        {
            RunMetadata = request.RunMetadata with { Seed = 42 }
        });

        await export.Should().ThrowAsync<ArgumentException>();
        Directory.Exists(root).Should().BeFalse();
    }

    private SimulationEvidenceExportRequest Request()
    {
        var definition = MinimalRecoveryScenarioDefinition.Create();
        var manifest = new ScenarioMaterializer().Materialize(definition);
        var start = new DateTimeOffset(2026, 8, 15, 14, 0, 0, TimeSpan.FromHours(2));
        return new(definition, manifest,
            new SimulationResult([], [], [], [], SimulationMetricsCalculator.Calculate([], []), [], [], [], [], []),
            SimulationRunMetadata.Create("run-001", manifest, start, start.AddMinutes(1)), root);
    }

    private static SimulationAttemptRecord Attempt(ScenarioManifest manifest, FestivalDayId day, int sequence) =>
        new(manifest.Metadata.ScenarioId, day, Guid.Parse($"20000000-0000-0000-0000-{sequence:D12}"),
            sequence, 2, manifest.Catalog.Attendees.Select(member => member.Id).ToImmutableArray(),
            new DateTimeOffset(2026, 8, 15, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 15, 14, 0, 1, TimeSpan.Zero), TimeSpan.FromSeconds(1),
            SimulationAttemptOutcome.Successful, null, null, null, 6, true, ExperienceQuality.Good, null);

    internal static JsonDocument Read(string directory, string file) => JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, file)));
    internal static Guid Id(JsonElement item, string property) => item.GetProperty(property).GetProperty("value").GetGuid();

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
