using Festival.Domain.Assignments;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Assignments;

public sealed class AssignmentSelectionPolicyTests
{
    private static readonly Zone Good = CreateZone(1, ZoneType.MiddleCenter);
    private static readonly Zone Medium = CreateZone(2, ZoneType.MiddleLeft);
    private static readonly Zone Bad = CreateZone(3, ZoneType.UpperLeft);

    [Fact]
    public void Select_ShouldReturnGoodZoneAndActualGoodQuality_ForGoodTarget()
    {
        var spots = Run(Good, 3).Concat(Run(Medium, 6)).Concat(Run(Bad, 9)).ToArray();
        var inventory = Inventory([Bad, Medium, Good], spots);

        var selection = AssignmentSelectionPolicy.Select(
            TargetQuality.Good, inventory, [Bad, Medium, Good], spots, GroupSize.Create(3));

        AssertSelection(selection, Good, ExperienceQuality.Good, 3);
    }

    [Fact]
    public void Select_ShouldDegradeGoodTargetToActualMedium_WhenGoodZoneIsFragmented()
    {
        Spot[] spots = [CreateSpot(Good, 1), CreateSpot(Good, 2), CreateSpot(Good, 4),
            .. Run(Medium, 3), .. Run(Bad, 3)];
        var inventory = Inventory([Good, Medium, Bad], spots);

        var selection = AssignmentSelectionPolicy.Select(
            TargetQuality.Good, inventory, [Bad, Medium, Good], spots, GroupSize.Create(3));

        AssertSelection(selection, Medium, ExperienceQuality.Medium, 3);
    }

    [Fact]
    public void Select_ShouldDegradeGoodTargetToActualBad_WhenGoodAndMediumAreInfeasible()
    {
        Spot[] spots = [.. Run(Good, 2), CreateSpot(Medium, 1), CreateSpot(Medium, 3), .. Run(Bad, 3)];
        var inventory = Inventory([Good, Medium, Bad], spots);

        var selection = AssignmentSelectionPolicy.Select(
            TargetQuality.Good, inventory, [Good, Medium, Bad], spots, GroupSize.Create(3));

        AssertSelection(selection, Bad, ExperienceQuality.Bad, 3);
    }

