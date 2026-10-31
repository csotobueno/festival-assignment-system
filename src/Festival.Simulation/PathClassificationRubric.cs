using System.Collections.Immutable;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;

namespace Festival.Simulation;

// Stage 5 analytical hypotheses, requiring stakeholder calibration. This is
// independent of production fairness policies and does not evaluate recovery.
public static class PathClassificationRubric
{
    public const string Version = "v1";

    private const decimal GoodQualityValue = 1m;
    private const decimal MediumQualityValue = 0m;
    private const decimal BadQualityValue = -1m;

    private const decimal CurrentQualityWeight = 0.60m;
    private const decimal NormalizedChangeWeight = 0.40m;

    private const decimal HighOverallQualityThreshold = 0.40m;
    private const decimal LowOverallQualityThreshold = -0.20m;
    private const decimal PositiveDirectionThreshold = 0.20m;
    private const decimal NegativeDirectionThreshold = -0.20m;
    
    private const decimal MaxQualityChange = 2m;

    public static AttendeePathRecord Evaluate(AttendeeId attendeeId, IEnumerable<AttendeePathEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var ordered = entries.OrderBy(entry => entry.AssignedAt)
            .ThenBy(entry => entry.AssignmentId.Value).ToImmutableArray();
        if (ordered.IsEmpty)
            return new AttendeePathRecord(attendeeId, ordered, null, null, null, null, null);

        var qualities = ordered.Select(entry => QualityValue(entry.ExperienceQuality)).ToArray();
        var overallQuality = qualities.Average();
        var qualityBand = GetOverallQualityBand(overallQuality);
        if (ordered.Length == 1)
            return new AttendeePathRecord(attendeeId, ordered, overallQuality, null, qualityBand,
                null, PathClassification.InsufficientEvidence);

        decimal weightedScores = 0;
        decimal totalWeight = 0;
        for (var index = 1; index < qualities.Length; index++)
        {
            var normalizedChange = (qualities[index] - qualities[index - 1]) / MaxQualityChange;
            var transitionScore = CurrentQualityWeight * qualities[index] + NormalizedChangeWeight * normalizedChange;
            weightedScores += transitionScore * index;
            totalWeight += index;
        }

        var directionScore = weightedScores / totalWeight;
        var direction = GetDirection(directionScore);
        var classification = (qualityBand, direction) switch
        {
            (OverallQualityBand.High, PathDirection.Positive or PathDirection.Neutral) => PathClassification.Balanced,
            (OverallQualityBand.High, PathDirection.Negative) => PathClassification.Acceptable,
            (OverallQualityBand.Medium, PathDirection.Positive) => PathClassification.Balanced,
            (OverallQualityBand.Medium, PathDirection.Neutral) => PathClassification.Acceptable,
            (OverallQualityBand.Medium, PathDirection.Negative) => PathClassification.NotBalanced,
            (OverallQualityBand.Low, PathDirection.Positive) => PathClassification.Acceptable,
            (OverallQualityBand.Low, PathDirection.Neutral or PathDirection.Negative) => PathClassification.NotBalanced,
            _ => throw new InvalidOperationException("Undefined analytical band combination.")
        };
        return new AttendeePathRecord(attendeeId, ordered, overallQuality, directionScore,
            qualityBand, direction, classification);
    }

    private static decimal QualityValue(ExperienceQuality quality) => quality switch
    {
        ExperienceQuality.Good => GoodQualityValue,
        ExperienceQuality.Medium => MediumQualityValue,
        ExperienceQuality.Bad => BadQualityValue,
        _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, "Experience quality must be defined.")
    };

    internal static OverallQualityBand GetOverallQualityBand(decimal score) => score switch
    {
        >= HighOverallQualityThreshold => OverallQualityBand.High,
        <= LowOverallQualityThreshold => OverallQualityBand.Low,
        _ => OverallQualityBand.Medium
    };

    internal static PathDirection GetDirection(decimal score) => score switch
    {
        >= PositiveDirectionThreshold => PathDirection.Positive,
        <= NegativeDirectionThreshold => PathDirection.Negative,
        _ => PathDirection.Neutral
    };
}
