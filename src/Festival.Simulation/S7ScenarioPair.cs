using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Simulation;

// Both variants share the exact S2 materialization, not just its Seed/profile.
public sealed class S7ScenarioPair
{
    public const string ControlScenarioId = "s7-control";
    public const string FragmentedScenarioId = "s7-fragmented";
    public const string WorkloadSourceScenarioId = "s2-expected-baseline";
    public const string PatternVersion = "s7-preoccupation-v1";
    public const int OpeningOccupiedSpotCount = 1000;

    public ScenarioDefinition SourceDefinition { get; }
    public ScenarioManifest SourceManifest { get; }
    public ScenarioManifest Control { get; }
    public ScenarioManifest Fragmented { get; }

    private S7ScenarioPair(ScenarioDefinition definition, ScenarioManifest source,
        ScenarioManifest control, ScenarioManifest fragmented)
    {
        SourceDefinition = definition;
        SourceManifest = source;
        Control = control;
        Fragmented = fragmented;
    }

    public static S7ScenarioPair Create(int seed, DateOnly firstDate)
    {
        var definition = S2ExpectedBaselineScenarioDefinition.Create(seed, firstDate);
        var source = new ScenarioMaterializer().Materialize(definition);
        if (source.Catalog.Spots.Length != 4998)
            throw new InvalidOperationException("S7 v1 requires the contracted 4,998-Spot topology.");

        var zones = source.Catalog.Zones.OrderBy(zone => zone.Type.ToString(), StringComparer.Ordinal).ToArray();
        var zoneSpots = zones.Select(zone => source.Catalog.Spots.Where(spot => spot.ZoneId == zone.Id).ToArray()).ToArray();
        var zoneQuotas = AllocateQuotas(zoneSpots.Select(spots => spots.Length).ToArray(),
            OpeningOccupiedSpotCount);
        var controlSpots = new List<Spot>();
        var fragmentedSpots = new List<Spot>();
        for (var z = 0; z < zones.Length; z++)
        {
            var rows = zoneSpots[z].GroupBy(spot => spot.RowCode)
                .OrderBy(row => row.Key.Value, StringComparer.Ordinal)
                .Select(row => row.OrderBy(spot => spot.Number.Value).ToArray()).ToArray();
            var rowQuotas = AllocateQuotas(rows.Select(row => row.Length).ToArray(), zoneQuotas[z]);
            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var row = rows[rowIndex];
                if (row.Zip(row.Skip(1)).Any(pair => pair.Second.Number.Value != pair.First.Number.Value + 1))
                    throw new InvalidOperationException("S7 v1 requires consecutive SpotNumbers within each Row.");
                var occupied = rowQuotas[rowIndex];
                if (occupied <= 0 || occupied >= row.Length)
                    throw new InvalidOperationException("S7 v1 requires partially occupied Rows.");
                controlSpots.AddRange(row.Take(occupied));

                // Balance k+1 available gaps; longer gaps come first. Exactly k
                // occupied positions separate them, including both edge gaps.
                var available = row.Length - occupied;
                var gap = available / (occupied + 1);
                var longerGaps = available % (occupied + 1);
                var position = 0;
                for (var index = 0; index < occupied; index++)
                {
                    position += gap + (index < longerGaps ? 1 : 0);
                    fragmentedSpots.Add(row[position++]);
                }
            }
        }

        var sourceHash = Convert.ToHexString(SHA256.HashData(
            JsonSerializer.SerializeToUtf8Bytes(source, SimulationEvidenceExporter.JsonOptions))).ToLowerInvariant();
        return new S7ScenarioPair(definition, source,
            CreateVariant(ControlScenarioId, controlSpots), CreateVariant(FragmentedScenarioId, fragmentedSpots));

