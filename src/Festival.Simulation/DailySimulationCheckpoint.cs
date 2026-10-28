using Festival.Domain.FestivalDays;

namespace Festival.Simulation;

// Drained end-of-day observation. Attempt counts are daily and evaluated-only;
// capacity is global, FestivalDay-scoped and includes the effects of setup occupancy.
// Raw available counts do not imply a feasible contiguous block for any group.
public sealed record DailySimulationCheckpoint(
    string ScenarioId,
    FestivalDayId FestivalDayId,
    int EvaluatedRequestCount,
    int SuccessfulAttemptCount,
    int RejectedAttemptCount,
    int GoodAvailableSpotCount,
    int MediumAvailableSpotCount,
    int BadAvailableSpotCount,
    int TotalAvailableSpotCount);
