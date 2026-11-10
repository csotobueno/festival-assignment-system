using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Festival.Simulation;

internal static class ArrivalRuns
{
    private static readonly string[] Files = ["run-metadata", "definition", "manifest", "attempts",
        "checkpoints", "attendee-paths", "recovery-observations", "metrics"];
    private static readonly JsonSerializerOptions Json = Options();

    internal static async Task RunAsync(string[] args)
    {
        var additional = args[0] == "--arrival-add";
        var resume = args[0] == "--arrival-resume";
        if (additional ? args.Length != 4 : resume ? args.Length != 3 : args.Length != 1)
            throw new ArgumentException("Usage: --arrival OR --arrival-add <index> <pattern> <reason-file> OR --arrival-resume <stopped-index> <remediation-file>");
        var revision = Environment.GetEnvironmentVariable("FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA");
        if (string.IsNullOrWhiteSpace(revision)) throw new ArgumentException("Supply the known repository SHA explicitly.");
        var hashes = SourceHashes();
        var reference = JsonNode.Parse(await File.ReadAllTextAsync("docs/stage-5/s8-concurrency-analysis.json"))!;
        foreach (var pair in hashes.Where(p => p.Key.StartsWith("src/", StringComparison.Ordinal)
            && !p.Key.StartsWith("src/Festival.Simulation/", StringComparison.Ordinal)))
            if (reference["sourceSha256"]![pair.Key]?.GetValue<string>() != pair.Value)
                throw new InvalidOperationException("Production source differs from retained S8: " + pair.Key);
        var entries = new JsonArray();
        var excludedExecutions = new JsonArray();
        JsonNode? preservedSourceHashes = null;
        string? reason = null;
        var matrix = new List<(ArrivalPattern Pattern, int Seed)>();
        if (resume)
        {
            var prior = JsonNode.Parse(await File.ReadAllTextAsync(args[1]))!;
            reason = await File.ReadAllTextAsync(args[2]);
            var priorRuns = prior["runs"]!.AsArray();
            if (prior["status"]!.GetValue<string>() != "ExecutionStopped" || priorRuns.Count != 3
                || string.IsNullOrWhiteSpace(reason) || priorRuns[2]!["pattern"]!.GetValue<string>() != "BurstLike"
                || priorRuns[2]!["seed"]!.GetValue<int>() != 1001)
                throw new InvalidOperationException("Resume requires the documented three-run observer-audit stop.");
            // The scheduler, runner and production path must be byte-identical.
            // Only a new read-only final-state resolver is added to Simulation.
            foreach (var pair in prior["sourceSha256"]!.AsObject().Where(p => p.Key.StartsWith("src/", StringComparison.Ordinal)))
                if (hashes.GetValueOrDefault(pair.Key) != pair.Value!.GetValue<string>())
                    throw new InvalidOperationException("Execution source changed during observer-audit correction: " + pair.Key);
            preservedSourceHashes = prior["sourceSha256"]!.DeepClone();
            foreach (var entry in priorRuns) VerifyFiles(entry!);
            foreach (var entry in priorRuns.Take(2)) entries.Add(entry!.DeepClone());
            excludedExecutions.Add(new JsonObject { ["entry"] = priorRuns[2]!.DeepClone(),
                ["disposition"] = "Retained observer-audit stop; excluded from valid comparison; original three findings unchanged" });
            matrix.Add((ArrivalPattern.BurstLike, 1001));
            matrix.Add((ArrivalPattern.BurstLike, 2001));
        }
        else if (additional)
        {
            var prior = JsonNode.Parse(await File.ReadAllTextAsync(args[1]))!;
            var pattern = Enum.Parse<ArrivalPattern>(args[2]);
            reason = await File.ReadAllTextAsync(args[3]);
            if (prior["role"]!.GetValue<string>() != "arrival-concurrency" || prior["status"]!.GetValue<string>() != "Completed"
                || prior["runs"]!.AsArray().Count is < 4 or > 5 || string.IsNullOrWhiteSpace(reason)
                || !JsonNode.DeepEquals(prior["sourceSha256"], JsonSerializer.SerializeToNode(hashes, Json)))
                throw new InvalidOperationException("Expansion requires a completed current-source matrix and documented reason.");
            foreach (var entry in prior["runs"]!.AsArray())
            {
                VerifyFiles(entry!);
                entries.Add(entry!.DeepClone());
            }
            if (prior["excludedExecutions"] is JsonArray excluded)
                foreach (var entry in excluded) excludedExecutions.Add(entry!.DeepClone());
            if (entries.Count + excludedExecutions.Count >= 6)
                throw new InvalidOperationException("Actual official execution cap reached; no additional run authorized.");
            preservedSourceHashes = prior["preservedRunSourceSha256"]?.DeepClone();
            if (entries.Any(e => e!["pattern"]!.GetValue<string>() == pattern.ToString() && e["seed"]!.GetValue<int>() == 3001))
                throw new InvalidOperationException("Seed 3001 already executed for this pattern.");
            matrix.Add((pattern, 3001));
        }
        else
            foreach (var pattern in Enum.GetValues<ArrivalPattern>())
            foreach (var seed in new[] { 1001, 2001 }) matrix.Add((pattern, seed));

        var sources = new Dictionary<int, (ScenarioDefinition Definition, ScenarioManifest Manifest)>();
        foreach (var seed in matrix.Select(m => m.Seed).Distinct())
        {
            var definition = S2ExpectedBaselineScenarioDefinition.Create(seed, new(2026, 8, 15));
            var source = new ScenarioMaterializer().Materialize(definition);
            var retained = reference["runs"]!.AsArray().First(e => e!["seed"]!.GetValue<int>() == seed
                && e["concurrencyLevel"]!.GetValue<int>() == 1)!;
            VerifyFiles(retained);
            var priorRoot = retained["evidencePath"]!.GetValue<string>();
            var oldManifest = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(priorRoot, "manifest.json")))!;
            var current = JsonSerializer.SerializeToNode(source, Json)!;
            if (!JsonNode.DeepEquals(JsonSerializer.SerializeToNode(definition, Json),
                    JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(priorRoot, "definition.json"))))
                || new[] { "catalog", "setup", "dailyPlans" }.Any(k => !JsonNode.DeepEquals(current[k], oldManifest[k])))
                throw new InvalidOperationException("Business input differs from retained S2/C1: " + seed);
            sources.Add(seed, (definition, source));
            Console.WriteLine($"INPUT VERIFIED S2/{seed}: exact retained definition/catalog/setup/daily plans");
        }

        var root = Path.Combine("artifacts", "simulations");
        var set = Path.Combine(root, $"arrival-concurrency-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(set);
        var indexPath = Path.Combine(set, "index.json");
        object? failure = null;
        async Task SaveAsync(string status) => await File.WriteAllTextAsync(indexPath, JsonSerializer.Serialize(new
        {
            role = "arrival-concurrency", status, repositoryCommitSha = revision, sourceSha256 = hashes,
            retryCount = 0, warmup = "None; first use-case calls remain in measured samples", buildConfiguration = "Debug",
            dockerApiVersion = Environment.GetEnvironmentVariable("DOCKER_API_VERSION"),
            referenceAnalysisPath = "docs/stage-5/s8-concurrency-analysis.json",
            referenceAnalysisSha256 = Hash(File.ReadAllBytes("docs/stage-5/s8-concurrency-analysis.json")),
            priorIndex = additional || resume ? args[1] : null,
            priorIndexSha256 = additional || resume ? Hash(File.ReadAllBytes(args[1])) : null,
            expansionReason = additional ? reason : null, remediationReason = resume ? reason : null,
            preservedRunSourceSha256 = preservedSourceHashes,
            actualOfficialExecutionCount = entries.Count + excludedExecutions.Count,
            excludedExecutions, failure, runs = entries
        }, Json) + "\n");
        await SaveAsync("Running");
        Console.WriteLine($"SET {indexPath}");
        foreach (var (pattern, seed) in matrix)
        {
            var (definition, source) = sources[seed];
            var manifest = ArrivalDrivenExecution.Create(source, pattern);
            if (!ReferenceEquals(source.Catalog, manifest.Catalog) || !ReferenceEquals(source.Setup, manifest.Setup)
                || !source.DailyPlans.Equals(manifest.DailyPlans)) throw new InvalidOperationException("Workload changed.");
            var runId = $"official-arrival-{pattern.ToString().ToLowerInvariant()}-{seed}-{Guid.NewGuid():N}";
            Console.WriteLine($"START {runId} {DateTimeOffset.UtcNow:O}");
            try
            {
                var startedAt = DateTimeOffset.UtcNow;
                var result = await MinimalSimulationRunner.RunAsync(manifest);
                var metadata = SimulationRunMetadata.Create(runId, manifest, startedAt, DateTimeOffset.UtcNow, revision);
                var destination = await new SimulationEvidenceExporter().ExportAsync(new(definition, manifest, result, metadata, root));
                var observedCommits = PostCommitObservationResolver.Resolve(manifest, result);
                var audit = S8Runs.Audit(manifest, result, observedCommits);
                var outcomes = result.Attempts.GroupBy(a => a.Outcome.ToString()).ToDictionary(g => g.Key, g => g.Count());
                var peak = SimulationOverlapAnalyzer.MaxActiveRequests(result.Attempts);
                entries.Add(JsonSerializer.SerializeToNode(new
                {
                    pattern = pattern.ToString(), seed, runId, evidencePath = destination, metadata,
                    sourceManifestSha256 = Hash(JsonSerializer.SerializeToUtf8Bytes(source, Json)),
                    exactSourceCatalogSetupDailyPlans = true, maxActiveRequests = peak,
                    postCommitObservationRequests = observedCommits.Select(pair => new
                    {
                        simulationRequestId = pair.Key, persistedRequestId = pair.Value.Value,
                        persistedRequestStatus = result.Requests.Single(r => r.AssignmentRequestId == pair.Value.Value).Status,
                        persistedAssignmentIds = result.Assignments.Where(a => a.AssignmentRequestId == pair.Value).Select(a => a.Id.Value).ToArray(),
                        persistedAssignmentCount = result.Assignments.Count(a => a.AssignmentRequestId == pair.Value)
                    }).OrderBy(p => p.simulationRequestId).ToArray(),
                    integrity = new { invariantViolationCount = audit.Count, violations = audit, finalStateOnly = true,
                        persistedRequestCount = result.Requests.Count, persistedAssignmentCount = result.Assignments.Count,
                        completedFestivalDays = result.DailyCheckpoints.Count,
                        sha256 = Files.ToDictionary(n => n + ".json", n => Hash(File.ReadAllBytes(Path.Combine(destination, n + ".json")))) },
                    outcomes
                }, Json));
                await SaveAsync(audit.Count == 0 ? "Running" : "CriticalInvariantFinding");
                Console.WriteLine($"EXPORTED {runId} outcomes={JsonSerializer.Serialize(outcomes)} peak={peak} violations={audit.Count} lifecycle={metadata.CompletedAt - startedAt}");
                if (audit.Count != 0) throw new InvalidOperationException("Persisted/execution finding; evidence retained; stop and investigate.");
                if (!SourceHashes().SequenceEqual(hashes)) throw new InvalidOperationException("Source changed during execution.");
            }
            catch (Exception error)
            {
                failure = new { runId, pattern = pattern.ToString(), seed, type = error.GetType().FullName, error.Message };
                await SaveAsync("ExecutionStopped");
                throw;
            }
        }
        await SaveAsync("Completed");
        Console.WriteLine($"COMPLETE {indexPath}");
    }

    private static void VerifyFiles(JsonNode entry)
    {
        foreach (var pair in entry["integrity"]!["sha256"]!.AsObject())
            if (Hash(File.ReadAllBytes(Path.Combine(entry["evidencePath"]!.GetValue<string>(), pair.Key))) != pair.Value!.GetValue<string>())
                throw new InvalidOperationException("Retained evidence checksum mismatch: " + pair.Key);
    }

    private static SortedDictionary<string, string> SourceHashes() => new(Directory.EnumerateFiles("src", "*", SearchOption.AllDirectories)
        .Concat(Directory.EnumerateFiles("scripts/stage-5-representative-runs", "*.cs"))
        .Where(p => !p.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj") && (p.EndsWith(".cs") || p.EndsWith(".csproj")))
        .Append("scripts/stage-5-representative-runs/RepresentativeRuns.csproj")
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
