using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Festival.Domain.Assignments;
using Festival.Domain.Zones;
using Festival.Simulation;
using FluentAssertions;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class MinimalSimulationTests
{
    [Fact]
    public async Task RunAsync_ObservesEligibleCapacityAtEachEvaluationAndGlobalClosingInventory()
    {
        var definition = MinimalRecoveryScenarioDefinition.Create();
        definition = definition with
        {
            AttendeeCount = 6,
            Topology =
            [
                new("front", ZoneType.FrontStanding, 2),
                new("medium", ZoneType.MiddleLeft, 2),
                new("bad", ZoneType.UpperLeft, 2)
            ],
            Days =
            [
                new(1, [],
                [
                    new(1, [3, 4, 5], new RequestEligibility(false)),
                    new(2, [3, 4, 5], new RequestEligibility(true)),
                    new(3, [1, 2], new RequestEligibility(false)),
                    new(4, [3, 4, 5], new RequestEligibility(false))
                ])
            ]
        };
        var manifest = new ScenarioMaterializer().Materialize(definition);

        var result = await MinimalSimulationRunner.RunAsync(manifest);

        result.Attempts.Should().HaveCount(4);
        result.Attempts.Select(attempt => attempt.Outcome).Should().Equal(
            SimulationAttemptOutcome.DomainRejected, SimulationAttemptOutcome.DomainRejected,
            SimulationAttemptOutcome.Successful, SimulationAttemptOutcome.DomainRejected);
        result.Attempts.Select(attempt => attempt.EligibleAvailableSpotCount).Should().Equal(4, 6, 4, 2);
        result.Attempts.Select(attempt => attempt.FeasibleContiguousBlockFound).Should().Equal(false, false, true, false);
        result.Attempts.Select(attempt => attempt.SelectedExperienceQuality).Should().Equal(
            null, null, ExperienceQuality.Medium, null);
        // First rejections have enough eligible raw capacity split across two-Spot blocks;
        // the final rejection has only two eligible Spots for three members.
        var rejected = result.Attempts.Where(attempt => attempt.Outcome == SimulationAttemptOutcome.DomainRejected).ToArray();
        rejected.Should().OnlyContain(attempt => attempt.DiagnosticCode == "NO_CONTIGUOUS_SPOTS_AVAILABLE"
            && attempt.PersistedRequestId.HasValue && attempt.DiagnosticType == null);
        var rejectedIds = rejected.Select(attempt => attempt.PersistedRequestId!.Value.Value).ToHashSet();
        result.Requests.Where(request => rejectedIds.Contains(request.AssignmentRequestId))
            .Should().HaveCount(3).And.OnlyContain(request => request.Status == "Rejected");
        result.Assignments.Should().HaveCount(2).And.NotContain(assignment => rejectedIds.Contains(assignment.AssignmentRequestId.Value));
        result.Attempts.Should().OnlyContain(attempt => attempt.StartedAt <= attempt.CompletedAt
            && attempt.Duration >= TimeSpan.Zero);
        result.DailyCheckpoints.Should().ContainSingle().Which.Should().Be(
            new DailySimulationCheckpoint(definition.ScenarioId, manifest.DailyPlans[0].FestivalDayId,
                4, 1, 3, 2, 0, 2, 4));

        // Catalog attendee 6 never requests. Rejected-only attendees 3..5 remain
        // evaluated, without synthetic Bad entries or empty classifications.
        result.AttendeePaths.Select(path => path.AttendeeId).Should().BeEquivalentTo(
            manifest.DailyPlans.SelectMany(day => day.Requests).SelectMany(request => request.Members)
                .Select(member => member.Id).Distinct());
        result.AttendeePaths.Should().HaveCount(5);
        result.AttendeePaths.Where(path => path.PathLength == 1).Should().HaveCount(2)
            .And.OnlyContain(path => path.Entries[0].ExperienceQuality == ExperienceQuality.Medium
                && path.Classification == PathClassification.InsufficientEvidence
                && path.DirectionScore == null && path.Direction == null);
        result.AttendeePaths.Where(path => path.PathLength == 0).Should().HaveCount(3)
            .And.OnlyContain(path => path.Entries.IsEmpty && path.OverallQuality == null
                && path.DirectionScore == null && path.OverallQualityBand == null
                && path.Direction == null && path.Classification == null);
        result.RecoveryObservations.Select(record => record.AttendeeId).Should().Equal(
            result.AttendeePaths.Select(path => path.AttendeeId));
        result.RecoveryObservations.Should().HaveCount(5).And.OnlyContain(record => record.Episodes.IsEmpty);
        var fairness = result.Metrics.Fairness;
        fairness.EvaluatedAttendeeCount.Should().Be(5);
        fairness.NoPersistedPathCount.Should().Be(3);
        fairness.InsufficientEvidenceCount.Should().Be(2);
        fairness.ClassifiablePathCount.Should().Be(0);
        fairness.NoPersistedPathRate.Should().Be(0.6m);
        fairness.InsufficientEvidenceRate.Should().Be(0.4m);
        fairness.BalancedPathRate.Should().BeNull();
        fairness.PersistedOutcomeCount.Should().Be(2);
        fairness.MediumOutcomeCount.Should().Be(2);
        fairness.MediumOutcomeRate.Should().Be(1m);
        fairness.BadOutcomeCount.Should().Be(0);
        result.Metrics.Recovery.RecoveryEpisodeCount.Should().Be(0);
        result.Metrics.Recovery.RecoveryRate.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_ExecutesManifestMembershipAndSequenceInsteadOfCollectionOrder()
    {
        var definition = MinimalRecoveryScenarioDefinition.Create();
        definition = definition with
        {
            Days =
            [
                definition.Days[0] with
                {
                    Requests =
                    [
                        new(20, [2], new RequestEligibility(true)),
                        new(10, [1], new RequestEligibility(true))
                    ]
                },
                definition.Days[1] with { Requests = [new(30, [1, 2], new RequestEligibility(true))] }
            ]
        };
        var materialized = new ScenarioMaterializer().Materialize(definition);
        var manifest = materialized with
        {
            DailyPlans = materialized.DailyPlans.Reverse().Select(day => day with
            {
                Requests = day.Requests.Reverse().ToImmutableArray()
            }).ToImmutableArray()
        };

        var result = await MinimalSimulationRunner.RunAsync(manifest);

        result.Attempts.Select(attempt => attempt.AttemptSequence).Should().Equal(10, 20, 30);
        result.Attempts.Select(attempt => attempt.GroupSize).Should().Equal(1, 1, 2);
        result.Attempts.Select(attempt => attempt.EligibleAvailableSpotCount).Should().Equal(2, 1, 6);
        result.Attempts.Should().OnlyContain(attempt => attempt.Outcome == SimulationAttemptOutcome.Successful);
        result.OpeningStates.Select(state => state.FestivalDayId).Should().Equal(
            materialized.DailyPlans.Select(day => day.FestivalDayId));
        result.DailyCheckpoints.Select(checkpoint => checkpoint.EvaluatedRequestCount).Should().Equal(2, 1);
        result.DailyCheckpoints.Select(checkpoint => checkpoint.SuccessfulAttemptCount).Should().Equal(2, 1);
        result.DailyCheckpoints.Select(checkpoint => checkpoint.RejectedAttemptCount).Should().Equal(0, 0);
        var orderedRequests = materialized.DailyPlans.SelectMany(day => day.Requests).ToArray();
        for (var index = 0; index < orderedRequests.Length; index++)
        {
            var attempt = result.Attempts[index];
            var planned = orderedRequests[index];
            attempt.SimulationRequestId.Should().Be(planned.SimulationRequestId);
            var assignments = result.Assignments.Where(assignment =>
                assignment.AssignmentRequestId == attempt.PersistedRequestId).ToArray();
            assignments.Select(assignment => assignment.AttendeeId).Should().BeEquivalentTo(
                planned.Members.Select(member => member.Id));
            assignments.Should().OnlyContain(assignment => assignment.AssignedAt == planned.AssignedAt);
        }
    }

    [Fact]
    public async Task RunAsync_RecoversFromPersistedBadHistoryAcrossFestivalDays()
    {
        var definition = MinimalRecoveryScenarioDefinition.Create();
        var manifest = new ScenarioMaterializer().Materialize(definition);
        var originalManifest = new ScenarioMaterializer().Materialize(definition);
        var startedAt = DateTimeOffset.UtcNow;
        var result = await MinimalSimulationRunner.RunAsync(manifest);

        await VerifyEvidencePackageAsync(definition, manifest, result, startedAt);

        manifest.Should().BeEquivalentTo(originalManifest, options => options.WithStrictOrdering());
        result.Attempts.Select(attempt => attempt.SimulationRequestId).Should().Equal(
            manifest.DailyPlans.SelectMany(day => day.Requests).Select(request => request.SimulationRequestId));

        result.Attempts.Should().HaveCount(2);
        result.Attempts.Select(attempt => attempt.AttemptSequence).Should().Equal(1, 2);
        result.Attempts.Should().OnlyContain(attempt =>
            attempt.Outcome == SimulationAttemptOutcome.Successful && attempt.GroupSize == 2
            && attempt.PersistedRequestId.HasValue && attempt.Diagnostic == null);
        result.Attempts.Should().OnlyContain(attempt => attempt.ScenarioId == definition.ScenarioId
            && attempt.StartedAt <= attempt.CompletedAt && attempt.Duration >= TimeSpan.Zero
            && attempt.FeasibleContiguousBlockFound == true
            && attempt.DiagnosticCode == null && attempt.DiagnosticType == null);
        result.Attempts.Select(attempt => attempt.EligibleAvailableSpotCount).Should().Equal(2, 6);
        result.Attempts.Select(attempt => attempt.SelectedExperienceQuality).Should().Equal(
            ExperienceQuality.Bad, ExperienceQuality.Good);
        for (var index = 0; index < manifest.DailyPlans.Length; index++)
        {
            var day = manifest.DailyPlans[index];
            var attempt = result.Attempts[index];
            attempt.FestivalDayId.Should().Be(day.FestivalDayId);
            attempt.AttendeeIds.Should().Equal(day.Requests[0].Members.Select(member => member.Id));
        }
        result.DailyCheckpoints.Should().Equal(
            new DailySimulationCheckpoint(definition.ScenarioId, manifest.DailyPlans[0].FestivalDayId,
                1, 1, 0, 0, 0, 0, 0),
            new DailySimulationCheckpoint(definition.ScenarioId, manifest.DailyPlans[1].FestivalDayId,
                1, 1, 0, 0, 2, 2, 4));
        var evaluatedRequestIds = result.Attempts
            .Select(attempt => attempt.PersistedRequestId!.Value.Value).ToHashSet();
        var evaluatedRequests = result.Requests
            .Where(request => evaluatedRequestIds.Contains(request.AssignmentRequestId)).ToArray();
        evaluatedRequests.Should().HaveCount(2).And.OnlyContain(request => request.Status == "Completed");
        evaluatedRequests.Select(request => request.AssignmentRequestId).Should().Equal(
            result.Attempts.Select(attempt => attempt.PersistedRequestId!.Value.Value));
        var evaluatedAssignments = result.Assignments
            .Where(assignment => evaluatedRequestIds.Contains(assignment.AssignmentRequestId.Value)).ToArray();
        evaluatedAssignments.Should().HaveCount(4);

        // Supporting occupancy is initial persisted input, not an evaluated attempt.
        // Keep it visible: two setup requests/four Assignments plus the evaluated graph.
        result.Requests.Should().HaveCount(4).And.OnlyContain(request => request.Status == "Completed");
        result.Assignments.Should().HaveCount(8);
        var occupancyRequests = result.Requests
            .Where(request => !evaluatedRequestIds.Contains(request.AssignmentRequestId)).ToArray();
        occupancyRequests.Should().HaveCount(2).And.OnlyContain(request =>
            request.FestivalDayId == result.Attempts[0].FestivalDayId.Value
            && request.RequestedAt < evaluatedRequests[0].RequestedAt);
        occupancyRequests.Select(request => request.AssignmentRequestId).Should().BeEquivalentTo(
            manifest.Setup.CompletedRequests.Select(request => request.Id.Value));
        var occupancyAssignments = result.Assignments
            .Where(assignment => !evaluatedRequestIds.Contains(assignment.AssignmentRequestId.Value)).ToArray();
        occupancyAssignments.Should().HaveCount(4).And.OnlyContain(assignment =>
            assignment.FestivalDayId == result.Attempts[0].FestivalDayId
            && assignment.ExperienceQuality != ExperienceQuality.Bad
            && !evaluatedAssignments.Select(item => item.AttendeeId).Contains(assignment.AttendeeId));
        occupancyAssignments.Should().BeEquivalentTo(manifest.Setup.Assignments);
        evaluatedAssignments.Select(assignment => assignment.AttendeeId).Distinct().Should().BeEquivalentTo(
            manifest.Catalog.Attendees.Select(attendee => attendee.Id));

        var evaluatedAttendees = manifest.DailyPlans.SelectMany(day => day.Requests)
            .SelectMany(request => request.Members).Select(member => member.Id).Distinct().ToArray();
        result.AttendeePaths.Select(path => path.AttendeeId).Should().BeEquivalentTo(evaluatedAttendees);
        result.AttendeePaths.Should().HaveCount(2).And.NotContain(path =>
            manifest.Setup.SupportingAttendees.Any(attendee => attendee.Id == path.AttendeeId));
        result.RecoveryObservations.Select(record => record.AttendeeId).Should().BeEquivalentTo(evaluatedAttendees);
        result.RecoveryObservations.Should().HaveCount(2).And.NotContain(record =>
            manifest.Setup.SupportingAttendees.Any(attendee => attendee.Id == record.AttendeeId));
        var fairness = result.Metrics.Fairness;
        fairness.EvaluatedAttendeeCount.Should().Be(2);
        fairness.ClassifiablePathCount.Should().Be(2);
        fairness.BalancedPathCount.Should().Be(2);
        fairness.BalancedPathRate.Should().Be(1m);
        fairness.PersistedOutcomeCount.Should().Be(4);
        fairness.GoodOutcomeCount.Should().Be(2);
        fairness.MediumOutcomeCount.Should().Be(0);
        fairness.BadOutcomeCount.Should().Be(2);
        fairness.GoodOutcomeRate.Should().Be(0.5m);
        fairness.BadOutcomeRate.Should().Be(0.5m);
        var recovery = result.Metrics.Recovery;
        recovery.RecoveryEpisodeCount.Should().Be(2);
        recovery.RecoveredEpisodeCount.Should().Be(2);
        recovery.RecoveryEligibleEpisodeCount.Should().Be(2);
        recovery.RecoveryIneligibleEpisodeCount.Should().Be(0);
        recovery.UnresolvedEpisodeCount.Should().Be(0);
        recovery.RecoveryRate.Should().Be(1m);
        recovery.RecoveredToGoodCount.Should().Be(2);
        recovery.RecoveredToMediumCount.Should().Be(0);
        recovery.AverageOpportunitiesToRecovery.Should().Be(1m);
        recovery.MedianOpportunitiesToRecovery.Should().Be(1m);
        recovery.OpportunityDistribution.Should().ContainSingle().Which
            .Should().Be(new RecoveryOpportunityDistribution(1, 2, 1m));
        foreach (var path in result.AttendeePaths)
        {
            var persisted = evaluatedAssignments.Where(assignment => assignment.AttendeeId == path.AttendeeId)
                .OrderBy(assignment => assignment.AssignedAt).ThenBy(assignment => assignment.Id.Value).ToArray();
            path.Entries.Select(entry => entry.AssignmentId).Should().Equal(persisted.Select(assignment => assignment.Id));
            path.Entries.Select(entry => entry.FestivalDayId).Should().Equal(persisted.Select(assignment => assignment.FestivalDayId));
            path.Entries.Select(entry => entry.FestivalDate).Should().Equal(persisted.Select(assignment =>
                manifest.Catalog.FestivalDays.Single(day => day.Id == assignment.FestivalDayId).Date));
            path.Entries.Select(entry => entry.AssignedAt).Should().Equal(persisted.Select(assignment => assignment.AssignedAt));
            path.Entries.Select(entry => entry.ExperienceQuality).Should().Equal(ExperienceQuality.Bad, ExperienceQuality.Good);
            path.PathLength.Should().Be(2);
            path.OverallQuality.Should().Be(0m);
            path.DirectionScore.Should().Be(1m);
            path.OverallQualityBand.Should().Be(OverallQualityBand.Medium);
            path.Direction.Should().Be(PathDirection.Positive);
            path.Classification.Should().Be(PathClassification.Balanced);

            var episode = result.RecoveryObservations.Single(record => record.AttendeeId == path.AttendeeId)
                .Episodes.Should().ContainSingle().Subject;
            episode.AttendeeId.Should().Be(path.AttendeeId);
            episode.StartAssignmentId.Should().Be(persisted[0].Id);
            episode.StartFestivalDayId.Should().Be(persisted[0].FestivalDayId);
            episode.StartFestivalDate.Should().Be(path.Entries[0].FestivalDate);
            episode.StartAssignedAt.Should().Be(persisted[0].AssignedAt);
            episode.RecoveryAssignmentId.Should().Be(persisted[1].Id);
            episode.RecoveryFestivalDayId.Should().Be(persisted[1].FestivalDayId);
            episode.RecoveryFestivalDate.Should().Be(path.Entries[1].FestivalDate);
            episode.RecoveryAssignedAt.Should().Be(persisted[1].AssignedAt);
            episode.Status.Should().Be(RecoveryEpisodeStatus.Recovered);
            episode.RecoveryQuality.Should().Be(ExperienceQuality.Good);
            episode.ObservedOpportunities.Should().Be(1);
            episode.RecoveryEligible.Should().BeTrue();
            episode.OpportunitiesToRecovery.Should().Be(1);
        }
        foreach (var request in occupancyRequests)
        {
            var assignments = occupancyAssignments
                .Where(assignment => assignment.AssignmentRequestId.Value == request.AssignmentRequestId).ToArray();
            assignments.Should().HaveCount(2);
            assignments.Select(assignment => assignment.ZoneId).Distinct().Should().ContainSingle();
        }

        var dayOne = result.Attempts[0].FestivalDayId;
        var dayTwo = result.Attempts[1].FestivalDayId;
        var firstDay = evaluatedAssignments.Where(assignment => assignment.FestivalDayId == dayOne).ToArray();
        var secondDay = evaluatedAssignments.Where(assignment => assignment.FestivalDayId == dayTwo).ToArray();
        firstDay.Should().HaveCount(2).And.OnlyContain(assignment => assignment.ExperienceQuality == ExperienceQuality.Bad);
        secondDay.Should().HaveCount(2).And.OnlyContain(assignment => assignment.ExperienceQuality == ExperienceQuality.Good);
        secondDay.Select(assignment => assignment.AttendeeId).Should().BeEquivalentTo(
            firstDay.Select(assignment => assignment.AttendeeId));
        foreach (var attempt in result.Attempts)
        {
            var assignments = evaluatedAssignments.Where(assignment => assignment.FestivalDayId == attempt.FestivalDayId).ToArray();
            assignments.Should().OnlyContain(assignment => assignment.AssignmentRequestId == attempt.PersistedRequestId);
            assignments.Select(assignment => assignment.ZoneId).Distinct().Should().ContainSingle();
            assignments.Select(assignment => assignment.RowCode).Distinct().Should().ContainSingle();
            assignments.Select(assignment => assignment.SpotNumber.Value).Order().Should().Equal(1, 2);
        }

        result.OpeningStates.Select(state => state.FestivalDayId).Should().Equal(dayOne, dayTwo);
        var firstOpening = result.OpeningStates[0];
        var secondOpening = result.OpeningStates[1];
        firstOpening.Zones.Should().HaveCount(3);
        firstOpening.Zones.Select(zone => ZoneExperienceQualityPolicy.GetQuality(zone.Type))
            .Should().BeEquivalentTo(new[] { ExperienceQuality.Bad, ExperienceQuality.Good, ExperienceQuality.Medium });
        firstOpening.Spots.Should().HaveCount(6);
        foreach (var zone in firstOpening.Zones)
        {
            var spots = firstOpening.Spots.Where(spot => spot.ZoneId == zone.Id).ToArray();
            spots.Should().HaveCount(2);
            spots.Select(spot => spot.RowCode).Distinct().Should().ContainSingle();
            spots.Select(spot => spot.Number.Value).Order().Should().Equal(1, 2);
        }

        // Compare identities and all physical properties before each request and
        // after both days, so adding topology between days cannot pass this test.
        secondOpening.Zones.Should().BeEquivalentTo(firstOpening.Zones);
        secondOpening.Spots.Should().BeEquivalentTo(firstOpening.Spots);
        result.Zones.Should().BeEquivalentTo(firstOpening.Zones);
        result.Spots.Should().BeEquivalentTo(firstOpening.Spots);
        var occupiedCodes = occupancyAssignments.Select(assignment => assignment.SpotCode).ToArray();
        firstOpening.AvailableSpots.Should().HaveCount(2);
        firstOpening.AvailableSpots.Select(spot => spot.Code)
            .Should().BeEquivalentTo(firstDay.Select(assignment => assignment.SpotCode));
        firstOpening.Spots.ExceptBy(occupiedCodes, spot => spot.Code).Select(spot => spot.Code)
            .Should().BeEquivalentTo(firstOpening.AvailableSpots.Select(spot => spot.Code));
        secondOpening.AvailableSpots.Should().BeEquivalentTo(firstOpening.Spots);

        // A control using the real selection policy and the real Day 2 availability:
        // the same physical Spots occupied on Day 1 are reusable on Day 2.
        // Empty history selects Medium with equal Good/Medium inventory. Thus Good
        // cannot pass merely because it is the only feasible quality or is surplus.
        var inventory = RemainingInventoryCalculator.Calculate(secondOpening.Zones, secondOpening.AvailableSpots);
        inventory.GoodRemaining.Should().Be(2);
        inventory.MediumRemaining.Should().Be(2);
        var emptyHistoryScore = GroupRotationScore.Calculate(
            Enumerable.Repeat(FairnessHistory.Create([]), 2));
        var control = AssignmentSelectionPolicy.Select(TargetQualityPolicy.Calculate(emptyHistoryScore),
            inventory, secondOpening.Zones, secondOpening.AvailableSpots, GroupSize.Create(2));
        control.Should().NotBeNull();
        control!.ExperienceQuality.Should().Be(ExperienceQuality.Medium);

        // Derive recovery only from the persisted Day 1 rows; never feed it to the runner.
        var historicalScore = GroupRotationScore.Calculate(firstDay.Select(assignment =>
            FairnessHistory.Create([assignment.ExperienceQuality])));
        historicalScore.Should().BeGreaterThanOrEqualTo(1m);
        TargetQualityPolicy.Calculate(historicalScore).Should().Be(TargetQuality.Good);
        secondDay.Should().OnlyContain(assignment => assignment.AssignedAt > firstDay[0].AssignedAt);
    }

    private static async Task VerifyEvidencePackageAsync(ScenarioDefinition definition,
        ScenarioManifest manifest, SimulationResult result, DateTimeOffset startedAt)
    {
        var root = Path.Combine(Path.GetTempPath(), $"festival-recovery-evidence-{Guid.NewGuid():N}");
        try
        {
            var metadata = SimulationRunMetadata.Create("run-001", manifest, startedAt, DateTimeOffset.UtcNow);
            var directory = await new SimulationEvidenceExporter().ExportAsync(new(definition, manifest, result, metadata, root));
            Directory.GetFiles(directory).Select(Path.GetFileName).Should()
                .BeEquivalentTo(SimulationEvidenceExporterTests.FileNames);
            foreach (var file in Directory.GetFiles(directory))
            {
                using var validJson = JsonDocument.Parse(await File.ReadAllTextAsync(file));
            }
            using var input = SimulationEvidenceExporterTests.Read(directory, "manifest.json");
            input.RootElement.GetRawText().Should().Be(JsonSerializer.Serialize(manifest, SimulationEvidenceExporter.JsonOptions));
            input.RootElement.GetProperty("setup").GetProperty("assignments").GetArrayLength().Should().Be(4);
            input.RootElement.GetProperty("dailyPlans").EnumerateArray()
                .SelectMany(day => day.GetProperty("requests").EnumerateArray())
                .Select(request => request.GetProperty("simulationRequestId").GetGuid()).Should()
                .Equal(result.Attempts.Select(attempt => attempt.SimulationRequestId));

            using var attempts = SimulationEvidenceExporterTests.Read(directory, "attempts.json");
            attempts.RootElement.GetRawText().Should().Be(JsonSerializer.Serialize(result.Attempts, SimulationEvidenceExporter.JsonOptions));
            using var checkpoints = SimulationEvidenceExporterTests.Read(directory, "checkpoints.json");
            checkpoints.RootElement.GetRawText().Should().Be(JsonSerializer.Serialize(result.DailyCheckpoints, SimulationEvidenceExporter.JsonOptions));
            using var paths = SimulationEvidenceExporterTests.Read(directory, "attendee-paths.json");
            var evaluatedIds = result.AttendeePaths.Select(path => path.AttendeeId.Value).Order().ToArray();
            paths.RootElement.EnumerateArray().Select(path => SimulationEvidenceExporterTests.Id(path, "attendeeId"))
                .Should().Equal(evaluatedIds).And.NotIntersectWith(manifest.Setup.SupportingAttendees.Select(attendee => attendee.Id.Value));
            foreach (var path in paths.RootElement.EnumerateArray())
            {
                var source = result.AttendeePaths.Single(item => item.AttendeeId.Value == SimulationEvidenceExporterTests.Id(path, "attendeeId"));
                JsonNode.DeepEquals(JsonNode.Parse(path.GetRawText()),
                    JsonSerializer.SerializeToNode(source, SimulationEvidenceExporter.JsonOptions)).Should().BeTrue();
                path.GetProperty("entries").EnumerateArray().Select(entry => entry.GetProperty("experienceQuality").GetString())
                    .Should().Equal("Bad", "Good");
            }
            using var recovery = SimulationEvidenceExporterTests.Read(directory, "recovery-observations.json");
            recovery.RootElement.EnumerateArray().Select(record => SimulationEvidenceExporterTests.Id(record, "attendeeId"))
                .Should().Equal(evaluatedIds).And.NotIntersectWith(manifest.Setup.SupportingAttendees.Select(attendee => attendee.Id.Value));
            foreach (var record in recovery.RootElement.EnumerateArray())
            {
                var source = result.RecoveryObservations.Single(item => item.AttendeeId.Value == SimulationEvidenceExporterTests.Id(record, "attendeeId"));
                JsonNode.DeepEquals(JsonNode.Parse(record.GetRawText()),
                    JsonSerializer.SerializeToNode(source, SimulationEvidenceExporter.JsonOptions)).Should().BeTrue();
                var episode = record.GetProperty("episodes")[0];
                episode.GetProperty("status").GetString().Should().Be("Recovered");
                episode.GetProperty("recoveryQuality").GetString().Should().Be("Good");
                episode.GetProperty("opportunitiesToRecovery").GetInt32().Should().Be(1);
            }
            using var metrics = SimulationEvidenceExporterTests.Read(directory, "metrics.json");
            metrics.RootElement.GetRawText().Should().Be(JsonSerializer.Serialize(result.Metrics, SimulationEvidenceExporter.JsonOptions));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
