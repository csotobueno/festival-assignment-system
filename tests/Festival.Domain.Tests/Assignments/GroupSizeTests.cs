using Festival.Domain.Assignments;

namespace Festival.Domain.Tests.Assignments;

public sealed class GroupSizeTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Create_ShouldReturnGroupSize_WhenValueIsValid(int value)
    {
        var groupSize = GroupSize.Create(value);

        Assert.Equal(value, groupSize.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Create_ShouldThrow_WhenValueIsOutsideAllowedRange(int value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => GroupSize.Create(value));

        Assert.Equal("value", exception.ParamName);
    }
}
