using Festival.Domain.Assignments;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Zones;

public sealed class ZoneEvaluationPolicyTests
{
    [Theory]
    [InlineData(10, 100, 200)]
    [InlineData(1, 100, 200)]
    public void Order_ShouldPrioritizeGoodThenMediumThenBad_ForGoodTarget(
        int goodCount, int mediumCount, int badCount)
    {
        var good = CreateZone(3, ZoneType.MiddleCenter);
        var medium = CreateZone(2, ZoneType.MiddleLeft);
        var bad = CreateZone(1, ZoneType.UpperLeft);
        var inventory = RemainingInventoryState.Create(
            [Entry(bad, badCount), Entry(medium, mediumCount), Entry(good, goodCount)]);

        var result = ZoneEvaluationPolicy.Order(TargetQuality.Good, inventory, [bad, medium, good]);

        Assert.Equal(new[] { good, medium, bad }, result);
    }

    [Theory]
    [InlineData(21, 20, true)]
    [InlineData(19, 20, false)]
    [InlineData(20, 20, false)]
    public void Order_ShouldUseDerivedInventoryTotals_ForMediumTarget(
        int goodCount, int mediumCount, bool goodFirst)
    {
        var good = CreateZone(3, ZoneType.FrontStanding);
        var medium = CreateZone(2, ZoneType.UpperCenter);
        var bad = CreateZone(1, ZoneType.UpperRight);
        var inventory = RemainingInventoryState.Create(
            [Entry(good, goodCount), Entry(medium, mediumCount), Entry(bad, 200)]);

        var result = ZoneEvaluationPolicy.Order(TargetQuality.Medium, inventory, [bad, medium, good]);

        Zone[] expected = goodFirst ? [good, medium, bad] : [medium, good, bad];
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Order_ShouldCompareTotalsAcrossAllSuppliedInventoryEntries()
    {
        var good = CreateZone(1, ZoneType.MiddleCenter);
        var medium = CreateZone(2, ZoneType.MiddleLeft);
        var otherGood = CreateZone(3, ZoneType.FrontStanding);
        var inventory = RemainingInventoryState.Create(
            [Entry(good, 1), Entry(medium, 20), Entry(otherGood, 20)]);

        var result = ZoneEvaluationPolicy.Order(TargetQuality.Medium, inventory, [medium, good]);

        Assert.Equal(new[] { good, medium }, result);
    }

    [Theory]
    [InlineData(ZoneType.MiddleCenter)]
    [InlineData(ZoneType.MiddleRight)]
    [InlineData(ZoneType.UpperLeft)]
    public void Order_ShouldUseDescendingAvailability_WithinEachQuality(ZoneType type)
    {
        var low = CreateZone(1, type);
        var high = CreateZone(2, type);
        var middle = CreateZone(3, type);
        var inventory = RemainingInventoryState.Create([Entry(low, 15), Entry(high, 40), Entry(middle, 25)]);

        var result = ZoneEvaluationPolicy.Order(TargetQuality.Good, inventory, [low, middle, high]);

        Assert.Equal(new[] { high, middle, low }, result);
    }

    [Fact]
    public void Order_ShouldBreakExactTiesByAscendingZoneId_WithoutZoneTypePriority()
    {
        var first = CreateZone(1, ZoneType.MiddleCenter);
        var second = CreateZone(2, ZoneType.FrontStanding);
        var inventory = RemainingInventoryState.Create([Entry(second, 20), Entry(first, 20)]);

        var result = ZoneEvaluationPolicy.Order(TargetQuality.Good, inventory, [second, first]);

        Assert.Equal(new[] { first, second }, result);
    }

    [Fact]
    public void Order_ShouldIncludeOnlyEligibleZonesWithMatchingInventoryIds()
    {
        var eligible = CreateZone(1, ZoneType.MiddleLeft);
        var absent = CreateZone(2, ZoneType.MiddleCenter);
        var ineligible = CreateZone(3, ZoneType.FrontStanding);
        var inventory = RemainingInventoryState.Create([Entry(ineligible, 30), Entry(eligible, 10)]);

        var result = ZoneEvaluationPolicy.Order(TargetQuality.Good, inventory, [absent, eligible]);

        Assert.Same(eligible, Assert.Single(result));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void Order_ShouldReturnEmpty_WhenNoZoneParticipates(bool hasEligible, bool hasInventory)
    {
        var eligible = CreateZone(1, ZoneType.MiddleCenter);
        var other = CreateZone(2, ZoneType.MiddleLeft);
        var inventory = RemainingInventoryState.Create(hasInventory ? [Entry(other, 10)] : []);

        var result = ZoneEvaluationPolicy.Order(TargetQuality.Medium, inventory, hasEligible ? [eligible] : []);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(TargetQuality.Good)]
    [InlineData(TargetQuality.Medium)]
    public void Order_ShouldBeIndependentOfEligibleAndInventoryInputOrder(TargetQuality target)
    {
        var goodFirst = CreateZone(1, ZoneType.MiddleCenter);
        var goodSecond = CreateZone(2, ZoneType.FrontStanding);
        var mediumLow = CreateZone(3, ZoneType.MiddleLeft);
        var mediumHigh = CreateZone(4, ZoneType.UpperCenter);
        var bad = CreateZone(5, ZoneType.UpperRight);
        Zone[] zones = [bad, mediumLow, goodSecond, mediumHigh, goodFirst];
        ZoneAvailabilityState[] entries = [Entry(goodSecond, 10), Entry(bad, 100),
            Entry(mediumLow, 20), Entry(goodFirst, 10), Entry(mediumHigh, 30)];
        Zone[] expected = target == TargetQuality.Good
            ? [goodFirst, goodSecond, mediumHigh, mediumLow, bad]
            : [mediumHigh, mediumLow, goodFirst, goodSecond, bad];

        for (var offset = 0; offset < zones.Length; offset++)
        {
            var eligible = zones.Skip(offset).Concat(zones.Take(offset));
            foreach (var inventoryEntries in new[] { entries, entries.Reverse().ToArray() })
            {
                var inventory = RemainingInventoryState.Create(inventoryEntries);

                var result = ZoneEvaluationPolicy.Order(target, inventory, eligible);

                Assert.Equal(expected.Select(zone => zone.Id), result.Select(zone => zone.Id));
            }
        }
    }

    [Fact]
    public void Order_ShouldRejectCurrentQualityMismatch_EvenInLastPriorityBucket()
    {
        var good = CreateZone(1, ZoneType.MiddleCenter);
        var bad = CreateZone(2, ZoneType.UpperLeft);
        var inventory = RemainingInventoryState.Create([Entry(good, 10),
            ZoneAvailabilityState.Create(bad.Id, ExperienceQuality.Medium, 20)]);

        var exception = Assert.Throws<ArgumentException>(() =>
            ZoneEvaluationPolicy.Order(TargetQuality.Good, inventory, [good, bad]));

        Assert.Equal("inventory", exception.ParamName);
        Assert.Contains(bad.Id.ToString(), exception.Message);
    }

    [Fact]
    public void Order_ShouldNotValidateQualityForIneligibleInventoryZones()
    {
        var good = CreateZone(1, ZoneType.MiddleCenter);
        var inventory = RemainingInventoryState.Create(
            [ZoneAvailabilityState.Create(good.Id, ExperienceQuality.Bad, 10)]);

        Assert.Empty(ZoneEvaluationPolicy.Order(TargetQuality.Good, inventory, []));
    }

    [Fact]
    public void Order_ShouldReturnReadOnlySnapshot_WithoutModifyingInputs()
    {
        var first = CreateZone(1, ZoneType.MiddleCenter);
        var second = CreateZone(2, ZoneType.FrontStanding);
        var eligible = new List<Zone> { second, first };
        ZoneAvailabilityState[] entries = [Entry(second, 10), Entry(first, 20)];
        var inventory = RemainingInventoryState.Create(entries);

        var result = ZoneEvaluationPolicy.Order(TargetQuality.Good, inventory, eligible);

        Assert.Equal(new[] { second, first }, eligible);
        Assert.Equal(entries, inventory.ZoneAvailabilities);
        eligible.Clear();
        Assert.Equal(new[] { first, second }, result);
        Assert.Throws<NotSupportedException>(() => ((IList<Zone>)result).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<Zone>)result)[0] = second);
    }

