using Festival.Domain.Assignments;

namespace Festival.Domain.Tests.Assignments;

public sealed class GoodExperienceDeficitTests
{
    [Fact]
    public void Calculate_ShouldReturnZero_WhenHistoryIsEmpty()
    {
        var history = FairnessHistory.Create([]);

        Assert.Equal(0, GoodExperienceDeficit.Calculate(history));
    }

    [Theory]
    [InlineData(ExperienceQuality.Medium)]
    [InlineData(ExperienceQuality.Bad, ExperienceQuality.Medium, ExperienceQuality.Bad)]
    [InlineData(ExperienceQuality.Medium, ExperienceQuality.Medium, ExperienceQuality.Medium)]
    public void Calculate_ShouldReturnOne_WhenNonEmptyHistoryContainsNoGood(
        params ExperienceQuality[] experiences)
    {
        var history = FairnessHistory.Create(experiences);

        Assert.Equal(1, GoodExperienceDeficit.Calculate(history));
    }

    [Theory]
    [InlineData(ExperienceQuality.Good, ExperienceQuality.Bad, ExperienceQuality.Bad)]
    [InlineData(ExperienceQuality.Bad, ExperienceQuality.Good, ExperienceQuality.Medium)]
    [InlineData(ExperienceQuality.Bad, ExperienceQuality.Medium, ExperienceQuality.Good)]
    [InlineData(ExperienceQuality.Good, ExperienceQuality.Medium, ExperienceQuality.Good)]
    public void Calculate_ShouldReturnZero_WhenHistoryContainsGood(
        params ExperienceQuality[] experiences)
    {
        var history = FairnessHistory.Create(experiences);

        Assert.Equal(0, GoodExperienceDeficit.Calculate(history));
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenHistoryIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => GoodExperienceDeficit.Calculate(null!));

        Assert.Equal("history", exception.ParamName);
    }
}
