using Festival.Domain.Assignments;

namespace Festival.Domain.Tests.Assignments;

public sealed class HistoricalDeficitTests
{
    [Fact]
    public void Calculate_ShouldReturnZero_WhenHistoryIsEmpty()
    {
        var history = FairnessHistory.Create([]);

        Assert.Equal(0, HistoricalDeficit.Calculate(history));
    }

    [Fact]
    public void Calculate_ShouldReturnPositiveDeficit_WhenUnfavorableExperiencesAccumulate()
    {
        var history = FairnessHistory.Create(
            [ExperienceQuality.Bad, ExperienceQuality.Bad, ExperienceQuality.Medium]);

        Assert.Equal(2, HistoricalDeficit.Calculate(history));
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenExperiencesBalance()
    {
        var history = FairnessHistory.Create(
            [ExperienceQuality.Good, ExperienceQuality.Medium, ExperienceQuality.Bad]);

        Assert.Equal(0, HistoricalDeficit.Calculate(history));
    }

    [Fact]
    public void Calculate_ShouldReturnNegativeDeficit_WhenFavorableExperiencesAccumulate()
    {
        var history = FairnessHistory.Create(
            [ExperienceQuality.Good, ExperienceQuality.Good, ExperienceQuality.Medium]);

        Assert.Equal(-2, HistoricalDeficit.Calculate(history));
    }

    [Theory]
    [InlineData(ExperienceQuality.Good, -1)]
    [InlineData(ExperienceQuality.Medium, 0)]
    [InlineData(ExperienceQuality.Bad, 1)]
    public void Calculate_ShouldReturnContribution_WhenHistoryContainsOneExperience(
        ExperienceQuality quality,
        int expectedDeficit)
    {
        var history = FairnessHistory.Create([quality]);

        Assert.Equal(expectedDeficit, HistoricalDeficit.Calculate(history));
    }

    [Fact]
    public void Calculate_ShouldReturnSameDeficit_WhenChronologicalOrderDiffers()
    {
        var firstHistory = FairnessHistory.Create(
            [ExperienceQuality.Good, ExperienceQuality.Medium, ExperienceQuality.Bad]);
        var secondHistory = FairnessHistory.Create(
            [ExperienceQuality.Bad, ExperienceQuality.Medium, ExperienceQuality.Good]);

        Assert.Equal(0, HistoricalDeficit.Calculate(firstHistory));
        Assert.Equal(0, HistoricalDeficit.Calculate(secondHistory));
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenHistoryIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => HistoricalDeficit.Calculate(null!));

        Assert.Equal("history", exception.ParamName);
    }
}
