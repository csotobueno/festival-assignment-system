using Festival.Domain.Assignments;

namespace Festival.Domain.Tests.Assignments;

public sealed class RemainingInventoryStateTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(120, 180, 60)]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue)]
    public void Create_ShouldPreserveNonNegativeCounts(int good, int medium, int bad)
    {
        var state = RemainingInventoryState.Create(good, medium, bad);

        Assert.Equal(good, state.GoodRemaining);
        Assert.Equal(medium, state.MediumRemaining);
        Assert.Equal(bad, state.BadRemaining);
    }

    [Theory]
    [InlineData(-1, 0, 0, "goodRemaining")]
    [InlineData(0, -1, 0, "mediumRemaining")]
    [InlineData(0, 0, -1, "badRemaining")]
    public void Create_ShouldRejectNegativeCounts(int good, int medium, int bad, string parameter)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            RemainingInventoryState.Create(good, medium, bad));

        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact]
    public void Default_ShouldRepresentEmptyInventory()
    {
        Assert.Equal(RemainingInventoryState.Create(0, 0, 0), default(RemainingInventoryState));
    }
}
