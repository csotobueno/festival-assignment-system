using System.Globalization;
using System.Collections.Immutable;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.Zones;
using Festival.Simulation;
using FluentAssertions;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class RepresentativeScenarioMaterializerTests
{
    private static readonly DateOnly FirstDate = new(2026, 8, 15);

    internal static ScenarioDefinition Definition(bool baseline, int seed = 42) => baseline
        ? S2ExpectedBaselineScenarioDefinition.Create(seed, FirstDate)
        : S1BestPlausibleScenarioDefinition.Create(seed, FirstDate);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Definitions_RecordTheDocumentedWorkloadProfiles(bool baseline)
    {
        var definition = Definition(baseline);
        var workload = definition.Workload!;

        definition.AttendeeCount.Should().Be(5500);
        definition.GeneratorVersion.Should().Be("v2");
        definition.ExecutionMode.Should().Be(SimulationExecutionMode.Sequential);
        definition.ConcurrencyLevel.Should().Be(1);
        workload.DailyRequesterCount.Should().Be(baseline ? 4400 : 3300);
        workload.DailyParticipationContinuity.Should().Be(.90m);
        workload.AllowsFrontStandingRequestShare.Should().Be(.50m);
        workload.GroupSizeProfile.Select(band => (band.MinSize, band.MaxSize, band.Percentage)).Should().Equal(baseline
            ? [(1, 1, .15m), (2, 2, .30m), (3, 3, .25m), (4, 4, .15m), (5, 5, .08m), (6, 7, .05m), (8, 10, .02m)]
            : [(1, 1, .25m), (2, 2, .35m), (3, 3, .25m), (4, 4, .10m), (5, 10, .05m)]);
        workload.ArrivalProfile.Select(band => (band.Start.Hour, band.End.Hour, band.Percentage)).Should().Equal(baseline
            ? [(7, 9, .10m), (9, 12, .40m), (12, 15, .15m), (15, 16, .20m), (16, 18, .07m), (18, 21, .05m), (21, 23, .03m)]
            : [(7, 9, .15m), (9, 12, .25m), (12, 15, .20m), (15, 18, .20m), (18, 21, .15m), (21, 23, .05m)]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Materialize_SameDefinitionSeedAndVersionReproduceExactManifestInputs(bool baseline)
    {
        var materializer = new ScenarioMaterializer();
        var first = materializer.Materialize(Definition(baseline));
        var previousCulture = CultureInfo.CurrentCulture;
        ScenarioManifest second;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            var definition = Definition(baseline);
            second = materializer.Materialize(definition with
            {
                Topology = definition.Topology.Reverse().ToImmutableArray(),
                Days = definition.Days.Reverse().ToImmutableArray()
            });
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }

        second.Metadata.Should().Be(first.Metadata);
        second.Catalog.Attendees.Select(attendee => (attendee.Id, attendee.Code, attendee.Name))
            .Should().Equal(first.Catalog.Attendees.Select(attendee => (attendee.Id, attendee.Code, attendee.Name)));
        second.Catalog.FestivalDays.Select(day => (day.Id, day.Date, day.AssignmentWindow))
            .Should().Equal(first.Catalog.FestivalDays.Select(day => (day.Id, day.Date, day.AssignmentWindow)));
        second.Catalog.Zones.Select(zone => (zone.Id, zone.Type, zone.Name))
            .Should().Equal(first.Catalog.Zones.Select(zone => (zone.Id, zone.Type, zone.Name)));
        second.Catalog.Spots.Select(spot => (spot.Code, spot.ZoneId, spot.RowCode, spot.Number))
            .Should().Equal(first.Catalog.Spots.Select(spot => (spot.Code, spot.ZoneId, spot.RowCode, spot.Number)));
        second.Setup.Should().BeEquivalentTo(first.Setup);
        second.DailyPlans.Select(day => (day.DayNumber, day.FestivalDayId)).Should()
            .Equal(first.DailyPlans.Select(day => (day.DayNumber, day.FestivalDayId)));
        var expected = first.DailyPlans.SelectMany(day => day.Requests).ToArray();
        var actual = second.DailyPlans.SelectMany(day => day.Requests).ToArray();
        actual.Select(request => (request.SimulationRequestId, request.Sequence, request.RequestedAt,
                request.AssignedAt, request.Eligibility)).Should().Equal(expected.Select(request =>
                (request.SimulationRequestId, request.Sequence, request.RequestedAt, request.AssignedAt, request.Eligibility)));
        actual.Select(request => request.Members.Length).Should().Equal(expected.Select(request => request.Members.Length));
        actual.SelectMany(request => request.Members.Select(member => member.Id)).Should()
            .Equal(expected.SelectMany(request => request.Members.Select(member => member.Id)));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 42)]
    [InlineData(false, -1)]
    [InlineData(true, 0)]
    [InlineData(true, 42)]
    [InlineData(true, -1)]
    public void Materialize_AllFiveDaysHaveExactUniqueMembershipAndRequesterContinuity(bool baseline, int seed)
    {
        var manifest = new ScenarioMaterializer().Materialize(Definition(baseline, seed));
        var count = baseline ? 4400 : 3300;
        var retainedCount = baseline ? 3960 : 2970;
        var registered = manifest.Catalog.Attendees.Select(attendee => attendee.Id).ToHashSet();
        var seen = new HashSet<Guid>();
        HashSet<Guid>? previous = null;
        var hasReturningReplacement = false;

        manifest.Catalog.Attendees.Should().HaveCount(5500);
        registered.Should().HaveCount(5500);
        manifest.Catalog.Attendees.Select(attendee => attendee.Code).Should().OnlyHaveUniqueItems();
        manifest.DailyPlans.Should().HaveCount(5);
        manifest.Setup.SupportingAttendees.Should().BeEmpty();
        manifest.Setup.CompletedRequests.Should().BeEmpty();
        manifest.Setup.Assignments.Should().BeEmpty();
        foreach (var day in manifest.DailyPlans)
        {
            var members = day.Requests.SelectMany(request => request.Members).ToArray();
            members.Should().HaveCount(count).And.OnlyContain(member => registered.Contains(member.Id));
            members.Select(member => member.Id).Should().OnlyHaveUniqueItems();
            day.Requests.Should().OnlyContain(request => request.Members.Length >= 1 && request.Members.Length <= 10);
            var current = members.Select(member => member.Id.Value).ToHashSet();
            if (previous is not null)
            {
                current.Intersect(previous).Should().HaveCount(retainedCount);
                var replacements = current.Except(previous).ToArray();
                replacements.Should().HaveCount(count - retainedCount)
                    .And.NotContain(id => previous.Contains(id));
                hasReturningReplacement |= replacements.Any(seen.Contains);
            }
            seen.UnionWith(current);
            previous = current;
        }
        hasReturningReplacement.Should().BeTrue("replacement selection permits earlier-day attendees to return");
        manifest.DailyPlans.SelectMany(day => day.Requests).Select(request => request.SimulationRequestId)
            .Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Materialize_GroupAndArrivalSharesMatchTheirRequestLevelTargets(bool baseline)
    {
        var definition = Definition(baseline);
        var profile = definition.Workload!;
        var manifest = new ScenarioMaterializer().Materialize(definition);
        var sequence = 0;

        foreach (var day in manifest.DailyPlans)
        {
            var requests = day.Requests;
            requests.Should().HaveCount(baseline ? 1464 : 1333);
            var festivalDay = manifest.Catalog.FestivalDays.Single(value => value.Id == day.FestivalDayId);
            foreach (var band in profile.GroupSizeProfile)
            {
                var actual = requests.Count(request => request.Members.Length >= band.MinSize
                    && request.Members.Length <= band.MaxSize);
                // These representative populations can reconcile inside ranged buckets.
                // Hamilton allocation then differs from each ideal request quota by < 1 group.
                Math.Abs(actual - requests.Length * band.Percentage).Should().BeLessThan(1m);
            }

            var counts = profile.ArrivalProfile.Select(band => requests.Count(request =>
                TimeOnly.FromDateTime(request.RequestedAt.UtcDateTime) >= band.Start
                && TimeOnly.FromDateTime(request.RequestedAt.UtcDateTime) < band.End)).ToArray();
            counts.Sum().Should().Be(requests.Length);
            counts.Should().Equal(baseline
                ? [146, 586, 220, 293, 102, 73, 44]
                : [200, 333, 267, 266, 200, 67]);
            for (var band = 0; band < counts.Length; band++)
                Math.Abs(counts[band] - requests.Length * profile.ArrivalProfile[band].Percentage)
                    .Should().BeLessThan(1m);
            if (baseline)
            {
                counts[1].Should().Be(counts.Max(), "09:00–12:00 is the primary peak");
                counts[3].Should().BeGreaterThan(counts.Where((_, index) => index != 1 && index != 3).Max(),
                    "15:00–16:00 is the secondary peak");
            }
            requests.Select(request => request.SimulationRequestId).Should().Equal(requests
                .OrderBy(request => request.RequestedAt).ThenBy(request => request.SimulationRequestId)
                .Select(request => request.SimulationRequestId));
            foreach (var request in requests)
            {
                request.Sequence.Should().Be(++sequence, "the existing runner processes Sequence order");
                request.RequestedAt.Offset.Should().Be(TimeSpan.Zero);
                DateOnly.FromDateTime(request.RequestedAt.UtcDateTime).Should().Be(festivalDay.Date);
                festivalDay.AssignmentWindow.Contains(TimeOnly.FromDateTime(request.RequestedAt.UtcDateTime)).Should().BeTrue();
                request.AssignedAt.Should().Be(request.RequestedAt);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Materialize_HalfOfEachDaysRequestsAllowFrontStandingWithDeclaredOrderRemainder(bool baseline)
    {
        var manifest = new ScenarioMaterializer().Materialize(Definition(baseline));

        foreach (var day in manifest.DailyPlans)
        {
            // S1 has 1,333 requests: the tied remainder goes to the first (allowed) bucket.
            // S2 has 1,464 requests, so both buckets have exactly 732 requests.
            day.Requests.Count(request => request.Eligibility.AllowsFrontStanding).Should().Be(baseline ? 732 : 667);
            day.Requests.Count(request => !request.Eligibility.AllowsFrontStanding).Should().Be(baseline ? 732 : 666);
        }
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    public void Materialize_EligibilityShareDoesNotChangeMembershipGroupsContinuityOrArrivals(bool baseline, int share)
    {
        var definition = Definition(baseline);
        var materializer = new ScenarioMaterializer();
        var expected = materializer.Materialize(definition);
        var actual = materializer.Materialize(definition with
        {
            Workload = definition.Workload! with { AllowsFrontStandingRequestShare = share }
        });

        foreach (var (expectedDay, actualDay) in expected.DailyPlans.Zip(actual.DailyPlans))
        {
            var members = actualDay.Requests.SelectMany(request => request.Members).ToArray();
            members.Should().HaveCount(baseline ? 4400 : 3300);
            members.Select(member => member.Id).Should().OnlyHaveUniqueItems();
            actualDay.Requests.Should().OnlyContain(request => request.Eligibility.AllowsFrontStanding == (share == 1));
            actualDay.Requests.Select(request => (request.SimulationRequestId, request.Sequence,
                    request.RequestedAt, request.AssignedAt, request.Members.Length)).Should()
                .Equal(expectedDay.Requests.Select(request => (request.SimulationRequestId, request.Sequence,
                    request.RequestedAt, request.AssignedAt, request.Members.Length)));
            members.Select(member => (member.Id, member.Code, member.Name)).Should().Equal(expectedDay.Requests
                .SelectMany(request => request.Members.Select(member => (member.Id, member.Code, member.Name))));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Materialize_SameAttendeeCanReproduciblyJoinRequestsWithDifferentEligibilityAcrossDays(bool baseline)
    {
        var materializer = new ScenarioMaterializer();
        var first = materializer.Materialize(Definition(baseline));
        var second = materializer.Materialize(Definition(baseline));

        var occurrences = first.DailyPlans.SelectMany(day => day.Requests.SelectMany(request =>
            request.Members.Select(member => (day.DayNumber, Member: member, request.Eligibility.AllowsFrontStanding))));
        var changingAttendee = occurrences.GroupBy(value => value.Member.Id).First(group =>
            group.Select(value => value.AllowsFrontStanding).Distinct().Count() == 2).ToArray();
        changingAttendee.Select(value => value.DayNumber).Should().OnlyHaveUniqueItems();
        changingAttendee.Should().OnlyContain(value => ReferenceEquals(value.Member, changingAttendee[0].Member),
            "the same catalog attendee participates with eligibility held only by each daily request");
        second.DailyPlans.SelectMany(day => day.Requests.Where(request =>
                request.Members.Any(member => member.Id == changingAttendee[0].Member.Id))
            .Select(request => (day.DayNumber, request.Eligibility.AllowsFrontStanding))).Should()
            .Equal(changingAttendee.Select(value => (value.DayNumber, value.AllowsFrontStanding)));
    }

    [Fact]
    public void Attendee_DoesNotExposeRequestEligibilityState()
    {
        typeof(Attendee).GetProperties().Should().NotContain(property =>
            property.Name == nameof(RequestEligibility.AllowsFrontStanding)
            || property.PropertyType == typeof(RequestEligibility));
    }

    [Fact]
    public void Materialize_BothScenariosShareExactCatalogAndProgressiveRowTopology()
    {
        var first = new ScenarioMaterializer().Materialize(Definition(false));
        var second = new ScenarioMaterializer().Materialize(Definition(true, seed: 9));
        first.Catalog.Spots.Should().HaveCount(4998);
        first.Catalog.Spots.Select(spot => spot.Code).Should().OnlyHaveUniqueItems();
        second.Catalog.Spots.Select(spot => (spot.Code, spot.ZoneId, spot.RowCode, spot.Number)).Should()
            .Equal(first.Catalog.Spots.Select(spot => (spot.Code, spot.ZoneId, spot.RowCode, spot.Number)));
        second.Catalog.Zones.Select(zone => (zone.Id, zone.Type)).Should()
            .Equal(first.Catalog.Zones.Select(zone => (zone.Id, zone.Type)));
        second.Catalog.Attendees.Select(attendee => attendee.Id).Should()
            .Equal(first.Catalog.Attendees.Select(attendee => attendee.Id));
        first.Catalog.FestivalDays.Select(day => day.Date).Should()
            .Equal(Enumerable.Range(0, 5).Select(offset => FirstDate.AddDays(offset)));
        var capacities = new Dictionary<ZoneType, (int Total, int Rows, int Start)>
        {
            [ZoneType.FrontStanding] = (750, 10, 75), [ZoneType.MiddleCenter] = (1002, 12, 78),
            [ZoneType.MiddleLeft] = (750, 12, 57), [ZoneType.MiddleRight] = (750, 12, 57),
            [ZoneType.UpperCenter] = (750, 12, 57), [ZoneType.UpperLeft] = (498, 12, 36),
            [ZoneType.UpperRight] = (498, 12, 36)
        };
        first.Catalog.Zones.Should().HaveCount(7);
        // Fixed v2 vectors protect the shared identity procedure across future edits.
        first.Catalog.Attendees[0].Id.Value.Should().Be(Guid.Parse("af92ae0b-d3a0-eb3e-0acd-45fafd60999f"));
        first.Catalog.Zones.Single(zone => zone.Type == ZoneType.FrontStanding).Id.Value.Should()
            .Be(Guid.Parse("56131625-1456-bdce-b1d0-82fa0c66bb12"));
        foreach (var zone in first.Catalog.Zones)
        {
            var expected = capacities[zone.Type];
            var spots = first.Catalog.Spots.Where(spot => spot.ZoneId == zone.Id).ToArray();
            spots.Should().HaveCount(expected.Total);
            var rows = spots.GroupBy(spot => spot.RowCode.Value).OrderBy(row => row.Key, StringComparer.Ordinal).ToArray();
            rows.Should().HaveCount(expected.Rows);
            rows.Select(row => row.Count()).Should().Equal(Enumerable.Range(0, expected.Rows)
                .Select(row => expected.Start + (zone.Type == ZoneType.FrontStanding ? 0 : row)));
            foreach (var row in rows)
                row.Select(spot => spot.Number.Value).Should().Equal(Enumerable.Range(1, row.Count()));
        }
        first.Catalog.Spots.GroupBy(spot => ZoneExperienceQualityPolicy.GetQuality(
            first.Catalog.Zones.Single(zone => zone.Id == spot.ZoneId).Type))
            .ToDictionary(group => group.Key, group => group.Count()).Should().BeEquivalentTo(
                new Dictionary<ExperienceQuality, int>
                { [ExperienceQuality.Good] = 1752, [ExperienceQuality.Medium] = 2250, [ExperienceQuality.Bad] = 996 });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Materialize_DifferentSeedsChangeConcreteWorkloadWhilePreservingCatalog(bool baseline)
    {
        var materializer = new ScenarioMaterializer();
        var first = materializer.Materialize(Definition(baseline, 0));
        var second = materializer.Materialize(Definition(baseline, 1));
        second.Catalog.Attendees.Select(member => member.Id).Should().Equal(first.Catalog.Attendees.Select(member => member.Id));
        second.DailyPlans[0].Requests.Select(request => request.RequestedAt).Should()
            .NotEqual(first.DailyPlans[0].Requests.Select(request => request.RequestedAt));
        second.DailyPlans[0].Requests.SelectMany(request => request.Members.Select(member => member.Id)).Should()
            .NotEqual(first.DailyPlans[0].Requests.SelectMany(request => request.Members.Select(member => member.Id)));
    }
}
