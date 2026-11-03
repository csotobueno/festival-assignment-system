using System.Collections.Immutable;
using Festival.Domain.Assignments;
using Festival.Simulation;
using FluentAssertions;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class DistributionGeneratorTests
{
    [Fact]
    public void DeterministicRandom_V2MatchesFixedSplitMix64BoundedSamplingVectors()
    {
        var random = new DeterministicRandom(0);

        Enumerable.Range(0, 5).Select(_ => random.NextInt64(1_000_000)).Should()
            .Equal(883310L, 431527L, 26433L, 970881L, 106346L);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(17)]
    [InlineData(3300)]
    [InlineData(4400)]
    public void GroupGeneration_AlwaysProducesExactMembershipWithValidDeterministicSizes(int count)
    {
        var profile = RepresentativeScenarioMaterializerTests.Definition(false).Workload!.GroupSizeProfile;
        var sizes = GroupDistributionGenerator.Generate(count, profile, new DeterministicRandom(5));

        sizes.Sum().Should().Be(count);
        sizes.Should().OnlyContain(size => size >= 1 && size <= 10);
        sizes.Should().Equal(GroupDistributionGenerator.Generate(count, profile, new DeterministicRandom(5)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    [InlineData(19)]
    public void GroupGeneration_PrioritizesExactPopulationWhenRequestedFixedSizesCannotFit(int count)
    {
        var sizes = GroupDistributionGenerator.Generate(count, [new(10, 10, 1m)], new DeterministicRandom(0));

        sizes.Sum().Should().Be(count);
        sizes.Should().OnlyContain(size => size >= 1 && size <= 10);
        sizes.Should().Equal(GroupDistributionGenerator.Generate(count, [new(10, 10, 1m)], new DeterministicRandom(0)));
    }

    [Fact]
    public void GroupGeneration_ConcreteSizesWithinRangesVaryWithSeed()
    {
        var first = GroupDistributionGenerator.Generate(400, [new(5, 10, 1m)], new DeterministicRandom(1));
        var second = GroupDistributionGenerator.Generate(400, [new(5, 10, 1m)], new DeterministicRandom(2));

        first.Should().OnlyContain(size => size >= 5 && size <= 10);
        second.Should().OnlyContain(size => size >= 5 && size <= 10);
        first.Distinct().Should().HaveCountGreaterThan(1);
        first.Should().NotEqual(second);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GroupGeneration_RejectsNonpositivePopulation(int count)
    {
        var generate = () => GroupDistributionGenerator.Generate(count, [new(1, 1, 1m)], new DeterministicRandom(0));
        generate.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 11)]
    [InlineData(4, 3)]
    public void GroupGeneration_RejectsInvalidSizeBands(int minimum, int maximum)
    {
        var generate = () => GroupDistributionGenerator.Generate(100, [new(minimum, maximum, 1m)], new DeterministicRandom(0));
        generate.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-.1, 1.1)]
    [InlineData(.2, .7)]
    [InlineData(.6, .6)]
    public void GroupGeneration_RejectsNegativeOrIncompletePercentageTotals(double first, double second)
    {
        var generate = () => GroupDistributionGenerator.Generate(100,
            [new(1, 1, (decimal)first), new(2, 2, (decimal)second)], new DeterministicRandom(0));
        generate.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GroupGeneration_RejectsOverlappingBands()
    {
        var generate = () => GroupDistributionGenerator.Generate(100,
            [new(1, 3, .5m), new(3, 4, .5m)], new DeterministicRandom(0));
        generate.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GroupGeneration_RejectsEmptyProfiles()
    {
        var generate = () => GroupDistributionGenerator.Generate(100, [], new DeterministicRandom(0));
        generate.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ArrivalGeneration_AllocatesRemaindersInDeclaredBandOrderAndRetainsEveryGroup()
    {
        var requests = Requests(7);
        ImmutableArray<ArrivalTimeBand> profile =
        [new(new(7, 0), new(9, 0), .25m), new(new(9, 0), new(12, 0), .25m), new(new(12, 0), new(15, 0), .50m)];
        var date = new DateOnly(2026, 8, 15);
        var generated = ArrivalDistributionGenerator.Generate(requests, profile, date, new DeterministicRandom(1));

        // Ideal counts 1.75, 1.75, 3.5: two residues go to the first two bands.
        profile.Select(band => generated.Count(request => InBand(request, band))).Should().Equal(2, 2, 3);
        generated.Select(request => request.SimulationRequestId).Should()
            .BeEquivalentTo(requests.Select(request => request.SimulationRequestId));
        generated.SelectMany(request => request.Members.Select(member => member.Id)).Should()
            .BeEquivalentTo(requests.SelectMany(request => request.Members.Select(member => member.Id)));
        generated.Should().BeEquivalentTo(ArrivalDistributionGenerator.Generate(requests, profile, date,
            new DeterministicRandom(1)), options => options.WithStrictOrdering());
        generated.Should().OnlyContain(request => request.RequestedAt.Date == date.ToDateTime(TimeOnly.MinValue).Date
            && request.RequestedAt.Offset == TimeSpan.Zero && request.AssignedAt == request.RequestedAt);
    }

    [Fact]
    public void ArrivalGeneration_ExactTimestampTiesUseSimulationRequestIdOrderAndExclusiveEnd()
    {
        var start = new TimeOnly(7, 0);
        var end = new TimeOnly(start.Ticks + 1);
        var date = new DateOnly(2026, 8, 15);
        var generated = ArrivalDistributionGenerator.Generate(Requests(5).Reverse().ToArray(),
            [new(start, end, 1m)], date, new DeterministicRandom(1));

        generated.Select(request => request.SimulationRequestId).Should()
            .Equal(Requests(5).OrderBy(request => request.SimulationRequestId).Select(request => request.SimulationRequestId));
        generated.Should().OnlyContain(request => request.RequestedAt ==
            new DateTimeOffset(date.ToDateTime(start), TimeSpan.Zero));
    }

    [Fact]
    public void ArrivalGeneration_DifferentSeedsChangeTimestampsAndPreserveAllocatedBandCounts()
    {
        ImmutableArray<ArrivalTimeBand> profile = [new(new(7, 0), new(9, 0), .4m), new(new(9, 0), new(12, 0), .6m)];
        var first = ArrivalDistributionGenerator.Generate(Requests(10), profile, new(2026, 8, 15), new DeterministicRandom(0));
        var second = ArrivalDistributionGenerator.Generate(Requests(10), profile, new(2026, 8, 15), new DeterministicRandom(1));

        first.Select(request => request.RequestedAt).Should().NotEqual(second.Select(request => request.RequestedAt));
        profile.Select(band => first.Count(request => InBand(request, band))).Should().Equal(4, 6);
        profile.Select(band => second.Count(request => InBand(request, band))).Should().Equal(4, 6);
    }

    [Theory]
    [InlineData(9, 9)]
    [InlineData(10, 9)]
    public void ArrivalGeneration_RejectsInvalidTimeBands(int start, int end)
    {
        var generate = () => ArrivalDistributionGenerator.Generate(Requests(1),
            [new(new(start, 0), new(end, 0), 1m)], new(2026, 8, 15), new DeterministicRandom(0));
        generate.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(8, 12)]
    [InlineData(6, 7)]
    public void ArrivalGeneration_RejectsOverlappingOrUnorderedBands(int secondStart, int secondEnd)
    {
        var generate = () => ArrivalDistributionGenerator.Generate(Requests(1),
            [new(new(7, 0), new(9, 0), .5m), new(new(secondStart, 0), new(secondEnd, 0), .5m)],
            new(2026, 8, 15), new DeterministicRandom(0));
        generate.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-.1, 1.1)]
    [InlineData(.2, .7)]
    [InlineData(.6, .6)]
    public void ArrivalGeneration_RejectsNegativeOrIncompletePercentages(double first, double second)
    {
        var generate = () => ArrivalDistributionGenerator.Generate(Requests(1),
            [new(new(7, 0), new(9, 0), (decimal)first), new(new(9, 0), new(12, 0), (decimal)second)],
            new(2026, 8, 15), new DeterministicRandom(0));
        generate.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ArrivalGeneration_RejectsEmptyProfiles()
    {
        var generate = () => ArrivalDistributionGenerator.Generate(Requests(1), [], new(2026, 8, 15), new DeterministicRandom(0));
        generate.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-.01)]
    [InlineData(1.01)]
    public void Materialize_RejectsInvalidContinuity(double continuity)
    {
        var definition = RepresentativeScenarioMaterializerTests.Definition(false);
        var materialize = () => new ScenarioMaterializer().Materialize(definition with
        { Workload = definition.Workload! with { DailyParticipationContinuity = (decimal)continuity } });
        materialize.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-.01)]
    [InlineData(1.01)]
    public void Materialize_RejectsInvalidEligibilityShare(double share)
    {
        var definition = RepresentativeScenarioMaterializerTests.Definition(false);
        var materialize = () => new ScenarioMaterializer().Materialize(definition with
        { Workload = definition.Workload! with { AllowsFrontStandingRequestShare = (decimal)share } });
        materialize.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5501)]
    [InlineData(5500)]
    public void Materialize_RejectsInvalidOrInfeasibleRequesterCounts(int count)
    {
        var definition = RepresentativeScenarioMaterializerTests.Definition(false);
        var materialize = () => new ScenarioMaterializer().Materialize(definition with
        { Workload = definition.Workload! with { DailyRequesterCount = count } });
        materialize.Should().Throw<ArgumentException>();
    }

    private static DailyRequestPlan[] Requests(int count)
    {
        var request = new ScenarioMaterializer().Materialize(MinimalRecoveryScenarioDefinition.Create()).DailyPlans[0].Requests[0];
        return Enumerable.Range(1, count).Select(number => request with
            { SimulationRequestId = new Guid(number, 0, 0, new byte[8]), Sequence = number }).ToArray();
    }

    private static bool InBand(DailyRequestPlan request, ArrivalTimeBand band)
    {
        var time = TimeOnly.FromDateTime(request.RequestedAt.UtcDateTime);
        return time >= band.Start && time < band.End;
    }
}
