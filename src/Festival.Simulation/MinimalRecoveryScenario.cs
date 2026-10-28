using Festival.Application.Assignments.Ports;
using Festival.Application.Assignments.ProcessAssignmentRequest;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;
using Festival.Infrastructure.Persistence;

namespace Festival.Simulation;

// Code-defined development scenario, not representative festival-scale evidence.
internal static class MinimalRecoveryScenario
{
    private static readonly FestivalDayId DayOne = FestivalDayId.Create(
        Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly FestivalDayId DayTwo = FestivalDayId.Create(
        Guid.Parse("10000000-0000-0000-0000-000000000002"));
    private static readonly AttendeeCode[] Codes =
        [AttendeeCode.Create("SIM-001"), AttendeeCode.Create("SIM-002")];

    internal static IReadOnlyList<ProcessAssignmentRequestCommand> Requests { get; } =
        Array.AsReadOnly(new[] { CreateRequest(DayOne, 15), CreateRequest(DayTwo, 16) });

    internal static async Task SeedAsync(
        FestivalDbContext context,
        IAssignmentRequestRepository requestRepository,
        CancellationToken cancellationToken)
    {
        context.Attendees.AddRange(
            Attendee.Create(AttendeeId.Create(Guid.Parse("30000000-0000-0000-0000-000000000001")),
                Codes[0], "First attendee"),
            Attendee.Create(AttendeeId.Create(Guid.Parse("30000000-0000-0000-0000-000000000002")),
                Codes[1], "Second attendee"));

        foreach (var request in Requests)
        {
            context.FestivalDays.Add(FestivalDay.Create(request.FestivalDayId,
                DateOnly.FromDateTime(request.RequestedAt.UtcDateTime),
                AssignmentWindow.Create(new TimeOnly(9, 0), new TimeOnly(18, 0))));
        }

        // All physical topology is created once. Supporting attendees occupy the
        // Good/Medium blocks on Day 1 only; the evaluated group has no initial history.
        AddBlock(context, "20000000-0000-0000-0000-000000000001", ZoneType.UpperLeft, "BAD");
        await SeedOccupiedBlockAsync(context, requestRepository,
            "20000000-0000-0000-0000-000000000002", ZoneType.MiddleCenter, "GOOD", 1,
            cancellationToken);
        await SeedOccupiedBlockAsync(context, requestRepository,
            "20000000-0000-0000-0000-000000000003", ZoneType.MiddleLeft, "MEDIUM", 2,
            cancellationToken);
    }

    private static async Task SeedOccupiedBlockAsync(
        FestivalDbContext context,
        IAssignmentRequestRepository requestRepository,
        string zoneId,
        ZoneType zoneType,
        string code,
        int sequence,
        CancellationToken cancellationToken)
    {
        var spots = AddBlock(context, zoneId, zoneType, code);
        var occupants = Enumerable.Range(1, 2).Select(number => Attendee.Create(
            AttendeeId.Create(Guid.Parse($"30000000-0000-0000-0000-{sequence * 10 + number:D12}")),
            AttendeeCode.Create($"OCC-{code}-{number:D3}"), $"{code} occupant {number}")).ToArray();
        context.Attendees.AddRange(occupants);

        var requestedAt = Requests[0].RequestedAt.AddHours(-1).AddMinutes(sequence);
        var assignedAt = requestedAt.AddSeconds(10);
        var request = AssignmentRequest.Create(
            AssignmentRequestId.Create(Guid.Parse($"40000000-0000-0000-0000-{sequence:D12}")),
            DayOne, occupants.Select(attendee => attendee.Code).ToArray(), requestedAt,
            new RequestEligibility(allowsFrontStanding: true));
        var assignments = occupants.Select((attendee, index) => Assignment.Create(
            AssignmentId.Create(Guid.Parse($"50000000-0000-0000-0000-{sequence * 10 + index + 1:D12}")),
            request.Id, DayOne, attendee.Id, spots[index].Code, spots[index].ZoneId,
            spots[index].RowCode, spots[index].Number,
            ZoneExperienceQualityPolicy.GetQuality(zoneType), assignedAt)).ToArray();

        // Seed complete, valid occupancy graphs, not reservations or target-group outcomes.
        AssignmentGroup.Create(request.Id, DayOne, occupants.Select(attendee => attendee.Id))
            .EnsureValidResult(assignments);
        request.Complete(assignedAt);
        await requestRepository.AddAsync(request, cancellationToken);
        context.Assignments.AddRange(assignments);
    }

    private static ProcessAssignmentRequestCommand CreateRequest(FestivalDayId day, int date)
    {
        var requestedAt = new DateTimeOffset(2026, 8, date, 14, 0, 0, TimeSpan.Zero);
        return new ProcessAssignmentRequestCommand(day, Codes, requestedAt,
            requestedAt.AddMinutes(1), new RequestEligibility(allowsFrontStanding: true));
    }

    private static Spot[] AddBlock(FestivalDbContext context, string id, ZoneType type, string code)
    {
        var zoneId = ZoneId.Create(Guid.Parse(id));
        context.Zones.Add(Zone.Create(zoneId, code, type));
        var spots = Enumerable.Range(1, 2).Select(number =>
            Spot.Create(SpotCode.Create($"{code}-A-{number:D3}"),
                zoneId, RowCode.Create("A"), SpotNumber.Create(number))).ToArray();
        context.Spots.AddRange(spots);
        return spots;
    }
}
