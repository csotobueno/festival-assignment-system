using Festival.Domain.Assignments;
using Festival.Domain.Spots;

namespace Festival.Domain.Zones;

public static class FeasibleZoneCandidateFinder
{
    /// <summary>
    /// Returns the first supplied Zone with feasible blocks, preserving finder block order.
    /// The caller supplies Zone priority and current availability; no block is selected.
    /// </summary>
    public static FeasibleZoneCandidate? Find(
        IEnumerable<Zone> orderedZones,
        IEnumerable<Spot> availableSpots,
        GroupSize groupSize)
    {
        ArgumentNullException.ThrowIfNull(orderedZones);
        ArgumentNullException.ThrowIfNull(availableSpots);

        if (groupSize == default)
        {
            throw new ArgumentException("Group size is required.", nameof(groupSize));
        }

        var zones = orderedZones.ToArray();
        if (zones.Any(zone => zone is null))
        {
            throw new ArgumentException("Ordered zones cannot contain null values.", nameof(orderedZones));
        }

        if (zones.Select(zone => zone.Id).Distinct().Count() != zones.Length)
        {
            throw new ArgumentException("Ordered zones cannot contain duplicate ids.", nameof(orderedZones));
        }

        var spots = availableSpots.ToArray();
        if (spots.Any(spot => spot is null))
        {
            throw new ArgumentException("Available spots cannot contain null values.", nameof(availableSpots));
        }

        // Validate the entire snapshot before filtering or returning an early candidate.
        if (spots.Select(spot => spot.Code).Distinct().Count() != spots.Length
            || spots.Select(spot => (spot.ZoneId, spot.RowCode, spot.Number)).Distinct().Count() != spots.Length)
        {
            throw new ArgumentException("Available spots cannot contain duplicate codes or physical positions.", nameof(availableSpots));
        }

        var spotsByZone = spots.ToLookup(spot => spot.ZoneId);
        foreach (var zone in zones)
        {
            var blocks = FeasibleSpotBlockFinder.Find(zone, spotsByZone[zone.Id], groupSize);
            if (blocks.Count > 0)
            {
                return FeasibleZoneCandidate.Create(zone, blocks);
            }
        }

        return null;
    }
}
