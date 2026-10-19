using Festival.Application.Zones.Ports;
using Festival.Domain.Zones;
using Festival.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.Zones.PostgreSql;

public sealed class PostgreSqlZoneCatalogProvider : IZoneCatalogProvider
{
    private readonly FestivalDbContext dbContext;

    public PostgreSqlZoneCatalogProvider(FestivalDbContext dbContext)
    {
        this.dbContext = dbContext
            ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<Zone>> GetZonesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var zones = await dbContext.Zones
            .AsNoTracking()
            // Technical ordering only; selection policies own business priority.
            .OrderBy(zone => zone.Id)
            .ToArrayAsync(cancellationToken);

        return Array.AsReadOnly(zones);
    }
}
