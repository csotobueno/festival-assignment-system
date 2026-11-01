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
    // Definition/framework versions have no explicit code value yet. The engine
    // label identifies the documented Stage 4 baseline, not an exact source revision.
    public static SimulationRunMetadata Create(string runId, ScenarioManifest manifest,
        DateTimeOffset startedAt, DateTimeOffset completedAt, string? repositoryCommitSha = null) =>
        new(runId, manifest.Metadata.ScenarioId, null, manifest.Metadata.GeneratorVersion,
            manifest.Metadata.Seed, "v0.3.0", repositoryCommitSha, PathClassificationRubric.Version,
            null, manifest.Metadata.ExecutionMode, manifest.Metadata.ConcurrencyLevel,
            startedAt, completedAt);
}

// DestinationRoot is the simulations root (normally artifacts/simulations),
// beneath which the exporter creates <ScenarioId>/<RunId>.
public sealed record SimulationEvidenceExportRequest(
    ScenarioDefinition Definition,
    ScenarioManifest Manifest,
    SimulationResult Result,
    SimulationRunMetadata RunMetadata,
    string DestinationRoot);
