using Festival.Domain.Spots;

namespace Festival.Domain.Tests.Spots;

public sealed class RowCodeTests
{
    [Fact]
    public void Create_ShouldReturnCode_WhenValueIsValid()
    {
        var code = RowCode.Create("A");

        Assert.Equal("A", code.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenValueIsEmpty(string? value)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => RowCode.Create(value));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldTrimAndNormalizeValue()
    {
        var code = RowCode.Create(" row-a ");

        Assert.Equal("ROW-A", code.Value);
    }

    [Fact]
    public void Codes_ShouldBeEqual_WhenNormalizedValuesAreEqual()
    {
        var first = RowCode.Create("A");
        var second = RowCode.Create(" a ");

        Assert.Equal(first, second);
    }
}