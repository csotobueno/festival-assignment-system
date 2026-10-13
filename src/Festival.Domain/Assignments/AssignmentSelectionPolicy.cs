using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Assignments;

public static class AssignmentSelectionPolicy
{
    /// <summary>
    /// Selects a Zone, block and actual quality from prepared decision inputs.
    /// Returns null when no participating Zone has a complete feasible block.
    /// </summary>
    public static AssignmentSelection? Select(
        TargetQuality targetQuality,
        RemainingInventoryState inventory,
        IEnumerable<Zone> eligibleZones,
        IEnumerable<Spot> availableSpots,
        GroupSize groupSize)
    {
        var orderedZones = ZoneEvaluationPolicy.Order(targetQuality, inventory, eligibleZones);

        ArgumentNullException.ThrowIfNull(availableSpots);
        // Both downstream policies must use the same availability, even for a single-use source.
        var spots = availableSpots.ToArray();
        var candidate = FeasibleZoneCandidateFinder.Find(orderedZones, spots, groupSize);
        if (candidate is null)
        {
            return null;
        }

        var block = BlockSelectionPolicy.Select(candidate, spots);
        var quality = ZoneExperienceQualityPolicy.GetQuality(candidate.Zone.Type);

        return AssignmentSelection.Create(candidate.Zone, block, quality);
    }
}
