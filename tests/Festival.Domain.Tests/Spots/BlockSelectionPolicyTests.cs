using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Spots;

public sealed class BlockSelectionPolicyTests
{
    private static readonly Zone Zone = Zone.Create(
        ZoneId.Create(Guid.Parse("20000000-0000-0000-0000-000000000001")), "Test", ZoneType.MiddleCenter);
    private static readonly ZoneId OtherZoneId = ZoneId.Create(
        Guid.Parse("20000000-0000-0000-0000-000000000002"));

    [Fact]
    public void Select_ShouldPreferLargerRemainingRun_OverMiddleBlock()
    {
        var spots = Run("A", 8);
        var edge = Block(spots, 1, 2, 3);
        var middle = Block(spots, 3, 4, 5);

        var selected = BlockSelectionPolicy.Select(Candidate(middle, edge), spots);

        Assert.Same(edge, selected);
    }

    [Fact]
    public void Select_ShouldBreakSymmetricEdgeTie_ByStartingSpotNumber()
    {
        var spots = Run("A", 8);
        var left = Block(spots, 1, 2, 3);
        var right = Block(spots, 6, 7, 8);

        var selected = BlockSelectionPolicy.Select(Candidate(right, left), spots);

        Assert.Same(left, selected);
    }

    [Fact]
    public void Select_ShouldRespectExistingGaps_AndConsiderEveryRemainingRun()
    {
        Spot[] spots = [CreateSpot(1), CreateSpot(2), CreateSpot(3), CreateSpot(6), CreateSpot(7), CreateSpot(8), CreateSpot(9)];
        var firstRun = Block(spots, 1, 2, 3);
        var secondRun = Block(spots, 6, 7, 8);

        var selected = BlockSelectionPolicy.Select(Candidate(secondRun, firstRun), spots);

        Assert.Same(firstRun, selected);
    }

    [Fact]
    public void Select_ShouldCompareRawRemainingRunsAcrossRows_BeforeRowCode()
    {
        var rowA = Run("A", 6);
        var rowB = Run("B", 7);
        var first = Block(rowA, 1, 2, 3);
        var second = Block(rowB, 1, 2, 3);

        var selected = BlockSelectionPolicy.Select(Candidate(first, second), rowA.Concat(rowB));

        Assert.Same(second, selected);
    }

    [Fact]
    public void Select_ShouldUseLargestRunRatherThanTotalRemainingCapacity()
    {
        var rowA = Run("A", 7);
        Spot[] rowB = [CreateSpot(1, "B"), CreateSpot(2, "B"), CreateSpot(3, "B"),
            CreateSpot(6, "B"), CreateSpot(7, "B"), CreateSpot(9, "B"), CreateSpot(10, "B"), CreateSpot(12, "B")];
        var first = Block(rowA, 1, 2, 3);
        var second = Block(rowB, 1, 2, 3);

        var selected = BlockSelectionPolicy.Select(Candidate(second, first), rowA.Concat(rowB));

        Assert.Same(first, selected);
    }

    [Fact]
    public void Select_ShouldBreakEqualRunTie_ByOrdinalRowCode_BeforeStartingSpotNumber()
    {
        Spot[] earlierRow = [CreateSpot(10, "A10"), CreateSpot(11, "A10"), CreateSpot(12, "A10")];
        var laterRow = Run("A2", 3);
        var first = Block(earlierRow, 10, 11);
        var second = Block(laterRow, 1, 2);

        var selected = BlockSelectionPolicy.Select(Candidate(second, first), laterRow.Concat(earlierRow));

        Assert.Same(first, selected);
    }

