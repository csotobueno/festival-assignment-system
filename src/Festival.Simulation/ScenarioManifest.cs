using System.Collections.Immutable;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Simulation;

// Immutable collections and immutable domain catalog/Assignment objects keep execution
// state outside this input. Mutable AssignmentRequest aggregates are created when seeding.
public sealed record ScenarioManifest(
    ScenarioMetadata Metadata,
    ScenarioCatalog Catalog,
    ScenarioSetup Setup,
    ImmutableArray<DailyPlan> DailyPlans);

public sealed record ScenarioMetadata(
    string ScenarioId,
    int Seed,
    string GeneratorVersion,
    SimulationExecutionMode ExecutionMode,
    int ConcurrencyLevel);

// Physical topology is shared by every day. Attendees is the registered catalog;
// evaluated population is the distinct membership of DailyPlans' workload.
public sealed record ScenarioCatalog(
    ImmutableArray<FestivalDay> FestivalDays,
    ImmutableArray<Attendee> Attendees,
    ImmutableArray<Zone> Zones,
    ImmutableArray<Spot> Spots);

// Supporting occupancy is persisted before execution and never dispatched as attempts.
// There is no initial history for evaluated attendees in v1.
public sealed record ScenarioSetup(
    ImmutableArray<Attendee> SupportingAttendees,
    ImmutableArray<SupportingCompletedRequest> CompletedRequests,
    ImmutableArray<Assignment> Assignments);

public sealed record SupportingCompletedRequest(
    AssignmentRequestId Id,
    FestivalDayId FestivalDayId,
    ImmutableArray<AttendeeCode> AttendeeCodes,
    DateTimeOffset RequestedAt,
    DateTimeOffset CompletedAt,
    RequestEligibility Eligibility);

public sealed record DailyPlan(
    int DayNumber,
    FestivalDayId FestivalDayId,
    ImmutableArray<DailyRequestPlan> Requests);

public sealed record DailyRequestPlan(
    Guid SimulationRequestId,
    int Sequence,
    ImmutableArray<Attendee> Members,
    DateTimeOffset RequestedAt,
    DateTimeOffset AssignedAt,
    RequestEligibility Eligibility);
