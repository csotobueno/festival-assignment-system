using Festival.Domain.Spots;

namespace Festival.Domain.Tests.Spots;

public sealed class SpotCodeTests
{
    [Fact]
    public void Create_ShouldReturnCode_WhenValueIsValid()
    {
        var code = SpotCode.Create("FRONT-A-010");

        Assert.Equal("FRONT-A-010", code.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenValueIsEmpty(string? value)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => SpotCode.Create(value));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldTrimAndNormalizeValue()
    {
        var code = SpotCode.Create("  front-a-010  ");

        Assert.Equal("FRONT-A-010", code.Value);
    }

    [Fact]
    public void Codes_ShouldBeEqual_WhenNormalizedValuesAreEqual()
    {
        var first = SpotCode.Create("FRONT-A-010");
        var second = SpotCode.Create(" front-a-010 ");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Codes_ShouldNotBeEqual_WhenValuesAreDifferent()
    {
        var first = SpotCode.Create("FRONT-A-010");
        var second = SpotCode.Create("FRONT-A-011");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ToString_ShouldReturnCodeValue()
    {
        var code = SpotCode.Create("FRONT-A-010");

        Assert.Equal("FRONT-A-010", code.ToString());
    }
}