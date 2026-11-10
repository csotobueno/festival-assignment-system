using System.Text.Json.Serialization;
using System.Runtime.InteropServices;

namespace Festival.Simulation;

public sealed record SimulationRunMetadata(
    string RunId,
    string ScenarioId,
    string? ScenarioDefinitionVersion,
    string GeneratorVersion,
    int Seed,
    string? EngineVersion,
    string? RepositoryCommitSha,
    string RubricVersion,
    string? MetricsFrameworkVersion,
    SimulationExecutionMode ExecutionMode,
    int ConcurrencyLevel,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ArrivalSchedule? ArrivalSchedule { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Repetition { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SimulationEnvironmentMetadata? ExecutionEnvironment { get; init; }

    public string? WorkloadSourceScenarioId { get; init; }
    public string? OrderingTransformation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FragmentationPatternVersion { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OpeningOccupiedSpotCount { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WorkloadSourceManifestSha256 { get; init; }

    // Definition/framework versions have no explicit code value yet. The engine
    // label identifies the documented Stage 4 baseline, not an exact source revision.
    public static SimulationRunMetadata Create(string runId, ScenarioManifest manifest,
        DateTimeOffset startedAt, DateTimeOffset completedAt, string? repositoryCommitSha = null) =>
        new(runId, manifest.Metadata.ScenarioId, null, manifest.Metadata.GeneratorVersion,
            manifest.Metadata.Seed, "v0.3.0", repositoryCommitSha, PathClassificationRubric.Version,
            null, manifest.Metadata.ExecutionMode, manifest.Metadata.ConcurrencyLevel,
            startedAt, completedAt)
        {
            Repetition = manifest.Metadata.Repetition,
            ArrivalSchedule = manifest.Metadata.ArrivalSchedule,
            ExecutionEnvironment = manifest.Metadata.Repetition.HasValue
                || manifest.Metadata.ExecutionMode != SimulationExecutionMode.Sequential
                ? SimulationEnvironmentMetadata.Capture() : null,
            WorkloadSourceScenarioId = manifest.Metadata.WorkloadSourceScenarioId,
            OrderingTransformation = manifest.Metadata.OrderingTransformation,
            FragmentationPatternVersion = manifest.Metadata.FragmentationPatternVersion,
            OpeningOccupiedSpotCount = manifest.Metadata.OpeningOccupiedSpotCount,
            WorkloadSourceManifestSha256 = manifest.Metadata.WorkloadSourceManifestSha256
        };
}

public sealed record SimulationEnvironmentMetadata(string OperatingSystem, string Runtime,
    string DotNetVersion, int ProcessorCount, string PostgreSqlImage, string DatabaseContext)
{
    internal static SimulationEnvironmentMetadata Capture() => new(RuntimeInformation.OSDescription,
        RuntimeInformation.FrameworkDescription, Environment.Version.ToString(), Environment.ProcessorCount,
        MinimalSimulationRunner.PostgreSqlImage, "Runner-owned PostgreSQL via Docker/Testcontainers");
}

// DestinationRoot is the simulations root (normally artifacts/simulations),
// beneath which the exporter creates <ScenarioId>/<RunId>.
public sealed record SimulationEvidenceExportRequest(
    ScenarioDefinition Definition,
    ScenarioManifest Manifest,
    SimulationResult Result,
    SimulationRunMetadata RunMetadata,
    string DestinationRoot);
