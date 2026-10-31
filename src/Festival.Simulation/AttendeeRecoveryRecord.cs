using System.Collections.Immutable;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;

namespace Festival.Simulation;

public enum RecoveryEpisodeStatus { Recovered, Unresolved }

// Stage 5 evidence only. An unresolved episode retains observed opportunities
// without a recovery endpoint or OpportunitiesToRecovery; it does not establish failed recovery.
public sealed class RecoveryEpisode
{
    public AttendeeId AttendeeId { get; }
    public AssignmentId StartAssignmentId { get; }
    public FestivalDayId StartFestivalDayId { get; }
    public DateOnly StartFestivalDate { get; }
    public DateTimeOffset StartAssignedAt { get; }
    public AssignmentId? RecoveryAssignmentId { get; }
    public FestivalDayId? RecoveryFestivalDayId { get; }
    public DateOnly? RecoveryFestivalDate { get; }
    public DateTimeOffset? RecoveryAssignedAt { get; }
    public ExperienceQuality? RecoveryQuality { get; }
    public int ObservedOpportunities { get; }
    public bool RecoveryEligible => ObservedOpportunities > 0;
    public int? OpportunitiesToRecovery => Status == RecoveryEpisodeStatus.Recovered ? ObservedOpportunities : null;
    public RecoveryEpisodeStatus Status { get; }

    internal RecoveryEpisode(AttendeeId attendeeId, AttendeePathEntry start,
        AttendeePathEntry? recovery, int observedOpportunities)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(observedOpportunities);
        AttendeeId = attendeeId;
        StartAssignmentId = start.AssignmentId;
        StartFestivalDayId = start.FestivalDayId;
        StartFestivalDate = start.FestivalDate;
        StartAssignedAt = start.AssignedAt;
        RecoveryAssignmentId = recovery?.AssignmentId;
        RecoveryFestivalDayId = recovery?.FestivalDayId;
        RecoveryFestivalDate = recovery?.FestivalDate;
        RecoveryAssignedAt = recovery?.AssignedAt;
        RecoveryQuality = recovery?.ExperienceQuality;
        ObservedOpportunities = observedOpportunities;
        Status = recovery is null ? RecoveryEpisodeStatus.Unresolved : RecoveryEpisodeStatus.Recovered;
    }
}

public sealed class AttendeeRecoveryRecord
{
    public AttendeeId AttendeeId { get; }
    public ImmutableArray<RecoveryEpisode> Episodes { get; }

    // One record per evaluated attendee, including empty paths/no Bad outcomes.
    internal AttendeeRecoveryRecord(AttendeeId attendeeId, ImmutableArray<RecoveryEpisode> episodes)
    {
        AttendeeId = attendeeId;
        Episodes = episodes;
    }
}
