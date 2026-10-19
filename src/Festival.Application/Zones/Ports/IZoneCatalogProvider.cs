using Festival.Domain.Zones;

namespace Festival.Application.Zones.Ports;

public interface IZoneCatalogProvider
{
    Task<IReadOnlyList<Zone>> GetZonesAsync(
        CancellationToken cancellationToken = default);
}
