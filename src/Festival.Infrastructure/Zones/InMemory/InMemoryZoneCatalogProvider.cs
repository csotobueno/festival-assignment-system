using Festival.Application.Zones.Ports;
using Festival.Domain.Zones;

namespace Festival.Infrastructure.Zones.InMemory;

public sealed class InMemoryZoneCatalogProvider : IZoneCatalogProvider
{
    private readonly IReadOnlyList<Zone> zones;

    public InMemoryZoneCatalogProvider(IEnumerable<Zone> zones)
    {
        ArgumentNullException.ThrowIfNull(zones);
        this.zones = Array.AsReadOnly(zones.ToArray());
    }

    public Task<IReadOnlyList<Zone>> GetZonesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(zones);
    }
}
