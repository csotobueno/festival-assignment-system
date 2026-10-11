using Festival.Domain.Assignments;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Assignments;

public sealed class RemainingInventoryStateTests
{
    [Fact]
    public void Create_ShouldDeriveQualityTotalsFromIndependentZoneEntries()
    {
        ZoneAvailabilityState[] entries = [CreateEntry(1, ExperienceQuality.Good, 2),
            CreateEntry(2, ExperienceQuality.Good, 5), CreateEntry(3, ExperienceQuality.Medium, 3),
            CreateEntry(4, ExperienceQuality.Bad, 1)];
        var state = RemainingInventoryState.Create(entries);

        Assert.Equal(entries, state.ZoneAvailabilities);
        Assert.Equal(7, state.GoodRemaining);
        Assert.Equal(3, state.MediumRemaining);
        Assert.Equal(1, state.BadRemaining);
    }

    [Fact]
    public void Create_ShouldDeriveZeroTotals_FromEmptyAvailability()
    {
        var state = RemainingInventoryState.Create([]);

        Assert.Empty(state.ZoneAvailabilities);
        Assert.Equal(0, state.GoodRemaining);
        Assert.Equal(0, state.MediumRemaining);
        Assert.Equal(0, state.BadRemaining);
    }

    [Fact]
    public void SnapshotAndDerivedTotals_ShouldRemainUnchanged_WhenSourceIsModified()
    {
        var entry = CreateEntry(1, ExperienceQuality.Good, 3);
        var entries = new List<ZoneAvailabilityState> { entry };
        var state = RemainingInventoryState.Create(entries);

        entries[0] = CreateEntry(1, ExperienceQuality.Bad, 5);
        entries.Add(CreateEntry(2, ExperienceQuality.Medium, 4));

        Assert.Equal(entry, Assert.Single(state.ZoneAvailabilities));
        Assert.Equal(3, state.GoodRemaining);
        Assert.Equal(0, state.MediumRemaining);
        Assert.Equal(0, state.BadRemaining);
    }

    [Fact]
    public void ZoneAvailabilities_ShouldNotExposeMutableCollection()
    {
        var entry = CreateEntry(1, ExperienceQuality.Good, 3);
        var state = RemainingInventoryState.Create([entry]);
        var entries = (IList<ZoneAvailabilityState>)state.ZoneAvailabilities;

        Assert.Throws<NotSupportedException>(() => entries[0] = CreateEntry(2, ExperienceQuality.Bad, 1));
        Assert.Throws<NotSupportedException>(() => entries.Clear());
        Assert.Equal(entry, Assert.Single(state.ZoneAvailabilities));
        Assert.Equal(3, state.GoodRemaining);
    }

    [Fact]
    public void Create_ShouldRejectNullCollection()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => RemainingInventoryState.Create(null!));

        Assert.Equal("zoneAvailabilities", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldRejectNullElements()
    {
        var exception = Assert.Throws<ArgumentException>(() => RemainingInventoryState.Create([null!]));

        Assert.Equal("zoneAvailabilities", exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_ShouldRejectDuplicateZoneIds(bool sameObject)
    {
        var entry = CreateEntry(1, ExperienceQuality.Good, 3);
        var duplicate = sameObject ? entry : CreateEntry(1, ExperienceQuality.Bad, 1);

        var exception = Assert.Throws<ArgumentException>(() => RemainingInventoryState.Create([entry, duplicate]));

        Assert.Equal("zoneAvailabilities", exception.ParamName);
    }

    private static ZoneAvailabilityState CreateEntry(int id, ExperienceQuality quality, int count) =>
        ZoneAvailabilityState.Create(
            ZoneId.Create(Guid.Parse($"20000000-0000-0000-0000-{id:D12}")), quality, count);
}
