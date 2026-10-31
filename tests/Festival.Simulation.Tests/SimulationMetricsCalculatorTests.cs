using System.Collections.Immutable;
using System.Globalization;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Simulation;
using FluentAssertions;
using Xunit;
using static Festival.Simulation.PathClassification;

namespace Festival.Simulation.Tests;

public sealed class SimulationMetricsCalculatorTests
{
    [Fact]
    public void Calculate_ReportsPopulationCoverageClassificationAndPersistedOutcomeDenominators()
    {
        var paths = Paths(("GG", Balanced), ("MM", Acceptable), ("BG", Balanced),
            ("G", InsufficientEvidence), ("", null));

        var fairness = Calculate(paths).Fairness;

        fairness.EvaluatedAttendeeCount.Should().Be(5);
        fairness.NoPersistedPathCount.Should().Be(1);
        fairness.InsufficientEvidenceCount.Should().Be(1);
        fairness.ClassifiablePathCount.Should().Be(3);
        fairness.BalancedPathCount.Should().Be(2);
        fairness.AcceptablePathCount.Should().Be(1);
        fairness.NotBalancedPathCount.Should().Be(0);
        fairness.NoPersistedPathRate.Should().Be(0.2m);
        fairness.InsufficientEvidenceRate.Should().Be(0.2m);
        fairness.ClassifiablePathRate.Should().Be(0.6m);
        fairness.BalancedPathRate.Should().Be(2m / 3m);
        fairness.AcceptablePathRate.Should().Be(1m / 3m);
        fairness.NotBalancedPathRate.Should().Be(0m);
        fairness.PersistedOutcomeCount.Should().Be(7);
        fairness.GoodOutcomeCount.Should().Be(4);
        fairness.MediumOutcomeCount.Should().Be(2);
        fairness.BadOutcomeCount.Should().Be(1);
        fairness.GoodOutcomeRate.Should().Be(4m / 7m);
        fairness.MediumOutcomeRate.Should().Be(2m / 7m);
        fairness.BadOutcomeRate.Should().Be(1m / 7m);
        (fairness.GoodOutcomeRate + fairness.MediumOutcomeRate + fairness.BadOutcomeRate)!.Value
            .Should().BeApproximately(1m, 0.000000000000000000000000001m);
    }

    [Fact]
    public void Calculate_UsesStoredClassificationWithoutReevaluatingQualities()
    {
        // The supplied classification is deliberately different from a new
        // rubric evaluation of GG. Aggregation must trust the existing record.
        var paths = Paths(("GG", NotBalanced));

        var fairness = Calculate(paths).Fairness;

        fairness.BalancedPathCount.Should().Be(0);
        fairness.NotBalancedPathCount.Should().Be(1);
        fairness.NotBalancedPathRate.Should().Be(1m);
        fairness.GoodOutcomeCount.Should().Be(2);
        paths[0].Classification.Should().Be(NotBalanced);
    }

    [Fact]
    public void Calculate_AllThreeClassificationRatesSumToOneWithinDecimalPrecision()
    {
        var fairness = Calculate(Paths(("GG", Balanced), ("MM", Acceptable), ("BB", NotBalanced))).Fairness;

        fairness.BalancedPathCount.Should().Be(1);
        fairness.AcceptablePathCount.Should().Be(1);
        fairness.NotBalancedPathCount.Should().Be(1);
        fairness.BalancedPathRate.Should().Be(1m / 3m);
        fairness.AcceptablePathRate.Should().Be(1m / 3m);
        fairness.NotBalancedPathRate.Should().Be(1m / 3m);
        (fairness.BalancedPathRate + fairness.AcceptablePathRate + fairness.NotBalancedPathRate)!.Value
            .Should().BeApproximately(1m, 0.000000000000000000000000001m);
    }

