using Festival.Domain.Assignments;

namespace Festival.Domain.Zones;

public static class ZoneEligibilityPolicy
{
    public static IReadOnlyList<Zone> Filter(
        AssignmentRequest request,
        IEnumerable<Zone> availableZones)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(availableZones);

        var zones = availableZones.ToArray();

        if (zones.Any(zone => zone is null))
        {
            throw new ArgumentException(
                "Available zones cannot contain null values.",
                nameof(availableZones));
        }

        return Array.AsReadOnly(zones
            .Where(zone => request.Eligibility.AllowsFrontStanding || !zone.IsFrontStanding)
            .ToArray());
    }
}
