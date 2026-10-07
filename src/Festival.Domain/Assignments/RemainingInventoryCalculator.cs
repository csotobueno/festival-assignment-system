using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Assignments;

public static class RemainingInventoryCalculator
{
    /// <summary>
    /// Counts only the supplied available Spots, using the global Zone quality policy.
    /// The caller supplies availability; eligibility and physical feasibility are separate concerns.
    /// </summary>
    public static RemainingInventoryState Calculate(
        IEnumerable<Zone> zones,
        IEnumerable<Spot> availableSpots)
    {
        ArgumentNullException.ThrowIfNull(zones);
        ArgumentNullException.ThrowIfNull(availableSpots);

        var catalog = zones.ToArray();
        if (catalog.Any(zone => zone is null))
        {
            throw new ArgumentException("Zones cannot contain null values.", nameof(zones));
        }

        if (catalog.Select(zone => zone.Id).Distinct().Count() != catalog.Length)
        {
            throw new ArgumentException("Zones cannot contain duplicate ids.", nameof(zones));
        }

        var zonesById = catalog.ToDictionary(zone => zone.Id);
        var spots = availableSpots.ToArray();
        if (spots.Any(spot => spot is null))
        {
            throw new ArgumentException("Available spots cannot contain null values.", nameof(availableSpots));
        }

        if (spots.Any(spot => !zonesById.ContainsKey(spot.ZoneId)))
        {
            throw new ArgumentException("Available spots must belong to the supplied zone catalog.", nameof(availableSpots));
        }

        if (spots.Select(spot => spot.Code).Distinct().Count() != spots.Length
            || spots.Select(spot => (spot.ZoneId, spot.RowCode, spot.Number)).Distinct().Count() != spots.Length)
        {
            throw new ArgumentException("Available spots cannot contain duplicate codes or physical positions.", nameof(availableSpots));
        }

        var goodRemaining = 0;
        var mediumRemaining = 0;
        var badRemaining = 0;
        foreach (var spot in spots)
        {
            var quality = ZoneExperienceQualityPolicy.GetQuality(zonesById[spot.ZoneId].Type);
            switch (quality)
            {
                case ExperienceQuality.Good:
                    goodRemaining++;
                    break;
                case ExperienceQuality.Medium:
                    mediumRemaining++;
                    break;
                case ExperienceQuality.Bad:
                    badRemaining++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(quality), quality, "Experience quality must be defined.");
            }
        }

        return RemainingInventoryState.Create(goodRemaining, mediumRemaining, badRemaining);
    }
}
