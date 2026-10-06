using Festival.Domain.Assignments;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Zones;

public sealed class ZoneExperienceQualityPolicyTests
{
    [Theory]
    [InlineData(ZoneType.FrontStanding, ExperienceQuality.Good)]
    [InlineData(ZoneType.MiddleCenter, ExperienceQuality.Good)]
    [InlineData(ZoneType.MiddleLeft, ExperienceQuality.Medium)]
    [InlineData(ZoneType.MiddleRight, ExperienceQuality.Medium)]
    [InlineData(ZoneType.UpperCenter, ExperienceQuality.Medium)]
    [InlineData(ZoneType.UpperLeft, ExperienceQuality.Bad)]
    [InlineData(ZoneType.UpperRight, ExperienceQuality.Bad)]
    public void GetQuality_ShouldReturnAgreedClassification_WhenZoneTypeIsValid(
        ZoneType zoneType,
        ExperienceQuality expectedQuality)
    {
        var quality = ZoneExperienceQualityPolicy.GetQuality(zoneType);

        Assert.Equal(expectedQuality, quality);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(int.MaxValue)]
    public void GetQuality_ShouldThrow_WhenZoneTypeIsUndefined(int value)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => ZoneExperienceQualityPolicy.GetQuality((ZoneType)value));

        Assert.Equal("zoneType", exception.ParamName);
        Assert.Equal((ZoneType)value, exception.ActualValue);
    }
}
