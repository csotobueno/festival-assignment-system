using System.Globalization;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Simulation;
using FluentAssertions;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class PathClassificationRubricTests
{
    private static readonly AttendeeId AttendeeId = AttendeeId.Create(Guid.Parse("10000000-0000-0000-0000-000000000001"));

    [Theory]
    [InlineData("GGGGG", "1", "0.60", OverallQualityBand.High, PathDirection.Positive, PathClassification.Balanced)]
    [InlineData("GGGGB", "0.60", "-0.04", OverallQualityBand.High, PathDirection.Neutral, PathClassification.Balanced)]
    [InlineData("BBBBG", "-0.60", "0.04", OverallQualityBand.Low, PathDirection.Neutral, PathClassification.NotBalanced)]
    [InlineData("MMMMM", "0", "0", OverallQualityBand.Medium, PathDirection.Neutral, PathClassification.Acceptable)]
    [InlineData("BMMMG", "0", "0.34", OverallQualityBand.Medium, PathDirection.Positive, PathClassification.Balanced)]
    [InlineData("GMMMB", "0", "-0.34", OverallQualityBand.Medium, PathDirection.Negative, PathClassification.NotBalanced)]
    [InlineData("BBMGG", "0", "0.46", OverallQualityBand.Medium, PathDirection.Positive, PathClassification.Balanced)]
    [InlineData("BBBGG", "-0.20", "0.36", OverallQualityBand.Low, PathDirection.Positive, PathClassification.Acceptable)]
    [InlineData("BMBMG", "-0.20", "0.24", OverallQualityBand.Low, PathDirection.Positive, PathClassification.Acceptable)]
    [InlineData("MBMBM", "-0.40", "-0.20", OverallQualityBand.Low, PathDirection.Negative, PathClassification.NotBalanced)]
    [InlineData("BMG", "0", "0.60", OverallQualityBand.Medium, PathDirection.Positive, PathClassification.Balanced)]
    [InlineData("GMB", "0", "-0.60", OverallQualityBand.Medium, PathDirection.Negative, PathClassification.NotBalanced)]
    // Four participations and the High/Negative matrix cell, with the exact -0.20 boundary.
    [InlineData("GGGB", "0.50", "-0.20", OverallQualityBand.High, PathDirection.Negative, PathClassification.Acceptable)]
    [InlineData("GGGMG", "0.80", "0.44", OverallQualityBand.High, PathDirection.Positive, PathClassification.Balanced)]
    [InlineData("GGMGB", "0.40", "-0.14", OverallQualityBand.High, PathDirection.Neutral, PathClassification.Balanced)]
    [InlineData("BBG", "-0.3333333333333333333333333333", "0.4666666666666666666666666667", OverallQualityBand.Low, PathDirection.Positive, PathClassification.Acceptable)]
    public void Evaluate_AppliesQuantitativeRubricToRepresentativePaths(string path,
        string overallQuality, string directionScore, OverallQualityBand qualityBand,
        PathDirection direction, PathClassification classification)
    {
        var result = PathClassificationRubric.Evaluate(AttendeeId, Entries(path));

        result.AttendeeId.Should().Be(AttendeeId);
        result.PathLength.Should().Be(path.Length);
        result.OverallQuality!.Value.Should().BeApproximately(Decimal(overallQuality), 0.000000001m);
        result.DirectionScore!.Value.Should().BeApproximately(Decimal(directionScore), 0.000000001m);
        result.OverallQualityBand.Should().Be(qualityBand);
        result.Direction.Should().Be(direction);
        result.Classification.Should().Be(classification);
        result.RubricVersion.Should().Be("v1");
        result.OverallQuality.Value.Should().BeInRange(-1m, 1m);
        result.DirectionScore.Value.Should().BeInRange(-1m, 1m);
    }

    [Theory]
    [InlineData("GG", "1", "0.60", OverallQualityBand.High, PathDirection.Positive, PathClassification.Balanced)]
    [InlineData("GM", "0.50", "-0.20", OverallQualityBand.High, PathDirection.Negative, PathClassification.Acceptable)]
    [InlineData("GB", "0", "-1", OverallQualityBand.Medium, PathDirection.Negative, PathClassification.NotBalanced)]
    [InlineData("MG", "0.50", "0.80", OverallQualityBand.High, PathDirection.Positive, PathClassification.Balanced)]
    [InlineData("MM", "0", "0", OverallQualityBand.Medium, PathDirection.Neutral, PathClassification.Acceptable)]
    [InlineData("MB", "-0.50", "-0.80", OverallQualityBand.Low, PathDirection.Negative, PathClassification.NotBalanced)]
    [InlineData("BG", "0", "1", OverallQualityBand.Medium, PathDirection.Positive, PathClassification.Balanced)]
    [InlineData("BM", "-0.50", "0.20", OverallQualityBand.Low, PathDirection.Positive, PathClassification.Acceptable)]
    [InlineData("BB", "-1", "-0.60", OverallQualityBand.Low, PathDirection.Negative, PathClassification.NotBalanced)]
    public void Evaluate_AccountsForCurrentLevelAndChangeInEachTransition(string path,
        string overallQuality, string directionScore, OverallQualityBand qualityBand,
        PathDirection direction, PathClassification classification)
    {
        var result = PathClassificationRubric.Evaluate(AttendeeId, Entries(path));

        result.OverallQuality.Should().Be(Decimal(overallQuality));
        result.DirectionScore.Should().Be(Decimal(directionScore));
        result.OverallQualityBand.Should().Be(qualityBand);
        result.Direction.Should().Be(direction);
        result.Classification.Should().Be(classification);
    }

    [Theory]
    [InlineData("-1", OverallQualityBand.Low)]
    [InlineData("-0.200001", OverallQualityBand.Low)]
    [InlineData("-0.20", OverallQualityBand.Low)]
    [InlineData("-0.199999", OverallQualityBand.Medium)]
    [InlineData("0", OverallQualityBand.Medium)]
    [InlineData("0.399999", OverallQualityBand.Medium)]
    [InlineData("0.40", OverallQualityBand.High)]
    [InlineData("0.400001", OverallQualityBand.High)]
    [InlineData("1", OverallQualityBand.High)]
    public void OverallQualityBands_PreserveInclusiveBoundaries(string score, OverallQualityBand expected)
    {
        PathClassificationRubric.GetOverallQualityBand(Decimal(score)).Should().Be(expected);
    }

    [Theory]
    [InlineData("-0.200001", PathDirection.Negative)]
    [InlineData("-0.20", PathDirection.Negative)]
    [InlineData("-0.199999", PathDirection.Neutral)]
    [InlineData("0", PathDirection.Neutral)]
    [InlineData("0.199999", PathDirection.Neutral)]
    [InlineData("0.20", PathDirection.Positive)]
    [InlineData("0.200001", PathDirection.Positive)]
    public void DirectionBands_PreserveInclusiveBoundaries(string score, PathDirection expected)
    {
        PathClassificationRubric.GetDirection(Decimal(score)).Should().Be(expected);
    }

    [Theory]
    [InlineData("G", "1", OverallQualityBand.High)]
    [InlineData("M", "0", OverallQualityBand.Medium)]
    [InlineData("B", "-1", OverallQualityBand.Low)]
    public void Evaluate_SingleParticipationHasDiagnosticQualityButInsufficientEvidence(
        string path, string overallQuality, OverallQualityBand qualityBand)
    {
        var result = PathClassificationRubric.Evaluate(AttendeeId, Entries(path));

        result.PathLength.Should().Be(1);
        result.OverallQuality.Should().Be(Decimal(overallQuality));
        result.OverallQualityBand.Should().Be(qualityBand);
        result.DirectionScore.Should().BeNull();
        result.Direction.Should().BeNull();
        result.Classification.Should().Be(PathClassification.InsufficientEvidence);
    }

    [Fact]
    public void Evaluate_NoAssignmentsPreservesAttendeeWithoutScoresOrClassification()
    {
        var result = PathClassificationRubric.Evaluate(AttendeeId, []);

        result.AttendeeId.Should().Be(AttendeeId);
        result.Entries.Should().BeEmpty();
        result.PathLength.Should().Be(0);
        result.OverallQuality.Should().BeNull();
        result.DirectionScore.Should().BeNull();
        result.OverallQualityBand.Should().BeNull();
        result.Direction.Should().BeNull();
        result.Classification.Should().BeNull();
    }

    [Fact]
    public void Evaluate_AbsentDaysAndElapsedTimeDoNotWeightDirection()
    {
        var entries = Entries("BMG");
        var spaced = entries.Select((entry, index) => entry with
        {
            FestivalDate = entry.FestivalDate.AddDays(index * index),
            AssignedAt = entry.AssignedAt.AddDays(index * index).AddHours(index)
        });

        var result = PathClassificationRubric.Evaluate(AttendeeId, spaced);
        var consecutive = PathClassificationRubric.Evaluate(AttendeeId, entries);

        result.Entries.Should().HaveCount(3);
        result.DirectionScore.Should().Be(consecutive.DirectionScore);
        result.OverallQuality.Should().Be(consecutive.OverallQuality);
        result.Classification.Should().Be(consecutive.Classification);
    }

    [Fact]
    public void Evaluate_SnapshotsEvidenceInTimestampThenAssignmentIdOrder()
    {
        var original = Entries("BMG");
        var timestamp = original[0].AssignedAt;
        var tied = original.Select(entry => entry with { AssignedAt = timestamp }).ToArray();
        var input = tied.Reverse().ToArray();

        var result = PathClassificationRubric.Evaluate(AttendeeId, input);
        input[0] = original[0];

        result.Entries.Should().Equal(tied);
        result.DirectionScore.Should().Be(0.60m);
    }

    [Fact]
    public void Evaluate_UndefinedQualityCannotBecomeASyntheticOutcome()
    {
        var entry = Entries("G")[0] with { ExperienceQuality = (ExperienceQuality)42 };

        var evaluate = () => PathClassificationRubric.Evaluate(AttendeeId, [entry]);

        evaluate.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static decimal Decimal(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static AttendeePathEntry[] Entries(string path) => path.Select((quality, index) =>
        new AttendeePathEntry(
            AssignmentId.Create(Guid.Parse($"00000000-0000-0000-0000-{index + 1:D12}")),
            FestivalDayId.Create(Guid.Parse($"20000000-0000-0000-0000-{index + 1:D12}")),
            new DateOnly(2026, 8, 15).AddDays(index),
            new DateTimeOffset(2026, 8, 15, 14, 0, 0, TimeSpan.Zero).AddDays(index),
            quality switch { 'G' => ExperienceQuality.Good, 'M' => ExperienceQuality.Medium, _ => ExperienceQuality.Bad }))
        .ToArray();
}
