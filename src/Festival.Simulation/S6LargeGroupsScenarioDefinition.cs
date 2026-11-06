namespace Festival.Simulation;

public static class S6LargeGroupsScenarioDefinition
{
    public static ScenarioDefinition Create(int seed, DateOnly firstDate)
    {
        var baseline = S2ExpectedBaselineScenarioDefinition.Create(seed, firstDate);
        return baseline with
        {
            ScenarioId = "s6-large-groups",
            Workload = baseline.Workload! with
            {
                GroupSizeProfile =
                [
                    new(1, 1, .10m), new(2, 2, .20m), new(3, 3, .20m), new(4, 4, .15m),
                    new(5, 5, .10m), new(6, 7, .15m), new(8, 10, .10m)
                ]
            }
        };
    }
}
