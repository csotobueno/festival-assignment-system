using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Spots;

public sealed class SpotTests
{
    [Fact]
    public void Create_ShouldReturnSpot_WhenDataIsValid()
    {
        var code = SpotCode.Create("FRONT-A-010");
        var zoneId = ZoneId.New();
        var rowCode = RowCode.Create("A");
        var number = SpotNumber.Create(10);

        var spot = Spot.Create(
            code,
            zoneId,
            rowCode,
            number);

        Assert.Equal(code, spot.Code);
        Assert.Equal(zoneId, spot.ZoneId);
        Assert.Equal(rowCode, spot.RowCode);
        Assert.Equal(number, spot.Number);
    }

    [Fact]
    public void Create_ShouldThrow_WhenCodeIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => Spot.Create(
                null!,
                ZoneId.New(),
                RowCode.Create("A"),
                SpotNumber.Create(10)));

        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenZoneIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Spot.Create(
                SpotCode.Create("FRONT-A-010"),
                default,
                RowCode.Create("A"),
                SpotNumber.Create(10)));

        Assert.Equal("zoneId", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenRowCodeIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => Spot.Create(
                SpotCode.Create("FRONT-A-010"),
                ZoneId.New(),
                null!,
                SpotNumber.Create(10)));

        Assert.Equal("rowCode", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenSpotNumberIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Spot.Create(
                SpotCode.Create("FRONT-A-010"),
                ZoneId.New(),
                RowCode.Create("A"),
                default));

        Assert.Equal("number", exception.ParamName);
    }
}