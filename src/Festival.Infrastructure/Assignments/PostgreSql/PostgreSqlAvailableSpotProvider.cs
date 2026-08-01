using Festival.Application.Assignments.Ports;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.Assignments.PostgreSql;

public sealed class PostgreSqlAvailableSpotProvider
    : IAvailableSpotProvider
{
    private readonly FestivalDbContext dbContext;

    public PostgreSqlAvailableSpotProvider(FestivalDbContext dbContext)
    {
        this.dbContext = dbContext
            ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<Spot>> GetAvailableSpotsAsync(
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

        var spots = await dbContext.Spots
            .AsNoTracking()
            .Where(spot => !dbContext.Assignments.Any(
                assignment =>
                    assignment.FestivalDayId == festivalDayId
                    && assignment.SpotCode == spot.Code))
            .OrderBy(spot => spot.ZoneId)
            .ThenBy(spot => spot.RowCode)
            .ThenBy(spot => spot.Number)
            .ThenBy(spot => spot.Code)
            .ToArrayAsync(cancellationToken);

        return Array.AsReadOnly(spots);
    }
}
