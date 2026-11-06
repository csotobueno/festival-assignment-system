using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.Zones;
using Festival.Simulation;
using FluentAssertions;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class S6LargeGroupsScenarioTests
{
    private static readonly DateOnly FirstDate = new(2026, 8, 15);
    private static readonly ImmutableArray<GroupSizeBand> ExpectedProfile =
    [
        new(1, 1, .10m), new(2, 2, .20m), new(3, 3, .20m), new(4, 4, .15m),
        new(5, 5, .10m), new(6, 7, .15m), new(8, 10, .10m)
    ];

    [Fact]
    public void Create_PreservesS2ControlsAndChangesOnlyScenarioIdentityAndGroupProfile()
    {
        var baseline = S2ExpectedBaselineScenarioDefinition.Create(1001, FirstDate);
        var definition = S6LargeGroupsScenarioDefinition.Create(1001, FirstDate);
        var workload = definition.Workload!;

        definition.ScenarioId.Should().Be("s6-large-groups").And.NotBe(baseline.ScenarioId);
        definition.AttendeeCount.Should().Be(5500);
        definition.Days.Should().HaveCount(5);
        definition.GeneratorVersion.Should().Be("v2");
        definition.ExecutionMode.Should().Be(SimulationExecutionMode.Sequential);
        definition.ConcurrencyLevel.Should().Be(1);
        workload.DailyRequesterCount.Should().Be(4400);
        workload.DailyParticipationContinuity.Should().Be(.90m);
        workload.AllowsFrontStandingRequestShare.Should().Be(.50m);
        workload.GroupSizeProfile.Should().Equal(ExpectedProfile);
        workload.GroupSizeProfile.Sum(band => band.Percentage).Should().Be(1m);
        workload.GroupSizeProfile.Where(band => band.MinSize >= 6).Sum(band => band.Percentage)
            .Should().Be(.25m);
        workload.ArrivalProfile.Should().Equal(baseline.Workload!.ArrivalProfile);
        definition.Should().BeEquivalentTo(baseline with
        {
            ScenarioId = "s6-large-groups",
            Workload = baseline.Workload with { GroupSizeProfile = ExpectedProfile }
        });
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(2001)]
    [InlineData(3001)]
    public void Materialize_AllFiveDaysHaveExactUniquePopulationAndPlannedRequesterContinuity(int seed)
    {
        var manifest = Materialize(seed);
        var registered = manifest.Catalog.Attendees.Select(member => member.Id).ToHashSet();
        HashSet<AttendeeId>? previous = null;

        registered.Should().HaveCount(5500);
        manifest.DailyPlans.Should().HaveCount(5);
        manifest.Setup.SupportingAttendees.Should().BeEmpty();
        manifest.Setup.CompletedRequests.Should().BeEmpty();
        manifest.Setup.Assignments.Should().BeEmpty("Day 1 starts without synthetic history or occupancy");
        foreach (var day in manifest.DailyPlans)
        {
            var members = day.Requests.SelectMany(request => request.Members).ToArray();
            members.Should().HaveCount(4400).And.OnlyContain(member => registered.Contains(member.Id));
            members.Select(member => member.Id).Should().OnlyHaveUniqueItems();
            day.Requests.Should().OnlyContain(request => request.Members.Length >= 1 && request.Members.Length <= 10);
            var current = members.Select(member => member.Id).ToHashSet();
            if (previous is not null)
            {
                current.Intersect(previous).Should().HaveCount(3960);
                current.Except(previous).Should().HaveCount(440);
            }
            previous = current;
        }
        manifest.DailyPlans.SelectMany(day => day.Requests).Select(request => request.SimulationRequestId)
            .Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(2001)]
    [InlineData(3001)]
    public void Materialize_GroupSharesApproximateRequestLevelTargetsWithoutChangingAttendeeDemand(int seed)
    {
        foreach (var day in Materialize(seed).DailyPlans)
        {
            var requests = day.Requests;
            requests.Sum(request => request.Members.Length).Should().Be(4400);
            foreach (var band in ExpectedProfile)
            {
                var count = requests.Count(request => request.Members.Length >= band.MinSize
                    && request.Members.Length <= band.MaxSize);
                // At this population, reconciliation fits within the ranged buckets.
                // Largest-remainder allocation differs from each ideal quota by < 1 request.
                Math.Abs(count - requests.Length * band.Percentage).Should().BeLessThan(1m);
            }
            // The combined 6–10 bucket sums two rounded quotas: allow < 2 requests.
            var largeCount = requests.Count(request => request.Members.Length >= 6);
            Math.Abs(largeCount - requests.Length * .25m).Should().BeLessThan(2m);
        }
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(2001)]
    [InlineData(3001)]
    public void Materialize_PreservesS2ArrivalProfileAndNormalOrderWithoutS5Reversal(int seed)
    {
        var manifest = Materialize(seed);
        var profile = S2ExpectedBaselineScenarioDefinition.Create(seed, FirstDate).Workload!.ArrivalProfile;
        var sequence = 0;

        manifest.Metadata.WorkloadSourceScenarioId.Should().BeNull();
        manifest.Metadata.OrderingTransformation.Should().BeNull();
        foreach (var day in manifest.DailyPlans)
        {
            var date = FirstDate.AddDays(day.DayNumber - 1);
            var counts = profile.Select(band => day.Requests.Count(request =>
                TimeOnly.FromDateTime(request.RequestedAt.UtcDateTime) >= band.Start
                && TimeOnly.FromDateTime(request.RequestedAt.UtcDateTime) < band.End)).ToArray();
            counts.Sum().Should().Be(day.Requests.Length, "each timestamp falls in a configured half-open band");
            for (var index = 0; index < profile.Length; index++)
                Math.Abs(counts[index] - day.Requests.Length * profile[index].Percentage)
                    .Should().BeLessThan(1m, "arrival quotas use the unchanged integer allocation convention");
            day.Requests.Select(request => request.SimulationRequestId).Should().Equal(day.Requests
                .OrderBy(request => request.RequestedAt).ThenBy(request => request.SimulationRequestId)
                .Select(request => request.SimulationRequestId));
            foreach (var request in day.Requests)
            {
                request.Sequence.Should().Be(++sequence);
                request.ExecutionSequence.Should().Be(request.Sequence, "S6 must never apply S5 reversal");
                request.RequestedAt.Offset.Should().Be(TimeSpan.Zero);
                DateOnly.FromDateTime(request.RequestedAt.UtcDateTime).Should().Be(date);
                request.AssignedAt.Should().Be(request.RequestedAt);
            }
        }
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(2001)]
    [InlineData(3001)]
    public void Materialize_AllocatesHalfOfRequestsToFrontStandingUsingDeclaredOrderRemainder(int seed)
    {
        foreach (var day in Materialize(seed).DailyPlans)
        {
            // At 50%, an odd request count awards the tied remainder to allowed (first bucket).
            day.Requests.Count(request => request.Eligibility.AllowsFrontStanding)
                .Should().Be((day.Requests.Length + 1) / 2);
            day.Requests.Count(request => !request.Eligibility.AllowsFrontStanding)
                .Should().Be(day.Requests.Length / 2);
        }
    }

    [Fact]
    public void Materialize_SharesS2CatalogIdentitiesAndExactVenueTopology()
    {
        var baseline = new ScenarioMaterializer().Materialize(S2ExpectedBaselineScenarioDefinition.Create(1001, FirstDate));
        var manifest = Materialize(1001);

        manifest.Catalog.Attendees.Should().BeEquivalentTo(baseline.Catalog.Attendees, options => options.WithStrictOrdering());
        manifest.Catalog.Zones.Should().BeEquivalentTo(baseline.Catalog.Zones, options => options.WithStrictOrdering());
        manifest.Catalog.Spots.Should().BeEquivalentTo(baseline.Catalog.Spots, options => options.WithStrictOrdering());
        manifest.Catalog.FestivalDays.Select(day => (day.Date, day.AssignmentWindow)).Should()
            .Equal(baseline.Catalog.FestivalDays.Select(day => (day.Date, day.AssignmentWindow)));
        manifest.Catalog.Spots.Should().HaveCount(4998);
        manifest.Catalog.Zones.ToDictionary(zone => zone.Type,
            zone => manifest.Catalog.Spots.Count(spot => spot.ZoneId == zone.Id)).Should().BeEquivalentTo(
            new Dictionary<ZoneType, int>
            {
                [ZoneType.FrontStanding] = 750, [ZoneType.MiddleCenter] = 1002,
                [ZoneType.MiddleLeft] = 750, [ZoneType.MiddleRight] = 750,
                [ZoneType.UpperCenter] = 750, [ZoneType.UpperLeft] = 498, [ZoneType.UpperRight] = 498
            });
        manifest.Catalog.Spots.GroupBy(spot => ZoneExperienceQualityPolicy.GetQuality(
                manifest.Catalog.Zones.Single(zone => zone.Id == spot.ZoneId).Type))
            .ToDictionary(group => group.Key, group => group.Count()).Should().BeEquivalentTo(
                new Dictionary<ExperienceQuality, int>
                { [ExperienceQuality.Good] = 1752, [ExperienceQuality.Medium] = 2250, [ExperienceQuality.Bad] = 996 });
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(2001)]
    [InlineData(3001)]
    public void Materialize_SameDefinitionSeedAndVersionReproduceEveryManifestInput(int seed)
    {
        var first = Materialize(seed);
        var previousCulture = CultureInfo.CurrentCulture;
        ScenarioManifest second;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            second = Materialize(seed);
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }

        // The exporter serialization includes the full catalog, cohorts, ordered members,
        // eligibility, timestamps and both sequences, not just aggregate counts.
        JsonSerializer.Serialize(second, SimulationEvidenceExporter.JsonOptions).Should()
            .Be(JsonSerializer.Serialize(first, SimulationEvidenceExporter.JsonOptions));
        second.Metadata.ScenarioId.Should().Be("s6-large-groups");
        second.Metadata.Seed.Should().Be(seed);
        second.Metadata.GeneratorVersion.Should().Be("v2");
    }

    [Fact]
    public void Materialize_DifferentSeedsProduceDifferentConcreteGroupsWithTheSameCatalog()
    {
        var first = Materialize(1001);
        var second = Materialize(2001);

        second.Catalog.Attendees.Select(member => member.Id).Should().Equal(first.Catalog.Attendees.Select(member => member.Id));
        second.DailyPlans[0].Requests.SelectMany(request => request.Members.Select(member => member.Id)).Should()
            .NotEqual(first.DailyPlans[0].Requests.SelectMany(request => request.Members.Select(member => member.Id)));
        second.DailyPlans[0].Requests.Select(request => request.RequestedAt).Should()
            .NotEqual(first.DailyPlans[0].Requests.Select(request => request.RequestedAt));
    }

    [Fact]
    public async Task ExportAsync_AcceptsS6IdentityAndManifestWithoutScenarioSpecificExporterChanges()
    {
        var definition = S6LargeGroupsScenarioDefinition.Create(1001, FirstDate);
        var manifest = new ScenarioMaterializer().Materialize(definition);
        var root = Path.Combine(Path.GetTempPath(), $"festival-s6-export-test-{Guid.NewGuid():N}");
        var timestamp = new DateTimeOffset(2026, 8, 15, 7, 0, 0, TimeSpan.Zero);
        // Synthetic empty observations test serialization only; no runner or database is executed.
        var result = new SimulationResult([], [], [], [], SimulationMetricsCalculator.Calculate([], []), [], [], [], [], []);
        var metadata = SimulationRunMetadata.Create("serialization-test", manifest, timestamp, timestamp);
        try
        {
            var directory = await new SimulationEvidenceExporter().ExportAsync(
                new(definition, manifest, result, metadata, root));

            directory.Should().Be(Path.Combine(root, "s6-large-groups", "serialization-test"));
            Directory.GetFiles(directory).Select(Path.GetFileName).Should()
                .BeEquivalentTo(SimulationEvidenceExporterTests.FileNames);
            using var runJson = SimulationEvidenceExporterTests.Read(directory, "run-metadata.json");
            runJson.RootElement.GetProperty("scenarioId").GetString().Should().Be("s6-large-groups");
            runJson.RootElement.GetProperty("generatorVersion").GetString().Should().Be("v2");
            using var definitionJson = SimulationEvidenceExporterTests.Read(directory, "definition.json");
            definitionJson.RootElement.GetRawText().Should()
                .Be(JsonSerializer.Serialize(definition, SimulationEvidenceExporter.JsonOptions));
            using var manifestJson = SimulationEvidenceExporterTests.Read(directory, "manifest.json");
            manifestJson.RootElement.GetRawText().Should()
                .Be(JsonSerializer.Serialize(manifest, SimulationEvidenceExporter.JsonOptions));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static ScenarioManifest Materialize(int seed) =>
        new ScenarioMaterializer().Materialize(S6LargeGroupsScenarioDefinition.Create(seed, FirstDate));
}
