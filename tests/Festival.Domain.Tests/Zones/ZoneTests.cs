using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Zones;

public sealed class ZoneTests
{
    [Fact]
    public void Create_ShouldReturnZone_WhenDataIsValid()
    {
        var id = ZoneId.New();

        var zone = Zone.Create(id, "Front");

        Assert.Equal(id, zone.Id);
        Assert.Equal("Front", zone.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenNameIsEmpty(string? name)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Zone.Create(ZoneId.New(), name));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var zone = Zone.Create(
            ZoneId.New(),
            "  Front  ");

        Assert.Equal("Front", zone.Name);
    }

    [Fact]
    public void Create_ShouldThrow_WhenIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Zone.Create(default, "Front"));

        Assert.Equal("id", exception.ParamName);
    }
}