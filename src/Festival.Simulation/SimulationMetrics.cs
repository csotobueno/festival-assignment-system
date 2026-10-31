using System.Collections.Immutable;

namespace Festival.Simulation;

// Rates are decimal ratios (0..1); null means N/A, never zero evidence.
public sealed record SimulationMetrics(
    ScenarioFairnessMetrics Fairness,
    ScenarioRecoveryMetrics Recovery);

public sealed record ScenarioFairnessMetrics(
    int EvaluatedAttendeeCount,
    int NoPersistedPathCount,
    int InsufficientEvidenceCount,
    int ClassifiablePathCount,
    int BalancedPathCount,
    int AcceptablePathCount,
    int NotBalancedPathCount,
    decimal? NoPersistedPathRate,
    decimal? InsufficientEvidenceRate,
    decimal? ClassifiablePathRate,
    decimal? BalancedPathRate,
    decimal? AcceptablePathRate,
    decimal? NotBalancedPathRate,
    int PersistedOutcomeCount,
    int GoodOutcomeCount,
    int MediumOutcomeCount,
    int BadOutcomeCount,
    decimal? GoodOutcomeRate,
    decimal? MediumOutcomeRate,
    decimal? BadOutcomeRate);

public sealed record ScenarioRecoveryMetrics(
    int RecoveryEpisodeCount,
    int RecoveredEpisodeCount,
    int UnresolvedEpisodeCount,
    int RecoveryEligibleEpisodeCount,
    int RecoveryIneligibleEpisodeCount,
    decimal? RecoveryRate,
    int RecoveredToMediumCount,
    int RecoveredToGoodCount,
    decimal? AverageOpportunitiesToRecovery,
    decimal? MedianOpportunitiesToRecovery,
    ImmutableArray<RecoveryOpportunityDistribution> OpportunityDistribution);

public sealed record RecoveryOpportunityDistribution(
    int Opportunities,
    int EpisodeCount,
    decimal RateAmongRecovered);
