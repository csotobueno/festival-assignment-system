using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;
using Festival.Infrastructure.Assignments.InMemory;

namespace Festival.Infrastructure.Tests.Assignments.InMemory;

public sealed class InMemoryAvailableSpotProviderTests
{
    [Fact]
    public async Task GetAvailableSpotsAsync_ShouldReturnAvailableSpotsForFestivalDay()
    {
        var firstFestivalDayId = CreateFestivalDayId(1);
        var secondFestivalDayId = CreateFestivalDayId(2);
        var expectedSpot = CreateSpot(1);
        var otherSpot = CreateSpot(2);
        var provider = new InMemoryAvailableSpotProvider(
            [
                (firstFestivalDayId, expectedSpot),
                (secondFestivalDayId, otherSpot)
            ]);

        var availableSpots = await provider.GetAvailableSpotsAsync(
            firstFestivalDayId);

        var spot = Assert.Single(availableSpots);

        Assert.Equal(expectedSpot, spot);
    }

    private static FestivalDayId CreateFestivalDayId(int number)
    {
        return FestivalDayId.Create(
            Guid.Parse($"20000000-0000-0000-0000-{number:000000000000}"));
    }

    private static Spot CreateSpot(int number)
    {
        var zoneId = ZoneId.Create(
            Guid.Parse($"30000000-0000-0000-0000-{number:000000000000}"));

        return Spot.Create(
            SpotCode.Create($"SPOT-{number:000}"),
            zoneId,
            RowCode.Create("A"),
            SpotNumber.Create(number));
    }
}
