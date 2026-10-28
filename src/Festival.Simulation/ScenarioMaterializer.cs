using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Simulation;

public sealed class ScenarioMaterializer
{
    public ScenarioManifest Materialize(ScenarioDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Validate(definition);

        var attendees = Enumerable.Range(1, definition.AttendeeCount).Select(number =>
            Attendee.Create(AttendeeId.Create(Id(definition, "attendee", $"evaluated-{Number(number)}")),
                AttendeeCode.Create($"SIM-{Number(number)}"), $"Evaluated attendee {Number(number)}"))
            .ToImmutableArray();
        var zones = definition.Topology.OrderBy(block => block.Key, StringComparer.Ordinal)
            .Select(block => Zone.Create(ZoneId.Create(Id(definition, "zone", block.Key)),
                block.Key.ToUpperInvariant(), block.ZoneType)).ToImmutableArray();
        var spots = definition.Topology.OrderBy(block => block.Key, StringComparer.Ordinal)
            .SelectMany(block => Enumerable.Range(1, block.SpotCount).Select(number =>
                Spot.Create(SpotCode.Create($"{block.Key.ToUpperInvariant()}-A-{Number(number)}"),
                    ZoneId.Create(Id(definition, "zone", block.Key)), RowCode.Create("A"),
                    SpotNumber.Create(number)))).ToImmutableArray();

        var days = ImmutableArray.CreateBuilder<FestivalDay>();
        var supportingAttendees = ImmutableArray.CreateBuilder<Attendee>();
        var supportingRequests = ImmutableArray.CreateBuilder<SupportingCompletedRequest>();
        var supportingAssignments = ImmutableArray.CreateBuilder<Assignment>();
        var plans = ImmutableArray.CreateBuilder<DailyPlan>();

        foreach (var day in definition.Days.OrderBy(day => day.DayNumber))
        {
            var date = definition.FirstDate.AddDays(day.DayNumber - 1);
            var dayId = FestivalDayId.Create(Id(definition, "day", Number(day.DayNumber)));
            days.Add(FestivalDay.Create(dayId, date,
                AssignmentWindow.Create(new TimeOnly(9, 0), new TimeOnly(18, 0))));
            var businessTime = new DateTimeOffset(date.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero);

            var occupiedBlocks = day.OccupiedBlockKeys.Order(StringComparer.Ordinal).ToArray();
            for (var index = 0; index < occupiedBlocks.Length; index++)
            {
                var blockKey = occupiedBlocks[index];
                var key = $"day-{Number(day.DayNumber)}/{blockKey}";
                var zone = zones.Single(zone => zone.Id == ZoneId.Create(Id(definition, "zone", blockKey)));
                var blockSpots = spots.Where(spot => spot.ZoneId == zone.Id)
                    .OrderBy(spot => spot.Number.Value).ToArray();
                var occupants = blockSpots.Select((_, spotIndex) => Attendee.Create(
                    AttendeeId.Create(Id(definition, "attendee", $"supporting/{key}/{Number(spotIndex + 1)}")),
                    AttendeeCode.Create($"OCC-{Number(day.DayNumber)}-{zone.Name}-{Number(spotIndex + 1)}"),
                    $"{zone.Name} occupant {Number(spotIndex + 1)}")).ToArray();
                supportingAttendees.AddRange(occupants);

                var requestedAt = businessTime.AddHours(-1).AddMinutes(index + 1);
                var completedAt = requestedAt.AddSeconds(10);
                var requestId = AssignmentRequestId.Create(Id(definition, "setup-request", key));
                var eligibility = new RequestEligibility(allowsFrontStanding: true);
                var assignments = occupants.Select((attendee, spotIndex) => Assignment.Create(
                    AssignmentId.Create(Id(definition, "setup-assignment", $"{key}/{Number(spotIndex + 1)}")),
                    requestId, dayId, attendee.Id, blockSpots[spotIndex].Code, zone.Id,
                    blockSpots[spotIndex].RowCode, blockSpots[spotIndex].Number,
                    ZoneExperienceQualityPolicy.GetQuality(zone.Type), completedAt)).ToArray();

                // Validate the exact supporting graph without executing any use case or I/O.
                AssignmentGroup.Create(requestId, dayId, occupants.Select(attendee => attendee.Id))
                    .EnsureValidResult(assignments);
                supportingRequests.Add(new SupportingCompletedRequest(requestId, dayId,
                    occupants.Select(attendee => attendee.Code).ToImmutableArray(),
                    requestedAt, completedAt, eligibility));
                supportingAssignments.AddRange(assignments);
            }

            var requests = day.Requests.OrderBy(request => request.Sequence).Select((request, index) =>
                new DailyRequestPlan(Id(definition, "simulation-request", Number(request.Sequence)),
                    request.Sequence, request.AttendeeNumbers.Select(number => attendees[number - 1]).ToImmutableArray(),
                    businessTime.AddMinutes(index * 2), businessTime.AddMinutes(index * 2 + 1),
                    request.Eligibility)).ToImmutableArray();
            plans.Add(new DailyPlan(day.DayNumber, dayId, requests));
        }

        // Seed is recorded for reproducibility. v1 contains no probabilistic decisions.
        return new ScenarioManifest(new ScenarioMetadata(definition.ScenarioId, definition.Seed,
                definition.GeneratorVersion, definition.ExecutionMode, definition.ConcurrencyLevel),
            new ScenarioCatalog(days.ToImmutable(), attendees, zones, spots),
            new ScenarioSetup(supportingAttendees.ToImmutable(), supportingRequests.ToImmutable(),
                supportingAssignments.ToImmutable()), plans.ToImmutable());
    }

