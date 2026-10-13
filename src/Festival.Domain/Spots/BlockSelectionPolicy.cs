using Festival.Domain.Zones;

namespace Festival.Domain.Spots;

public static class BlockSelectionPolicy
{
    /// <summary>
    /// Selects an already feasible block by the largest contiguous run remaining
    /// in its affected Row, then ordinal RowCode and starting SpotNumber ascending.
    /// The caller supplies current availability; Zone selection is already complete.
    /// </summary>
    public static FeasibleSpotBlock Select(
        FeasibleZoneCandidate candidate,
        IEnumerable<Spot> availableSpots)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(availableSpots);

        var spots = availableSpots.ToArray();
        if (spots.Any(spot => spot is null))
        {
            throw new ArgumentException("Available spots cannot contain null values.", nameof(availableSpots));
        }

        if (spots.Select(spot => spot.Code).Distinct().Count() != spots.Length
            || spots.Select(spot => (spot.ZoneId, spot.RowCode, spot.Number)).Distinct().Count() != spots.Length)
        {
            throw new ArgumentException("Available spots cannot contain duplicate codes or physical positions.", nameof(availableSpots));
        }

        var spotsByCode = spots.ToDictionary(spot => spot.Code);
        foreach (var spot in candidate.FeasibleBlocks.SelectMany(block => block.Spots))
        {
            if (!spotsByCode.TryGetValue(spot.Code, out var available)
                || available.ZoneId != spot.ZoneId
                || available.RowCode != spot.RowCode
                || available.Number != spot.Number)
            {
                throw new ArgumentException(
                    "Available spots must contain every candidate spot at its candidate physical position.",
                    nameof(availableSpots));
            }
        }

        var spotsByRow = spots.Where(spot => spot.ZoneId == candidate.Zone.Id)
            .ToLookup(spot => spot.RowCode);

        return candidate.FeasibleBlocks
            .OrderByDescending(block => LargestRemainingContiguousRun(block, spotsByRow[block.RowCode]))
            .ThenBy(block => block.RowCode.Value, StringComparer.Ordinal)
            .ThenBy(block => block.Spots.Min(spot => spot.Number.Value))
            .First();
    }

    private static int LargestRemainingContiguousRun(FeasibleSpotBlock block, IEnumerable<Spot> rowSpots)
    {
        var consumedNumbers = block.Spots.Select(spot => spot.Number.Value).ToHashSet();
        var remainingNumbers = rowSpots.Select(spot => spot.Number.Value)
            .Where(number => !consumedNumbers.Contains(number))
            .OrderBy(number => number);

        var longestRun = 0;
        var currentRun = 0;
        var previousNumber = 0;
        foreach (var number in remainingNumbers)
        {
            currentRun = currentRun > 0 && (long)number == (long)previousNumber + 1
                ? currentRun + 1
                : 1;
            longestRun = Math.Max(longestRun, currentRun);
            previousNumber = number;
        }

        return longestRun;
    }
}
