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
        Assert.Equal(RemainingInventoryState.Create(0, 0, 0),
            RemainingInventoryCalculator.Calculate([Good, Medium, Bad], []));
        Assert.Equal(RemainingInventoryState.Create(0, 0, 0),
            RemainingInventoryCalculator.Calculate([], []));
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

        Assert.Equal(RemainingInventoryState.Create(3, 2, 1), state);
    }

    [Fact]
    public void Calculate_ShouldAggregateMultipleZonesWithTheSameQuality()
    {
        var front = CreateZone(4, ZoneType.FrontStanding);
        var spots = Enumerable.Range(1, 3).Select(number => CreateSpot(front, number))
            .Concat(Enumerable.Range(1, 4).Select(number => CreateSpot(Good, number)));

        Assert.Equal(RemainingInventoryState.Create(7, 0, 0),
            RemainingInventoryCalculator.Calculate([front, Good], spots));
    }

    [Fact]
    public void Calculate_ShouldCountOnlySuppliedAvailableSpots()
    {
        Spot[] catalogSpots = [CreateSpot(Good, 1), CreateSpot(Good, 2),
            CreateSpot(Medium, 1), CreateSpot(Bad, 1)];
        var availableSpots = new[] { catalogSpots[1], catalogSpots[3] };

        Assert.Equal(RemainingInventoryState.Create(1, 0, 1),
            RemainingInventoryCalculator.Calculate([Good, Medium, Bad], availableSpots));
    }

    [Fact]
    public void Calculate_ShouldCountRawSpotsAcrossGapsRowsAndZones()
    {
        var front = CreateZone(4, ZoneType.FrontStanding);
        Spot[] fragmented = [CreateSpot(Good, 1), CreateSpot(Good, 5),
            CreateSpot(Good, 1, "B"), CreateSpot(front, 10), CreateSpot(front, 20)];

        Assert.Equal(RemainingInventoryState.Create(5, 0, 0),
            RemainingInventoryCalculator.Calculate([Good, front], fragmented));
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

        Assert.Equal(RemainingInventoryState.Create(1, 2, 1), state);
        Assert.Equal(state, RemainingInventoryCalculator.Calculate(zones.Reverse(), spots.Reverse()));
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

    private static Zone CreateZone(int id, ZoneType type) => Zone.Create(
        ZoneId.Create(Guid.Parse($"20000000-0000-0000-0000-{id:D12}")), "Test", type);

    private static Spot CreateSpot(Zone zone, int number, string row = "A") => Spot.Create(
        SpotCode.Create($"{zone.Id.Value}-{row}-{number}"), zone.Id,
        RowCode.Create(row), SpotNumber.Create(number));
}
