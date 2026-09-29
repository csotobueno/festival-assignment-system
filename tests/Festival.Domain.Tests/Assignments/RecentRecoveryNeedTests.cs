using Festival.Domain.Assignments;

namespace Festival.Domain.Tests.Assignments;

public sealed class RecentRecoveryNeedTests
{
    [Fact]
    public void Calculate_ShouldReturnZero_WhenHistoryIsEmpty()
    {
        var history = FairnessHistory.Create([]);

        Assert.Equal(0, RecentRecoveryNeed.Calculate(history));
    }

    [Theory]
    [InlineData(ExperienceQuality.Bad, ExperienceQuality.Medium, ExperienceQuality.Good, -1)]
    [InlineData(ExperienceQuality.Good, ExperienceQuality.Bad, ExperienceQuality.Medium, 0)]
    [InlineData(ExperienceQuality.Good, ExperienceQuality.Medium, ExperienceQuality.Bad, 1)]
    public void Calculate_ShouldReturnLatestRecoveryNeed_WhenHistoryContainsMultipleExperiences(
        ExperienceQuality first,
        ExperienceQuality second,
        ExperienceQuality latest,
        int expectedNeed)
    {
        var history = FairnessHistory.Create([first, second, latest]);

        Assert.Equal(expectedNeed, RecentRecoveryNeed.Calculate(history));
    }

    [Theory]
    [InlineData(ExperienceQuality.Good, -1)]
    [InlineData(ExperienceQuality.Medium, 0)]
    [InlineData(ExperienceQuality.Bad, 1)]
    public void Calculate_ShouldReturnSameNeed_WhenEarlierExperiencesDiffer(
        ExperienceQuality latest,
        int expectedNeed)
    {
        var singleExperience = FairnessHistory.Create([latest]);
        var unfavorableHistory = FairnessHistory.Create(
            [ExperienceQuality.Bad, ExperienceQuality.Bad, latest]);
        var favorableHistory = FairnessHistory.Create(
            [ExperienceQuality.Good, ExperienceQuality.Good, latest]);

        Assert.Equal(expectedNeed, RecentRecoveryNeed.Calculate(singleExperience));
        Assert.Equal(expectedNeed, RecentRecoveryNeed.Calculate(unfavorableHistory));
        Assert.Equal(expectedNeed, RecentRecoveryNeed.Calculate(favorableHistory));
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenHistoryIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => RecentRecoveryNeed.Calculate(null!));

        Assert.Equal("history", exception.ParamName);
    }
}
