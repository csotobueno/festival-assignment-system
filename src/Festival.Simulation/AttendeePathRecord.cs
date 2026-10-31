using System.Collections.Immutable;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;

namespace Festival.Simulation;

// Persisted evidence only; calendar gaps do not add entries or transition weights.
public sealed record AttendeePathEntry(
    AssignmentId AssignmentId,
    FestivalDayId FestivalDayId,
    DateOnly FestivalDate,
    DateTimeOffset AssignedAt,
    ExperienceQuality ExperienceQuality);

public enum OverallQualityBand { High, Medium, Low }

public enum PathDirection { Positive, Neutral, Negative }

public enum PathClassification { Balanced, Acceptable, NotBalanced, InsufficientEvidence }

public sealed class AttendeePathRecord
{
    public AttendeeId AttendeeId { get; }
    public ImmutableArray<AttendeePathEntry> Entries { get; }
    public int PathLength => Entries.Length;
    public decimal? OverallQuality { get; }
    public decimal? DirectionScore { get; }
    public OverallQualityBand? OverallQualityBand { get; }
    public PathDirection? Direction { get; }
    public PathClassification? Classification { get; }
    public string RubricVersion => PathClassificationRubric.Version;

    // A zero-length record preserves evaluated population context, without an
    // EvaluatedPath or classification. A singleton has no directional evidence.
    internal AttendeePathRecord(AttendeeId attendeeId, ImmutableArray<AttendeePathEntry> entries,
        decimal? overallQuality, decimal? directionScore, OverallQualityBand? overallQualityBand,
        PathDirection? direction, PathClassification? classification)
    {
        AttendeeId = attendeeId;
        Entries = entries;
        OverallQuality = overallQuality;
        DirectionScore = directionScore;
        OverallQualityBand = overallQualityBand;
        Direction = direction;
        Classification = classification;
    }
}
