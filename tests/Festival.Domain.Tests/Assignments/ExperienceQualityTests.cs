using Festival.Domain.Assignments;

namespace Festival.Domain.Tests.Assignments;

public sealed class ExperienceQualityTests
{
    [Theory]
    [InlineData(ExperienceQuality.Good, -1)]
    [InlineData(ExperienceQuality.Medium, 0)]
    [InlineData(ExperienceQuality.Bad, 1)]
    public void GetFairnessContribution_ShouldReturnContribution_WhenQualityIsValid(
        ExperienceQuality quality,
        int expectedContribution)
    {
        var contribution = quality.GetFairnessContribution();

        Assert.Equal(expectedContribution, contribution);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(99)]
    public void GetFairnessContribution_ShouldThrow_WhenQualityIsUndefined(int value)
    {
        var quality = (ExperienceQuality)value;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => quality.GetFairnessContribution());

        Assert.Equal("quality", exception.ParamName);
    }
}
