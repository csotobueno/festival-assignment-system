using Festival.Domain.Attendees;

namespace Festival.Domain.Tests.Attendees;

public sealed class AttendeeCodeTests
{
    [Fact]
    public void Create_ShouldReturnCode_WhenValueIsValid()
    {
        var code = AttendeeCode.Create("ATT-001");

        Assert.Equal("ATT-001", code.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenValueIsEmpty(string? value)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => AttendeeCode.Create(value));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldTrimAndNormalizeValue()
    {
        var code = AttendeeCode.Create("  att-001  ");

        Assert.Equal("ATT-001", code.Value);
    }

    [Fact]
    public void Codes_ShouldBeEqual_WhenNormalizedValuesAreEqual()
    {
        var first = AttendeeCode.Create("ATT-001");
        var second = AttendeeCode.Create(" att-001 ");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Codes_ShouldNotBeEqual_WhenValuesAreDifferent()
    {
        var first = AttendeeCode.Create("ATT-001");
        var second = AttendeeCode.Create("ATT-002");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ToString_ShouldReturnCodeValue()
    {
        var code = AttendeeCode.Create("ATT-001");

        Assert.Equal("ATT-001", code.ToString());
    }
}