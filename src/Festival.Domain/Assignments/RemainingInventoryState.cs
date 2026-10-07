namespace Festival.Domain.Assignments;

/// <summary>
/// Raw remaining Spot counts by experience quality, not contiguous group-feasible capacity.
/// </summary>
public readonly record struct RemainingInventoryState
{
    public int GoodRemaining { get; }
    public int MediumRemaining { get; }
    public int BadRemaining { get; }

    private RemainingInventoryState(int goodRemaining, int mediumRemaining, int badRemaining)
    {
        GoodRemaining = goodRemaining;
        MediumRemaining = mediumRemaining;
        BadRemaining = badRemaining;
    }

    public static RemainingInventoryState Create(int goodRemaining, int mediumRemaining, int badRemaining)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(goodRemaining);
        ArgumentOutOfRangeException.ThrowIfNegative(mediumRemaining);
        ArgumentOutOfRangeException.ThrowIfNegative(badRemaining);

        return new RemainingInventoryState(goodRemaining, mediumRemaining, badRemaining);
    }
}
