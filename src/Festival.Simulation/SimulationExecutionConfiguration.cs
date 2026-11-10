namespace Festival.Simulation;

public sealed record SimulationExecutionConfiguration
{
    public SimulationExecutionMode ExecutionMode { get; }
    public int ConcurrencyLevel { get; }
    public int? Repetition { get; }
    public ArrivalSchedule? ArrivalSchedule { get; }

    public SimulationExecutionConfiguration(SimulationExecutionMode executionMode, int concurrencyLevel,
        int? repetition = null, ArrivalSchedule? arrivalSchedule = null)
    {
        if (!(executionMode == SimulationExecutionMode.Sequential && concurrencyLevel == 1
            || executionMode == SimulationExecutionMode.Concurrent && concurrencyLevel is 4 or 16
            || executionMode == SimulationExecutionMode.ArrivalDriven && concurrencyLevel == 0
                && arrivalSchedule is not null && repetition is null))
            throw new NotSupportedException("Supported configurations: Sequential/1, Concurrent/4 or 16, ArrivalDriven/0 with a schedule.");
        if (executionMode != SimulationExecutionMode.ArrivalDriven && arrivalSchedule is not null)
            throw new ArgumentException("Arrival timing requires ArrivalDriven mode.", nameof(arrivalSchedule));
        if (repetition is not null and not (>= 1 and <= 3))
            throw new ArgumentOutOfRangeException(nameof(repetition), "S8 repetition must be 1, 2 or 3.");
        ExecutionMode = executionMode;
        ConcurrencyLevel = concurrencyLevel;
        Repetition = repetition;
        ArrivalSchedule = arrivalSchedule;
    }
}
