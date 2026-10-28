using Festival.Application.Assignments.Ports;
using Festival.Application.Assignments.ProcessAssignmentRequest;
using Festival.Application.Zones.Ports;
using Festival.Domain.Assignments;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Simulation;

// Scoped pass-through: retain the actual port responses only for this invocation.
// No extra database read, changed provider response or duplicate block search.
internal sealed class RequestEvaluationObservation(
    IZoneCatalogProvider zoneProvider,
    IAvailableSpotProvider spotProvider) : IZoneCatalogProvider, IAvailableSpotProvider
{
    private IReadOnlyList<Zone>? zones;
    private IReadOnlyList<Spot>? availableSpots;

    public async Task<IReadOnlyList<Zone>> GetZonesAsync(CancellationToken cancellationToken = default)
    {
        zones = await zoneProvider.GetZonesAsync(cancellationToken);
        return zones;
    }

    public async Task<IReadOnlyList<Spot>> GetAvailableSpotsAsync(
        FestivalDayId festivalDayId, CancellationToken cancellationToken = default)
    {
        availableSpots = await spotProvider.GetAvailableSpotsAsync(festivalDayId, cancellationToken);
        return availableSpots;
    }

    public int? GetEligibleAvailableSpotCount(ProcessAssignmentRequestCommand command, Guid simulationRequestId)
    {
        if (zones is null || availableSpots is null)
            return null; // An early rejection/failure did not reach these reads.

        // The policy takes an AssignmentRequest. This unpersisted copy supplies the
        // same command eligibility/codes; its stable ID has no role in filtering.
        var request = AssignmentRequest.Create(AssignmentRequestId.Create(simulationRequestId),
            command.FestivalDayId, command.AttendeeCodes, command.RequestedAt, command.Eligibility);
        var eligibleZoneIds = ZoneEligibilityPolicy.Filter(request, zones)
            .Select(zone => zone.Id).ToHashSet();
        return availableSpots.Count(spot => eligibleZoneIds.Contains(spot.ZoneId));
    }
}
