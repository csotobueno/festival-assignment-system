using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Festival.Domain.Assignments;
using Festival.Domain.Zones;
using Festival.Simulation;

// Task-specific execution/audit only: no runner, scheduler or persistence changes.
internal static class S8Runs
{
    private static readonly string[] Files = ["run-metadata", "definition", "manifest", "attempts",
        "checkpoints", "attendee-paths", "recovery-observations", "metrics"];
    private static readonly JsonSerializerOptions Json = Options();

    internal static async Task RunAsync(string[] args)
    {
        var smoke = args[0] == "--s8-smoke";
        if (smoke ? args.Length != 1 : args.Length != 2)
            throw new ArgumentException("Usage: --s8-smoke OR --s8 <successful-smoke-index.json>");
        var revision = Environment.GetEnvironmentVariable("FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA");
        if (string.IsNullOrWhiteSpace(revision)) throw new ArgumentException("Supply the known repository SHA explicitly.");
        var sourceHashes = SourceHashes();
        JsonNode? smokeProof = null;
        if (!smoke)
        {
            smokeProof = JsonNode.Parse(await File.ReadAllTextAsync(args[1]))!;
            if (smokeProof["role"]!.GetValue<string>() != "smoke" || smokeProof["status"]!.GetValue<string>() != "Passed"
                || !JsonNode.DeepEquals(smokeProof["sourceSha256"], JsonSerializer.SerializeToNode(sourceHashes, Json)))
                throw new InvalidOperationException("Official execution requires successful smoke over identical source/harness.");
            foreach (var entry in smokeProof["runs"]!.AsArray()) VerifyFiles(entry!);
        }

        var root = Path.Combine("artifacts", "simulations");
        var set = Path.Combine(root, $"s8-{(smoke ? "smoke" : "initial-concurrency")}-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(set);
        var entries = new List<object>();
        var sources = new Dictionary<int, (ScenarioDefinition Definition, ScenarioManifest Manifest)>();
        foreach (var seed in smoke ? new[] { 1001 } : new[] { 1001, 2001, 3001 })
        {
            var definition = S2ExpectedBaselineScenarioDefinition.Create(seed, new(2026, 8, 15));
            if (smoke) definition = definition with
            {
                AttendeeCount = 48,
                Workload = new RepresentativeWorkloadDefinition(40, .90m,
                    [new(2, 2, 1m)], [new(new(7, 0), new(23, 0), 1m)], .50m),
                Topology = [new("smoke-medium", ZoneType.MiddleLeft, 4)],
                Days = [new(1, [], [])]
            };
            var source = new ScenarioMaterializer().Materialize(definition);
            // Non-official negative control: a real invalid resolution timestamp
            // throws in the use case and verifies unexpected-failure capture.
            if (smoke)
            {
                var day = source.DailyPlans[0];
                var last = day.Requests[^1];
                source = source with { DailyPlans = [day with { Requests = day.Requests.SetItem(day.Requests.Length - 1,
                    last with { AssignedAt = last.RequestedAt.AddTicks(-1) }) }] };
            }
            sources.Add(seed, (definition, source));
        }
        var indexPath = Path.Combine(set, "index.json");
        async Task SaveAsync(string status) => await File.WriteAllTextAsync(indexPath,
            JsonSerializer.Serialize(new
            {
                role = smoke ? "smoke" : "official", status, repositoryCommitSha = revision,
                sourceSha256 = sourceHashes, smokeIndex = smoke ? null : args[1],
                smokeIndexSha256 = smoke ? null : Hash(await File.ReadAllBytesAsync(args[1])),
                retryCount = 0, warmup = "None; first use-case calls remain in measured samples",
                buildConfiguration = "Debug", dockerApiVersion = Environment.GetEnvironmentVariable("DOCKER_API_VERSION"),
                runs = entries
            }, Json) + "\n");
        await SaveAsync("Running");
        Console.WriteLine($"SET {indexPath}");
        foreach (var level in new[] { 4, 16 })
        foreach (var seed in sources.Keys)
        {
            var (definition, source) = sources[seed];
            var manifest = S8ConcurrentExecution.Create(source, level, 1);
            if (!ReferenceEquals(source.Catalog, manifest.Catalog) || !source.DailyPlans.Equals(manifest.DailyPlans)
                || !ReferenceEquals(source.Setup, manifest.Setup)) throw new InvalidOperationException("Workload changed.");
            var runId = $"{(smoke ? "smoke" : "official")}-s8-c{level}-{seed}-{Guid.NewGuid():N}";
            Console.WriteLine($"START {runId} {DateTimeOffset.UtcNow:O}");
            var startedAt = DateTimeOffset.UtcNow;
            var result = await MinimalSimulationRunner.RunAsync(manifest);
            var metadata = SimulationRunMetadata.Create(runId, manifest, startedAt, DateTimeOffset.UtcNow, revision);
            var destination = await new SimulationEvidenceExporter().ExportAsync(new(definition, manifest, result, metadata, root));
            var audit = Audit(manifest, result);
            var outcomes = result.Attempts.GroupBy(a => a.Outcome.ToString()).ToDictionary(g => g.Key, g => g.Count());
            var hashes = Files.ToDictionary(name => name + ".json", name => Hash(File.ReadAllBytes(Path.Combine(destination, name + ".json"))));
            entries.Add(new
            {
                scenario = manifest.Metadata.ScenarioId, seed, concurrencyLevel = level, repetition = 1, runId,
                evidencePath = destination, metadata, sourceManifestSha256 = Hash(JsonSerializer.SerializeToUtf8Bytes(source, Json)),
                exactSourceCatalogSetupDailyPlans = true,
                integrity = new { invariantViolationCount = audit.Count, violations = audit, finalStateOnly = true,
                    persistedRequestCount = result.Requests.Count, persistedAssignmentCount = result.Assignments.Count,
                    completedFestivalDays = result.DailyCheckpoints.Count, sha256 = hashes }, outcomes
            });
            await SaveAsync("Running");
            Console.WriteLine($"EXPORTED {runId} outcomes={JsonSerializer.Serialize(outcomes)} violations={audit.Count} lifecycle={metadata.CompletedAt - startedAt}");
            if (audit.Count != 0)
            {
                await SaveAsync("CriticalInvariantFinding");
                throw new InvalidOperationException("Critical persisted/execution audit finding; evidence retained; official execution stopped.");
            }
            if (smoke && (!outcomes.ContainsKey("KnownConcurrencyConflict")
                    || outcomes.GetValueOrDefault("UnexpectedTechnicalFailure") != 1
                    || result.Attempts[^1].Outcome != SimulationAttemptOutcome.UnexpectedTechnicalFailure))
            {
                await SaveAsync("SmokeFailed");
                throw new InvalidOperationException("Smoke did not exercise conflict and negative-control failure; official execution blocked.");
            }
            if (!SourceHashes().SequenceEqual(sourceHashes)) throw new InvalidOperationException("Source changed during execution.");
        }
        await SaveAsync(smoke ? "Passed" : "Completed");
        Console.WriteLine($"COMPLETE {indexPath}");
    }

    private static List<string> Audit(ScenarioManifest manifest, SimulationResult result)
    {
        var findings = new List<string>();
        void Check(bool valid, string message) { if (!valid) findings.Add(message); }
        Check(result.Attempts.Count == manifest.DailyPlans.Sum(d => d.Requests.Length), "Attempt coverage");
        Check(result.Attempts.Select(a => a.SimulationRequestId).Distinct().Count() == result.Attempts.Count, "Duplicate attempt / retry");
        Check(result.Assignments.GroupBy(a => (a.FestivalDayId, a.SpotCode)).All(g => g.Count() == 1), "INV-01 Spot/day");
        Check(result.Assignments.GroupBy(a => (a.FestivalDayId, a.AttendeeId)).All(g => g.Count() == 1), "INV-02 Attendee/day");
        var assignments = result.Assignments.ToLookup(a => a.AssignmentRequestId.Value);
        var requests = result.Requests.ToDictionary(r => r.AssignmentRequestId);
        var byPersistedId = result.Attempts.Where(a => a.PersistedRequestId.HasValue)
            .ToDictionary(a => a.PersistedRequestId!.Value.Value);
        var spots = manifest.Catalog.Spots.ToDictionary(s => s.Code);
        Check(result.Assignments.All(a => spots.TryGetValue(a.SpotCode, out var s) && s.ZoneId == a.ZoneId
            && s.RowCode == a.RowCode && s.Number == a.SpotNumber && requests.ContainsKey(a.AssignmentRequestId.Value)), "INV-07 catalog/request identity");
        Check(result.Requests.Count == byPersistedId.Count && result.Requests.All(r => byPersistedId.ContainsKey(r.AssignmentRequestId)),
            "Orphan/fake persisted request");
        foreach (var day in manifest.DailyPlans)
        {
            var plans = day.Requests.OrderBy(r => r.Sequence).ToArray();
            var attempts = result.Attempts.Where(a => a.FestivalDayId == day.FestivalDayId).OrderBy(a => a.BaselineSequence).ToArray();
            Check(attempts.Select(a => a.SimulationRequestId).SequenceEqual(plans.Select(p => p.SimulationRequestId)), "Daily exact request order/identity");
            for (var i = 0; i < attempts.Length; i++)
            {
                var a = attempts[i];
                var p = plans[i];
                Check(a.BaselineSequence == p.Sequence && a.AttemptSequence == p.Sequence && a.WindowNumber == i / manifest.Metadata.ConcurrencyLevel + 1
                    && a.WindowPosition == i % manifest.Metadata.ConcurrencyLevel + 1 && a.AttendeeIds.SequenceEqual(p.Members.Select(m => m.Id)), "Window/sequence/membership provenance");
                Check(a.ProcessingStartedOffset.HasValue && a.Duration >= TimeSpan.Zero, "Missing monotonic timing");
                if (a.DatabaseSqlState == "40P01") Check(a.Outcome == SimulationAttemptOutcome.KnownConcurrencyConflict
                    && a.DiagnosticCode == "DeadlockDetected", "40P01 classification");
                if (a.Outcome is SimulationAttemptOutcome.KnownConcurrencyConflict or SimulationAttemptOutcome.UnexpectedTechnicalFailure)
                {
                    Check(a.PersistedRequestId is null && a.SelectedExperienceQuality is null, "Failed/conflict attempt claims persisted result");
                    Check(!result.Assignments.Any(row => row.FestivalDayId == day.FestivalDayId && a.AttendeeIds.Contains(row.AttendeeId)), "Conflict/failure loser partial graph");
                    continue;
                }
                if (a.PersistedRequestId is not { } id || !requests.TryGetValue(id.Value, out var request)) { findings.Add("INV-08 missing persisted result"); continue; }
                Check(request.FestivalDayId == day.FestivalDayId.Value && (request.RequestedAt - p.RequestedAt).Duration() < TimeSpan.FromMicroseconds(1), "Persisted request identity/time");
                var rows = assignments[id.Value].ToArray();
                if (a.Outcome == SimulationAttemptOutcome.DomainRejected) Check(request.Status == "Rejected" && rows.Length == 0, "INV-08 rejected graph");
                else
                {
                    Check(a.Outcome == SimulationAttemptOutcome.Successful && request.Status == "Completed", "INV-08 completed status");
                    try { AssignmentGroup.Create(id, day.FestivalDayId, p.Members.Select(m => m.Id)).EnsureValidResult(rows); }
                    catch (ArgumentException error) { findings.Add("INV-03/04/05/07/08 " + error.Message); }
                    Check(rows.All(row => row.AssignedAt == p.AssignedAt || (row.AssignedAt - p.AssignedAt).Duration() < TimeSpan.FromMicroseconds(1)), "AssignedAt mismatch");
                }
            }
            var windows = attempts.GroupBy(a => a.WindowNumber).ToArray();
            for (var i = 1; i < windows.Length; i++)
                Check(windows[i].Min(a => a.ProcessingStartedOffset!.Value)
                    >= windows[i - 1].Max(a => a.ProcessingStartedOffset!.Value + a.Duration), "Overlapping windows");
            var checkpoint = result.DailyCheckpoints.Single(c => c.FestivalDayId == day.FestivalDayId);
            var opening = result.OpeningStates.Single(s => s.FestivalDayId == day.FestivalDayId);
            Check(opening.AvailableSpots.Count == manifest.Catalog.Spots.Length, "Opening inventory changed");
            Check(checkpoint.EvaluatedRequestCount == attempts.Length && checkpoint.TotalAvailableSpotCount
                == manifest.Catalog.Spots.Length - result.Assignments.Count(a => a.FestivalDayId == day.FestivalDayId), "Checkpoint reconciliation");
        }
        Check(result.AttendeePaths.Sum(p => p.PathLength) == result.Assignments.Count, "Persisted path coverage");
        return findings;
    }

    private static void VerifyFiles(JsonNode entry)
    {
        foreach (var hash in entry["integrity"]!["sha256"]!.AsObject())
            if (Hash(File.ReadAllBytes(Path.Combine(entry["evidencePath"]!.GetValue<string>(), hash.Key))) != hash.Value!.GetValue<string>())
                throw new InvalidOperationException("Smoke evidence checksum mismatch.");
    }
    private static SortedDictionary<string, string> SourceHashes() => new(Directory.EnumerateFiles("src", "*", SearchOption.AllDirectories)
        .Where(p => !p.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj") && (p.EndsWith(".cs") || p.EndsWith(".csproj")))
        .Concat(new[] { "scripts/stage-5-representative-runs/Program.cs", "scripts/stage-5-representative-runs/S8Runs.cs",
            "scripts/stage-5-representative-runs/RepresentativeRuns.csproj" })
        .ToDictionary(p => p, p => Hash(File.ReadAllBytes(p))), StringComparer.Ordinal);
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static JsonSerializerOptions Options()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new EvidenceTimestampConverter());
        return options;
    }
}
