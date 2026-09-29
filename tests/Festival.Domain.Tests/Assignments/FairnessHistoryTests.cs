using Festival.Domain.Assignments;

namespace Festival.Domain.Tests.Assignments;

public sealed class FairnessHistoryTests
{
    [Fact]
    public void Create_ShouldReturnEmptyHistory_WhenNoExperiencesAreProvided()
    {
        var history = FairnessHistory.Create([]);

        Assert.Empty(history.Experiences);
    }

    [Theory]
    [InlineData(ExperienceQuality.Good)]
    [InlineData(ExperienceQuality.Medium)]
    [InlineData(ExperienceQuality.Bad)]
    public void Create_ShouldPreserveSingleExperience(ExperienceQuality quality)
    {
        var history = FairnessHistory.Create([quality]);

        Assert.Equal(quality, Assert.Single(history.Experiences));
    }

    [Fact]
    public void Create_ShouldPreserveChronologicalOrder_WhenMultipleExperiencesAreProvided()
    {
        ExperienceQuality[] experiences =
            [ExperienceQuality.Bad, ExperienceQuality.Medium, ExperienceQuality.Good];

        var history = FairnessHistory.Create(experiences);

        Assert.Equal(experiences, history.Experiences);
    }

    [Fact]
    public void Create_ShouldPreserveRepeatedExperiences()
    {
        var history = FairnessHistory.Create([ExperienceQuality.Bad, ExperienceQuality.Bad]);

        Assert.Equal(
            new[] { ExperienceQuality.Bad, ExperienceQuality.Bad },
            history.Experiences);
    }

    [Fact]
    public void Create_ShouldContainOnlyRealExperiences_WhenParticipationIsIntermittent()
    {
        // Day 1: Bad; Day 2: absent; Day 3: Medium.
        var history = FairnessHistory.Create([ExperienceQuality.Bad, ExperienceQuality.Medium]);

        Assert.Equal(
            new[] { ExperienceQuality.Bad, ExperienceQuality.Medium },
            history.Experiences);
    }

    [Fact]
    public void Experiences_ShouldRemainUnchanged_WhenSourceCollectionIsModified()
    {
        var experiences = new List<ExperienceQuality> { ExperienceQuality.Bad };
        var history = FairnessHistory.Create(experiences);

        experiences[0] = ExperienceQuality.Good;
        experiences.Add(ExperienceQuality.Medium);

        Assert.Equal(ExperienceQuality.Bad, Assert.Single(history.Experiences));
    }

    [Fact]
    public void Experiences_ShouldNotExposeMutableCollection()
    {
        var history = FairnessHistory.Create([ExperienceQuality.Bad]);
        var experiences = (IList<ExperienceQuality>)history.Experiences;

        Assert.Throws<NotSupportedException>(() => experiences[0] = ExperienceQuality.Good);
        Assert.Throws<NotSupportedException>(() => experiences.Add(ExperienceQuality.Medium));
        Assert.Equal(ExperienceQuality.Bad, Assert.Single(history.Experiences));
    }

    [Fact]
    public void Create_ShouldThrow_WhenExperiencesAreNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => FairnessHistory.Create(null!));

        Assert.Equal("experiences", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(99)]
    public void Create_ShouldThrow_WhenExperienceQualityIsUndefined(int value)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => FairnessHistory.Create([ExperienceQuality.Good, (ExperienceQuality)value]));

        Assert.Equal("experiences", exception.ParamName);
    }
}