    [Fact]
    public void Order_ShouldRejectNullInventory()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            ZoneEvaluationPolicy.Order(TargetQuality.Good, null!, []));

        Assert.Equal("inventory", exception.ParamName);
    }

    [Fact]
    public void Order_ShouldRejectNullEligibleZones()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            ZoneEvaluationPolicy.Order(TargetQuality.Good, RemainingInventoryState.Create([]), null!));

        Assert.Equal("eligibleZones", exception.ParamName);
    }

    [Fact]
    public void Order_ShouldRejectNullZoneElements_EvenWithEmptyInventory()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ZoneEvaluationPolicy.Order(TargetQuality.Good, RemainingInventoryState.Create([]), [null!]));

        Assert.Equal("eligibleZones", exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Order_ShouldRejectDuplicateEligibleZoneIds_EvenWithEmptyInventory(bool sameObject)
    {
        var zone = CreateZone(1, ZoneType.MiddleCenter);
        var duplicate = sameObject ? zone : CreateZone(1, ZoneType.MiddleLeft);

        var exception = Assert.Throws<ArgumentException>(() =>
            ZoneEvaluationPolicy.Order(TargetQuality.Good, RemainingInventoryState.Create([]), [zone, duplicate]));

        Assert.Equal("eligibleZones", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void Order_ShouldRejectUndefinedTargetQuality_EvenWithEmptyInputs(int value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ZoneEvaluationPolicy.Order((TargetQuality)value, RemainingInventoryState.Create([]), []));

        Assert.Equal("targetQuality", exception.ParamName);
        Assert.Equal((TargetQuality)value, exception.ActualValue);
    }

    private static Zone CreateZone(int id, ZoneType type) => Zone.Create(
        ZoneId.Create(Guid.Parse($"20000000-0000-0000-0000-{id:D12}")), $"Zone {id}", type);

    private static ZoneAvailabilityState Entry(Zone zone, int count) => ZoneAvailabilityState.Create(
        zone.Id, ZoneExperienceQualityPolicy.GetQuality(zone.Type), count);
}
