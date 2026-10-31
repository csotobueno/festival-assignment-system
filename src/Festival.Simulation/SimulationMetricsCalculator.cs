using System.Collections.Immutable;
using Festival.Domain.Assignments;

namespace Festival.Simulation;

// Pure aggregation of the same evaluated population's existing analytical
// evidence. Individual classification and recovery interpretation stay upstream.
public static class SimulationMetricsCalculator
{
    public static SimulationMetrics Calculate(IReadOnlyList<AttendeePathRecord> attendeePaths,
        IReadOnlyList<AttendeeRecoveryRecord> recoveryObservations)
    {
        ArgumentNullException.ThrowIfNull(attendeePaths);
        ArgumentNullException.ThrowIfNull(recoveryObservations);
        return new SimulationMetrics(CalculateFairness(attendeePaths), CalculateRecovery(recoveryObservations));
    }

    private static ScenarioFairnessMetrics CalculateFairness(IReadOnlyList<AttendeePathRecord> paths)
    {
        var noPersisted = paths.Count(path => path.PathLength == 0);
        var insufficient = paths.Count(path => path.PathLength == 1);
        var classifiable = paths.Where(path => path.PathLength >= 2).ToArray();
        var balanced = classifiable.Count(path => path.Classification == PathClassification.Balanced);
        var acceptable = classifiable.Count(path => path.Classification == PathClassification.Acceptable);
        var notBalanced = classifiable.Count(path => path.Classification == PathClassification.NotBalanced);
        var outcomes = paths.SelectMany(path => path.Entries).ToArray();
        var good = outcomes.Count(entry => entry.ExperienceQuality == ExperienceQuality.Good);
        var medium = outcomes.Count(entry => entry.ExperienceQuality == ExperienceQuality.Medium);
        var bad = outcomes.Count(entry => entry.ExperienceQuality == ExperienceQuality.Bad);

        return new ScenarioFairnessMetrics(paths.Count, noPersisted, insufficient, classifiable.Length,
            balanced, acceptable, notBalanced,
            Ratio(noPersisted, paths.Count), Ratio(insufficient, paths.Count), Ratio(classifiable.Length, paths.Count),
            Ratio(balanced, classifiable.Length), Ratio(acceptable, classifiable.Length), Ratio(notBalanced, classifiable.Length),
            outcomes.Length, good, medium, bad,
            Ratio(good, outcomes.Length), Ratio(medium, outcomes.Length), Ratio(bad, outcomes.Length));
    }

    private static ScenarioRecoveryMetrics CalculateRecovery(IReadOnlyList<AttendeeRecoveryRecord> observations)
    {
        var episodes = observations.SelectMany(record => record.Episodes).ToArray();
        var recovered = episodes.Where(episode => episode.Status == RecoveryEpisodeStatus.Recovered).ToArray();
        var unresolved = episodes.Count(episode => episode.Status == RecoveryEpisodeStatus.Unresolved);
        var eligible = episodes.Count(episode => episode.RecoveryEligible);
        var ineligible = episodes.Count(episode => !episode.RecoveryEligible);
        var opportunities = recovered.Select(episode => episode.OpportunitiesToRecovery)
            .Where(value => value.HasValue).Select(value => value!.Value).Order().ToArray();
        var distribution = opportunities.GroupBy(value => value).OrderBy(bucket => bucket.Key)
            .Select(bucket => new RecoveryOpportunityDistribution(bucket.Key, bucket.Count(),
                (decimal)bucket.Count() / recovered.Length)).ToImmutableArray();

        return new ScenarioRecoveryMetrics(episodes.Length, recovered.Length, unresolved, eligible, ineligible,
            Ratio(recovered.Length, eligible),
            recovered.Count(episode => episode.RecoveryQuality == ExperienceQuality.Medium),
            recovered.Count(episode => episode.RecoveryQuality == ExperienceQuality.Good),
            opportunities.Length == 0 ? null : opportunities.Average(value => (decimal)value),
            Median(opportunities), distribution);
    }

    private static decimal? Ratio(int numerator, int denominator) =>
        denominator == 0 ? null : (decimal)numerator / denominator;

    private static decimal? Median(int[] ordered)
    {
        if (ordered.Length == 0)
            return null;

        return ((decimal)ordered[(ordered.Length - 1) / 2] + ordered[ordered.Length / 2]) / 2m;
    }
}
