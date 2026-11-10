using System.Text.Json.Serialization;

namespace Festival.Simulation;

public enum ArrivalPattern { Moderate, BurstLike }

// Experimental elapsed timing only; never changes simulated business timestamps.
public sealed record ArrivalSchedule
{
    public ArrivalPattern Pattern { get; }
    public string Version => "arrival-v1";
    public int? InterArrivalMilliseconds => Pattern == ArrivalPattern.Moderate ? 25 : null;
    public int? ClusterSize => Pattern == ArrivalPattern.BurstLike ? 4 : null;
    public int? WithinClusterMilliseconds => Pattern == ArrivalPattern.BurstLike ? 5 : null;
    public int? BetweenClustersMilliseconds => Pattern == ArrivalPattern.BurstLike ? 50 : null;

    [JsonConstructor]
    public ArrivalSchedule(ArrivalPattern pattern)
    {
        if (!Enum.IsDefined(pattern)) throw new ArgumentOutOfRangeException(nameof(pattern));
        Pattern = pattern;
    }

    public TimeSpan OffsetAt(int ordinal)
    {
        if (ordinal < 0) throw new ArgumentOutOfRangeException(nameof(ordinal));
        var milliseconds = Pattern == ArrivalPattern.Moderate ? 25L * ordinal
            : 65L * (ordinal / 4) + 5L * (ordinal % 4);
        return TimeSpan.FromTicks(checked(milliseconds * TimeSpan.TicksPerMillisecond));
    }
}
