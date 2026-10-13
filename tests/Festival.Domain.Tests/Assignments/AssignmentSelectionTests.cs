using Festival.Domain.Assignments;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Assignments;

public sealed class AssignmentSelectionTests
{
    private static readonly Zone Zone = CreateZone(1, ZoneType.MiddleCenter);

    [Theory]
    [InlineData(ZoneType.FrontStanding, ExperienceQuality.Good)]
    [InlineData(ZoneType.MiddleCenter, ExperienceQuality.Good)]
    [InlineData(ZoneType.MiddleLeft, ExperienceQuality.Medium)]
    [InlineData(ZoneType.MiddleRight, ExperienceQuality.Medium)]
    [InlineData(ZoneType.UpperCenter, ExperienceQuality.Medium)]
    [InlineData(ZoneType.UpperLeft, ExperienceQuality.Bad)]
    [InlineData(ZoneType.UpperRight, ExperienceQuality.Bad)]
    public void Create_ShouldPreserveZoneBlockAndActualQuality(ZoneType type, ExperienceQuality quality)
    {
        var zone = CreateZone(1, type);
        var block = Block(zone);

        var selection = AssignmentSelection.Create(zone, block, quality);

        Assert.Same(zone, selection.Zone);
        Assert.Same(block, selection.Block);
        Assert.Equal(zone.Id, selection.Block.ZoneId);
        Assert.Equal(quality, selection.ExperienceQuality);
        Assert.Equal(ZoneExperienceQualityPolicy.GetQuality(zone.Type), selection.ExperienceQuality);
    }

    [Fact]
    public void Create_ShouldRejectNullZone()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            AssignmentSelection.Create(null!, Block(Zone), ExperienceQuality.Good));

        Assert.Equal("zone", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldRejectNullBlock()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            AssignmentSelection.Create(Zone, null!, ExperienceQuality.Good));

        Assert.Equal("block", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldRejectBlockFromAnotherZone()
    {
        var other = CreateZone(2, ZoneType.MiddleCenter);

        var exception = Assert.Throws<ArgumentException>(() =>
            AssignmentSelection.Create(Zone, Block(other), ExperienceQuality.Good));

        Assert.Equal("block", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public void Create_ShouldRejectUndefinedQuality(int value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            AssignmentSelection.Create(Zone, Block(Zone), (ExperienceQuality)value));

        Assert.Equal("experienceQuality", exception.ParamName);
        Assert.Equal((ExperienceQuality)value, exception.ActualValue);
    }

    [Theory]
    [InlineData(ZoneType.MiddleCenter, ExperienceQuality.Medium)]
    [InlineData(ZoneType.MiddleCenter, ExperienceQuality.Bad)]
    [InlineData(ZoneType.MiddleLeft, ExperienceQuality.Good)]
    [InlineData(ZoneType.MiddleLeft, ExperienceQuality.Bad)]
    [InlineData(ZoneType.UpperLeft, ExperienceQuality.Good)]
    [InlineData(ZoneType.UpperLeft, ExperienceQuality.Medium)]
    public void Create_ShouldRejectQualityInconsistentWithZone(ZoneType type, ExperienceQuality quality)
    {
        var zone = CreateZone(1, type);

        var exception = Assert.Throws<ArgumentException>(() =>
            AssignmentSelection.Create(zone, Block(zone), quality));

        Assert.Equal("experienceQuality", exception.ParamName);
    }

    private static Zone CreateZone(int id, ZoneType type) => Zone.Create(
        ZoneId.Create(Guid.Parse($"20000000-0000-0000-0000-{id:D12}")), $"Zone {id}", type);

    private static FeasibleSpotBlock Block(Zone zone) => FeasibleSpotBlock.Create(
        [Spot.Create(SpotCode.Create($"{zone.Id}-A-1"), zone.Id, RowCode.Create("A"), SpotNumber.Create(1))]);
}