    [Theory]
    [InlineData(4, 3, ExperienceQuality.Good)]
    [InlineData(3, 4, ExperienceQuality.Medium)]
    [InlineData(3, 3, ExperienceQuality.Medium)]
    public void Select_ShouldHonorInventoryPreference_AndResolveActualQuality_ForMediumTarget(
        int goodCount, int mediumCount, ExperienceQuality actualQuality)
    {
        var spots = Run(Good, goodCount).Concat(Run(Medium, mediumCount)).ToArray();
        var inventory = Inventory([Good, Medium], spots);

        var selection = AssignmentSelectionPolicy.Select(
            TargetQuality.Medium, inventory, [Good, Medium], spots, GroupSize.Create(3));

        AssertSelection(selection, actualQuality == ExperienceQuality.Good ? Good : Medium, actualQuality, 3);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Select_ShouldHonorSameQualityZoneOrder_AndSkipEarlierInfeasibleZone(bool earlierFeasible)
    {
        var higherAvailability = CreateZone(4, ZoneType.FrontStanding);
        var earlierSpots = earlierFeasible ? Run(higherAvailability, 5)
            : new[] { CreateSpot(higherAvailability, 1), CreateSpot(higherAvailability, 3),
                CreateSpot(higherAvailability, 5), CreateSpot(higherAvailability, 7), CreateSpot(higherAvailability, 9) };
        var spots = Run(Good, 3).Concat(earlierSpots).ToArray();
        var inventory = Inventory([Good, higherAvailability], spots);

        var selection = AssignmentSelectionPolicy.Select(
            TargetQuality.Good, inventory, [Good, higherAvailability], spots, GroupSize.Create(3));

        AssertSelection(selection, earlierFeasible ? higherAvailability : Good, ExperienceQuality.Good, 3);
    }

    [Fact]
    public void Select_ShouldHonorBlockSelection_WithoutReconsideringZonePriority()
    {
        var spots = Run(Good, 6).Concat(Run(Good, 7, "B")).Concat(Run(Medium, 20)).ToArray();
        var size = GroupSize.Create(3);
        var inventory = Inventory([Good, Medium], spots);
        var candidate = FeasibleZoneCandidateFinder.Find([Good], spots, size);
        Assert.NotNull(candidate);
        var expected = BlockSelectionPolicy.Select(candidate, spots);

        var selection = AssignmentSelectionPolicy.Select(
            TargetQuality.Good, inventory, [Medium, Good], spots, size);

        AssertSelection(selection, Good, ExperienceQuality.Good, 3);
        Assert.Equal(expected.Spots, selection!.Block.Spots);
        Assert.Equal(RowCode.Create("B"), selection.Block.RowCode);
        Assert.Equal(new[] { 1, 2, 3 }, selection.Block.Spots.Select(spot => spot.Number.Value));
    }

    [Fact]
    public void Select_ShouldReturnNull_WhenEveryZoneIsPhysicallyInfeasible()
    {
        Spot[] spots = [CreateSpot(Good, 1), CreateSpot(Good, 3), CreateSpot(Medium, 1),
            CreateSpot(Medium, 2, "B"), CreateSpot(Bad, 1)];
        var inventory = Inventory([Good, Medium, Bad], spots);

        Assert.Null(AssignmentSelectionPolicy.Select(
            TargetQuality.Good, inventory, [Good, Medium, Bad], spots, GroupSize.Create(2)));
    }

    [Fact]
    public void Select_ShouldReturnNull_WithEmptyEligibleZones()
    {
        var spots = Run(Good, 3);

        Assert.Null(AssignmentSelectionPolicy.Select(
            TargetQuality.Good, Inventory([Good], spots), [], spots, GroupSize.Create(3)));
    }

    [Fact]
    public void Select_ShouldReturnNull_WithEmptyAvailableSpots()
    {
        Assert.Null(AssignmentSelectionPolicy.Select(
            TargetQuality.Good, RemainingInventoryState.Create([]), [Good], [], GroupSize.Create(3)));
    }

    [Fact]
    public void Select_ShouldReturnNull_WhenNoEligibleZoneParticipatesInInventory()
    {
        var spots = Run(Good, 3).Concat(Run(Medium, 3)).ToArray();

        Assert.Null(AssignmentSelectionPolicy.Select(
            TargetQuality.Good, Inventory([Medium], spots), [Good], spots, GroupSize.Create(3)));
    }

    [Theory]
    [InlineData(TargetQuality.Good)]
    [InlineData(TargetQuality.Medium)]
    public void Select_ShouldPreserveSelection_ForEquivalentInputPermutations(TargetQuality target)
    {
        var otherGood = CreateZone(4, ZoneType.FrontStanding);
        Zone[] zones = [Bad, otherGood, Medium, Good];
        var spots = Run(Good, 6).Concat(Run(otherGood, 6))
            .Concat(Run(Medium, 6)).Concat(Run(Medium, 7, "B")).Concat(Run(Bad, 3)).ToArray();
        var inventory = Inventory(zones, spots);
        var size = GroupSize.Create(3);
        var expected = AssignmentSelectionPolicy.Select(target, inventory, zones, spots, size);
        Assert.NotNull(expected);

        for (var offset = 0; offset < zones.Length; offset++)
        {
            var eligible = zones.Skip(offset).Concat(zones.Take(offset));
            var permutedInventory = RemainingInventoryState.Create(inventory.ZoneAvailabilities.Reverse());
            var permutedSpots = spots.Skip(offset).Concat(spots.Take(offset)).Reverse();

            var selected = AssignmentSelectionPolicy.Select(target, permutedInventory, eligible, permutedSpots, size);

            AssertSelection(selected, expected.Zone, expected.ExperienceQuality, 3);
            Assert.Equal(expected.Block.Spots, selected!.Block.Spots);
        }
    }

    [Fact]
    public void Select_ShouldUseOneAvailabilitySnapshot_ForSingleUseEnumerable()
    {
        var spots = Run(Good, 8);
        var enumerated = false;
        IEnumerable<Spot> AvailableSpots()
        {
            if (enumerated)
            {
                throw new InvalidOperationException("Availability can only be enumerated once.");
            }

            enumerated = true;
            foreach (var spot in spots)
            {
                yield return spot;
            }
        }

        var selection = AssignmentSelectionPolicy.Select(
            TargetQuality.Good, Inventory([Good], spots), [Good], AvailableSpots(), GroupSize.Create(3));

        AssertSelection(selection, Good, ExperienceQuality.Good, 3);
        Assert.Equal(spots.Take(3), selection!.Block.Spots);
    }

    [Fact]
    public void Select_ShouldPreserveInputs_AndReturnImmutableBlockSnapshot()
    {
        var zones = new List<Zone> { Medium, Good };
        var spots = Run(Medium, 3).Concat(Run(Good, 3).Reverse()).ToList();
        var before = spots.ToArray();
        var inventory = Inventory(zones, spots);
        var entries = inventory.ZoneAvailabilities.ToArray();

        var selection = AssignmentSelectionPolicy.Select(
            TargetQuality.Good, inventory, zones, spots, GroupSize.Create(3));

        AssertSelection(selection, Good, ExperienceQuality.Good, 3);
        Assert.Equal(new[] { Medium, Good }, zones);
        Assert.Equal(before, spots);
        Assert.Equal(entries, inventory.ZoneAvailabilities);
        zones.Clear();
        spots.Clear();
        Assert.Equal(before.Where(spot => spot.ZoneId == Good.Id).Reverse(), selection!.Block.Spots);
        Assert.Throws<NotSupportedException>(() => ((IList<Spot>)selection.Block.Spots).Clear());
    }

    [Fact]
    public void Select_ShouldRejectNullInventory()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            AssignmentSelectionPolicy.Select(TargetQuality.Good, null!, [], [], GroupSize.Create(1)));
        Assert.Equal("inventory", exception.ParamName);
    }

    [Fact]
    public void Select_ShouldRejectNullEligibleZones()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => AssignmentSelectionPolicy.Select(
            TargetQuality.Good, RemainingInventoryState.Create([]), null!, [], GroupSize.Create(1)));
        Assert.Equal("eligibleZones", exception.ParamName);
    }

    [Fact]
    public void Select_ShouldRejectNullAvailableSpots()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => AssignmentSelectionPolicy.Select(
            TargetQuality.Good, RemainingInventoryState.Create([]), [], null!, GroupSize.Create(1)));
        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Fact]
    public void Select_ShouldRejectUndefinedTargetQuality()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => AssignmentSelectionPolicy.Select(
            (TargetQuality)2, RemainingInventoryState.Create([]), [], [], GroupSize.Create(1)));
        Assert.Equal("targetQuality", exception.ParamName);
    }

    [Fact]
    public void Select_ShouldRejectDefaultGroupSize_EvenWithoutParticipatingZones()
    {
        var exception = Assert.Throws<ArgumentException>(() => AssignmentSelectionPolicy.Select(
            TargetQuality.Good, RemainingInventoryState.Create([]), [], [], default));
        Assert.Equal("groupSize", exception.ParamName);
    }

    [Fact]
    public void Select_ShouldReuseInventoryQualityValidation_BeforeSelectingFeasibleZone()
    {
        var spots = Run(Good, 1).Concat(Run(Bad, 1)).ToArray();
        var inventory = RemainingInventoryState.Create([
            ZoneAvailabilityState.Create(Good.Id, ExperienceQuality.Good, 1),
            ZoneAvailabilityState.Create(Bad.Id, ExperienceQuality.Medium, 1)]);

        var exception = Assert.Throws<ArgumentException>(() => AssignmentSelectionPolicy.Select(
            TargetQuality.Good, inventory, [Good, Bad], spots, GroupSize.Create(1)));
        Assert.Equal("inventory", exception.ParamName);
    }

    [Fact]
    public void Select_ShouldReuseWholeSnapshotValidation_BeforeReturningEarlySelection()
    {
        var spots = Run(Good, 1).Concat(Run(Bad, 1)).ToArray();
        var inventory = Inventory([Good, Bad], spots);

        var exception = Assert.Throws<ArgumentException>(() => AssignmentSelectionPolicy.Select(
            TargetQuality.Good, inventory, [Good], [.. spots, spots[1]], GroupSize.Create(1)));
        Assert.Equal("availableSpots", exception.ParamName);
    }

    private static void AssertSelection(AssignmentSelection? selection, Zone zone, ExperienceQuality quality, int size)
    {
        Assert.NotNull(selection);
        Assert.Same(zone, selection.Zone);
        Assert.Equal(zone.Id, selection.Block.ZoneId);
        Assert.Equal(size, selection.Block.Spots.Count);
        Assert.Equal(quality, selection.ExperienceQuality);
        Assert.Equal(ZoneExperienceQualityPolicy.GetQuality(zone.Type), selection.ExperienceQuality);
    }

    private static Zone CreateZone(int id, ZoneType type) => Zone.Create(
        ZoneId.Create(Guid.Parse($"20000000-0000-0000-0000-{id:D12}")), $"Zone {id}", type);

    private static Spot CreateSpot(Zone zone, int number, string row = "A") => Spot.Create(
        SpotCode.Create($"{zone.Id}-{row}-{number}"), zone.Id, RowCode.Create(row), SpotNumber.Create(number));

    private static Spot[] Run(Zone zone, int count, string row = "A") => Enumerable.Range(1, count)
        .Select(number => CreateSpot(zone, number, row)).ToArray();

    private static RemainingInventoryState Inventory(IEnumerable<Zone> zones, IReadOnlyCollection<Spot> spots) =>
        RemainingInventoryState.Create(zones.Select(zone => ZoneAvailabilityState.Create(
            zone.Id, ZoneExperienceQualityPolicy.GetQuality(zone.Type), spots.Count(spot => spot.ZoneId == zone.Id))));
}
