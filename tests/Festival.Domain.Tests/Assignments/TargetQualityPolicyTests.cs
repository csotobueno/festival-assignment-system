using Festival.Domain.Assignments;

namespace Festival.Domain.Tests.Assignments;

public sealed class TargetQualityPolicyTests
{
    public static TheoryData<decimal, TargetQuality> ReferenceTargets => new()
    {
        { -2.00m, TargetQuality.Medium },
        { -0.50m, TargetQuality.Medium },
        { 0.00m, TargetQuality.Medium },
        { 0.25m, TargetQuality.Medium },
        { 0.50m, TargetQuality.Medium },
        { 0.99m, TargetQuality.Medium },
        { 1.00m, TargetQuality.Good },
        { 1.01m, TargetQuality.Good },
        { 1.25m, TargetQuality.Good },
        { 1.50m, TargetQuality.Good },
        { 1.75m, TargetQuality.Good },
        { decimal.MinValue, TargetQuality.Medium },
        { decimal.MaxValue, TargetQuality.Good }
    };

    public static TheoryData<ExperienceQuality[], TargetQuality> ReferenceHistories => new()
    {
        { [ExperienceQuality.Good, ExperienceQuality.Bad], TargetQuality.Medium },
        { [ExperienceQuality.Bad, ExperienceQuality.Medium], TargetQuality.Good },
        { [ExperienceQuality.Good, ExperienceQuality.Bad, ExperienceQuality.Bad], TargetQuality.Good },
        { [ExperienceQuality.Good, ExperienceQuality.Bad, ExperienceQuality.Bad,
            ExperienceQuality.Medium], TargetQuality.Good }
    };

    [Theory]
    [MemberData(nameof(ReferenceTargets))]
    public void Calculate_ShouldReturnTarget_ForResultingScore(decimal score, TargetQuality expectedTarget)
    {
        Assert.Equal(expectedTarget, TargetQualityPolicy.Calculate(score));
    }

    [Theory]
    [MemberData(nameof(ReferenceHistories))]
    public void Calculate_ShouldReturnTarget_ForDocumentedIndividualHistory(
        ExperienceQuality[] experiences,
        TargetQuality expectedTarget)
    {
        var score = RotationScore.Calculate(FairnessHistory.Create(experiences));

        Assert.Equal(expectedTarget, TargetQualityPolicy.Calculate(score));
    }

    [Theory]
    [InlineData(2, TargetQuality.Good)]
    [InlineData(3, TargetQuality.Medium)]
    public void Calculate_ShouldUseResultingGroupScore(int memberCount, TargetQuality expectedTarget)
    {
        // Score 2.00 shared with new attendees produces 1.00 or 2/3.
        var experienced = FairnessHistory.Create(
            [ExperienceQuality.Good, ExperienceQuality.Bad, ExperienceQuality.Bad,
                ExperienceQuality.Bad, ExperienceQuality.Medium]);
        var histories = new[] { experienced }.Concat(
            Enumerable.Range(1, memberCount - 1).Select(_ => FairnessHistory.Create([])));
        var score = GroupRotationScore.Calculate(histories);

        Assert.Equal(expectedTarget, TargetQualityPolicy.Calculate(score));
    }

    [Fact]
    public void TargetQuality_ShouldDefineOnlyGoodAndMedium_WithoutBadTarget()
    {
        Assert.Equal(new[] { TargetQuality.Good, TargetQuality.Medium }, Enum.GetValues<TargetQuality>());
    }
}
