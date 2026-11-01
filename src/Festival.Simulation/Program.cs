using Festival.Simulation;

var definition = MinimalRecoveryScenarioDefinition.Create();
var manifest = new ScenarioMaterializer().Materialize(definition);
var runId = Guid.NewGuid().ToString("N");
var startedAt = DateTimeOffset.UtcNow;
var result = await MinimalSimulationRunner.RunAsync(manifest);
var metadata = SimulationRunMetadata.Create(runId, manifest, startedAt, DateTimeOffset.UtcNow,
    Environment.GetEnvironmentVariable("FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA"));
var destination = await new SimulationEvidenceExporter().ExportAsync(new(
    definition, manifest, result, metadata, Path.Combine("artifacts", "simulations")));
Console.WriteLine($"Simulation evidence exported to {destination}");
return result.Attempts.All(attempt => attempt.Outcome == SimulationAttemptOutcome.Successful)
    ? 0
    : 1;
