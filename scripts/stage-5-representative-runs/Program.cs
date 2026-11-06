using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Festival.Domain.Assignments;
using Festival.Simulation;

// Task-specific protocol. Definitions, runner and eight-file exporter are unchanged.
var firstDate = new DateOnly(2026, 8, 15);
if (args.Length > 1 || (args.Length == 1 && args[0] != "--s5"))
    throw new ArgumentException("Usage: RepresentativeRuns [--s5]");
var s5Protocol = args.Length == 1;
var controls = new Dictionary<int, (ScenarioDefinition Definition, ScenarioManifest Manifest, JsonObject Entry)>();
if (s5Protocol)
{
    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    options.Converters.Add(new JsonStringEnumConverter());
    options.Converters.Add(new EvidenceTimestampConverter());
    var official = JsonNode.Parse(await File.ReadAllTextAsync("docs/stage-5/s1-s2-representative-results.json"))!;
    foreach (var seed in new[] { 1001, 2001, 3001 })
    {
        var entry = official["officialRuns"]!.AsArray().Single(r =>
            r!["scenario"]!.GetValue<string>() == "s2" && r["seed"]!.GetValue<int>() == seed)!.AsObject();
        var path = entry["evidencePath"]!.GetValue<string>();
        foreach (var hash in entry["integrity"]!["sha256"]!.AsObject())
        {
            var bytes = await File.ReadAllBytesAsync(Path.Combine(path, hash.Key));
            using var parsed = JsonDocument.Parse(bytes);
            if (Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() != hash.Value!.GetValue<string>())
                throw new InvalidOperationException($"S2/{seed}: checksum mismatch in {hash.Key}");
        }
        var definition = S2ExpectedBaselineScenarioDefinition.Create(seed, firstDate);
        var baseline = new ScenarioMaterializer().Materialize(definition);
        var generated = JsonSerializer.SerializeToNode(baseline, options)!;
        // Retained controls predate additive sequence/provenance fields. Remove
        // only their baseline defaults before structural equality; never write controls.
        generated["metadata"]!.AsObject().Remove("workloadSourceScenarioId");
        generated["metadata"]!.AsObject().Remove("orderingTransformation");
        foreach (var day in generated["dailyPlans"]!.AsArray())
        foreach (var request in day!["requests"]!.AsArray())
            request!.AsObject().Remove("executionSequence");
        var retained = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(path, "manifest.json")));
        var retainedDefinition = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(path, "definition.json")));
        if (!JsonNode.DeepEquals(generated, retained)
            || !JsonNode.DeepEquals(JsonSerializer.SerializeToNode(definition, options), retainedDefinition))
            throw new InvalidOperationException($"S2/{seed}: materialized workload differs from retained control; stop.");
        controls.Add(seed, (definition, baseline, entry));
        Console.WriteLine($"PAIRED INPUT VERIFIED S2/{seed} {path}");
    }
}
var setId = $"{(s5Protocol ? "s5-order-sensitivity" : "representative")}-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}";
var root = Path.Combine("artifacts", "simulations");
var setDirectory = Path.Combine(root, setId);
Directory.CreateDirectory(setDirectory);
var json = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
var index = new List<object>();
Console.WriteLine($"SET {setDirectory}");

foreach (var scenario in s5Protocol ? new[] { "s5" } : new[] { "s1", "s2" })
foreach (var seed in new[] { 1001, 2001, 3001 })
    await Execute(scenario, seed, "official");
await Execute(s5Protocol ? "s5" : "s1", 1001, "reproduction");
Console.WriteLine($"COMPLETE {setDirectory}/index.json");

