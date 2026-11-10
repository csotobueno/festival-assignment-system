namespace Festival.Simulation;

public static class ArrivalDrivenExecution
{
    public static ScenarioManifest Create(ScenarioManifest source, ArrivalPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(source);
        var metadata = source.Metadata;
        if (metadata.ScenarioId != S8ConcurrentExecution.WorkloadSourceScenarioId
            || metadata.GeneratorVersion != "v2" || metadata.ExecutionMode != SimulationExecutionMode.Sequential
            || metadata.ConcurrencyLevel != 1 || metadata.Repetition is not null || metadata.ArrivalSchedule is not null
            || metadata.WorkloadSourceScenarioId is not null || metadata.OrderingTransformation is not null
            || metadata.FragmentationPatternVersion is not null || metadata.OpeningOccupiedSpotCount is not null
            || metadata.WorkloadSourceManifestSha256 is not null
            || !source.Setup.SupportingAttendees.IsEmpty || !source.Setup.CompletedRequests.IsEmpty
            || !source.Setup.Assignments.IsEmpty)
            throw new ArgumentException("Arrival execution requires the untransformed S2 v2 workload and empty setup.", nameof(source));
        var requests = source.DailyPlans.SelectMany(day => day.Requests).ToArray();
        if (requests.Any(request => request.Sequence <= 0 || request.ExecutionSequence != request.Sequence)
            || requests.Select(request => request.Sequence).Distinct().Count() != requests.Length)
            throw new ArgumentException("Arrival execution preserves unique positive baseline sequences.", nameof(source));
        return source with
        {
            Metadata = metadata with
            {
                ExecutionMode = SimulationExecutionMode.ArrivalDriven,
                // Zero denotes no configured worker cap, never measured concurrency.
                ConcurrencyLevel = 0,
                ArrivalSchedule = new ArrivalSchedule(pattern)
            }
        };
    }
}
