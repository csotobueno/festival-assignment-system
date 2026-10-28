using Festival.Domain.Assignments;
using Festival.Domain.Zones;
using Festival.Simulation;
using FluentAssertions;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class MinimalSimulationTests
{
    [Fact]
    public async Task RunAsync_RecoversFromPersistedBadHistoryAcrossFestivalDays()
    {
        var result = await MinimalSimulationRunner.RunAsync();

        result.Attempts.Should().HaveCount(2);
        result.Attempts.Select(attempt => attempt.AttemptSequence).Should().Equal(1, 2);
        result.Attempts.Should().OnlyContain(attempt =>
            attempt.Outcome == SimulationAttemptOutcome.Successful && attempt.GroupSize == 2
            && attempt.PersistedRequestId.HasValue && attempt.Diagnostic == null);
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
        var occupancyAssignments = result.Assignments
            .Where(assignment => !evaluatedRequestIds.Contains(assignment.AssignmentRequestId.Value)).ToArray();
        occupancyAssignments.Should().HaveCount(4).And.OnlyContain(assignment =>
            assignment.FestivalDayId == result.Attempts[0].FestivalDayId
            && assignment.ExperienceQuality != ExperienceQuality.Bad
            && !evaluatedAssignments.Select(item => item.AttendeeId).Contains(assignment.AttendeeId));
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
}
