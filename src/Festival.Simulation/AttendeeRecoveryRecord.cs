using System.Collections.Immutable;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;

namespace Festival.Simulation;

public enum RecoveryEpisodeStatus { Recovered, Unresolved }

// Stage 5 evidence only. An unresolved episode has no observed recovery endpoint
// or distance; it does not establish failed recovery.
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
    public int? OpportunitiesToRecovery { get; }
    public RecoveryEpisodeStatus Status { get; }

    internal RecoveryEpisode(AttendeeId attendeeId, AttendeePathEntry start,
        AttendeePathEntry? recovery, int? opportunitiesToRecovery)
    {
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
        OpportunitiesToRecovery = opportunitiesToRecovery;
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
