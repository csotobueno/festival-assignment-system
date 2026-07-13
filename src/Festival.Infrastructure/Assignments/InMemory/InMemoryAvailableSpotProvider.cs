using Festival.Application.Assignments.Ports;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;

namespace Festival.Infrastructure.Assignments.InMemory;

public sealed class InMemoryAvailableSpotProvider : IAvailableSpotProvider
{
    private readonly IReadOnlyDictionary<FestivalDayId, IReadOnlyList<Spot>> availableSpotsByFestivalDay;

    public InMemoryAvailableSpotProvider(
        IEnumerable<(FestivalDayId FestivalDayId, Spot Spot)> availableSpots)
    {
        ArgumentNullException.ThrowIfNull(availableSpots);

        availableSpotsByFestivalDay = CreateAvailableSpotsByFestivalDay(
            availableSpots.ToArray());
    }

    public Task<IReadOnlyList<Spot>> GetAvailableSpotsAsync(
        FestivalDayId festivalDayId,
        CancellationToken cancellationToken = default)
    {
        if (festivalDayId == default)
        {
            throw new ArgumentException(
                "Festival day id is required.",
                nameof(festivalDayId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var availableSpots = availableSpotsByFestivalDay.TryGetValue(
            festivalDayId,
            out var spots)
            ? spots
            : Array.AsReadOnly(Array.Empty<Spot>());

        return Task.FromResult(availableSpots);
    }

    private static IReadOnlyDictionary<FestivalDayId, IReadOnlyList<Spot>>
        CreateAvailableSpotsByFestivalDay(
            IReadOnlyCollection<(FestivalDayId FestivalDayId, Spot Spot)> materializedSpots)
    {
        if (materializedSpots.Any(entry => entry.FestivalDayId == default))
        {
            throw new ArgumentException(
                "Festival day id is required.",
                nameof(materializedSpots));
        }

        if (materializedSpots.Any(entry => entry.Spot is null))
        {
            throw new ArgumentException(
                "Available spots cannot contain null values.",
                nameof(materializedSpots));
        }

        return materializedSpots
            .GroupBy(entry => entry.FestivalDayId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Spot>)Array.AsReadOnly(
                    group.Select(entry => entry.Spot).ToArray()));
    }
}
