using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Zones;

public sealed class FeasibleZoneCandidateTests
{
    private static readonly Zone Zone = Zone.Create(
        ZoneId.Create(Guid.Parse("20000000-0000-0000-0000-000000000001")), "Test", ZoneType.MiddleCenter);

    [Fact]
    public void Create_ShouldPreserveZoneAndBlockOrder_InReadOnlySnapshot()
    {
        var first = Block(Zone, 1);
        var second = Block(Zone, 2);
        var input = new List<FeasibleSpotBlock> { second, first };

        var candidate = FeasibleZoneCandidate.Create(Zone, input);
        input.Clear();

        Assert.Same(Zone, candidate.Zone);
        Assert.Equal(new[] { second, first }, candidate.FeasibleBlocks);
        Assert.Throws<NotSupportedException>(() => ((IList<FeasibleSpotBlock>)candidate.FeasibleBlocks).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<FeasibleSpotBlock>)candidate.FeasibleBlocks)[0] = first);
    }

    [Fact]
    public void Create_ShouldRejectNullZone()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => FeasibleZoneCandidate.Create(null!, [Block(Zone, 1)]));
        Assert.Equal("zone", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldRejectNullBlocks()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => FeasibleZoneCandidate.Create(Zone, null!));
        Assert.Equal("feasibleBlocks", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldRejectEmptyBlocks()
    {
        var exception = Assert.Throws<ArgumentException>(() => FeasibleZoneCandidate.Create(Zone, []));
        Assert.Equal("feasibleBlocks", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldRejectNullBlockElements()
    {
        var exception = Assert.Throws<ArgumentException>(() => FeasibleZoneCandidate.Create(Zone, [Block(Zone, 1), null!]));
        Assert.Equal("feasibleBlocks", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldRejectBlocksFromAnotherZone()
    {
        var other = Zone.Create(ZoneId.Create(Guid.Parse("20000000-0000-0000-0000-000000000002")), "Other", ZoneType.MiddleLeft);

        var exception = Assert.Throws<ArgumentException>(() => FeasibleZoneCandidate.Create(Zone, [Block(Zone, 1), Block(other, 2)]));
        Assert.Equal("feasibleBlocks", exception.ParamName);
    }

    private static FeasibleSpotBlock Block(Zone zone, int number) => FeasibleSpotBlock.Create(
        [Spot.Create(SpotCode.Create($"{zone.Id}-{number}"), zone.Id, RowCode.Create("A"), SpotNumber.Create(number))]);
}
