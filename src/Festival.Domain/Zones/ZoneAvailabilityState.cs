using Festival.Domain.Assignments;

namespace Festival.Domain.Zones;

/// <summary>
/// Raw available Spot count for one Zone, not contiguous group-feasible capacity.
/// </summary>
public sealed record ZoneAvailabilityState
{
    public ZoneId ZoneId { get; }
    public ExperienceQuality ExperienceQuality { get; }
    public int AvailableSpotCount { get; }

    private ZoneAvailabilityState(ZoneId zoneId, ExperienceQuality experienceQuality, int availableSpotCount)
    {
        ZoneId = zoneId;
        ExperienceQuality = experienceQuality;
        AvailableSpotCount = availableSpotCount;
    }

    public static ZoneAvailabilityState Create(
        ZoneId zoneId,
        ExperienceQuality experienceQuality,
        int availableSpotCount)
    {
        if (zoneId == default)
        {
            throw new ArgumentException("Zone id is required.", nameof(zoneId));
        }

        if (!Enum.IsDefined(experienceQuality))
        {
            throw new ArgumentOutOfRangeException(
                nameof(experienceQuality), experienceQuality, "Experience quality must be defined.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(availableSpotCount);

        return new ZoneAvailabilityState(zoneId, experienceQuality, availableSpotCount);
    }
}
