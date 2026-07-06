using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;

namespace Festival.Application.Assignments;

public interface IAvailableSpotProvider
{
    Task<IReadOnlyList<Spot>> GetAvailableSpotsAsync(
        FestivalDayId festivalDayId,
        CancellationToken cancellationToken = default);
}
