using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Simulation;
using FluentAssertions;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class AttendeeRecoveryAnalyzerTests
{
    private static readonly AttendeeId AttendeeId = AttendeeId.Create(Guid.Parse("10000000-0000-0000-0000-000000000001"));

    [Theory]
    [InlineData("")]
    [InlineData("GGM")]
    [InlineData("MG")]
    [InlineData("GM")]
    public void Analyze_NoBadRetainsAttendeeWithNoEpisodes(string sequence)
    {
        var path = PathClassificationRubric.Evaluate(AttendeeId, Entries(sequence));

        var result = AttendeeRecoveryAnalyzer.Analyze(path);

        result.AttendeeId.Should().Be(AttendeeId);
        result.Episodes.Should().BeEmpty();
    }

    [Theory]
    [InlineData("BM", ExperienceQuality.Medium, 1)]
    [InlineData("BG", ExperienceQuality.Good, 1)]
    [InlineData("BBM", ExperienceQuality.Medium, 2)]
    [InlineData("BBBG", ExperienceQuality.Good, 3)]
    [InlineData("BBMG", ExperienceQuality.Medium, 2)]
    public void Analyze_FirstMediumOrGoodClosesOneEpisodeFromFirstBad(
        string sequence, ExperienceQuality recoveryQuality, int opportunities)
    {
        var entries = Entries(sequence);
        var path = PathClassificationRubric.Evaluate(AttendeeId, entries);

        var episode = AttendeeRecoveryAnalyzer.Analyze(path).Episodes.Should().ContainSingle().Subject;

        episode.AttendeeId.Should().Be(AttendeeId);
        episode.StartAssignmentId.Should().Be(entries[0].AssignmentId);
        episode.StartFestivalDayId.Should().Be(entries[0].FestivalDayId);
        episode.StartFestivalDate.Should().Be(entries[0].FestivalDate);
        episode.StartAssignedAt.Should().Be(entries[0].AssignedAt);
        episode.Status.Should().Be(RecoveryEpisodeStatus.Recovered);
        episode.RecoveryAssignmentId.Should().Be(entries[opportunities].AssignmentId);
        episode.RecoveryFestivalDayId.Should().Be(entries[opportunities].FestivalDayId);
        episode.RecoveryFestivalDate.Should().Be(entries[opportunities].FestivalDate);
        episode.RecoveryAssignedAt.Should().Be(entries[opportunities].AssignedAt);
        episode.RecoveryQuality.Should().Be(recoveryQuality);
        episode.OpportunitiesToRecovery.Should().Be(opportunities);
    }

    [Theory]
    [InlineData("B")]
    [InlineData("BB")]
    [InlineData("GBBB")]
    public void Analyze_PathEndingInBadLeavesOneUnresolvedEpisode(string sequence)
    {
        var entries = Entries(sequence);
        var path = PathClassificationRubric.Evaluate(AttendeeId, entries);

        var episode = AttendeeRecoveryAnalyzer.Analyze(path).Episodes.Should().ContainSingle().Subject;

        episode.AttendeeId.Should().Be(AttendeeId);
        episode.StartAssignmentId.Should().Be(entries[sequence.IndexOf('B')].AssignmentId);
        episode.Status.Should().Be(RecoveryEpisodeStatus.Unresolved);
        episode.RecoveryAssignmentId.Should().BeNull();
        episode.RecoveryFestivalDayId.Should().BeNull();
        episode.RecoveryFestivalDate.Should().BeNull();
        episode.RecoveryAssignedAt.Should().BeNull();
        episode.RecoveryQuality.Should().BeNull();
        episode.OpportunitiesToRecovery.Should().BeNull();
    }

    [Theory]
    [InlineData("BMBG", 1, 1)]
    [InlineData("BBMBBG", 2, 2)]
    [InlineData("BBMGBBG", 2, 2)]
    public void Analyze_LaterBadAfterRecoveryStartsAnIndependentEpisode(
        string sequence, int firstOpportunities, int secondOpportunities)
    {
        var entries = Entries(sequence);
        var path = PathClassificationRubric.Evaluate(AttendeeId, entries);

        var episodes = AttendeeRecoveryAnalyzer.Analyze(path).Episodes;

        episodes.Should().HaveCount(2).And.OnlyContain(episode => episode.Status == RecoveryEpisodeStatus.Recovered);
        episodes.Select(episode => episode.OpportunitiesToRecovery).Should().Equal(firstOpportunities, secondOpportunities);
        episodes.Select(episode => episode.RecoveryQuality).Should().Equal(ExperienceQuality.Medium, ExperienceQuality.Good);
        episodes[0].StartAssignmentId.Should().Be(entries[0].AssignmentId);
        episodes[0].RecoveryAssignmentId.Should().Be(entries[firstOpportunities].AssignmentId);
        episodes[1].StartAssignmentId.Should().Be(entries[sequence.IndexOf('B', firstOpportunities + 1)].AssignmentId);
        episodes[1].RecoveryAssignmentId.Should().Be(entries[^1].AssignmentId);
    }

    [Fact]
    public void Analyze_RecoveredEpisodeCanBeFollowedByAnUnresolvedEpisode()
    {
        var entries = Entries("BMBB");
        var path = PathClassificationRubric.Evaluate(AttendeeId, entries);

        var episodes = AttendeeRecoveryAnalyzer.Analyze(path).Episodes;

        episodes.Should().HaveCount(2);
        episodes[0].Status.Should().Be(RecoveryEpisodeStatus.Recovered);
        episodes[0].OpportunitiesToRecovery.Should().Be(1);
        episodes[1].Status.Should().Be(RecoveryEpisodeStatus.Unresolved);
        episodes[1].StartAssignmentId.Should().Be(entries[2].AssignmentId);
        episodes[1].RecoveryAssignmentId.Should().BeNull();
        episodes[1].RecoveryQuality.Should().BeNull();
        episodes[1].OpportunitiesToRecovery.Should().BeNull();
    }

    [Fact]
    public void Analyze_CalendarGapsDoNotAddAssignedOpportunities()
    {
        var entries = Entries("BG");
        entries[1] = entries[1] with
        {
            FestivalDate = entries[0].FestivalDate.AddDays(4),
            AssignedAt = entries[0].AssignedAt.AddDays(4)
        };
        // Absence or requests without persisted Assignments leave no entries.
        var path = PathClassificationRubric.Evaluate(AttendeeId, entries);

        var episode = AttendeeRecoveryAnalyzer.Analyze(path).Episodes.Should().ContainSingle().Subject;

        episode.OpportunitiesToRecovery.Should().Be(1);
        episode.StartFestivalDate.Should().Be(entries[0].FestivalDate);
        episode.RecoveryFestivalDate.Should().Be(entries[1].FestivalDate);
        episode.RecoveryAssignedAt.Should().Be(entries[1].AssignedAt);
    }

    [Fact]
    public void Analyze_UsesReconstructedEntryOrderRatherThanFestivalDates()
    {
        var entries = Entries("BMG");
        entries[1] = entries[1] with { FestivalDate = entries[0].FestivalDate.AddDays(10) };
        var path = PathClassificationRubric.Evaluate(AttendeeId, entries.Reverse());

        var episode = AttendeeRecoveryAnalyzer.Analyze(path).Episodes.Should().ContainSingle().Subject;

        episode.RecoveryAssignmentId.Should().Be(entries[1].AssignmentId);
        episode.RecoveryQuality.Should().Be(ExperienceQuality.Medium);
        episode.OpportunitiesToRecovery.Should().Be(1);
    }

    [Fact]
    public void Analyze_EpisodeCollectionIsReadOnlyAndIndependentOfLaterAnalysis()
    {
        var path = PathClassificationRubric.Evaluate(AttendeeId, Entries("BMBG"));
        var result = AttendeeRecoveryAnalyzer.Analyze(path);
        var collection = (IList<RecoveryEpisode>)result.Episodes;

        var replace = () => collection[0] = collection[1];
        var clear = () => collection.Clear();

        collection.IsReadOnly.Should().BeTrue();
        replace.Should().Throw<NotSupportedException>();
        clear.Should().Throw<NotSupportedException>();
        AttendeeRecoveryAnalyzer.Analyze(PathClassificationRubric.Evaluate(AttendeeId, Entries("B")))
            .Episodes.Should().ContainSingle().Which.Status.Should().Be(RecoveryEpisodeStatus.Unresolved);
        result.Episodes.Should().HaveCount(2).And.OnlyContain(episode => episode.Status == RecoveryEpisodeStatus.Recovered);
    }

    [Fact]
    public void Analyze_RecoveryDoesNotOverrideQuantitativePathClassification()
    {
        var path = PathClassificationRubric.Evaluate(AttendeeId, Entries("BBBBG"));

        var recovery = AttendeeRecoveryAnalyzer.Analyze(path);

        recovery.Episodes.Should().ContainSingle().Which.Status.Should().Be(RecoveryEpisodeStatus.Recovered);
        path.OverallQuality.Should().Be(-0.60m);
        path.DirectionScore.Should().Be(0.04m);
        path.OverallQualityBand.Should().Be(OverallQualityBand.Low);
        path.Direction.Should().Be(PathDirection.Neutral);
        path.Classification.Should().Be(PathClassification.NotBalanced);
        path.RubricVersion.Should().Be("v1");
    }

    [Fact]
    public void Analyze_NullPathIsRejected()
    {
        var analyze = () => AttendeeRecoveryAnalyzer.Analyze(null!);

        analyze.Should().Throw<ArgumentNullException>();
    }

    private static AttendeePathEntry[] Entries(string sequence) => sequence.Select((quality, index) =>
        new AttendeePathEntry(
            AssignmentId.Create(Guid.Parse($"00000000-0000-0000-0000-{index + 1:D12}")),
            FestivalDayId.Create(Guid.Parse($"20000000-0000-0000-0000-{index + 1:D12}")),
            new DateOnly(2026, 8, 15).AddDays(index),
            new DateTimeOffset(2026, 8, 15, 14, 0, 0, TimeSpan.Zero).AddDays(index),
            quality switch { 'G' => ExperienceQuality.Good, 'M' => ExperienceQuality.Medium, _ => ExperienceQuality.Bad }))
        .ToArray();
}