    [Fact]
    public void Select_ShouldUseLowestBlockNumber_RegardlessOfSpotConstructionOrder()
    {
        var spots = Run("A", 4);
        var first = FeasibleSpotBlock.Create([spots[1], spots[0]]);
        var last = FeasibleSpotBlock.Create([spots[3], spots[2]]);

        Assert.Same(first, BlockSelectionPolicy.Select(Candidate(last, first), spots.Reverse()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void Select_ShouldAcceptCandidateThatConsumesEntireRow(int size)
    {
        var spots = Run("A", size);
        var block = FeasibleSpotBlock.Create(spots);

        Assert.Same(block, BlockSelectionPolicy.Select(Candidate(block), spots));
    }

    [Fact]
    public void Select_ShouldCompareEmptyRowRemainders_AsEqualZeroScores()
    {
        var rowA = Run("A", 2);
        var rowB = Run("B", 2);
        var first = FeasibleSpotBlock.Create(rowA);
        var second = FeasibleSpotBlock.Create(rowB);

        Assert.Same(first, BlockSelectionPolicy.Select(Candidate(second, first), rowB.Concat(rowA)));
    }

    [Fact]
    public void Select_ShouldReturnSingleCandidate_WithRemainingAvailability()
    {
        var spots = Run("A", 8);
        var block = Block(spots, 3, 4, 5);

        Assert.Same(block, BlockSelectionPolicy.Select(Candidate(block), spots));
    }

    [Fact]
    public void Select_ShouldScopeEachScoreToCandidateZoneAndAffectedRow()
    {
        var rowA = Run("A", 6);
        var rowB = Run("B", 7);
        var first = Block(rowA, 1, 2, 3);
        var second = Block(rowB, 1, 2, 3);
        var unrelatedRow = Run("C", 20);
        var otherZone = Enumerable.Range(7, 20).Select(number => CreateSpot(number, "A", OtherZoneId));
        var spots = rowA.Concat(rowB).Concat(unrelatedRow).Concat(otherZone);

        Assert.Same(second, BlockSelectionPolicy.Select(Candidate(first, second), spots));
    }

    [Fact]
    public void Select_ShouldBeIndependentOfBlockAndSpotInputOrder()
    {
        var spots = Run("A", 8);
        var expected = Block(spots, 1, 2, 3);
        FeasibleSpotBlock[] blocks = [Block(spots, 3, 4, 5), Block(spots, 6, 7, 8), expected];

        for (var offset = 0; offset < blocks.Length; offset++)
        {
            var orderedBlocks = blocks.Skip(offset).Concat(blocks.Take(offset));
            var candidate = FeasibleZoneCandidate.Create(Zone, orderedBlocks);
            for (var spotOffset = 0; spotOffset < spots.Length; spotOffset++)
            {
                var orderedSpots = spots.Skip(spotOffset).Concat(spots.Take(spotOffset)).Reverse();

                Assert.Same(expected, BlockSelectionPolicy.Select(candidate, orderedSpots));
            }
        }
    }

    [Fact]
    public void Select_ShouldCountConsecutiveNumbersAtIntegerLimit()
    {
        Spot[] spots = [CreateSpot(1), CreateSpot(int.MaxValue), CreateSpot(int.MaxValue - 1)];
        var low = Block(spots, 1);
        var high = Block(spots, int.MaxValue);

        Assert.Same(low, BlockSelectionPolicy.Select(Candidate(high, low), spots));
    }

    [Fact]
    public void Select_ShouldAcceptSnapshotWithEquivalentSpotValues_InDifferentObjects()
    {
        var spots = Run("A", 8);
        var block = Block(spots, 1, 2, 3);
        var snapshot = spots.Select(spot => Spot.Create(
            spot.Code, spot.ZoneId, spot.RowCode, spot.Number));

        Assert.Same(block, BlockSelectionPolicy.Select(Candidate(block), snapshot));
    }

    [Fact]
    public void Select_ShouldValidateMissingSpotsInEveryBlock_EvenIfAnotherBlockWouldWin()
    {
        var spots = Run("A", 8);
        var edge = Block(spots, 1, 2, 3);
        var middle = Block(spots, 3, 4, 5);

        var exception = Assert.Throws<ArgumentException>(() => BlockSelectionPolicy.Select(
            Candidate(edge, middle), spots.Where(spot => spot.Number.Value != 4)));

        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Fact]
    public void Select_ShouldRejectEmptySnapshot_WhenCandidateSpotsAreMissing()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            BlockSelectionPolicy.Select(Candidate(FeasibleSpotBlock.Create([CreateSpot(1)])), []));

        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Theory]
    [InlineData("code")]
    [InlineData("zone")]
    [InlineData("row")]
    [InlineData("number")]
    public void Select_ShouldRejectCandidateSpotWithChangedIdentityOrPhysicalPosition(string changed)
    {
        var spot = CreateSpot(1);
        var replacement = Spot.Create(
            changed == "code" ? SpotCode.Create("OTHER") : spot.Code,
            changed == "zone" ? OtherZoneId : spot.ZoneId,
            changed == "row" ? RowCode.Create("B") : spot.RowCode,
            changed == "number" ? SpotNumber.Create(2) : spot.Number);

        var exception = Assert.Throws<ArgumentException>(() =>
            BlockSelectionPolicy.Select(Candidate(FeasibleSpotBlock.Create([spot])), [replacement]));

        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Fact]
    public void Select_ShouldRejectNullCandidate()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => BlockSelectionPolicy.Select(null!, []));

