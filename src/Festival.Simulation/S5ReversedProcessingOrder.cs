using System.Collections.Immutable;

namespace Festival.Simulation;

// Transform an already materialized S2 workload; never generate S5 inputs anew.
public static class S5ReversedProcessingOrder
{
    public const string ScenarioId = "s5-adverse-arrival-order";
    public const string WorkloadSourceScenarioId = "s2-expected-baseline";
    public const string Transformation = "reverse-daily-execution-order";

    public static ScenarioManifest Create(ScenarioManifest s2Manifest)
    {
        ArgumentNullException.ThrowIfNull(s2Manifest);
        var metadata = s2Manifest.Metadata;
        if (metadata.ScenarioId != WorkloadSourceScenarioId
            || metadata.WorkloadSourceScenarioId is not null || metadata.OrderingTransformation is not null
            || metadata.ExecutionMode != SimulationExecutionMode.Sequential || metadata.ConcurrencyLevel != 1)
            throw new ArgumentException("S5 requires an untransformed, sequential S2 manifest.", nameof(s2Manifest));

        var requests = s2Manifest.DailyPlans.SelectMany(day => day.Requests).ToArray();
        if (requests.Any(request => request.Sequence <= 0 || request.ExecutionSequence != request.Sequence)
            || requests.Select(request => request.Sequence).Distinct().Count() != requests.Length)
            throw new ArgumentException("S2 baseline sequences must be positive, unique and equal execution sequences.",
                nameof(s2Manifest));

        return s2Manifest with
        {
            Metadata = metadata with
            {
                ScenarioId = ScenarioId,
                WorkloadSourceScenarioId = metadata.ScenarioId,
                OrderingTransformation = Transformation
            },
            DailyPlans = s2Manifest.DailyPlans.Select(day =>
            {
                var baseline = day.Requests.OrderBy(request => request.Sequence).ToArray();
                return day with
                {
                    // Keep baseline Sequence and every non-order field. Mirror the
                    // existing global sequence slots within this FestivalDay only.
                    Requests = baseline.Select((request, index) => request with
                    {
                        ExecutionSequence = baseline[^(index + 1)].Sequence
                    }).ToImmutableArray()
                };
            }).ToImmutableArray()
        };
    }
}
