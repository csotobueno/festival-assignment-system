using Festival.Domain.Assignments;
using Festival.Domain.Zones;

namespace Festival.Simulation;

// Two-day development proof, not representative festival-scale evidence.
public static class MinimalRecoveryScenarioDefinition
{
    public static ScenarioDefinition Create() => new(
        ScenarioId: "minimal-recovery",
        Seed: 0,
        GeneratorVersion: "v1",
        FirstDate: new DateOnly(2026, 8, 15),
        AttendeeCount: 2,
        Topology:
        [
            new("bad", ZoneType.UpperLeft, 2),
            new("good", ZoneType.MiddleCenter, 2),
            new("medium", ZoneType.MiddleLeft, 2)
        ],
        Days:
        [
            new(1, ["good", "medium"], [new(1, [1, 2], new RequestEligibility(true))]),
            new(2, [], [new(2, [1, 2], new RequestEligibility(true))])
        ],
        ExecutionMode: SimulationExecutionMode.Sequential,
        ConcurrencyLevel: 1);
}
