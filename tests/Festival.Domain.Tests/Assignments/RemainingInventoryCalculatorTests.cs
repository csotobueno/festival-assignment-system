using Festival.Domain.Assignments;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Assignments;

public sealed class RemainingInventoryCalculatorTests
{
    private static readonly Zone Good = CreateZone(1, ZoneType.MiddleCenter);
    private static readonly Zone Medium = CreateZone(2, ZoneType.MiddleLeft);
    private static readonly Zone Bad = CreateZone(3, ZoneType.UpperLeft);

    [Fact]
    public void Calculate_ShouldReturnZeroCounts_ForEmptyAvailability()
    {
        foreach (var state in new[] { RemainingInventoryCalculator.Calculate([Good, Medium, Bad], []),
                     RemainingInventoryCalculator.Calculate([], []) })
        {
            Assert.Empty(state.ZoneAvailabilities);
            Assert.Equal(0, state.GoodRemaining);
            Assert.Equal(0, state.MediumRemaining);
            Assert.Equal(0, state.BadRemaining);
        }
    }

    [Fact]
    public void Calculate_ShouldReturnSingleZoneWithItsRawAvailableCount()
    {
        var state = RemainingInventoryCalculator.Calculate([Good],
            [CreateSpot(Good, 1), CreateSpot(Good, 2), CreateSpot(Good, 3)]);

        Assert.Equal(ZoneAvailabilityState.Create(Good.Id, ExperienceQuality.Good, 3),
            Assert.Single(state.ZoneAvailabilities));
    }

    [Theory]
    [InlineData(ZoneType.FrontStanding)]
    [InlineData(ZoneType.MiddleCenter)]
    [InlineData(ZoneType.MiddleLeft)]
    [InlineData(ZoneType.MiddleRight)]
    [InlineData(ZoneType.UpperCenter)]
    [InlineData(ZoneType.UpperLeft)]
    [InlineData(ZoneType.UpperRight)]
    public void Calculate_ShouldCountOnlyTheQualityResolvedByPolicy_ForEveryZoneType(ZoneType type)
    {
        var zone = CreateZone(1, type);
        var quality = ZoneExperienceQualityPolicy.GetQuality(type);

        var state = RemainingInventoryCalculator.Calculate([zone],
            [CreateSpot(zone, 1), CreateSpot(zone, 2)]);

        Assert.Equal(ZoneAvailabilityState.Create(zone.Id, quality, 2), Assert.Single(state.ZoneAvailabilities));
        Assert.Equal(quality == ExperienceQuality.Good ? 2 : 0, state.GoodRemaining);
        Assert.Equal(quality == ExperienceQuality.Medium ? 2 : 0, state.MediumRemaining);
        Assert.Equal(quality == ExperienceQuality.Bad ? 2 : 0, state.BadRemaining);
    }

    [Fact]
    public void Calculate_ShouldCountMixedQualitiesIndependently()
    {
        var state = RemainingInventoryCalculator.Calculate([Good, Medium, Bad],
            [CreateSpot(Good, 1), CreateSpot(Good, 2), CreateSpot(Good, 3),
                CreateSpot(Medium, 1), CreateSpot(Medium, 2), CreateSpot(Bad, 1)]);

        AssertZoneEntries(state, ZoneAvailabilityState.Create(Good.Id, ExperienceQuality.Good, 3),
            ZoneAvailabilityState.Create(Medium.Id, ExperienceQuality.Medium, 2),
            ZoneAvailabilityState.Create(Bad.Id, ExperienceQuality.Bad, 1));
        Assert.Equal(3, state.GoodRemaining);
        Assert.Equal(2, state.MediumRemaining);
        Assert.Equal(1, state.BadRemaining);
    }

    [Fact]
    public void Calculate_ShouldKeepMultipleZonesWithTheSameQualityIndependent()
    {
        var front = CreateZone(4, ZoneType.FrontStanding);
        var spots = Enumerable.Range(1, 2).Select(number => CreateSpot(front, number))
            .Concat(Enumerable.Range(1, 5).Select(number => CreateSpot(Good, number)));

        var state = RemainingInventoryCalculator.Calculate([front, Good], spots);

        AssertZoneEntries(state, ZoneAvailabilityState.Create(front.Id, ExperienceQuality.Good, 2),
            ZoneAvailabilityState.Create(Good.Id, ExperienceQuality.Good, 5));
        Assert.Equal(7, state.GoodRemaining);
        Assert.Equal(0, state.MediumRemaining);
        Assert.Equal(0, state.BadRemaining);
    }

    [Fact]
    public void Calculate_ShouldOmitCatalogZonesWithoutAvailableSpots()
    {
        var state = RemainingInventoryCalculator.Calculate([Good, Medium, Bad], [CreateSpot(Medium, 1)]);

        Assert.Equal(ZoneAvailabilityState.Create(Medium.Id, ExperienceQuality.Medium, 1),
            Assert.Single(state.ZoneAvailabilities));
    }

    [Fact]
    public void Calculate_ShouldCountOnlySuppliedAvailableSpots()
    {
        Spot[] catalogSpots = [CreateSpot(Good, 1), CreateSpot(Good, 2),
            CreateSpot(Medium, 1), CreateSpot(Bad, 1)];
        var availableSpots = new[] { catalogSpots[1], catalogSpots[3] };

        var state = RemainingInventoryCalculator.Calculate([Good, Medium, Bad], availableSpots);

        AssertZoneEntries(state, ZoneAvailabilityState.Create(Good.Id, ExperienceQuality.Good, 1),
            ZoneAvailabilityState.Create(Bad.Id, ExperienceQuality.Bad, 1));
    }

