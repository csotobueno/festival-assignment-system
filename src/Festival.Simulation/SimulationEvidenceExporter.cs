using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Festival.Simulation;

public sealed class SimulationEvidenceExporter
{
    internal static JsonSerializerOptions JsonOptions { get; } = CreateJsonOptions();

    // A completed directory is published only after all eight files are closed.
    // Any existing destination (even an empty directory) is rejected.
    public async Task<string> ExportAsync(SimulationEvidenceExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateIdentifier(request.RunMetadata.ScenarioId, "ScenarioId");
        ValidateIdentifier(request.RunMetadata.RunId, "RunId");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DestinationRoot);
        var metadata = request.RunMetadata;
        var input = request.Manifest.Metadata;
        var definitionScenarioId = input.ScenarioId;
        _ = new SimulationExecutionConfiguration(input.ExecutionMode, input.ConcurrencyLevel, input.Repetition);
        var isS8 = input.ScenarioId is S8ConcurrentExecution.LowScenarioId or S8ConcurrentExecution.ModerateScenarioId;
        var isS7 = input.ScenarioId is S7ScenarioPair.ControlScenarioId or S7ScenarioPair.FragmentedScenarioId;
        if (isS8)
        {
            var expectedLevel = input.ScenarioId == S8ConcurrentExecution.LowScenarioId ? 4 : 16;
            if (input.WorkloadSourceScenarioId != S8ConcurrentExecution.WorkloadSourceScenarioId
                || input.OrderingTransformation is not null || input.GeneratorVersion != "v2"
                || input.ExecutionMode != SimulationExecutionMode.Concurrent || input.ConcurrencyLevel != expectedLevel
                || input.Repetition is null || input.FragmentationPatternVersion is not null
                || input.OpeningOccupiedSpotCount is not null || input.WorkloadSourceManifestSha256 is not null
                || !request.Manifest.Setup.SupportingAttendees.IsEmpty || !request.Manifest.Setup.CompletedRequests.IsEmpty
                || !request.Manifest.Setup.Assignments.IsEmpty)
                throw new ArgumentException("Unsupported S8 execution or workload provenance.", nameof(request));
            definitionScenarioId = input.WorkloadSourceScenarioId;
        }
        else if (isS7)
        {
            if (input.WorkloadSourceScenarioId != S7ScenarioPair.WorkloadSourceScenarioId
                || input.OrderingTransformation is not null
                || input.FragmentationPatternVersion != S7ScenarioPair.PatternVersion
                || input.OpeningOccupiedSpotCount != S7ScenarioPair.OpeningOccupiedSpotCount
                || input.WorkloadSourceManifestSha256 is not { Length: 64 } hash
                || !hash.All(char.IsAsciiHexDigit)
                || input.GeneratorVersion != "v2"
                || input.ExecutionMode != SimulationExecutionMode.Sequential || input.ConcurrencyLevel != 1)
                throw new ArgumentException("Unsupported S7 setup or workload provenance.", nameof(request));
            definitionScenarioId = input.WorkloadSourceScenarioId;
        }
        else if (input.FragmentationPatternVersion is not null || input.OpeningOccupiedSpotCount is not null
            || input.WorkloadSourceManifestSha256 is not null)
            throw new ArgumentException("Pre-occupation provenance requires an S7 variant.", nameof(request));
        else if (input.WorkloadSourceScenarioId is not null || input.OrderingTransformation is not null)
        {
            if (input.ScenarioId != S5ReversedProcessingOrder.ScenarioId
                || input.WorkloadSourceScenarioId != S5ReversedProcessingOrder.WorkloadSourceScenarioId
                || input.OrderingTransformation != S5ReversedProcessingOrder.Transformation)
                throw new ArgumentException("Unsupported workload source or ordering transformation.", nameof(request));
            // definition.json retains the S2 source, while manifest/run metadata
            // identify S5. Never rename/rematerialize the source definition.
            definitionScenarioId = input.WorkloadSourceScenarioId;
        }
        if (metadata.ScenarioId != input.ScenarioId || metadata.Seed != input.Seed
            || metadata.GeneratorVersion != input.GeneratorVersion
            || metadata.ExecutionMode != input.ExecutionMode || metadata.ConcurrencyLevel != input.ConcurrencyLevel
            || metadata.Repetition != input.Repetition
            || metadata.WorkloadSourceScenarioId != input.WorkloadSourceScenarioId
            || metadata.OrderingTransformation != input.OrderingTransformation
            || metadata.FragmentationPatternVersion != input.FragmentationPatternVersion
            || metadata.OpeningOccupiedSpotCount != input.OpeningOccupiedSpotCount
            || metadata.WorkloadSourceManifestSha256 != input.WorkloadSourceManifestSha256
            || request.Definition.ScenarioId != definitionScenarioId || request.Definition.Seed != input.Seed
            || request.Definition.GeneratorVersion != input.GeneratorVersion
            || request.Definition.ExecutionMode != (isS8 ? SimulationExecutionMode.Sequential : input.ExecutionMode)
            || request.Definition.ConcurrencyLevel != (isS8 ? 1 : input.ConcurrencyLevel))
            throw new ArgumentException("Definition, manifest and run metadata must describe the same scenario.", nameof(request));
        if (metadata.CompletedAt < metadata.StartedAt)
            throw new ArgumentException("Run completion must not precede its start.", nameof(request));

