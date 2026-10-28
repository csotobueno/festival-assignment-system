using Festival.Domain.Assignments;
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
    FestivalDayId FestivalDayId,
    int AttemptSequence,
    int GroupSize,
    SimulationAttemptOutcome Outcome,
    AssignmentRequestId? PersistedRequestId,
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

// Raw final database observations, deliberately without metrics or artifact formats.
public sealed record SimulationResult(
    IReadOnlyList<SimulationAttemptRecord> Attempts,
    IReadOnlyList<PersistedSimulationRequest> Requests,
    IReadOnlyList<Assignment> Assignments,
    IReadOnlyList<Zone> Zones,
    IReadOnlyList<Spot> Spots,
    IReadOnlyList<SimulationDayOpeningState> OpeningStates);
