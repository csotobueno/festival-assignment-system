using System.Collections.Immutable;
using Festival.Domain.Assignments;
using Festival.Domain.Zones;

namespace Festival.Simulation;

public enum SimulationExecutionMode
{
    Sequential
}

// AttendeeCount is the registered catalog, excluding setup occupants. v1 uses
// explicit requests; v2 materializes the optional representative workload.
public sealed record ScenarioDefinition(
    string ScenarioId,
    int Seed,
    string GeneratorVersion,
    DateOnly FirstDate,
    int AttendeeCount,
    ImmutableArray<TopologyBlockDefinition> Topology,
    ImmutableArray<DayDefinition> Days,
    SimulationExecutionMode ExecutionMode,
    int ConcurrencyLevel,
    RepresentativeWorkloadDefinition? Workload = null);

// SpotCount is the starting row's capacity; defaults preserve the v1 single row.
public sealed record TopologyBlockDefinition(
    string Key, ZoneType ZoneType, int SpotCount, int RowCount = 1, int GrowthPerRow = 0);

public sealed record DayDefinition(
    int DayNumber,
    ImmutableArray<string> OccupiedBlockKeys,
    ImmutableArray<RequestDefinition> Requests);

// Sequence is explicit and unique across the scenario; membership order is preserved.
public sealed record RequestDefinition(
    int Sequence,
    ImmutableArray<int> AttendeeNumbers,
    RequestEligibility Eligibility);
