using Festival.Domain.Spots;

namespace Festival.Domain.Tests.Spots;

public sealed class SpotNumberTests
{
    [Fact]
    public void Create_ShouldReturnNumber_WhenValueIsPositive()
    {
        var number = SpotNumber.Create(10);

        Assert.Equal(10, number.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Create_ShouldThrow_WhenValueIsNotPositive(int value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => SpotNumber.Create(value));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Numbers_ShouldBeEqual_WhenValuesAreEqual()
    {
        var first = SpotNumber.Create(10);
        var second = SpotNumber.Create(10);

        Assert.Equal(first, second);
    }
}