    [Fact]
    public void Calculate_EmptyPopulationHasZeroCountsAndNotApplicableRatesAndSpeed()
    {
        var metrics = SimulationMetricsCalculator.Calculate([], []);

        metrics.Fairness.Should().Be(new ScenarioFairnessMetrics(
            0, 0, 0, 0, 0, 0, 0, null, null, null, null, null, null,
            0, 0, 0, 0, null, null, null));
        var recovery = metrics.Recovery;
        recovery.RecoveryEpisodeCount.Should().Be(0);
        recovery.RecoveredEpisodeCount.Should().Be(0);
        recovery.UnresolvedEpisodeCount.Should().Be(0);
        recovery.RecoveryEligibleEpisodeCount.Should().Be(0);
        recovery.RecoveryIneligibleEpisodeCount.Should().Be(0);
        recovery.RecoveryRate.Should().BeNull();
        recovery.RecoveredToMediumCount.Should().Be(0);
        recovery.RecoveredToGoodCount.Should().Be(0);
        recovery.AverageOpportunitiesToRecovery.Should().BeNull();
        recovery.MedianOpportunitiesToRecovery.Should().BeNull();
        recovery.OpportunityDistribution.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_AttendeesWithoutAssignmentsRemainInCoverageButHaveNoOutcomeDenominator()
    {
        var metrics = Calculate(Paths(("", null), ("", null)));
        var fairness = metrics.Fairness;

        fairness.EvaluatedAttendeeCount.Should().Be(2);
        fairness.NoPersistedPathCount.Should().Be(2);
        fairness.NoPersistedPathRate.Should().Be(1m);
        fairness.InsufficientEvidenceRate.Should().Be(0m);
        fairness.ClassifiablePathRate.Should().Be(0m);
        fairness.BalancedPathRate.Should().BeNull();
        fairness.AcceptablePathRate.Should().BeNull();
        fairness.NotBalancedPathRate.Should().BeNull();
        fairness.PersistedOutcomeCount.Should().Be(0);
        fairness.GoodOutcomeRate.Should().BeNull();
        fairness.MediumOutcomeRate.Should().BeNull();
        fairness.BadOutcomeRate.Should().BeNull();
        metrics.Recovery.RecoveryRate.Should().BeNull();
    }

    [Fact]
    public void Calculate_SingletonsContributeOutcomesButNotClassificationRates()
    {
        var metrics = Calculate(Paths(("G", InsufficientEvidence), ("M", InsufficientEvidence),
            ("B", InsufficientEvidence)));

        metrics.Fairness.InsufficientEvidenceCount.Should().Be(3);
        metrics.Fairness.InsufficientEvidenceRate.Should().Be(1m);
        metrics.Fairness.ClassifiablePathCount.Should().Be(0);
        metrics.Fairness.BalancedPathRate.Should().BeNull();
        metrics.Fairness.AcceptablePathRate.Should().BeNull();
        metrics.Fairness.NotBalancedPathRate.Should().BeNull();
        metrics.Fairness.PersistedOutcomeCount.Should().Be(3);
        metrics.Fairness.GoodOutcomeCount.Should().Be(1);
        metrics.Fairness.MediumOutcomeCount.Should().Be(1);
        metrics.Fairness.BadOutcomeCount.Should().Be(1);
        metrics.Fairness.BadOutcomeRate.Should().Be(1m / 3m);
        metrics.Recovery.RecoveryIneligibleEpisodeCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_EligibleUnresolvedEpisodesProduceZeroRecoveryRateWithoutInventingSpeed()
    {
        var recovery = Calculate(Paths(("BB", NotBalanced), ("BBB", NotBalanced))).Recovery;

        recovery.RecoveryEpisodeCount.Should().Be(2);
        recovery.RecoveryEligibleEpisodeCount.Should().Be(2);
        recovery.RecoveryIneligibleEpisodeCount.Should().Be(0);
        recovery.RecoveredEpisodeCount.Should().Be(0);
        recovery.UnresolvedEpisodeCount.Should().Be(2);
        recovery.RecoveryRate.Should().Be(0m);
        recovery.RecoveredToMediumCount.Should().Be(0);
        recovery.RecoveredToGoodCount.Should().Be(0);
        recovery.AverageOpportunitiesToRecovery.Should().BeNull();
        recovery.MedianOpportunitiesToRecovery.Should().BeNull();
        recovery.OpportunityDistribution.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_IneligibleUnresolvedEpisodesHaveNoRecoveryRateDenominator()
    {
        var recovery = Calculate(Paths(("B", InsufficientEvidence), ("GB", NotBalanced))).Recovery;

        recovery.RecoveryEpisodeCount.Should().Be(2);
        recovery.RecoveryEligibleEpisodeCount.Should().Be(0);
        recovery.RecoveryIneligibleEpisodeCount.Should().Be(2);
        recovery.UnresolvedEpisodeCount.Should().Be(2);
        recovery.RecoveryRate.Should().BeNull();
        recovery.AverageOpportunitiesToRecovery.Should().BeNull();
        recovery.MedianOpportunitiesToRecovery.Should().BeNull();
        recovery.OpportunityDistribution.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_RecoveryRateUsesEligibleEpisodesAndSpeedUsesOnlyRecoveredEpisodes()
    {
        var recovery = Calculate(Paths(("BM", Acceptable), ("BBG", Acceptable),
            ("BB", NotBalanced), ("B", InsufficientEvidence))).Recovery;

        recovery.RecoveryEpisodeCount.Should().Be(4);
        recovery.RecoveryEligibleEpisodeCount.Should().Be(3);
        recovery.RecoveryIneligibleEpisodeCount.Should().Be(1);
        recovery.RecoveredEpisodeCount.Should().Be(2);
        recovery.UnresolvedEpisodeCount.Should().Be(2);
        recovery.RecoveryRate.Should().Be(2m / 3m);
        recovery.RecoveredToMediumCount.Should().Be(1);
        recovery.RecoveredToGoodCount.Should().Be(1);
        recovery.AverageOpportunitiesToRecovery.Should().Be(1.5m);
        recovery.MedianOpportunitiesToRecovery.Should().Be(1.5m);
        recovery.OpportunityDistribution.Should().Equal(
            new RecoveryOpportunityDistribution(1, 1, 0.5m),
            new RecoveryOpportunityDistribution(2, 1, 0.5m));
    }

    [Fact]
    public void Calculate_IneligibleEpisodesDoNotPenalizeRecoveryRate()
    {
        var evidence = Enumerable.Repeat(("BG", (PathClassification?)Balanced), 68)
            .Concat(Enumerable.Repeat(("BB", (PathClassification?)NotBalanced), 12))
            .Concat(Enumerable.Repeat(("B", (PathClassification?)InsufficientEvidence), 20)).ToArray();

        var recovery = Calculate(Paths(evidence)).Recovery;

        recovery.RecoveryEpisodeCount.Should().Be(100);
        recovery.RecoveryEligibleEpisodeCount.Should().Be(80);
        recovery.RecoveryIneligibleEpisodeCount.Should().Be(20);
        recovery.RecoveredEpisodeCount.Should().Be(68);
        recovery.UnresolvedEpisodeCount.Should().Be(32);
        recovery.RecoveryRate.Should().Be(0.85m);
    }

    [Fact]
    public void Calculate_ConsecutiveBadOutcomesCountIndividuallyWithinOneRecoveryEpisode()
    {
        var metrics = Calculate(Paths(("MGBBG", Acceptable)));

        metrics.Fairness.BadOutcomeCount.Should().Be(2);
        metrics.Recovery.RecoveryEpisodeCount.Should().Be(1);
        metrics.Recovery.RecoveredEpisodeCount.Should().Be(1);
        metrics.Recovery.OpportunityDistribution.Should().ContainSingle().Which
            .Should().Be(new RecoveryOpportunityDistribution(2, 1, 1m));
    }

    [Fact]
    public void Calculate_MultipleEpisodesFromOneAttendeeCountSeparately()
    {
        var metrics = Calculate(Paths(("BMBG", Acceptable)));

        metrics.Fairness.EvaluatedAttendeeCount.Should().Be(1);
        metrics.Recovery.RecoveryEpisodeCount.Should().Be(2);
        metrics.Recovery.RecoveredEpisodeCount.Should().Be(2);
        metrics.Recovery.RecoveryEligibleEpisodeCount.Should().Be(2);
        metrics.Recovery.RecoveryRate.Should().Be(1m);
    }

    [Theory]
    [InlineData(new[] { 1, 1, 2, 3 }, "1.75", "1.5")]
    [InlineData(new[] { 1, 1, 1 }, "1", "1")]
    [InlineData(new[] { 3, 1, 2 }, "2", "2")]
    [InlineData(new[] { 6, 1, 6, 2 }, "3.75", "4")]
    public void Calculate_AverageAndMedianUseDecimalArithmeticAndSupportLongerRecoveries(
        int[] opportunities, string average, string median)
    {
        var paths = Paths(opportunities.Select(value =>
            (new string('B', value) + "G", (PathClassification?)NotBalanced)).ToArray());

        var recovery = Calculate(paths).Recovery;

        recovery.AverageOpportunitiesToRecovery.Should().Be(decimal.Parse(average, CultureInfo.InvariantCulture));
        recovery.MedianOpportunitiesToRecovery.Should().Be(decimal.Parse(median, CultureInfo.InvariantCulture));
        recovery.OpportunityDistribution.Sum(bucket => bucket.EpisodeCount).Should().Be(opportunities.Length);
    }

    [Fact]
    public void Calculate_DistributionBucketsAreAscendingAndUseRecoveredCountAsDenominator()
    {
        var recovery = Calculate(Paths(("BBBG", Acceptable), ("BM", Acceptable),
            ("BBBBBG", NotBalanced), ("BG", Balanced))).Recovery;

        recovery.OpportunityDistribution.Should().Equal(
            new RecoveryOpportunityDistribution(1, 2, 0.5m),
            new RecoveryOpportunityDistribution(3, 1, 0.25m),
            new RecoveryOpportunityDistribution(5, 1, 0.25m));
        recovery.OpportunityDistribution.Sum(bucket => bucket.RateAmongRecovered).Should().Be(1m);
        recovery.RecoveredToMediumCount.Should().Be(1);
        recovery.RecoveredToGoodCount.Should().Be(3);
    }

    [Fact]
    public void Calculate_ReturnsImmutableDistributionAndSnapshotsInputCounts()
    {
        var paths = Paths(("BG", Balanced)).ToList();
        var observations = paths.Select(AttendeeRecoveryAnalyzer.Analyze).ToList();
        var metrics = SimulationMetricsCalculator.Calculate(paths, observations);
        var distribution = (IList<RecoveryOpportunityDistribution>)metrics.Recovery.OpportunityDistribution;
        paths.Clear();
        observations.Clear();

        var clear = () => distribution.Clear();
        var replace = () => distribution[0] = new RecoveryOpportunityDistribution(99, 99, 1m);

        clear.Should().Throw<NotSupportedException>();
        replace.Should().Throw<NotSupportedException>();
        metrics.Fairness.EvaluatedAttendeeCount.Should().Be(1);
        metrics.Recovery.RecoveryEpisodeCount.Should().Be(1);
        metrics.Recovery.OpportunityDistribution.Should().ContainSingle().Which.Opportunities.Should().Be(1);
    }

    [Fact]
    public void Calculate_ConsumesProvidedRecoveryObservationsWithoutReconstructingThem()
    {
        var paths = Paths(("BG", Balanced));
        var observations = new[] { new AttendeeRecoveryRecord(paths[0].AttendeeId, []) };

        var metrics = SimulationMetricsCalculator.Calculate(paths, observations);

        metrics.Fairness.BadOutcomeCount.Should().Be(1);
        metrics.Recovery.RecoveryEpisodeCount.Should().Be(0);
        metrics.Recovery.RecoveryRate.Should().BeNull();
    }

    private static SimulationMetrics Calculate(AttendeePathRecord[] paths) =>
        SimulationMetricsCalculator.Calculate(paths, paths.Select(AttendeeRecoveryAnalyzer.Analyze).ToArray());

    // Classifications are supplied as existing evidence, never recomputed here.
    private static AttendeePathRecord[] Paths(params (string Sequence, PathClassification? Classification)[] evidence) =>
        evidence.Select((item, attendee) => new AttendeePathRecord(
            AttendeeId.Create(Guid.Parse($"10000000-0000-0000-0000-{attendee + 1:D12}")),
            item.Sequence.Select((quality, index) => new AttendeePathEntry(
                AssignmentId.Create(Guid.Parse($"00000000-0000-0000-0000-{attendee * 1000 + index + 1:D12}")),
                FestivalDayId.Create(Guid.Parse($"20000000-0000-0000-0000-{index + 1:D12}")),
                new DateOnly(2026, 8, 15).AddDays(index),
                new DateTimeOffset(2026, 8, 15, 14, 0, 0, TimeSpan.Zero).AddDays(index),
                quality switch { 'G' => ExperienceQuality.Good, 'M' => ExperienceQuality.Medium, _ => ExperienceQuality.Bad }))
                .ToImmutableArray(), null, null, null, null, item.Classification)).ToArray();
}
