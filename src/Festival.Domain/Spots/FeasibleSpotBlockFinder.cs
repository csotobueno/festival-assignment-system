using Festival.Domain.Assignments;
using Festival.Domain.Zones;

namespace Festival.Domain.Spots;

public static class FeasibleSpotBlockFinder
{
    public static IReadOnlyList<FeasibleSpotBlock> Find(
        Zone zone,
        IEnumerable<Spot> availableSpots,
        GroupSize groupSize)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(availableSpots);

        if (groupSize == default)
        {
            throw new ArgumentException("Group size is required.", nameof(groupSize));
        }

        var spots = availableSpots.ToArray();
        if (spots.Any(spot => spot is null))
        {
            throw new ArgumentException("Available spots cannot contain null values.", nameof(availableSpots));
        }

        if (spots.Any(spot => spot.ZoneId != zone.Id))
        {
            throw new ArgumentException("Available spots must belong to the supplied zone.", nameof(availableSpots));
        }

        if (spots.Select(spot => spot.Code).Distinct().Count() != spots.Length
            || spots.Select(spot => (spot.RowCode, spot.Number)).Distinct().Count() != spots.Length)
        {
            throw new ArgumentException("Available spots cannot contain duplicate codes or physical positions.", nameof(availableSpots));
        }

        var blocks = new List<FeasibleSpotBlock>();
        foreach (var row in spots.GroupBy(spot => spot.RowCode)
                     .OrderBy(row => row.Key.Value, StringComparer.Ordinal))
        {
            var ordered = row.OrderBy(spot => spot.Number.Value).ToArray();
            var runStart = 0;
            for (var index = 0; index < ordered.Length; index++)
            {
                if (index > 0
                    && (long)ordered[index].Number.Value != (long)ordered[index - 1].Number.Value + 1)
                {
                    runStart = index;
                }

                if (index - runStart + 1 >= groupSize.Value)
                {
                    blocks.Add(FeasibleSpotBlock.Create(
                        ordered.Skip(index - groupSize.Value + 1).Take(groupSize.Value)));
                }
            }
        }

        return blocks.AsReadOnly();
    }
}
