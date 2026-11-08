using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using Festival.Domain.Assignments;
using Festival.Domain.Spots;
using Festival.Domain.Zones;
using Festival.Infrastructure.Assignments.PostgreSql;
using Festival.Infrastructure.Persistence;
using Festival.Simulation;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class S7ScenarioPairTests : IDisposable
{
    private static readonly DateOnly FirstDate = new(2026, 8, 15);
    private readonly string root = Path.Combine(Path.GetTempPath(), $"festival-s7-test-{Guid.NewGuid():N}");

    [Theory]
    [InlineData(1001)]
    [InlineData(2001)]
    [InlineData(3001)]
    public void Create_ReusesOneExactS2WorkloadWithoutChangingCatalogRequestsOrOrder(int seed)
    {
        var pair = S7ScenarioPair.Create(seed, FirstDate);
        var s2 = new ScenarioMaterializer().Materialize(S2ExpectedBaselineScenarioDefinition.Create(seed, FirstDate));

        Hash(pair.SourceManifest).Should().Be(Hash(s2));
        pair.SourceManifest.Setup.Assignments.Should().BeEmpty();
        pair.SourceDefinition.Workload!.GroupSizeProfile.Select(band => band.Percentage).Should()
            .Equal(.15m, .30m, .25m, .15m, .08m, .05m, .02m);
        pair.SourceDefinition.Workload.DailyParticipationContinuity.Should().Be(.90m);
        pair.SourceDefinition.Workload.AllowsFrontStandingRequestShare.Should().Be(.50m);
        foreach (var variant in new[] { pair.Control, pair.Fragmented })
        {
            variant.Catalog.Should().BeSameAs(pair.SourceManifest.Catalog);
            variant.Catalog.Attendees.Should().HaveCount(5500);
            variant.Catalog.FestivalDays.Should().HaveCount(5);
            variant.DailyPlans.Should().Equal(pair.SourceManifest.DailyPlans);
            variant.Metadata.WorkloadSourceScenarioId.Should().Be(pair.SourceDefinition.ScenarioId);
            variant.Metadata.WorkloadSourceManifestSha256.Should().Be(Hash(s2));
            variant.Metadata.GeneratorVersion.Should().Be("v2");
            variant.Metadata.FragmentationPatternVersion.Should().Be("s7-preoccupation-v1");
            variant.Metadata.OpeningOccupiedSpotCount.Should().Be(1000);
            variant.Metadata.ExecutionMode.Should().Be(SimulationExecutionMode.Sequential);
            variant.Metadata.ConcurrencyLevel.Should().Be(1);
            variant.Metadata.OrderingTransformation.Should().BeNull();
            foreach (var (day, baseline) in variant.DailyPlans.Zip(pair.SourceManifest.DailyPlans))
            {
                day.Requests.Should().Equal(baseline.Requests, "all request fields and membership arrays are reused");
                day.Requests.SelectMany(request => request.Members).Select(member => member.Id)
                    .Distinct().Should().HaveCount(4400);
                day.Requests.Should().OnlyContain(request => request.Sequence == request.ExecutionSequence);
                day.Requests.Select(request => request.RequestedAt).Should().BeInAscendingOrder();
                day.Requests.Count(request => request.Eligibility.AllowsFrontStanding).Should().Be(732);
            }
        }
        pair.Control.Metadata.ScenarioId.Should().Be("s7-control");
        pair.Fragmented.Metadata.ScenarioId.Should().Be("s7-fragmented");
        // Supporting identities/requests are paired too; only Assignment positions differ.
        JsonSerializer.Serialize(pair.Control.Setup.SupportingAttendees).Should()
            .Be(JsonSerializer.Serialize(pair.Fragmented.Setup.SupportingAttendees));
        pair.Control.Setup.CompletedRequests.Should().BeEquivalentTo(pair.Fragmented.Setup.CompletedRequests,
            options => options.WithStrictOrdering());
        pair.Control.Setup.Assignments.Select(assignment => (assignment.Id, assignment.AssignmentRequestId,
            assignment.AttendeeId, assignment.ExperienceQuality, assignment.AssignedAt)).Should()
            .Equal(pair.Fragmented.Setup.Assignments.Select(assignment => (assignment.Id, assignment.AssignmentRequestId,
                assignment.AttendeeId, assignment.ExperienceQuality, assignment.AssignedAt)));
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(2001)]
    [InlineData(3001)]
    public void Create_ProtectsDailyRowZoneAndQualityQuotasWhileShorteningAvailableRuns(int seed)
    {
        var pair = S7ScenarioPair.Create(seed, FirstDate);
        var expectedZoneOccupied = new Dictionary<ZoneType, int>
        {
            [ZoneType.FrontStanding] = 150, [ZoneType.MiddleCenter] = 200,
            [ZoneType.MiddleLeft] = 150, [ZoneType.MiddleRight] = 150, [ZoneType.UpperCenter] = 150,
            [ZoneType.UpperLeft] = 100, [ZoneType.UpperRight] = 100
        };
        var expectedQualityAvailable = new Dictionary<ExperienceQuality, int>
        {
            [ExperienceQuality.Good] = 1402, [ExperienceQuality.Medium] = 1800, [ExperienceQuality.Bad] = 796
        };
        foreach (var day in pair.SourceManifest.DailyPlans)
        {
            var control = pair.Control.Setup.Assignments.Where(a => a.FestivalDayId == day.FestivalDayId).ToArray();
            var fragmented = pair.Fragmented.Setup.Assignments.Where(a => a.FestivalDayId == day.FestivalDayId).ToArray();
            control.Should().HaveCount(1000);
            fragmented.Should().HaveCount(1000);
            var controlCodes = control.Select(a => a.SpotCode).ToHashSet();
            var fragmentedCodes = fragmented.Select(a => a.SpotCode).ToHashSet();
            controlCodes.Should().HaveCount(1000);
            fragmentedCodes.Should().HaveCount(1000);
            controlCodes.SetEquals(fragmentedCodes).Should().BeFalse();
            foreach (var variant in new[] { controlCodes, fragmentedCodes })
            {
                var available = pair.Control.Catalog.Spots.Where(spot => !variant.Contains(spot.Code)).ToArray();
                available.Should().HaveCount(3998);
                foreach (var zone in pair.Control.Catalog.Zones)
                    pair.Control.Catalog.Spots.Count(spot => spot.ZoneId == zone.Id && variant.Contains(spot.Code))
                        .Should().Be(expectedZoneOccupied[zone.Type]);
                var byZone = pair.Control.Catalog.Zones.ToDictionary(zone => zone.Id);
                foreach (var (quality, expected) in expectedQualityAvailable)
                    available.Count(spot => ZoneExperienceQualityPolicy.GetQuality(byZone[spot.ZoneId].Type) == quality)
                        .Should().Be(expected);
            }

            var controlRunSum = 0;
            var fragmentedRunSum = 0;
            foreach (var row in pair.Control.Catalog.Spots.GroupBy(spot => (spot.ZoneId, spot.RowCode)))
            {
                var ordered = row.OrderBy(spot => spot.Number.Value).ToArray();
                var occupied = ordered.Count(spot => controlCodes.Contains(spot.Code));
                ordered.Count(spot => fragmentedCodes.Contains(spot.Code)).Should().Be(occupied);
                ordered.Where(spot => controlCodes.Contains(spot.Code)).Should().Equal(ordered.Take(occupied));
                var controlRun = LargestAvailableRun(ordered, controlCodes);
                var fragmentedRun = LargestAvailableRun(ordered, fragmentedCodes);
                controlRun.Should().Be(ordered.Length - occupied);
                fragmentedRun.Should().BeLessThan(controlRun, "capacity is equal but contiguous geometry must differ");
                var gaps = AvailableGaps(ordered, fragmentedCodes);
                gaps.Should().HaveCount(occupied + 1);
                (gaps.Max() - gaps.Min()).Should().BeLessThanOrEqualTo(1);
                controlRunSum += controlRun;
                fragmentedRunSum += fragmentedRun;
            }
            controlRunSum.Should().Be(3998);
            fragmentedRunSum.Should().BeLessThan(controlRunSum);
        }

        // Fixed expectations exercise largest-remainder ties by RowCode (not runtime enumeration).
        var firstDay = pair.Control.DailyPlans[0].FestivalDayId;
        foreach (var zone in pair.Control.Catalog.Zones)
        {
            int[] expectedRows = zone.Type switch
            {
                ZoneType.FrontStanding => Enumerable.Repeat(15, 10).ToArray(),
                ZoneType.MiddleCenter => [16, 16, 16, 16, 16, 17, 17, 17, 17, 17, 17, 18],
                ZoneType.UpperLeft or ZoneType.UpperRight => [7, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9],
                _ => [11, 12, 12, 12, 12, 12, 13, 13, 13, 13, 13, 14]
            };
            pair.Control.Setup.Assignments.Where(a => a.FestivalDayId == firstDay && a.ZoneId == zone.Id)
                .GroupBy(a => a.RowCode).OrderBy(row => row.Key.Value, StringComparer.Ordinal)
                .Select(row => row.Count()).Should().Equal(expectedRows);
        }
        var front = pair.Fragmented.Catalog.Zones.Single(zone => zone.Type == ZoneType.FrontStanding);
        pair.Fragmented.Setup.Assignments.Where(a => a.FestivalDayId == firstDay && a.ZoneId == front.Id
                && a.RowCode.Value == "R001").Select(a => a.SpotNumber.Value).Should()
            .Equal(5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 64, 68, 72);
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(2001)]
    [InlineData(3001)]
    public void Create_ReproducesBothVariantsAndIsolatesDayScopedSupportingGraphs(int seed)
    {
        var first = S7ScenarioPair.Create(seed, FirstDate);
        var repeat = S7ScenarioPair.Create(seed, FirstDate);
        Hash(first.SourceManifest).Should().Be(Hash(repeat.SourceManifest));
        Hash(first.Control).Should().Be(Hash(repeat.Control));
        Hash(first.Fragmented).Should().Be(Hash(repeat.Fragmented));
        foreach (var manifest in new[] { first.Control, first.Fragmented })
        {
            manifest.Setup.SupportingAttendees.Should().HaveCount(5000);
            manifest.Setup.SupportingAttendees.Select(a => a.Id).Distinct().Should().HaveCount(5000);
            manifest.Setup.SupportingAttendees.Select(a => a.Code).Distinct().Should().HaveCount(5000)
                .And.NotIntersectWith(manifest.Catalog.Attendees.Select(a => a.Code));
            var registeredIds = manifest.Catalog.Attendees.Select(a => a.Id).ToHashSet();
            manifest.Setup.SupportingAttendees.Should().NotContain(a => registeredIds.Contains(a.Id));
            manifest.Setup.Assignments.Should().NotContain(a => registeredIds.Contains(a.AttendeeId));
            var supporters = manifest.Setup.SupportingAttendees.ToDictionary(a => a.Id);
            var requests = manifest.Setup.CompletedRequests.ToDictionary(r => r.Id);
            var firstOccupied = manifest.Setup.Assignments.Where(a => a.FestivalDayId == manifest.DailyPlans[0].FestivalDayId)
                .Select(a => a.SpotCode).ToHashSet();
            foreach (var day in manifest.DailyPlans)
            {
                var setup = manifest.Setup.Assignments.Where(a => a.FestivalDayId == day.FestivalDayId).ToArray();
                setup.Select(a => a.SpotCode).Should().BeEquivalentTo(firstOccupied,
                    "the same opening pattern repeats without accumulating earlier days' occupancy");
                foreach (var assignment in setup)
                {
                    var request = requests[assignment.AssignmentRequestId];
                    request.AttendeeCodes.Should().Equal(supporters[assignment.AttendeeId].Code);
                    request.FestivalDayId.Should().Be(day.FestivalDayId);
                    request.CompletedAt.Should().BeOnOrBefore(day.Requests.Min(r => r.RequestedAt));
                    AssignmentGroup.Create(request.Id, request.FestivalDayId, [assignment.AttendeeId])
                        .EnsureValidResult([assignment]);
                }
            }
        }
    }

    [Theory]
    [InlineData(1001, false)]
    [InlineData(2001, false)]
    [InlineData(3001, false)]
    [InlineData(1001, true)]
    [InlineData(2001, true)]
    [InlineData(3001, true)]
    public async Task ExportAsync_RetainsS2SourceAndS7ProvenanceInTheExistingEightFiles(int seed, bool fragmented)
    {
        var pair = S7ScenarioPair.Create(seed, FirstDate);
        var manifest = fragmented ? pair.Fragmented : pair.Control;
        var timestamp = new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero);
        var metadata = SimulationRunMetadata.Create("serialization-only", manifest, timestamp, timestamp);
        var empty = new SimulationResult([], [], [], [], SimulationMetricsCalculator.Calculate([], []), [], [], [], [], []);
        var directory = await new SimulationEvidenceExporter().ExportAsync(new(pair.SourceDefinition,
            manifest, empty, metadata, root));

        Directory.GetFiles(directory).Select(Path.GetFileName).Should().BeEquivalentTo(SimulationEvidenceExporterTests.FileNames);
        using var run = SimulationEvidenceExporterTests.Read(directory, "run-metadata.json");
        run.RootElement.GetProperty("scenarioId").GetString().Should().Be(manifest.Metadata.ScenarioId);
        run.RootElement.GetProperty("workloadSourceScenarioId").GetString().Should().Be("s2-expected-baseline");
        run.RootElement.GetProperty("fragmentationPatternVersion").GetString().Should().Be("s7-preoccupation-v1");
        run.RootElement.GetProperty("openingOccupiedSpotCount").GetInt32().Should().Be(1000);
        run.RootElement.GetProperty("workloadSourceManifestSha256").GetString().Should().Be(Hash(pair.SourceManifest));
        run.RootElement.GetProperty("generatorVersion").GetString().Should().Be("v2");
        using var input = SimulationEvidenceExporterTests.Read(directory, "manifest.json");
        input.RootElement.GetRawText().Should().Be(JsonSerializer.Serialize(manifest, SimulationEvidenceExporter.JsonOptions));
        using var definition = SimulationEvidenceExporterTests.Read(directory, "definition.json");
        definition.RootElement.GetRawText().Should().Be(JsonSerializer.Serialize(pair.SourceDefinition, SimulationEvidenceExporter.JsonOptions));
    }

    [Theory]
    [InlineData("source")]
    [InlineData("pattern")]
    [InlineData("count")]
    [InlineData("hash")]
    [InlineData("order")]
    [InlineData("run")]
    public async Task ExportAsync_RejectsInvalidOrMismatchedS7ProvenanceBeforeWriting(string field)
    {
        var pair = S7ScenarioPair.Create(1001, FirstDate);
        var manifest = pair.Fragmented;
        var altered = manifest.Metadata;
        altered = field switch
        {
            "source" => altered with { WorkloadSourceScenarioId = "s6-large-groups" },
            "pattern" => altered with { FragmentationPatternVersion = "unversioned" },
            "count" => altered with { OpeningOccupiedSpotCount = 999 },
            "hash" => altered with { WorkloadSourceManifestSha256 = new string('z', 64) },
            "order" => altered with { OrderingTransformation = S5ReversedProcessingOrder.Transformation },
            _ => altered
        };
        manifest = manifest with { Metadata = altered };
        var timestamp = new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero);
        var metadata = SimulationRunMetadata.Create("invalid-provenance", manifest, timestamp, timestamp);
        if (field == "run") metadata = metadata with { FragmentationPatternVersion = "other" };
        var empty = new SimulationResult([], [], [], [], SimulationMetricsCalculator.Calculate([], []), [], [], [], [], []);
        var export = () => new SimulationEvidenceExporter().ExportAsync(new(pair.SourceDefinition,
            manifest, empty, metadata, root));

        await export.Should().ThrowAsync<ArgumentException>();
        Directory.Exists(root).Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistedSetup_BeforeAnyAttemptLeavesEvaluatedHistoryPathsAndRecoveryEmpty(bool fragmented)
    {
        var pair = S7ScenarioPair.Create(1001, FirstDate);
        var manifest = fragmented ? pair.Fragmented : pair.Control;
        await using var container = new PostgreSqlBuilder("postgres:16.3-alpine").Build();
        await container.StartAsync();
        var options = new DbContextOptionsBuilder<FestivalDbContext>().UseNpgsql(container.GetConnectionString()).Options;
        await using var database = new FestivalDbContext(options);
        await database.Database.MigrateAsync();
        database.Attendees.AddRange(manifest.Catalog.Attendees);
        database.Attendees.AddRange(manifest.Setup.SupportingAttendees);
        database.FestivalDays.AddRange(manifest.Catalog.FestivalDays);
        database.Zones.AddRange(manifest.Catalog.Zones);
        database.Spots.AddRange(manifest.Catalog.Spots);
        var repository = new PostgreSqlAssignmentRequestRepository(database);
        foreach (var supporting in manifest.Setup.CompletedRequests)
        {
            var request = AssignmentRequest.Create(supporting.Id, supporting.FestivalDayId,
                supporting.AttendeeCodes, supporting.RequestedAt, supporting.Eligibility);
            request.Complete(supporting.CompletedAt);
            await repository.AddAsync(request);
        }
        database.Assignments.AddRange(manifest.Setup.Assignments);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var evaluatedIds = manifest.DailyPlans.SelectMany(day => day.Requests).SelectMany(r => r.Members)
            .Select(member => member.Id).Distinct().ToArray();
        var histories = await new PostgreSqlFairnessHistoryProvider(database).GetHistoriesAsync(evaluatedIds);
        histories.Should().HaveCount(evaluatedIds.Length);
        histories.Values.Should().OnlyContain(history => history.Experiences.Count == 0);
        histories.Values.Select(RotationScore.Calculate).Should().OnlyContain(score => score == 0m);
        var paths = await AttendeePathReconstructor.ReconstructAsync(database, manifest);
        paths.Should().HaveCount(evaluatedIds.Length).And.OnlyContain(path => path.PathLength == 0
            && path.OverallQuality == null && path.Classification == null);
        var recovery = paths.Select(AttendeeRecoveryAnalyzer.Analyze).ToImmutableArray();
        recovery.Should().OnlyContain(record => record.Episodes.IsEmpty);
        var metrics = SimulationMetricsCalculator.Calculate(paths, recovery);
        metrics.Fairness.PersistedOutcomeCount.Should().Be(0);
        metrics.Fairness.NoPersistedPathCount.Should().Be(evaluatedIds.Length);
        metrics.Recovery.RecoveryEpisodeCount.Should().Be(0);
        // Every persisted request belongs to setup; none is an evaluated outcome.
        var persisted = await database.Database.SqlQueryRaw<PersistedSimulationRequest>(
            """
            SELECT "AssignmentRequestId", "FestivalDayId", "Status", "RequestedAt" FROM "AssignmentRequests"
            """).ToArrayAsync();
        persisted.Select(r => r.AssignmentRequestId).Order().Should()
            .Equal(manifest.Setup.CompletedRequests.Select(r => r.Id.Value).Order());
        persisted.Should().HaveCount(5000).And.OnlyContain(request => request.Status == "Completed");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_FiveDaySliceKeepsSetupOutsideEvaluatedPathsAndPreservesRealHistory(bool fragmented)
    {
        var pair = S7ScenarioPair.Create(1001, FirstDate);
        var original = fragmented ? pair.Fragmented : pair.Control;
        var first = original.DailyPlans[0];
        var second = original.DailyPlans[1];
        // Select unchanged S2 requests: a returning singleton, its next-day group,
        // and a larger group that can legitimately reject in fragmented inventory.
        var returning = first.Requests.First(r => r.Members.Length == 1 && second.Requests.Any(next =>
            next.Members.Length <= 4 && next.Members.Any(member => member.Id == r.Members[0].Id)));
        var next = second.Requests.Single(r => r.Members.Any(member => member.Id == returning.Members[0].Id));
        var large = first.Requests.First(r => r.Members.Length >= 5);
        var slice = original with
        {
            DailyPlans = original.DailyPlans.Select(day => day with
            {
                Requests = day.DayNumber switch
                {
                    1 => new[] { returning, large }.OrderBy(r => r.ExecutionSequence).ToImmutableArray(),
                    2 => [next],
                    _ => []
                }
            }).ToImmutableArray()
        };

        var result = await MinimalSimulationRunner.RunAsync(slice);

        result.Attempts.Should().HaveCount(3).And.OnlyContain(attempt =>
            attempt.Outcome == SimulationAttemptOutcome.Successful || attempt.Outcome == SimulationAttemptOutcome.DomainRejected);
        result.Attempts.Select(attempt => attempt.AttemptSequence).Should()
            .Equal(slice.DailyPlans.SelectMany(day => day.Requests).Select(r => r.ExecutionSequence));
        result.Attempts.Should().OnlyContain(attempt => attempt.AttemptSequence == attempt.BaselineSequence);
        foreach (var opening in result.OpeningStates)
        {
            opening.Spots.Should().HaveCount(4998);
            opening.AvailableSpots.Should().HaveCount(3998);
            var expected = original.Setup.Assignments.Where(a => a.FestivalDayId == opening.FestivalDayId)
                .Select(a => a.SpotCode).ToHashSet();
            var availableCodes = opening.AvailableSpots.Select(spot => spot.Code).ToHashSet();
            opening.Spots.Where(spot => !availableCodes.Contains(spot.Code))
                .Select(spot => spot.Code).Should().BeEquivalentTo(expected);
            var inventory = RemainingInventoryCalculator.Calculate(opening.Zones, opening.AvailableSpots);
            (inventory.GoodRemaining, inventory.MediumRemaining, inventory.BadRemaining).Should().Be((1402, 1800, 796));
        }
        result.DailyCheckpoints.Select(checkpoint => checkpoint.EvaluatedRequestCount).Should().Equal(2, 1, 0, 0, 0);
        var setupIds = original.Setup.SupportingAttendees.Select(a => a.Id).ToHashSet();
        result.AttendeePaths.Should().NotContain(path => setupIds.Contains(path.AttendeeId));
        result.RecoveryObservations.Should().NotContain(record => setupIds.Contains(record.AttendeeId));
        result.AttendeePaths.Select(path => path.AttendeeId).Should().BeEquivalentTo(
            slice.DailyPlans.SelectMany(day => day.Requests).SelectMany(r => r.Members).Select(m => m.Id).Distinct());
        var path = result.AttendeePaths.Single(p => p.AttendeeId == returning.Members[0].Id);
        path.Entries.Select(entry => entry.FestivalDayId).Should().Equal(first.FestivalDayId, second.FestivalDayId);
        path.Entries[0].ExperienceQuality.Should().Be(ExperienceQuality.Medium,
            "Day 1 has empty evaluated history despite 5,000 persisted setup Assignments");
        var successful = result.Attempts.Where(a => a.Outcome == SimulationAttemptOutcome.Successful).ToArray();
        result.Metrics.Fairness.PersistedOutcomeCount.Should().Be(successful.Sum(a => a.GroupSize));
        result.Assignments.Should().HaveCount(5000 + successful.Sum(a => a.GroupSize));
        result.Requests.Should().HaveCount(5003);
        var largeAttempt = result.Attempts.Single(a => a.SimulationRequestId == large.SimulationRequestId);
        if (fragmented)
        {
            largeAttempt.Outcome.Should().Be(SimulationAttemptOutcome.DomainRejected);
            largeAttempt.DiagnosticCode.Should().Be("NO_CONTIGUOUS_SPOTS_AVAILABLE");
            largeAttempt.EligibleAvailableSpotCount.Should().BeGreaterThanOrEqualTo(large.Members.Length);
            largeAttempt.FeasibleContiguousBlockFound.Should().BeFalse();
            foreach (var member in large.Members)
            {
                result.AttendeePaths.Single(p => p.AttendeeId == member.Id).Entries.Should().BeEmpty();
                result.RecoveryObservations.Single(r => r.AttendeeId == member.Id).Episodes.Should().BeEmpty();
            }
        }
        // Real slice output also traverses the unchanged eight-file export path.
        var timestamp = new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero);
        await new SimulationEvidenceExporter().ExportAsync(new(pair.SourceDefinition, slice, result,
            SimulationRunMetadata.Create("test-s7-slice", slice, timestamp, timestamp), root));
    }

    private static int LargestAvailableRun(IEnumerable<Spot> row, ISet<SpotCode> occupied) =>
        AvailableGaps(row, occupied).Max();

    private static int[] AvailableGaps(IEnumerable<Spot> row, ISet<SpotCode> occupied)
    {
        var gaps = new List<int>();
        var run = 0;
        int? previous = null;
        foreach (var spot in row.OrderBy(spot => spot.Number.Value))
        {
            if (previous.HasValue && spot.Number.Value != previous + 1)
            {
                gaps.Add(run);
                run = 0;
            }
            if (occupied.Contains(spot.Code))
            {
                gaps.Add(run);
                run = 0;
            }
            else run++;
            previous = spot.Number.Value;
        }
        gaps.Add(run);
        return gaps.ToArray();
    }

    private static string Hash(ScenarioManifest manifest) => Convert.ToHexString(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(manifest, SimulationEvidenceExporter.JsonOptions))).ToLowerInvariant();

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