        Assert.Equal("candidate", exception.ParamName);
    }

    [Fact]
    public void Select_ShouldRejectNullSnapshot()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            BlockSelectionPolicy.Select(Candidate(FeasibleSpotBlock.Create([CreateSpot(1)])), null!));

        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Fact]
    public void Select_ShouldRejectNullSpotElements_BeforeRanking()
    {
        var spot = CreateSpot(1);
        var exception = Assert.Throws<ArgumentException>(() =>
            BlockSelectionPolicy.Select(Candidate(FeasibleSpotBlock.Create([spot])), [spot, null!]));

        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Fact]
    public void Select_ShouldRejectDuplicateCodes_EvenOutsideCandidateZone()
    {
        var spot = CreateSpot(1);
        var duplicate = Spot.Create(spot.Code, OtherZoneId, RowCode.Create("B"), SpotNumber.Create(2));

        var exception = Assert.Throws<ArgumentException>(() =>
            BlockSelectionPolicy.Select(Candidate(FeasibleSpotBlock.Create([spot])), [spot, duplicate]));

        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Select_ShouldRejectDuplicatePhysicalPositions_EvenOutsideCandidateRow(bool sameObject)
    {
        var spot = CreateSpot(1);
        var other = CreateSpot(2, "B", OtherZoneId);
        var duplicate = sameObject ? other : Spot.Create(
            SpotCode.Create("OTHER"), other.ZoneId, other.RowCode, other.Number);

        var exception = Assert.Throws<ArgumentException>(() => BlockSelectionPolicy.Select(
            Candidate(FeasibleSpotBlock.Create([spot])), [spot, other, duplicate]));

        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Fact]
    public void Select_ShouldPreserveAvailabilityAndCandidateBlocks()
    {
        var spots = Run("A", 8).Reverse().ToArray();
        var before = spots.ToArray();
        var first = Block(spots, 1, 2, 3);
        var second = Block(spots, 3, 4, 5);
        FeasibleSpotBlock[] blocks = [second, first];
        var candidate = Candidate(blocks);

        Assert.Same(first, BlockSelectionPolicy.Select(candidate, spots));
        Assert.Equal(before, spots);
        Assert.Equal(blocks, candidate.FeasibleBlocks);
        Assert.Equal(new[] { 1, 2, 3 }, first.Spots.Select(spot => spot.Number.Value));
    }

    private static FeasibleZoneCandidate Candidate(params FeasibleSpotBlock[] blocks) =>
        FeasibleZoneCandidate.Create(Zone, blocks);

    private static Spot CreateSpot(int number, string row = "A", ZoneId? zoneId = null)
    {
        var id = zoneId ?? Zone.Id;
        return Spot.Create(
            SpotCode.Create($"{id}-{row}-{number}"), id, RowCode.Create(row), SpotNumber.Create(number));
    }

    private static Spot[] Run(string row, int count) => Enumerable.Range(1, count)
        .Select(number => CreateSpot(number, row)).ToArray();

    private static FeasibleSpotBlock Block(IEnumerable<Spot> spots, params int[] numbers) =>
        FeasibleSpotBlock.Create(spots.Where(spot => numbers.Contains(spot.Number.Value)));
}