    [Fact]
    public void Calculate_ShouldCountRawSpotsAcrossGapsRowsAndZones()
    {
        var front = CreateZone(4, ZoneType.FrontStanding);
        Spot[] fragmented = [CreateSpot(Good, 1), CreateSpot(Good, 5),
            CreateSpot(Good, 1, "B"), CreateSpot(front, 10), CreateSpot(front, 20)];

        var state = RemainingInventoryCalculator.Calculate([Good, front], fragmented);

        AssertZoneEntries(state, ZoneAvailabilityState.Create(Good.Id, ExperienceQuality.Good, 3),
            ZoneAvailabilityState.Create(front.Id, ExperienceQuality.Good, 2));
        Assert.Equal(5, state.GoodRemaining);
    }

    [Fact]
    public void Calculate_ShouldPreserveInputsAndBeIndependentOfOrdering()
    {
        Zone[] zones = [Bad, Good, Medium];
        Spot[] spots = [CreateSpot(Medium, 2), CreateSpot(Good, 1),
            CreateSpot(Bad, 1), CreateSpot(Medium, 1)];
        var originalZones = zones.ToArray();
        var originalSpots = spots.ToArray();

        var state = RemainingInventoryCalculator.Calculate(zones, spots);

        AssertZoneEntries(state, ZoneAvailabilityState.Create(Good.Id, ExperienceQuality.Good, 1),
            ZoneAvailabilityState.Create(Medium.Id, ExperienceQuality.Medium, 2),
            ZoneAvailabilityState.Create(Bad.Id, ExperienceQuality.Bad, 1));
        var reorderedState = RemainingInventoryCalculator.Calculate(zones.Reverse(), spots.Reverse());
        AssertZoneEntries(reorderedState, state.ZoneAvailabilities.ToArray());
        Assert.Equal(state.GoodRemaining, reorderedState.GoodRemaining);
        Assert.Equal(state.MediumRemaining, reorderedState.MediumRemaining);
        Assert.Equal(state.BadRemaining, reorderedState.BadRemaining);
        Assert.Equal(originalZones, zones);
        Assert.Equal(originalSpots, spots);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Calculate_ShouldRejectNullCollections(bool nullZones)
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            RemainingInventoryCalculator.Calculate(nullZones ? null! : [], nullZones ? [] : null!));

        Assert.Equal(nullZones ? "zones" : "availableSpots", exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Calculate_ShouldRejectNullElements(bool nullZone)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RemainingInventoryCalculator.Calculate(nullZone ? [null!] : [Good], nullZone ? [] : [null!]));

        Assert.Equal(nullZone ? "zones" : "availableSpots", exception.ParamName);
    }

    [Fact]
    public void Calculate_ShouldRejectUnknownZone()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RemainingInventoryCalculator.Calculate([Good], [CreateSpot(Medium, 1)]));

        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Calculate_ShouldRejectDuplicateZoneIds_EvenWithEmptyAvailability(bool sameObject)
    {
        var duplicate = sameObject ? Good : Zone.Create(Good.Id, "Other", ZoneType.UpperLeft);

        var exception = Assert.Throws<ArgumentException>(() =>
            RemainingInventoryCalculator.Calculate([Good, duplicate], []));

        Assert.Equal("zones", exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Calculate_ShouldRejectDuplicateSpotCodes_WithinOrAcrossZones(bool sameZone)
    {
        var first = CreateSpot(Good, 1);
        var duplicate = Spot.Create(SpotCode.Create(first.Code.Value.ToLowerInvariant()),
            sameZone ? Good.Id : Medium.Id, first.RowCode, SpotNumber.Create(2));

        var exception = Assert.Throws<ArgumentException>(() =>
            RemainingInventoryCalculator.Calculate([Good, Medium], [first, duplicate]));

        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Calculate_ShouldRejectRepeatedSpotsOrDuplicatePhysicalPositions(bool sameObject)
    {
        var first = CreateSpot(Good, 1);
        var duplicate = sameObject ? first : Spot.Create(SpotCode.Create("OTHER"),
            Good.Id, RowCode.Create("a"), first.Number);

        var exception = Assert.Throws<ArgumentException>(() =>
            RemainingInventoryCalculator.Calculate([Good], [first, duplicate]));

        Assert.Equal("availableSpots", exception.ParamName);
    }

    private static void AssertZoneEntries(RemainingInventoryState state, params ZoneAvailabilityState[] expected) =>
        Assert.Equal(expected.OrderBy(entry => entry.ZoneId.Value),
            state.ZoneAvailabilities.OrderBy(entry => entry.ZoneId.Value));

    private static Zone CreateZone(int id, ZoneType type) => Zone.Create(
        ZoneId.Create(Guid.Parse($"20000000-0000-0000-0000-{id:D12}")), "Test", type);

    private static Spot CreateSpot(Zone zone, int number, string row = "A") => Spot.Create(
        SpotCode.Create($"{zone.Id.Value}-{row}-{number}"), zone.Id,
        RowCode.Create(row), SpotNumber.Create(number));
}
