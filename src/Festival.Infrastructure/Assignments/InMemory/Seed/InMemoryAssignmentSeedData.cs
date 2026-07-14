using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Infrastructure.Assignments.InMemory.Seed;

public static class InMemoryAssignmentSeedData
{
    public static FestivalDayId FestivalDayId { get; } =
        FestivalDayId.Create(
            Guid.Parse("10000000-0000-0000-0000-000000000001"));

    public static IReadOnlyList<Attendee> Attendees { get; } =
        CreateAttendees();

    public static FestivalDay FestivalDay { get; } =
        FestivalDay.Create(
            FestivalDayId,
            new DateOnly(2026, 7, 10),
            AssignmentWindow.Create(
                new TimeOnly(9, 0),
                new TimeOnly(18, 0)));

    public static IReadOnlyList<Zone> Zones { get; } =
    [
        Zone.Create(
            ZoneId.Create(
                Guid.Parse("20000000-0000-0000-0000-000000000001")),
            "Zone A"),
        Zone.Create(
            ZoneId.Create(
                Guid.Parse("20000000-0000-0000-0000-000000000002")),
            "Zone B")
    ];

    public static IReadOnlyList<Spot> Spots { get; } =
        CreateSpots();

    public static IReadOnlyList<(FestivalDayId FestivalDayId, Spot Spot)>
        AvailableSpots { get; } =
        Spots
            .Select(spot => (FestivalDayId: FestivalDayId, Spot: spot))
            .ToArray();

    private static IReadOnlyList<Attendee> CreateAttendees()
    {
        return Enumerable
            .Range(1, 10)
            .Select(number => Attendee.Create(
                AttendeeId.Create(
                    Guid.Parse(
                        $"30000000-0000-0000-0000-{number:000000000000}")),
                AttendeeCode.Create($"ATT-{number:000}"),
                $"Attendee {number:000}"))
            .ToArray();
    }

    private static IReadOnlyList<Spot> CreateSpots()
    {
        return Zones
            .SelectMany((zone, zoneIndex) =>
                new[] { "A", "B" }
                    .SelectMany(rowCode =>
                        Enumerable
                            .Range(1, 4)
                            .Select(spotNumber => Spot.Create(
                                SpotCode.Create(
                                    $"{ZoneCode(zoneIndex)}-{rowCode}-{spotNumber:000}"),
                                zone.Id,
                                RowCode.Create(rowCode),
                                SpotNumber.Create(spotNumber)))))
            .ToArray();
    }

    private static string ZoneCode(int zoneIndex)
    {
        return zoneIndex == 0 ? "ZONE-A" : "ZONE-B";
    }
}