async Task Execute(string scenario, int seed, string role)
{
    var definition = scenario == "s5" ? controls[seed].Definition : scenario == "s1"
        ? S1BestPlausibleScenarioDefinition.Create(seed, firstDate)
        : S2ExpectedBaselineScenarioDefinition.Create(seed, firstDate);
    var manifest = scenario == "s5" ? S5ReversedProcessingOrder.Create(controls[seed].Manifest)
        : new ScenarioMaterializer().Materialize(definition);
    var runId = $"{role}-{scenario}-{seed}-{Guid.NewGuid():N}";
    Console.WriteLine($"START {scenario} seed={seed} role={role} run={runId} at={DateTimeOffset.UtcNow:O}");
    var startedAt = DateTimeOffset.UtcNow;
    var result = await MinimalSimulationRunner.RunAsync(manifest);
    var metadata = SimulationRunMetadata.Create(runId, manifest, startedAt, DateTimeOffset.UtcNow,
        Environment.GetEnvironmentVariable("FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA"));
    var destination = await new SimulationEvidenceExporter().ExportAsync(new(definition, manifest, result, metadata, root));
    var violations = Audit(manifest, result, definition.Workload!.DailyRequesterCount);
    var hashes = new Dictionary<string, string>();
    foreach (var name in new[] { "run-metadata", "definition", "manifest", "attempts", "checkpoints",
                 "attendee-paths", "recovery-observations", "metrics" })
    {
        var file = Path.Combine(destination, name + ".json");
        await using var stream = File.OpenRead(file);
        using var parsed = await JsonDocument.ParseAsync(stream);
        stream.Position = 0;
        hashes.Add(name + ".json", Convert.ToHexString(await SHA256.HashDataAsync(stream)).ToLowerInvariant());
    }
    var failures = result.Attempts.Count(a => a.Outcome == SimulationAttemptOutcome.UnexpectedTechnicalFailure);
    var conflicts = result.Attempts.Count(a => a.Outcome == SimulationAttemptOutcome.KnownConcurrencyConflict);
    index.Add(new
    {
        role, scenario, seed, runId, evidencePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), destination),
        metadata, pairedControl = scenario == "s5" ? controls[seed].Entry.DeepClone() : null,
        status = violations.Count == 0 && failures == 0 && conflicts == 0 ? "Completed" : "CompletedWithFindings",
        integrity = new
        {
            completedFestivalDays = result.DailyCheckpoints.Count,
            unexpectedTechnicalFailureCount = failures, knownConcurrencyConflictCount = conflicts,
            persistedAssignmentCount = result.Assignments.Count, persistedRequestCount = result.Requests.Count,
            // Final durable state: uniqueness, group completeness/contiguity, identity and outcome consistency.
            violations, invariantViolationCount = violations.Count,
            finalStateOnly = true, sha256 = hashes
        }
    });
    await File.WriteAllTextAsync(Path.Combine(setDirectory, "index.json"), JsonSerializer.Serialize(index, json) + "\n");
    Console.WriteLine($"EXPORTED {destination} attempts={result.Attempts.Count} failures={failures} conflicts={conflicts} violations={violations.Count} elapsed={metadata.CompletedAt - startedAt}");
    if (violations.Count != 0)
        throw new InvalidOperationException("Run integrity findings preserved in index.json; execution stopped.");
}

