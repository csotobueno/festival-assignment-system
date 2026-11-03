using System.Collections.Immutable;
using Festival.Domain.Zones;

namespace Festival.Simulation;

public static class S1BestPlausibleScenarioDefinition
{
    public static ScenarioDefinition Create(int seed, DateOnly firstDate, decimal allowsFrontStandingRequestShare) =>
        RepresentativeScenarioDefinitions.Create("s1-best-plausible", seed, firstDate,
            new RepresentativeWorkloadDefinition(3300, 0.90m,
                [new(1, 1, .25m), new(2, 2, .35m), new(3, 3, .25m), new(4, 4, .10m), new(5, 10, .05m)],
                [
                    new(new(7, 0), new(9, 0), .15m), new(new(9, 0), new(12, 0), .25m),
                    new(new(12, 0), new(15, 0), .20m), new(new(15, 0), new(18, 0), .20m),
                    new(new(18, 0), new(21, 0), .15m), new(new(21, 0), new(23, 0), .05m)
                ], allowsFrontStandingRequestShare));
}

public static class S2ExpectedBaselineScenarioDefinition
{
    public static ScenarioDefinition Create(int seed, DateOnly firstDate, decimal allowsFrontStandingRequestShare) =>
        RepresentativeScenarioDefinitions.Create("s2-expected-baseline", seed, firstDate,
            new RepresentativeWorkloadDefinition(4400, 0.90m,
                [
                    new(1, 1, .15m), new(2, 2, .30m), new(3, 3, .25m), new(4, 4, .15m),
                    new(5, 5, .08m), new(6, 7, .05m), new(8, 10, .02m)
                ],
                [
                    new(new(7, 0), new(9, 0), .10m), new(new(9, 0), new(12, 0), .40m),
                    new(new(12, 0), new(15, 0), .15m), new(new(15, 0), new(16, 0), .20m),
                    new(new(16, 0), new(18, 0), .07m), new(new(18, 0), new(21, 0), .05m),
                    new(new(21, 0), new(23, 0), .03m)
                ], allowsFrontStandingRequestShare));
}

internal static class RepresentativeScenarioDefinitions
{
    private static readonly ImmutableArray<TopologyBlockDefinition> Topology =
    [
        new("front-standing", ZoneType.FrontStanding, 75, 10),
        new("middle-center", ZoneType.MiddleCenter, 78, 12, 1),
        new("middle-left", ZoneType.MiddleLeft, 57, 12, 1),
        new("middle-right", ZoneType.MiddleRight, 57, 12, 1),
        new("upper-center", ZoneType.UpperCenter, 57, 12, 1),
        new("upper-left", ZoneType.UpperLeft, 36, 12, 1),
        new("upper-right", ZoneType.UpperRight, 36, 12, 1)
    ];

    internal static ScenarioDefinition Create(string id, int seed, DateOnly firstDate,
        RepresentativeWorkloadDefinition workload) => new(id, seed, "v2", firstDate, 5500,
        Topology, Enumerable.Range(1, 5).Select(day => new DayDefinition(day, [], [])).ToImmutableArray(),
        SimulationExecutionMode.Sequential, 1, workload);
}