        ScenarioManifest CreateVariant(string scenarioId, IReadOnlyList<Spot> occupiedSpots) => source with
        {
            Metadata = source.Metadata with
            {
                ScenarioId = scenarioId,
                WorkloadSourceScenarioId = WorkloadSourceScenarioId,
                FragmentationPatternVersion = PatternVersion,
                OpeningOccupiedSpotCount = OpeningOccupiedSpotCount,
                WorkloadSourceManifestSha256 = sourceHash
            },
            Setup = CreateSetup(source, occupiedSpots)
        };
    }

    // 20% is provisional, not tunable here. Integer division gives floor(n/5);
    // largest remainders get one extra. Input order supplies the ordinal tie-break:
    // ZoneType name first, then RowCode within each Zone.
    private static int[] AllocateQuotas(int[] capacities, int total)
    {
        var quotas = capacities.Select(capacity => capacity / 5).ToArray();
        var remaining = total - quotas.Sum();
        if (remaining < 0 || remaining > capacities.Length)
            throw new InvalidOperationException("S7 quotas no longer fit the contracted topology.");
        foreach (var index in Enumerable.Range(0, capacities.Length)
                     .OrderByDescending(index => capacities[index] % 5).ThenBy(index => index).Take(remaining))
            quotas[index]++;
        return quotas;
    }

    private static ScenarioSetup CreateSetup(ScenarioManifest source, IReadOnlyList<Spot> occupiedSpots)
    {
        var attendees = ImmutableArray.CreateBuilder<Attendee>();
        var requests = ImmutableArray.CreateBuilder<SupportingCompletedRequest>();
        var assignments = ImmutableArray.CreateBuilder<Assignment>();
        var zones = source.Catalog.Zones.ToDictionary(zone => zone.Id);
        foreach (var day in source.DailyPlans)
        {
            var festivalDay = source.Catalog.FestivalDays.Single(value => value.Id == day.FestivalDayId);
            var setupTime = new DateTimeOffset(festivalDay.Date.ToDateTime(festivalDay.AssignmentWindow.Start), TimeSpan.Zero);
            for (var index = 0; index < occupiedSpots.Count; index++)
            {
                var spot = occupiedSpots[index];
                var key = FormattableString.Invariant($"{day.FestivalDayId.Value:N}/{index + 1:D4}");
                // Identity is day/slot based and shared between variants. Only the
                // assigned Spot changes; no supporting identity ever joins DailyPlans.
                var attendee = Attendee.Create(AttendeeId.Create(Id("attendee", key)),
                    AttendeeCode.Create(FormattableString.Invariant($"S7-OCC-D{day.DayNumber:D3}-{index + 1:D4}")),
                    FormattableString.Invariant($"S7 setup day {day.DayNumber} occupant {index + 1}"));
                var requestId = AssignmentRequestId.Create(Id("request", key));
                var eligibility = new RequestEligibility(true);
                var request = AssignmentRequest.Create(requestId, day.FestivalDayId, [attendee.Code], setupTime, eligibility);
                request.Complete(setupTime);
                var assignment = Assignment.Create(AssignmentId.Create(Id("assignment", key)), requestId,
                    day.FestivalDayId, attendee.Id, spot.Code, spot.ZoneId, spot.RowCode, spot.Number,
                    ZoneExperienceQualityPolicy.GetQuality(zones[spot.ZoneId].Type), setupTime);
                AssignmentGroup.Create(requestId, day.FestivalDayId, [attendee.Id]).EnsureValidResult([assignment]);
                attendees.Add(attendee);
                requests.Add(new SupportingCompletedRequest(requestId, day.FestivalDayId, [attendee.Code],
                    setupTime, setupTime, eligibility));
                assignments.Add(assignment);
            }
        }
        return new ScenarioSetup(attendees.ToImmutable(), requests.ToImmutable(), assignments.ToImmutable());
    }

    private static Guid Id(string entity, string key)
    {
        var input = FormattableString.Invariant($"{PatternVersion.Length}:{PatternVersion}/{entity}/{key}");
        return new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(input)).AsSpan(0, 16), bigEndian: true);
    }
}
