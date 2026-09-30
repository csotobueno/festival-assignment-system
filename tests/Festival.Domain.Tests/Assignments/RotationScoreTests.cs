using Festival.Domain.Assignments;

namespace Festival.Domain.Tests.Assignments;

public sealed class RotationScoreTests
{
    public static TheoryData<ExperienceQuality[], decimal> ReferenceScores => new()
    {
        { [ExperienceQuality.Bad, ExperienceQuality.Bad, ExperienceQuality.Medium], 2.25m },
        { [ExperienceQuality.Good, ExperienceQuality.Bad, ExperienceQuality.Bad], 1.50m },
        { [ExperienceQuality.Medium, ExperienceQuality.Medium, ExperienceQuality.Medium], 0.25m },
        { [ExperienceQuality.Good, ExperienceQuality.Good, ExperienceQuality.Medium], -2.00m },
        { [ExperienceQuality.Bad, ExperienceQuality.Bad, ExperienceQuality.Good], 0.50m }
    };

    [Fact]
    public void Calculate_ShouldReturnZero_WhenHistoryIsEmpty()
    {
        var history = FairnessHistory.Create([]);

        Assert.Equal(0m, RotationScore.Calculate(history));
    }

    [Theory]
    [MemberData(nameof(ReferenceScores))]
    public void Calculate_ShouldReturnExactScore_ForReferenceHistory(
        ExperienceQuality[] experiences,
        decimal expectedScore)
    {
        var history = FairnessHistory.Create(experiences);

        Assert.Equal(expectedScore, RotationScore.Calculate(history));
    }

    [Fact]
    public void Calculate_ShouldOrderReferenceAttendees_ByRecoveryNeed()
    {
        var juan = RotationScore.Calculate(FairnessHistory.Create(
            [ExperienceQuality.Bad, ExperienceQuality.Bad, ExperienceQuality.Medium]));
        var luis = RotationScore.Calculate(FairnessHistory.Create(
            [ExperienceQuality.Good, ExperienceQuality.Bad, ExperienceQuality.Bad]));
        var ana = RotationScore.Calculate(FairnessHistory.Create(
            [ExperienceQuality.Medium, ExperienceQuality.Medium, ExperienceQuality.Medium]));
        var pedro = RotationScore.Calculate(FairnessHistory.Create(
            [ExperienceQuality.Good, ExperienceQuality.Good, ExperienceQuality.Medium]));

        Assert.True(juan > luis);
        Assert.True(luis > ana);
        Assert.True(ana > pedro);
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenHistoryIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => RotationScore.Calculate(null!));

        Assert.Equal("history", exception.ParamName);
    }
}
