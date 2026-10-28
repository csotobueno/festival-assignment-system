using System.Collections.Immutable;
using Festival.Domain.Assignments;
using Festival.Domain.Zones;

namespace Festival.Simulation;

public enum SimulationExecutionMode
{
    Sequential
}

// Compact intent: attendee numbers refer to the evaluated population, not setup occupants.
// v1 uses one contiguous row per block and starts evaluated attendees without history.
public sealed record ScenarioDefinition(
    string ScenarioId,
    int Seed,
    string GeneratorVersion,
    DateOnly FirstDate,
    int AttendeeCount,
    ImmutableArray<TopologyBlockDefinition> Topology,
    ImmutableArray<DayDefinition> Days,
    SimulationExecutionMode ExecutionMode,
    int ConcurrencyLevel);

public sealed record TopologyBlockDefinition(string Key, ZoneType ZoneType, int SpotCount);

public sealed record DayDefinition(
    int DayNumber,
    ImmutableArray<string> OccupiedBlockKeys,
    ImmutableArray<RequestDefinition> Requests);

// Sequence is explicit and unique across the scenario; membership order is preserved.
public sealed record RequestDefinition(
    int Sequence,
    ImmutableArray<int> AttendeeNumbers,
    RequestEligibility Eligibility);
