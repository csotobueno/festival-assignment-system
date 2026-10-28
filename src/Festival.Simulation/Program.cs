using Festival.Simulation;

var definition = MinimalRecoveryScenarioDefinition.Create();
var manifest = new ScenarioMaterializer().Materialize(definition);
var result = await MinimalSimulationRunner.RunAsync(manifest);
return result.Attempts.All(attempt => attempt.Outcome == SimulationAttemptOutcome.Successful)
    ? 0
    : 1;
