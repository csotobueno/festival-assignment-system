using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Spots;

public sealed class FeasibleSpotBlockTests
{
    private static readonly ZoneId ZoneId = ZoneId.Create(
        Guid.Parse("20000000-0000-0000-0000-000000000001"));

    [Fact]
    public void Create_ShouldOrderAndCopySpots_WithDerivedZoneAndRow()
    {
        var first = CreateSpot(1);
        Spot[] input = [CreateSpot(2), first];
        var block = FeasibleSpotBlock.Create(input);
        input[1] = CreateSpot(99);

        Assert.Equal(ZoneId, block.ZoneId);
        Assert.Equal(RowCode.Create("A"), block.RowCode);
        Assert.Same(first, block.Spots[0]);
        Assert.Equal(new[] { 1, 2 }, block.Spots.Select(spot => spot.Number.Value));
        Assert.Throws<NotSupportedException>(() => ((IList<Spot>)block.Spots).Clear());
    }

    [Fact]
    public void Create_ShouldRejectNullCollection()
    {
        Assert.Throws<ArgumentNullException>(() => FeasibleSpotBlock.Create(null!));
    }

    [Fact]
    public void Create_ShouldRejectNullElements()
    {
        Assert.Throws<ArgumentException>(() => FeasibleSpotBlock.Create([null!]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Create_ShouldRequireAValidGroupSize(int count)
    {
        var spots = Enumerable.Range(1, count).Select(number => CreateSpot(number));
        Assert.Throws<ArgumentOutOfRangeException>(() => FeasibleSpotBlock.Create(spots));
    }

    [Fact]
    public void Create_ShouldRejectMixedZones()
    {
        var other = Spot.Create(SpotCode.Create("OTHER"),
            ZoneId.Create(Guid.Parse("20000000-0000-0000-0000-000000000002")),
            RowCode.Create("A"), SpotNumber.Create(2));
        Assert.Throws<ArgumentException>(() => FeasibleSpotBlock.Create([CreateSpot(1), other]));
    }

    [Fact]
    public void Create_ShouldRejectMixedRows()
    {
        Assert.Throws<ArgumentException>(() => FeasibleSpotBlock.Create([CreateSpot(1), CreateSpot(2, "B")]));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Create_ShouldRejectRepeatedOrNonConsecutivePositions(int secondNumber)
    {
        Assert.Throws<ArgumentException>(() => FeasibleSpotBlock.Create([CreateSpot(1), CreateSpot(secondNumber)]));
    }

    [Fact]
    public void Create_ShouldRejectRepeatedSpotCodes()
    {
        var first = CreateSpot(1);
        var duplicate = Spot.Create(first.Code, ZoneId, first.RowCode, SpotNumber.Create(2));
        Assert.Throws<ArgumentException>(() => FeasibleSpotBlock.Create([first, duplicate]));
    }

    private static Spot CreateSpot(int number, string row = "A") => Spot.Create(
        SpotCode.Create($"{row}-{number}"), ZoneId, RowCode.Create(row), SpotNumber.Create(number));
}
