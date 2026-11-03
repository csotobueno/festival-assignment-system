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
            .SelectMany(block => Enumerable.Range(0, block.RowCount).SelectMany(row =>
                Enumerable.Range(1, block.SpotCount + row * block.GrowthPerRow).Select(number =>
                {
                    var rowCode = block.RowCount == 1 ? "A" : $"R{Number(row + 1)}";
                    return Spot.Create(SpotCode.Create($"{block.Key.ToUpperInvariant()}-{rowCode}-{Number(number)}"),
                        ZoneId.Create(Id(definition, "zone", block.Key)), RowCode.Create(rowCode),
                        SpotNumber.Create(number));
                }))).ToImmutableArray();

        var days = ImmutableArray.CreateBuilder<FestivalDay>();
        var supportingAttendees = ImmutableArray.CreateBuilder<Attendee>();
        var supportingRequests = ImmutableArray.CreateBuilder<SupportingCompletedRequest>();
        var supportingAssignments = ImmutableArray.CreateBuilder<Assignment>();
        var plans = ImmutableArray.CreateBuilder<DailyPlan>();
        int[] previousRequesters = [];
        var nextSequence = 0;

        foreach (var day in definition.Days.OrderBy(day => day.DayNumber))
        {
            var date = definition.FirstDate.AddDays(day.DayNumber - 1);
            var dayId = FestivalDayId.Create(Id(definition, "day", Number(day.DayNumber)));
            var arrival = definition.Workload?.ArrivalProfile;
            days.Add(FestivalDay.Create(dayId, date,
                AssignmentWindow.Create(arrival?[0].Start ?? new TimeOnly(9, 0),
                    arrival?[^1].End ?? new TimeOnly(18, 0))));
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

            var requests = definition.Workload is not null
                ? GenerateRequests(definition, day.DayNumber, date, attendees,
                    ref previousRequesters, ref nextSequence)
                : day.Requests.OrderBy(request => request.Sequence).Select((request, index) =>
                new DailyRequestPlan(Id(definition, "simulation-request", Number(request.Sequence)),
                    request.Sequence, request.AttendeeNumbers.Select(number => attendees[number - 1]).ToImmutableArray(),
                    businessTime.AddMinutes(index * 2), businessTime.AddMinutes(index * 2 + 1),
                    request.Eligibility)).ToImmutableArray();
            plans.Add(new DailyPlan(day.DayNumber, dayId, requests));
        }

        // v1 records the seed without sampling; v2 uses independent seeded daily streams.
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
        // Representative catalogs share exact attendee and Zone identities across S1/S2
        // and seeds, preserving the same venue tie-breaks. Workload IDs remain scenario-specific.
        var identityScope = definition.GeneratorVersion == "v2" ? entityType switch
        {
            "zone" => "representative-venue-v1",
            "attendee" => "representative-population-v1",
            _ => definition.ScenarioId
        } : definition.ScenarioId;
        var input = FormattableString.Invariant($"{identityScope.Length}:{identityScope}/{entityType}/{key}");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }

    private static string Number(int number) => number.ToString("D3", CultureInfo.InvariantCulture);

    private static ImmutableArray<DailyRequestPlan> GenerateRequests(ScenarioDefinition definition,
        int day, DateOnly date, ImmutableArray<Attendee> attendees,
        ref int[] previousRequesters, ref int nextSequence)
    {
        var workload = definition.Workload!;
        var participation = DeterministicRandom.For(definition, day, "participation");
        var registered = Enumerable.Range(1, definition.AttendeeCount).ToArray();
        int[] selected;
        if (day == 1)
        {
            participation.Shuffle(registered);
            selected = registered.Take(workload.DailyRequesterCount).ToArray();
        }
        else
        {
            var previousSet = previousRequesters.ToHashSet();
            var replacements = registered.Where(number => !previousSet.Contains(number)).ToArray();
            var retained = previousRequesters.ToArray();
            participation.Shuffle(retained);
            participation.Shuffle(replacements);
            var retainedCount = (int)decimal.Floor(workload.DailyRequesterCount * workload.DailyParticipationContinuity);
            selected = retained.Take(retainedCount).Concat(replacements.Take(
                workload.DailyRequesterCount - retainedCount)).ToArray();
        }
        var membership = DeterministicRandom.For(definition, day, "membership");
        membership.Shuffle(selected);
        previousRequesters = selected;

        var sizes = GroupDistributionGenerator.Generate(workload.DailyRequesterCount, workload.GroupSizeProfile,
            DeterministicRandom.For(definition, day, "groups"));
        var eligibleCount = PercentageAllocation.Allocate(sizes.Length,
            [workload.AllowsFrontStandingRequestShare, 1m - workload.AllowsFrontStandingRequestShare])[0];
        var eligibility = Enumerable.Range(0, sizes.Length).Select(index => index < eligibleCount).ToArray();
        DeterministicRandom.For(definition, day, "eligibility").Shuffle(eligibility);
        var requests = new List<DailyRequestPlan>(sizes.Length);
        var offset = 0;
        for (var index = 0; index < sizes.Length; index++)
        {
            requests.Add(new DailyRequestPlan(Id(definition, "simulation-request",
                    $"day-{Number(day)}/group-{Number(index + 1)}"), 0,
                selected.Skip(offset).Take(sizes[index]).Select(number => attendees[number - 1]).ToImmutableArray(),
                default, default, new RequestEligibility(eligibility[index])));
            offset += sizes[index];
        }
        var arrivals = ArrivalDistributionGenerator.Generate(requests, workload.ArrivalProfile, date,
            DeterministicRandom.For(definition, day, "arrivals"));
        var result = ImmutableArray.CreateBuilder<DailyRequestPlan>(arrivals.Length);
        foreach (var request in arrivals)
            result.Add(request with { Sequence = ++nextSequence });
        return result.ToImmutable();
    }

    private static void Validate(ScenarioDefinition definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.ScenarioId);
        if ((definition.GeneratorVersion != "v1" || definition.Workload is not null)
            && (definition.GeneratorVersion != "v2" || definition.Workload is null))
            throw new NotSupportedException("Use v1 for explicit plans or v2 for representative workloads.");
        if (definition.ExecutionMode != SimulationExecutionMode.Sequential || definition.ConcurrencyLevel != 1)
            throw new NotSupportedException("Only sequential execution with concurrency level 1 is supported.");
        if (definition.AttendeeCount < 1 || definition.Topology.IsDefaultOrEmpty || definition.Days.IsDefaultOrEmpty)
            throw new ArgumentException("A scenario requires evaluated attendees, topology and days.", nameof(definition));

        var keys = definition.Topology.Select(block => block.Key).ToArray();
        if (keys.Any(string.IsNullOrWhiteSpace) || keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != keys.Length
            || definition.Topology.Any(block => block.SpotCount < 1 || block.RowCount < 1 || block.GrowthPerRow < 0)
            || definition.Days.Any(day => day.DayNumber < 1 || day.OccupiedBlockKeys.IsDefault || day.Requests.IsDefault)
            || definition.Days.Select(day => day.DayNumber).Distinct().Count() != definition.Days.Length)
            throw new ArgumentException("Topology keys and day numbers must be valid and unique.", nameof(definition));

        if (definition.Workload is { } workload)
        {
            GroupDistributionGenerator.Validate(workload.GroupSizeProfile);
            ArrivalDistributionGenerator.Validate(workload.ArrivalProfile);
            if (workload.DailyRequesterCount < 1 || workload.DailyRequesterCount > definition.AttendeeCount
                || workload.DailyParticipationContinuity is < 0 or > 1
                || workload.AllowsFrontStandingRequestShare is < 0 or > 1)
                throw new ArgumentException("Workload populations and participation/eligibility ratios must be valid.", nameof(definition));
            var replacementCount = workload.DailyRequesterCount
                - (int)decimal.Floor(workload.DailyRequesterCount * workload.DailyParticipationContinuity);
            if (replacementCount > definition.AttendeeCount - workload.DailyRequesterCount
                || !definition.Days.Select(day => day.DayNumber).Order().SequenceEqual(
                    Enumerable.Range(1, definition.Days.Length))
                || definition.Days.Any(day => !day.Requests.IsEmpty || !day.OccupiedBlockKeys.IsEmpty))
                throw new ArgumentException("Representative days must be consecutive, with feasible replacements and no setup/explicit requests.", nameof(definition));
        }

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
