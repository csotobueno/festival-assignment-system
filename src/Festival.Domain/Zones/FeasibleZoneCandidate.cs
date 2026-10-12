using Festival.Domain.Spots;

namespace Festival.Domain.Zones;

/// <summary>
/// A Zone with its feasible contiguous blocks, without choosing one block.
/// </summary>
public sealed class FeasibleZoneCandidate
{
    public Zone Zone { get; }

    public IReadOnlyList<FeasibleSpotBlock> FeasibleBlocks { get; }

    private FeasibleZoneCandidate(Zone zone, FeasibleSpotBlock[] feasibleBlocks)
    {
        Zone = zone;
        FeasibleBlocks = Array.AsReadOnly(feasibleBlocks);
    }

    public static FeasibleZoneCandidate Create(Zone zone, IEnumerable<FeasibleSpotBlock> feasibleBlocks)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(feasibleBlocks);
        var blocks = feasibleBlocks.ToArray();

        if (blocks.Length == 0)
        {
            throw new ArgumentException("A feasible zone candidate requires at least one block.", nameof(feasibleBlocks));
        }

        if (blocks.Any(block => block is null))
        {
            throw new ArgumentException("Feasible blocks cannot contain null values.", nameof(feasibleBlocks));
        }

        if (blocks.Any(block => block.ZoneId != zone.Id))
        {
            throw new ArgumentException("Feasible blocks must belong to the candidate zone.", nameof(feasibleBlocks));
        }

        if (blocks.Any(block => block.Spots.Count != blocks[0].Spots.Count))
        {
            throw new ArgumentException("Feasible blocks must have the same group size.", nameof(feasibleBlocks));
        }

        return new FeasibleZoneCandidate(zone, blocks);
    }
}
