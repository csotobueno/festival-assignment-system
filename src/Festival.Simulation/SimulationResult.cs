using System.Collections.Immutable;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Simulation;

public enum SimulationAttemptOutcome
{
    Successful,
    DomainRejected,
    KnownConcurrencyConflict,
    UnexpectedTechnicalFailure,
    ExcludedInvalidInput
}

public sealed record SimulationAttemptRecord(
    string ScenarioId,
    FestivalDayId FestivalDayId,
    Guid SimulationRequestId,
    int AttemptSequence,
    int GroupSize,
    ImmutableArray<AttendeeId> AttendeeIds,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    TimeSpan Duration,
    SimulationAttemptOutcome Outcome,
    AssignmentRequestId? PersistedRequestId,
    string? DiagnosticCode,
    string? DiagnosticType,
    int? EligibleAvailableSpotCount,
    bool? FeasibleContiguousBlockFound,
    ExperienceQuality? SelectedExperienceQuality,
    string? Diagnostic);

// Primitive read projection avoids exposing Infrastructure's internal EF request rows.
public sealed record PersistedSimulationRequest(
    Guid AssignmentRequestId,
    Guid FestivalDayId,
    string Status,
    DateTimeOffset RequestedAt);

// Minimal raw facts for checking stable topology and day-scoped availability.
public sealed record SimulationDayOpeningState(
    FestivalDayId FestivalDayId,
    IReadOnlyList<Zone> Zones,
    IReadOnlyList<Spot> Spots,
    IReadOnlyList<Spot> AvailableSpots);

// Final persisted observations and individual path analytics, without aggregate
// metrics or artifact formats. Empty paths retain evaluated population context.
public sealed record SimulationResult(
    IReadOnlyList<SimulationAttemptRecord> Attempts,
    IReadOnlyList<DailySimulationCheckpoint> DailyCheckpoints,
    ImmutableArray<AttendeePathRecord> AttendeePaths,
    ImmutableArray<AttendeeRecoveryRecord> RecoveryObservations,
    IReadOnlyList<PersistedSimulationRequest> Requests,
    IReadOnlyList<Assignment> Assignments,
    IReadOnlyList<Zone> Zones,
    IReadOnlyList<Spot> Spots,
    IReadOnlyList<SimulationDayOpeningState> OpeningStates);
