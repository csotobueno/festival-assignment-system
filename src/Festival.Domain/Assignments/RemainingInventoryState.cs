using Festival.Domain.Zones;

namespace Festival.Domain.Assignments;

/// <summary>
/// Immutable Zone availability snapshot with derived quality totals.
/// Raw counts do not represent contiguous group-feasible capacity.
/// </summary>
public sealed class RemainingInventoryState
{
    /// <summary>Zone entries without any business priority or sequence-order contract.</summary>
    public IReadOnlyList<ZoneAvailabilityState> ZoneAvailabilities { get; }

    public int GoodRemaining => CountAvailableSpots(ExperienceQuality.Good);
    public int MediumRemaining => CountAvailableSpots(ExperienceQuality.Medium);
    public int BadRemaining => CountAvailableSpots(ExperienceQuality.Bad);

    private RemainingInventoryState(ZoneAvailabilityState[] zoneAvailabilities)
    {
        ZoneAvailabilities = Array.AsReadOnly(zoneAvailabilities);
    }

    public static RemainingInventoryState Create(IEnumerable<ZoneAvailabilityState> zoneAvailabilities)
    {
        ArgumentNullException.ThrowIfNull(zoneAvailabilities);
        var entries = zoneAvailabilities.ToArray();

        if (entries.Any(entry => entry is null))
        {
            throw new ArgumentException("Zone availabilities cannot contain null values.", nameof(zoneAvailabilities));
        }

        if (entries.Select(entry => entry.ZoneId).Distinct().Count() != entries.Length)
        {
            throw new ArgumentException("Zone availabilities cannot contain duplicate zone ids.", nameof(zoneAvailabilities));
        }

        return new RemainingInventoryState(entries);
    }

    private int CountAvailableSpots(ExperienceQuality quality) => ZoneAvailabilities
        .Where(entry => entry.ExperienceQuality == quality)
        .Sum(entry => entry.AvailableSpotCount);
}