    // v1: SHA-256 of a length-prefixed scenario ID, entity type and stable key;
    // first 16 bytes in network order. Neither seed nor runtime hashing affects IDs.
    private static Guid Id(ScenarioDefinition definition, string entityType, string key)
    {
        var input = FormattableString.Invariant($"{definition.ScenarioId.Length}:{definition.ScenarioId}/{entityType}/{key}");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }

    private static string Number(int number) => number.ToString("D3", CultureInfo.InvariantCulture);

    private static void Validate(ScenarioDefinition definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.ScenarioId);
        if (definition.GeneratorVersion != "v1")
            throw new NotSupportedException("Only scenario generator v1 is supported.");
        if (definition.ExecutionMode != SimulationExecutionMode.Sequential || definition.ConcurrencyLevel != 1)
            throw new NotSupportedException("Only sequential execution with concurrency level 1 is supported.");
        if (definition.AttendeeCount < 1 || definition.Topology.IsDefaultOrEmpty || definition.Days.IsDefaultOrEmpty)
            throw new ArgumentException("A scenario requires evaluated attendees, topology and days.", nameof(definition));

        var keys = definition.Topology.Select(block => block.Key).ToArray();
        if (keys.Any(string.IsNullOrWhiteSpace) || keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != keys.Length
            || definition.Topology.Any(block => block.SpotCount < 1)
            || definition.Days.Any(day => day.DayNumber < 1 || day.OccupiedBlockKeys.IsDefault || day.Requests.IsDefault)
            || definition.Days.Select(day => day.DayNumber).Distinct().Count() != definition.Days.Length)
            throw new ArgumentException("Topology keys and day numbers must be valid and unique.", nameof(definition));

        var requests = definition.Days.SelectMany(day => day.Requests).ToArray();
        if (requests.Any(request => request.Sequence < 1 || request.Eligibility is null
                || request.AttendeeNumbers.IsDefaultOrEmpty
                || request.AttendeeNumbers.Length > GroupSize.Maximum
                || request.AttendeeNumbers.Distinct().Count() != request.AttendeeNumbers.Length
                || request.AttendeeNumbers.Any(number => number < 1 || number > definition.AttendeeCount))
            || requests.Select(request => request.Sequence).Distinct().Count() != requests.Length
            || definition.Days.Any(day => day.OccupiedBlockKeys.Distinct(StringComparer.Ordinal).Count()
                != day.OccupiedBlockKeys.Length || day.OccupiedBlockKeys.Any(key => !keys.Contains(key, StringComparer.Ordinal))))
            throw new ArgumentException("Day plans require valid membership, unique sequences and known occupancy blocks.", nameof(definition));
    }
}
