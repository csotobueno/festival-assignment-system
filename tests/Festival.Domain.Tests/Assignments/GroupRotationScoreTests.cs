using Festival.Domain.Assignments;

namespace Festival.Domain.Tests.Assignments;

public sealed class GroupRotationScoreTests
{
    [Fact]
    public void Calculate_ShouldEqualIndividualScore_WhenGroupHasOneMember()
    {
        var history = FairnessHistory.Create(
            [ExperienceQuality.Bad, ExperienceQuality.Bad, ExperienceQuality.Medium]);

        Assert.Equal(RotationScore.Calculate(history), GroupRotationScore.Calculate([history]));
    }

    [Fact]
    public void Calculate_ShouldReturnMean_WhenMembersHaveMixedScores()
    {
        var positive = FairnessHistory.Create(
            [ExperienceQuality.Good, ExperienceQuality.Bad, ExperienceQuality.Bad,
                ExperienceQuality.Bad, ExperienceQuality.Medium]);
        var neutral = FairnessHistory.Create(
            [ExperienceQuality.Good, ExperienceQuality.Bad, ExperienceQuality.Medium]);
        var negative = FairnessHistory.Create(
            [ExperienceQuality.Bad, ExperienceQuality.Medium, ExperienceQuality.Good]);

        // Individual scores: 2.00, 0.00, -0.50.
        Assert.Equal(0.50m, GroupRotationScore.Calculate([positive, neutral, negative]));
    }

    [Fact]
    public void Calculate_ShouldIncludeNewAttendeeAsZero_InMemberAverage()
    {
        var experienced = FairnessHistory.Create(
            [ExperienceQuality.Bad, ExperienceQuality.Bad, ExperienceQuality.Medium]);
        var newAttendee = FairnessHistory.Create([]);

        Assert.Equal(1.125m, GroupRotationScore.Calculate([experienced, newAttendee]));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    public void Calculate_ShouldReturnZero_WhenAllMembersAreNew(int memberCount)
    {
        var histories = Enumerable.Range(0, memberCount)
            .Select(_ => FairnessHistory.Create([]));

        Assert.Equal(0m, GroupRotationScore.Calculate(histories));
    }

    [Fact]
    public void Calculate_ShouldReturnSameScore_WhenMemberOrderChanges()
    {
        var first = FairnessHistory.Create([ExperienceQuality.Bad]);
        var second = FairnessHistory.Create([]);
        var third = FairnessHistory.Create([ExperienceQuality.Good]);

        var original = GroupRotationScore.Calculate([first, second, third]);
        var reordered = GroupRotationScore.Calculate([third, first, second]);

        Assert.Equal(original, reordered);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenOpposingMemberNeedsCancel()
    {
        var positive = FairnessHistory.Create(
            [ExperienceQuality.Good, ExperienceQuality.Bad, ExperienceQuality.Bad,
                ExperienceQuality.Bad, ExperienceQuality.Medium]);
        var negative = FairnessHistory.Create(
            [ExperienceQuality.Good, ExperienceQuality.Good, ExperienceQuality.Medium]);

        // Individual scores: +2 and -2; internal dispersion is intentionally ignored.
        Assert.Equal(0m, GroupRotationScore.Calculate([positive, negative]));
    }

    [Fact]
    public void Calculate_ShouldReturnTwo_ForFiveMemberReferenceScenario()
    {
        // Good, then n Bad experiences, then Medium produces the reference scores
        // 2, 4, 1, and 3. The new attendee contributes 0.
        var histories = new[] { 3, 5, 2, 4 }
            .Select(badCount => FairnessHistory.Create(
                new[] { ExperienceQuality.Good }
                    .Concat(Enumerable.Repeat(ExperienceQuality.Bad, badCount))
                    .Append(ExperienceQuality.Medium)))
            .Append(FairnessHistory.Create([]));

        Assert.Equal(2m, GroupRotationScore.Calculate(histories));
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenMemberHistoriesAreNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => GroupRotationScore.Calculate(null!));

        Assert.Equal("memberHistories", exception.ParamName);
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenMemberHistoryIsNull()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => GroupRotationScore.Calculate([FairnessHistory.Create([]), null!]));

        Assert.Equal("memberHistories", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Calculate_ShouldThrow_WhenMemberCountIsOutsideGroupSizeRange(int memberCount)
    {
        var histories = Enumerable.Range(0, memberCount)
            .Select(_ => FairnessHistory.Create([]));

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => GroupRotationScore.Calculate(histories));

        Assert.Equal("value", exception.ParamName);
    }
}
