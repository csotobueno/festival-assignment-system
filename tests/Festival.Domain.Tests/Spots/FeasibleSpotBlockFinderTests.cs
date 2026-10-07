using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Spots;

public sealed class FeasibleSpotBlockFinderTests
{
    private static readonly Zone Zone = Zone.Create(
        ZoneId.Create(Guid.Parse("20000000-0000-0000-0000-000000000001")),
        "Test", ZoneType.MiddleLeft);

    [Fact]
    public void Find_ShouldReturnSingleSpot_ForGroupSizeOne()
    {
        var spot = CreateSpot("A", 1);
        var block = Assert.Single(Find([spot], 1));

        Assert.Same(spot, Assert.Single(block.Spots));
        Assert.Equal(Zone.Id, block.ZoneId);
        Assert.Equal(spot.RowCode, block.RowCode);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(GroupSize.Maximum)]
    public void Find_ShouldReturnOneCompleteBlock_ForExactFit(int size)
    {
        var spots = Enumerable.Range(1, size).Select(number => CreateSpot("A", number)).ToArray();

        Assert.Equal(spots, Assert.Single(Find(spots, size)).Spots);
    }

    [Fact]
    public void Find_ShouldReturnEveryOverlappingWindow_ForLargerRun()
    {
        var blocks = Find(Numbers(1, 2, 3, 4), 2);

        Assert.Equal(new[] { "A:1,2", "A:2,3", "A:3,4" }, Describe(blocks));
        Assert.Same(blocks[0].Spots[1], blocks[1].Spots[0]);
    }

    [Fact]
    public void Find_ShouldReturnNoBlocks_WhenTotalCapacityHasAGap()
    {
        Assert.Empty(Find(Numbers(1, 2, 4), 3));
    }

    [Fact]
    public void Find_ShouldKeepSeparateRunsIndependent()
    {
        Assert.Equal(new[] { "A:1,2", "A:2,3", "A:6,7", "A:7,8" },
            Describe(Find(Numbers(1, 2, 3, 6, 7, 8), 2)));
    }

    [Fact]
    public void Find_ShouldNotCrossRowBoundaries()
    {
        Assert.Empty(Find([CreateSpot("A", 1), CreateSpot("B", 2)], 2));
    }

    [Fact]
    public void Find_ShouldReturnBlocksFromEveryRow_InOrdinalRowAndNumericPositionOrder()
    {
        Spot[] spots = [CreateSpot("B", 2), CreateSpot("A", 11), CreateSpot("B", 1),
            CreateSpot("A", 9), CreateSpot("A", 10), CreateSpot("a", 12)];

        Assert.Equal(new[] { "A:9,10", "A:10,11", "A:11,12", "B:1,2" },
            Describe(Find(spots, 2)));
        Assert.Equal(Describe(Find(spots, 2)), Describe(Find(spots.Reverse(), 2)));
    }

    [Fact]
    public void Find_ShouldReturnNoPartialBlocks_WhenCapacityIsInsufficient()
    {
        Assert.Empty(Find(Numbers(1, 2), 3));
        Assert.Empty(Find([], 1));
    }

    [Fact]
    public void Find_ShouldHandlePositionsAtIntegerLimit()
    {
        Assert.Equal(new[] { $"A:{int.MaxValue - 1},{int.MaxValue}" },
            Describe(Find(Numbers(int.MaxValue, int.MaxValue - 1), 2)));
    }

    [Fact]
    public void Find_ShouldRejectSpotsFromAnotherZone()
    {
        var foreign = Spot.Create(SpotCode.Create("OTHER"),
            ZoneId.Create(Guid.Parse("20000000-0000-0000-0000-000000000002")),
            RowCode.Create("A"), SpotNumber.Create(2));

        var exception = Assert.Throws<ArgumentException>(() => Find([CreateSpot("A", 1), foreign], 2));
        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Fact]
    public void Find_ShouldRejectDuplicateCodes()
    {
        var first = CreateSpot("A", 1);
        var duplicate = Spot.Create(first.Code, Zone.Id, first.RowCode, SpotNumber.Create(2));

        Assert.Throws<ArgumentException>(() => Find([first, duplicate], 1));
    }

    [Fact]
    public void Find_ShouldRejectDuplicatePhysicalPositions()
    {
        var first = CreateSpot("A", 1);
        var duplicate = Spot.Create(SpotCode.Create("OTHER"), Zone.Id, first.RowCode, first.Number);

        Assert.Throws<ArgumentException>(() => Find([first, duplicate], 1));
        Assert.Throws<ArgumentException>(() => Find([first, first], 1));
    }

    [Fact]
    public void Find_ShouldRejectNullZone()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            FeasibleSpotBlockFinder.Find(null!, [], GroupSize.Create(1)));
        Assert.Equal("zone", exception.ParamName);
    }

    [Fact]
    public void Find_ShouldRejectNullCollection()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => Find(null!, 1));
        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Fact]
    public void Find_ShouldRejectNullElements()
    {
        var exception = Assert.Throws<ArgumentException>(() => Find([null!], 1));
        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Fact]
    public void Find_ShouldRejectUninitializedGroupSize()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            FeasibleSpotBlockFinder.Find(Zone, [], default));
        Assert.Equal("groupSize", exception.ParamName);
    }

    [Fact]
    public void Find_ShouldPreserveInputAndReturnReadOnlyResults()
    {
        Spot[] spots = [CreateSpot("A", 2), CreateSpot("A", 1)];
        var original = spots.ToArray();
        var blocks = Find(spots, 2);

        Assert.Equal(original, spots);
        Assert.Throws<NotSupportedException>(() => ((IList<FeasibleSpotBlock>)blocks).Clear());
    }

    [Fact]
    public void Find_ShouldProduceBlocksAcceptedByExistingAssignmentGroupInvariants()
    {
        var group = AssignmentGroup.Create(
            AssignmentRequestId.New(), FestivalDayId.New(), [AttendeeId.New(), AttendeeId.New()]);
        var blocks = Find(Numbers(1, 2, 3, 6, 7), 2);
        Assert.Equal(3, blocks.Count);

        foreach (var block in blocks)
        {
            var assignments = group.AttendeeIds.Zip(block.Spots).Select(pair => Assignment.Create(
                AssignmentId.New(), group.AssignmentRequestId, group.FestivalDayId, pair.First,
                pair.Second.Code, pair.Second.ZoneId, pair.Second.RowCode, pair.Second.Number,
                new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero)));

            Assert.Null(Record.Exception(() => group.EnsureValidResult(assignments)));
        }
    }

    private static IReadOnlyList<FeasibleSpotBlock> Find(IEnumerable<Spot> spots, int size) =>
        FeasibleSpotBlockFinder.Find(Zone, spots, GroupSize.Create(size));

    private static Spot CreateSpot(string row, int number) => Spot.Create(
        SpotCode.Create($"{row}-{number}"), Zone.Id, RowCode.Create(row), SpotNumber.Create(number));

    private static Spot[] Numbers(params int[] numbers) =>
        numbers.Select(number => CreateSpot("A", number)).ToArray();

    private static string[] Describe(IEnumerable<FeasibleSpotBlock> blocks) =>
        blocks.Select(block => $"{block.RowCode.Value}:{string.Join(",", block.Spots.Select(spot => spot.Number.Value))}")
            .ToArray();
}
