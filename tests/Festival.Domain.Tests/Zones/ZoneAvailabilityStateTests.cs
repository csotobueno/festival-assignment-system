using Festival.Domain.Assignments;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Zones;

public sealed class ZoneAvailabilityStateTests
{
    private static readonly ZoneId ZoneId = ZoneId.Create(
        Guid.Parse("20000000-0000-0000-0000-000000000001"));

    [Theory]
    [InlineData(ExperienceQuality.Good, 0)]
    [InlineData(ExperienceQuality.Good, 3)]
    [InlineData(ExperienceQuality.Medium, 5)]
    [InlineData(ExperienceQuality.Bad, int.MaxValue)]
    public void Create_ShouldPreserveZoneQualityAndNonNegativeCount(ExperienceQuality quality, int count)
    {
        var state = ZoneAvailabilityState.Create(ZoneId, quality, count);

        Assert.Equal(ZoneId, state.ZoneId);
        Assert.Equal(quality, state.ExperienceQuality);
        Assert.Equal(count, state.AvailableSpotCount);
        Assert.Equal(state, ZoneAvailabilityState.Create(ZoneId, quality, count));
    }

    [Fact]
    public void Create_ShouldRejectMissingZoneId()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ZoneAvailabilityState.Create(default, ExperienceQuality.Good, 1));

        Assert.Equal("zoneId", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public void Create_ShouldRejectUndefinedQuality(int quality)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ZoneAvailabilityState.Create(ZoneId, (ExperienceQuality)quality, 1));

        Assert.Equal("experienceQuality", exception.ParamName);
        Assert.Equal((ExperienceQuality)quality, exception.ActualValue);
    }

    [Fact]
    public void Create_ShouldRejectNegativeCount()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ZoneAvailabilityState.Create(ZoneId, ExperienceQuality.Good, -1));

        Assert.Equal("availableSpotCount", exception.ParamName);
    }
}
