using Festival.Domain.Assignments;

namespace Festival.Domain.Zones;

public static class ZoneExperienceQualityPolicy
{
    /// <summary>
    /// Returns the global Stage 4 v1 classification, independently of request eligibility.
    /// </summary>
    public static ExperienceQuality GetQuality(ZoneType zoneType)
    {
        return zoneType switch
        {
            ZoneType.FrontStanding or ZoneType.MiddleCenter => ExperienceQuality.Good,
            ZoneType.MiddleLeft or ZoneType.MiddleRight or ZoneType.UpperCenter => ExperienceQuality.Medium,
            ZoneType.UpperLeft or ZoneType.UpperRight => ExperienceQuality.Bad,
            _ => throw new ArgumentOutOfRangeException(
                nameof(zoneType), zoneType, "Zone type must be a defined ZoneType.")
        };
    }
}
