namespace Festival.Simulation;

public sealed record SimulationExecutionConfiguration
{
    public SimulationExecutionMode ExecutionMode { get; }
    public int ConcurrencyLevel { get; }
    public int? Repetition { get; }

    public SimulationExecutionConfiguration(SimulationExecutionMode executionMode, int concurrencyLevel,
        int? repetition = null)
    {
        if (!(executionMode == SimulationExecutionMode.Sequential && concurrencyLevel == 1
            || executionMode == SimulationExecutionMode.Concurrent && concurrencyLevel is 4 or 16))
            throw new NotSupportedException("Supported execution configurations are Sequential/1 and Concurrent/4 or 16.");
        if (repetition is not null and not (>= 1 and <= 3))
            throw new ArgumentOutOfRangeException(nameof(repetition), "S8 repetition must be 1, 2 or 3.");
        ExecutionMode = executionMode;
        ConcurrencyLevel = concurrencyLevel;
        Repetition = repetition;
    }
}
