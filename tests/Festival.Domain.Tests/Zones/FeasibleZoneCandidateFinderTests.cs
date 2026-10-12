using Festival.Domain.Assignments;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Zones;

public sealed class FeasibleZoneCandidateFinderTests
{
    private static readonly Zone First = CreateZone(1, ZoneType.UpperLeft);
    private static readonly Zone Second = CreateZone(2, ZoneType.MiddleCenter);
    private static readonly Zone Other = CreateZone(3, ZoneType.MiddleLeft);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Find_ShouldUseSuppliedZoneOrder_EvenWhenLaterZoneHasMoreBlocks(bool reverse)
    {
        Zone[] zones = reverse ? [Second, First] : [First, Second];
        var spots = Run(First, 2).Concat(Run(Second, 6)).ToArray();

        var candidate = FeasibleZoneCandidateFinder.Find(zones, spots, GroupSize.Create(2));

        Assert.NotNull(candidate);
        Assert.Same(zones[0], candidate.Zone);
        Assert.Equal(reverse ? 5 : 1, candidate.FeasibleBlocks.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    public void Find_ShouldSkipAbsentInsufficientOrFragmentedZone_AndReturnNextFeasible(int count)
    {
        var firstSpots = count == 3
            ? new[] { CreateSpot(First, 1), CreateSpot(First, 2), CreateSpot(First, 4) }
            : Run(First, count);
        var spots = firstSpots.Concat(Run(Second, 3));

        var candidate = FeasibleZoneCandidateFinder.Find([First, Second], spots, GroupSize.Create(3));

        Assert.NotNull(candidate);
        Assert.Same(Second, candidate.Zone);
        Assert.Equal(new[] { 1, 2, 3 }, Assert.Single(candidate.FeasibleBlocks).Spots.Select(spot => spot.Number.Value));
    }

    [Theory]
    [InlineData(GroupSize.Minimum)]
    [InlineData(3)]
    [InlineData(GroupSize.Maximum)]
    public void Find_ShouldReturnExactGroupSizeBlock(int size)
    {
        var spots = Run(First, size);

        var candidate = FeasibleZoneCandidateFinder.Find([First], spots, GroupSize.Create(size));

        Assert.NotNull(candidate);
        Assert.Equal(spots, Assert.Single(candidate.FeasibleBlocks).Spots);
    }

    [Fact]
    public void Find_ShouldReturnEveryBlockInSelectedZone_InExistingFinderOrder()
    {
        Spot[] spots = [CreateSpot(First, 2, "B"), CreateSpot(Second, 1), CreateSpot(First, 3),
            CreateSpot(First, 1), CreateSpot(First, 1, "B"), CreateSpot(First, 2), CreateSpot(Second, 2)];
        var size = GroupSize.Create(2);
        var expected = FeasibleSpotBlockFinder.Find(First, spots.Where(spot => spot.ZoneId == First.Id), size);

        var candidate = FeasibleZoneCandidateFinder.Find([First, Second], spots, size);
        var permuted = FeasibleZoneCandidateFinder.Find([First, Second], spots.Reverse(), size);

        Assert.NotNull(candidate);
        Assert.NotNull(permuted);
        Assert.Same(First, candidate.Zone);
        Assert.Equal(3, candidate.FeasibleBlocks.Count);
        Assert.Equal(Describe(expected), Describe(candidate.FeasibleBlocks));
        Assert.Equal(Describe(candidate.FeasibleBlocks), Describe(permuted.FeasibleBlocks));
        Assert.All(candidate.FeasibleBlocks, block => Assert.Equal(First.Id, block.ZoneId));
    }

    [Fact]
    public void Find_ShouldNotCombineSpotsAcrossZonesOrRows()
    {
        Spot[] spots = [CreateSpot(First, 1), CreateSpot(Second, 2), CreateSpot(First, 2, "B")];

        var candidate = FeasibleZoneCandidateFinder.Find([First, Second], spots, GroupSize.Create(2));

        Assert.Null(candidate);
    }

    [Fact]
    public void Find_ShouldReturnNull_WhenEveryZoneIsInfeasible()
    {
        var spots = new[] { CreateSpot(First, 1), CreateSpot(First, 3), CreateSpot(Second, 1) };

        Assert.Null(FeasibleZoneCandidateFinder.Find([First, Second], spots, GroupSize.Create(2)));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Find_ShouldReturnNull_WhenNoOrderedZoneHasMatchingSpots(bool hasZones, bool hasSpots)
    {
        Assert.Null(FeasibleZoneCandidateFinder.Find(
            hasZones ? [First, Second] : [], hasSpots ? Run(Other, 3) : [], GroupSize.Create(2)));
    }

    [Fact]
    public void Find_ShouldPreserveInputs_AndReturnReadOnlyBlocks()
    {
        Zone[] zones = [First, Second];
        Spot[] spots = [CreateSpot(First, 2), CreateSpot(First, 1), CreateSpot(Second, 1)];
        var originalSpots = spots.ToArray();

        var candidate = FeasibleZoneCandidateFinder.Find(zones, spots, GroupSize.Create(2));

        Assert.NotNull(candidate);
        Assert.Equal(new[] { First, Second }, zones);
        Assert.Equal(originalSpots, spots);
        spots[0] = CreateSpot(Other, 9);
        Assert.Equal(new[] { originalSpots[1], originalSpots[0] }, candidate.FeasibleBlocks[0].Spots);
        Assert.Throws<NotSupportedException>(() => ((IList<FeasibleSpotBlock>)candidate.FeasibleBlocks).Clear());
    }

    [Fact]
    public void Find_ShouldRejectNullOrderedZones()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            FeasibleZoneCandidateFinder.Find(null!, [], GroupSize.Create(1)));
        Assert.Equal("orderedZones", exception.ParamName);
    }

    [Fact]
    public void Find_ShouldRejectNullAvailableSpots()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            FeasibleZoneCandidateFinder.Find([], null!, GroupSize.Create(1)));
        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Fact]
    public void Find_ShouldRejectNullZoneAfterFeasibleZone()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            FeasibleZoneCandidateFinder.Find([First, null!], Run(First, 1), GroupSize.Create(1)));
        Assert.Equal("orderedZones", exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Find_ShouldRejectDuplicateZoneIds(bool sameObject)
    {
        var duplicate = sameObject ? First : CreateZone(1, ZoneType.MiddleCenter);

        var exception = Assert.Throws<ArgumentException>(() =>
            FeasibleZoneCandidateFinder.Find([First, duplicate], Run(First, 1), GroupSize.Create(1)));
        Assert.Equal("orderedZones", exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Find_ShouldRejectNullSpotBeforeTraversal_EvenWithoutOrderedZones(bool hasZones)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            FeasibleZoneCandidateFinder.Find(hasZones ? [First] : [], [CreateSpot(First, 1), null!], GroupSize.Create(1)));
        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Find_ShouldRejectDuplicateCodes_InLaterOrUnlistedZone_BeforeFirstZoneSucceeds(bool listed)
    {
        var later = CreateSpot(Second, 1);
        var duplicate = Spot.Create(later.Code, Second.Id, later.RowCode, SpotNumber.Create(2));

        var exception = Assert.Throws<ArgumentException>(() => FeasibleZoneCandidateFinder.Find(
            listed ? [First, Second] : [First], [CreateSpot(First, 1), later, duplicate], GroupSize.Create(1)));
        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Fact]
    public void Find_ShouldRejectDuplicateCodesAcrossZones()
    {
        var first = CreateSpot(First, 1);
        var duplicate = Spot.Create(first.Code, Second.Id, first.RowCode, SpotNumber.Create(1));

        var exception = Assert.Throws<ArgumentException>(() =>
            FeasibleZoneCandidateFinder.Find([First, Second], [first, duplicate], GroupSize.Create(1)));
        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Find_ShouldRejectDuplicatePositionsInLaterZone_BeforeFirstZoneSucceeds(bool sameObject)
    {
        var later = CreateSpot(Second, 1);
        var duplicate = sameObject ? later : Spot.Create(
            SpotCode.Create("DUPLICATE"), Second.Id, later.RowCode, later.Number);

        var exception = Assert.Throws<ArgumentException>(() => FeasibleZoneCandidateFinder.Find(
            [First, Second], [CreateSpot(First, 1), later, duplicate], GroupSize.Create(1)));
        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Fact]
    public void Find_ShouldRejectUninitializedGroupSize_EvenWithEmptyInputs()
    {
        var exception = Assert.Throws<ArgumentException>(() => FeasibleZoneCandidateFinder.Find([], [], default));
        Assert.Equal("groupSize", exception.ParamName);
    }

    private static Zone CreateZone(int id, ZoneType type) => Zone.Create(
        ZoneId.Create(Guid.Parse($"20000000-0000-0000-0000-{id:D12}")), $"Zone {id}", type);

    private static Spot CreateSpot(Zone zone, int number, string row = "A") => Spot.Create(
        SpotCode.Create($"{zone.Id}-{row}-{number}"), zone.Id, RowCode.Create(row), SpotNumber.Create(number));

    private static Spot[] Run(Zone zone, int count) => Enumerable.Range(1, count)
        .Select(number => CreateSpot(zone, number)).ToArray();

    private static string[] Describe(IEnumerable<FeasibleSpotBlock> blocks) => blocks
        .Select(block => string.Join(",", block.Spots.Select(spot => spot.Code.Value))).ToArray();
}