static List<string> Audit(ScenarioManifest manifest, SimulationResult result, int dailyRequesterCount)
{
    var violations = new List<string>();
    void Check(bool valid, string description) { if (!valid) violations.Add(description); }
    Check(manifest.Metadata.GeneratorVersion == "v2" && manifest.Metadata.ExecutionMode == SimulationExecutionMode.Sequential
        && manifest.Metadata.ConcurrencyLevel == 1, "Unexpected generator/execution mode.");
    Check(manifest.DailyPlans.Length == 5 && result.DailyCheckpoints.Count == 5, "Expected five completed FestivalDays.");
    Check(result.Assignments.GroupBy(a => (a.FestivalDayId, a.SpotCode)).All(g => g.Count() == 1), "INV-01: duplicate Spot/day.");
    Check(result.Assignments.GroupBy(a => (a.FestivalDayId, a.AttendeeId)).All(g => g.Count() == 1), "INV-02/06: duplicate Attendee/day in final state.");
    var spots = manifest.Catalog.Spots.ToDictionary(s => s.Code);
    var attendees = manifest.Catalog.Attendees.Select(a => a.Id).ToHashSet();
    var days = manifest.Catalog.FestivalDays.Select(d => d.Id).ToHashSet();
    var requests = result.Requests.ToDictionary(r => r.AssignmentRequestId);
    var assignments = result.Assignments.ToLookup(a => a.AssignmentRequestId.Value);
    Check(result.Assignments.All(a => a.Id != default && requests.ContainsKey(a.AssignmentRequestId.Value)
        && attendees.Contains(a.AttendeeId) && days.Contains(a.FestivalDayId)
        && spots.TryGetValue(a.SpotCode, out var spot) && spot.ZoneId == a.ZoneId
        && spot.RowCode == a.RowCode && spot.Number == a.SpotNumber), "INV-07: incomplete or inconsistent catalog identity.");
    Check(result.Requests.Count == result.Attempts.Count, "Persisted request count differs from attempts (inspect failures/conflicts).");
    foreach (var day in manifest.DailyPlans)
    {
        var members = day.Requests.SelectMany(r => r.Members).ToArray();
        Check(members.Length == dailyRequesterCount && members.Select(m => m.Id).Distinct().Count() == dailyRequesterCount,
            $"Day {day.DayNumber}: incorrect or duplicate requester membership.");
        Check(day.Requests.Count(r => r.Eligibility.AllowsFrontStanding) == (day.Requests.Length + 1) / 2,
            $"Day {day.DayNumber}: eligibility allocation differs from 50% largest-remainder convention.");
        var dailyAttempts = result.Attempts.Where(a => a.FestivalDayId == day.FestivalDayId).ToArray();
        Check(dailyAttempts.OrderBy(a => a.AttemptSequence).Select(a => a.SimulationRequestId)
                .SequenceEqual(day.Requests.OrderBy(r => r.ExecutionSequence).Select(r => r.SimulationRequestId)),
            $"Day {day.DayNumber}: attempt sequence/membership differs from manifest.");
        var plannedById = day.Requests.ToDictionary(r => r.SimulationRequestId);
        Check(dailyAttempts.All(a => plannedById.TryGetValue(a.SimulationRequestId, out var plan)
            && a.AttemptSequence == plan.ExecutionSequence && a.BaselineSequence == plan.Sequence),
            $"Day {day.DayNumber}: baseline/execution sequence evidence differs from dispatch plan.");
        var checkpoint = result.DailyCheckpoints.SingleOrDefault(c => c.FestivalDayId == day.FestivalDayId);
        Check(checkpoint is not null && checkpoint.EvaluatedRequestCount == day.Requests.Length
            && checkpoint.TotalAvailableSpotCount == manifest.Catalog.Spots.Length
                - result.Assignments.Count(a => a.FestivalDayId == day.FestivalDayId),
            $"Day {day.DayNumber}: checkpoint does not reconcile with durable Assignments.");
    }
    var plans = manifest.DailyPlans.SelectMany(d => d.Requests).ToDictionary(r => r.SimulationRequestId);
    foreach (var attempt in result.Attempts)
    {
        if (attempt.PersistedRequestId is not { } id) continue;
        var rows = assignments[id.Value].ToArray();
        Check(requests.TryGetValue(id.Value, out var request) && request.FestivalDayId == attempt.FestivalDayId.Value,
            $"INV-08: missing/wrong-day request {id.Value}.");
        if (request is null) continue;
        if (attempt.Outcome == SimulationAttemptOutcome.Successful)
        {
            Check(request.Status == "Completed", $"INV-08: successful request {id.Value} is not Completed.");
            try
            {
                AssignmentGroup.Create(id, attempt.FestivalDayId,
                    plans[attempt.SimulationRequestId].Members.Select(m => m.Id)).EnsureValidResult(rows);
            }
            catch (ArgumentException e) { violations.Add($"INV-03/04/05/07/08 request {id.Value}: {e.Message}"); }
        }
        else if (attempt.Outcome == SimulationAttemptOutcome.DomainRejected)
            Check(request.Status == "Rejected" && rows.Length == 0, $"INV-08: rejected request {id.Value} has inconsistent state.");
    }
    return violations;
}

// Match the existing exporter's exact UTC representation for input comparison.
sealed class EvidenceTimestampConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        reader.GetDateTimeOffset();
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
}
