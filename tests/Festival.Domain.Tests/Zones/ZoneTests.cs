using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Zones;

public sealed class ZoneTests
{
    [Theory]
    [InlineData(ZoneType.FrontStanding)]
    [InlineData(ZoneType.MiddleLeft)]
    [InlineData(ZoneType.MiddleCenter)]
    [InlineData(ZoneType.MiddleRight)]
    [InlineData(ZoneType.UpperLeft)]
    [InlineData(ZoneType.UpperCenter)]
    [InlineData(ZoneType.UpperRight)]
    public void Create_ShouldReturnZone_WhenDataIsValid(ZoneType type)
    {
        var id = ZoneId.New();

        var zone = Zone.Create(id, "Front", type);

        Assert.Equal(id, zone.Id);
        Assert.Equal("Front", zone.Name);
        Assert.Equal(type, zone.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenNameIsEmpty(string? name)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Zone.Create(ZoneId.New(), name, ZoneType.MiddleLeft));

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(int.MaxValue)]
    public void Create_ShouldRejectUndefinedZoneType(int value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => Zone.Create(ZoneId.New(), "Front", (ZoneType)value));

        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var zone = Zone.Create(
            ZoneId.New(),
            "  Front  ", ZoneType.MiddleLeft);

        Assert.Equal("Front", zone.Name);
    }

    [Fact]
    public void Create_ShouldThrow_WhenIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Zone.Create(default, "Front", ZoneType.MiddleLeft));

        Assert.Equal("id", exception.ParamName);
    }
}
