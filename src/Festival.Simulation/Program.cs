using Festival.Simulation;

var result = await MinimalSimulationRunner.RunAsync();
return result.Attempts.All(attempt => attempt.Outcome == SimulationAttemptOutcome.Successful)
    ? 0
    : 1;
