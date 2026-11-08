using System.Text.Json.Serialization;

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
            WorkloadSourceScenarioId = manifest.Metadata.WorkloadSourceScenarioId,
            OrderingTransformation = manifest.Metadata.OrderingTransformation,
            FragmentationPatternVersion = manifest.Metadata.FragmentationPatternVersion,
            OpeningOccupiedSpotCount = manifest.Metadata.OpeningOccupiedSpotCount,
            WorkloadSourceManifestSha256 = manifest.Metadata.WorkloadSourceManifestSha256
        };
}

// DestinationRoot is the simulations root (normally artifacts/simulations),
// beneath which the exporter creates <ScenarioId>/<RunId>.
public sealed record SimulationEvidenceExportRequest(
    ScenarioDefinition Definition,
    ScenarioManifest Manifest,
    SimulationResult Result,
    SimulationRunMetadata RunMetadata,
    string DestinationRoot);
