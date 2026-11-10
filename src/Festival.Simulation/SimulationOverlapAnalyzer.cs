namespace Festival.Simulation;

public static class SimulationOverlapAnalyzer
{
    public static int MaxActiveRequests(IEnumerable<SimulationAttemptRecord> attempts)
    {
        ArgumentNullException.ThrowIfNull(attempts);
        var events = new List<(long Offset, int Delta)>();
        foreach (var attempt in attempts.Where(a => a.Outcome != SimulationAttemptOutcome.ExcludedInvalidInput))
        {
            if (attempt.ProcessingStartedOffset is not { } start || start < TimeSpan.Zero || attempt.Duration < TimeSpan.Zero)
                throw new ArgumentException("Overlap requires nonnegative monotonic processing intervals.", nameof(attempts));
            if (attempt.Duration == TimeSpan.Zero) continue; // Empty half-open interval.
            events.Add((start.Ticks, 1));
            events.Add((checked(start.Ticks + attempt.Duration.Ticks), -1));
        }
        var active = 0;
        var maximum = 0;
        foreach (var entry in events.OrderBy(e => e.Offset).ThenBy(e => e.Delta))
        {
            active += entry.Delta; // Completions precede starts at equal offsets.
            maximum = Math.Max(maximum, active);
        }
        return maximum;
    }
}
