using Festival.Domain.Assignments;

namespace Festival.Domain.Zones;

public static class ZoneEvaluationPolicy
{
    /// <summary>
    /// Orders already eligible Zones present in the supplied inventory snapshot.
    /// Raw availability determines ordering, not physical feasibility or block selection.
    /// </summary>
    public static IReadOnlyList<Zone> Order(
        TargetQuality targetQuality,
        RemainingInventoryState inventory,
        IEnumerable<Zone> eligibleZones)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(eligibleZones);

        if (!Enum.IsDefined(targetQuality))
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetQuality), targetQuality, "Target quality must be defined.");
        }

        var zones = eligibleZones.ToArray();
        if (zones.Any(zone => zone is null))
        {
            throw new ArgumentException("Eligible zones cannot contain null values.", nameof(eligibleZones));
        }

        if (zones.Select(zone => zone.Id).Distinct().Count() != zones.Length)
        {
            throw new ArgumentException("Eligible zones cannot contain duplicate ids.", nameof(eligibleZones));
        }

        var availabilityById = inventory.ZoneAvailabilities.ToDictionary(entry => entry.ZoneId);
        var participants = new List<(Zone Zone, ZoneAvailabilityState Availability)>();
        foreach (var zone in zones)
        {
            if (!availabilityById.TryGetValue(zone.Id, out var availability))
            {
                continue;
            }

            if (ZoneExperienceQualityPolicy.GetQuality(zone.Type) != availability.ExperienceQuality)
            {
                throw new ArgumentException(
                    $"Inventory quality for zone {zone.Id} must match its current zone quality.",
                    nameof(inventory));
            }

            participants.Add((zone, availability));
        }

        var firstQuality = targetQuality == TargetQuality.Good
            || inventory.GoodRemaining > inventory.MediumRemaining
                ? ExperienceQuality.Good
                : ExperienceQuality.Medium;

        return Array.AsReadOnly(participants
            .OrderBy(participant => participant.Availability.ExperienceQuality == firstQuality
                ? 0
                : participant.Availability.ExperienceQuality == ExperienceQuality.Bad ? 2 : 1)
            .ThenByDescending(participant => participant.Availability.AvailableSpotCount)
            .ThenBy(participant => participant.Zone.Id.Value)
            .Select(participant => participant.Zone)
            .ToArray());
    }
}
