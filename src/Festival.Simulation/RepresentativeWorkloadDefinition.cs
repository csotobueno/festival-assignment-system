using System.Collections.Immutable;

namespace Festival.Simulation;

// Percentages are decimal ratios (0.25 = 25%) of requests/groups, not attendees.
public sealed record GroupSizeBand(int MinSize, int MaxSize, decimal Percentage);

public sealed record ArrivalTimeBand(TimeOnly Start, TimeOnly End, decimal Percentage);

// AllowsFrontStandingRequestShare is allocated over each day's requests, independently
// of attendee identity and eligibility on other FestivalDays.
public sealed record RepresentativeWorkloadDefinition(
    int DailyRequesterCount,
    decimal DailyParticipationContinuity,
    ImmutableArray<GroupSizeBand> GroupSizeProfile,
    ImmutableArray<ArrivalTimeBand> ArrivalProfile,
    decimal AllowsFrontStandingRequestShare);
