namespace Festival.Simulation;

// Change execution metadata only. Generator v2 must never resample under an S8 ID.
public static class S8ConcurrentExecution
{
    public const string WorkloadSourceScenarioId = "s2-expected-baseline";
    public const string LowScenarioId = "s8-concurrency-4";
    public const string ModerateScenarioId = "s8-concurrency-16";

    public static ScenarioManifest Create(ScenarioManifest s2Manifest, int concurrencyLevel, int repetition)
    {
        ArgumentNullException.ThrowIfNull(s2Manifest);
        var mode = concurrencyLevel == 1 ? SimulationExecutionMode.Sequential : SimulationExecutionMode.Concurrent;
        var configuration = new SimulationExecutionConfiguration(mode, concurrencyLevel, repetition);
        var metadata = s2Manifest.Metadata;
        if (metadata.ScenarioId != WorkloadSourceScenarioId || metadata.GeneratorVersion != "v2"
            || metadata.ExecutionMode != SimulationExecutionMode.Sequential || metadata.ConcurrencyLevel != 1
            || metadata.WorkloadSourceScenarioId is not null || metadata.OrderingTransformation is not null
            || metadata.FragmentationPatternVersion is not null || metadata.OpeningOccupiedSpotCount is not null
            || metadata.WorkloadSourceManifestSha256 is not null || metadata.Repetition is not null
            || !s2Manifest.Setup.SupportingAttendees.IsEmpty || !s2Manifest.Setup.CompletedRequests.IsEmpty
            || !s2Manifest.Setup.Assignments.IsEmpty)
            throw new ArgumentException("S8 requires the untransformed S2 v2 workload and normal empty setup.", nameof(s2Manifest));
        var requests = s2Manifest.DailyPlans.SelectMany(day => day.Requests).ToArray();
        if (requests.Any(request => request.Sequence <= 0 || request.ExecutionSequence != request.Sequence)
            || requests.Select(request => request.Sequence).Distinct().Count() != requests.Length)
            throw new ArgumentException("S8 requires unique positive S2 baseline sequences and unchanged dispatch order.", nameof(s2Manifest));

        return s2Manifest with
        {
            Metadata = metadata with
            {
                ScenarioId = concurrencyLevel switch { 4 => LowScenarioId, 16 => ModerateScenarioId, _ => metadata.ScenarioId },
                WorkloadSourceScenarioId = concurrencyLevel == 1 ? null : metadata.ScenarioId,
                ExecutionMode = configuration.ExecutionMode,
                ConcurrencyLevel = configuration.ConcurrencyLevel,
                Repetition = configuration.Repetition
            }
        };
    }
}
