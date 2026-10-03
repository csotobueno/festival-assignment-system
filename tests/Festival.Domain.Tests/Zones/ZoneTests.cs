using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Zones;

public sealed class ZoneTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_ShouldReturnZone_WhenDataIsValid(bool isFrontStanding)
    {
        var id = ZoneId.New();

        var zone = Zone.Create(id, "Front", isFrontStanding);

        Assert.Equal(id, zone.Id);
        Assert.Equal("Front", zone.Name);
        Assert.Equal(isFrontStanding, zone.IsFrontStanding);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenNameIsEmpty(string? name)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Zone.Create(ZoneId.New(), name, isFrontStanding: false));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var zone = Zone.Create(
            ZoneId.New(),
            "  Front  ", isFrontStanding: false);

        Assert.Equal("Front", zone.Name);
    }

    [Fact]
    public void Create_ShouldThrow_WhenIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Zone.Create(default, "Front", isFrontStanding: false));

        Assert.Equal("id", exception.ParamName);
    }
}