        var parent = Path.GetFullPath(Path.Combine(request.DestinationRoot, metadata.ScenarioId));
        var destination = Path.Combine(parent, metadata.RunId);
        if (Directory.Exists(destination) || File.Exists(destination))
            throw new IOException($"Evidence run already exists: {destination}");

        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(parent);
        var staging = Path.Combine(parent, $".{metadata.RunId}.{Guid.NewGuid():N}.tmp");
        Directory.CreateDirectory(staging);
        try
        {
            var result = request.Result;
            var dates = request.Manifest.Catalog.FestivalDays.ToDictionary(day => day.Id, day => day.Date);
            await WriteAsync("run-metadata.json", metadata);
            await WriteAsync("definition.json", request.Definition);
            // Serialize the consumed instance directly; never rematerialize or reorder it.
            await WriteAsync("manifest.json", request.Manifest);
            await WriteAsync("attempts.json", result.Attempts.OrderBy(attempt => attempt.AttemptSequence)
                .ThenBy(attempt => attempt.SimulationRequestId).ToArray());
            await WriteAsync("checkpoints.json", result.DailyCheckpoints
                .OrderBy(checkpoint => dates[checkpoint.FestivalDayId])
                .ThenBy(checkpoint => checkpoint.FestivalDayId.Value).ToArray());
            await WriteAsync("attendee-paths.json", result.AttendeePaths.OrderBy(path => path.AttendeeId.Value)
                .Select(path => new AttendeePathRecord(path.AttendeeId,
                    path.Entries.OrderBy(entry => entry.AssignedAt).ThenBy(entry => entry.AssignmentId.Value).ToImmutableArray(),
                    path.OverallQuality, path.DirectionScore, path.OverallQualityBand, path.Direction, path.Classification))
                .ToArray());
            await WriteAsync("recovery-observations.json", result.RecoveryObservations
                .OrderBy(record => record.AttendeeId.Value)
                .Select(record => new AttendeeRecoveryRecord(record.AttendeeId, record.Episodes
                    .OrderBy(episode => episode.StartAssignedAt).ThenBy(episode => episode.StartAssignmentId.Value)
                    .ToImmutableArray())).ToArray());
            await WriteAsync("metrics.json", result.Metrics with
            {
                Recovery = result.Metrics.Recovery with
                {
                    OpportunityDistribution = result.Metrics.Recovery.OpportunityDistribution
                        .OrderBy(bucket => bucket.Opportunities).ToImmutableArray()
                }
            });
            cancellationToken.ThrowIfCancellationRequested();
            // Move also rejects a destination created by another exporter meanwhile.
            Directory.Move(staging, destination);
            return destination;
        }
        catch
        {
            // Preserve the original export failure if best-effort cleanup also fails.
            try { Directory.Delete(staging, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            throw;
        }

        async Task WriteAsync<T>(string name, T value)
        {
            await using var stream = new FileStream(Path.Combine(staging, name), FileMode.CreateNew, FileAccess.Write);
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
            await stream.WriteAsync(new byte[] { (byte)'\n' }, cancellationToken);
        }
    }

    private static void ValidateIdentifier(string value, string name)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 128 || !char.IsAsciiLetterOrDigit(value[0])
            || value.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
            throw new ArgumentException("Use 1..128 ASCII letters/digits, '-' or '_', starting with a letter/digit.", name);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
        options.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        options.Converters.Add(new UtcTimestampConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }

    // System.Text.Json otherwise preserves input offsets. Evidence uses one UTC
    // representation, including timestamps nested in the exact manifest.
    private sealed class UtcTimestampConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.GetDateTimeOffset();

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
    }
}
