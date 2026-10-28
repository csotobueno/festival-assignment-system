using System.Collections.Immutable;
using System.Globalization;
using Festival.Domain.Assignments;
using Festival.Simulation;
using FluentAssertions;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class ScenarioMaterializerTests
{
    [Fact]
    public void Materialize_SameDefinitionSeedAndVersionReproduceAllExecutableInputs()
    {
        var materializer = new ScenarioMaterializer();

        var first = materializer.Materialize(MinimalRecoveryScenarioDefinition.Create());
        var second = materializer.Materialize(MinimalRecoveryScenarioDefinition.Create());

        // Recursive comparison includes IDs, timestamps, topology, setup graphs,
        // ordered members, eligibility and explicit request sequences.
        second.Should().BeEquivalentTo(first, options => options.WithStrictOrdering());
        first.Metadata.Should().Be(new ScenarioMetadata("minimal-recovery", 0, "v1",
            SimulationExecutionMode.Sequential, 1));
        first.Catalog.FestivalDays.Should().HaveCount(2);
        first.Catalog.Attendees.Should().HaveCount(2);
        first.Catalog.Zones.Should().HaveCount(3);
        first.Catalog.Spots.Should().HaveCount(6);
        first.DailyPlans.SelectMany(day => day.Requests).Select(request => request.Sequence)
            .Should().Equal(1, 2);
    }

    [Fact]
    public void Materialize_V1IdentifiersRemainStableAcrossSeedAndCultureChanges()
    {
        var definition = MinimalRecoveryScenarioDefinition.Create();
        var materializer = new ScenarioMaterializer();
        var first = materializer.Materialize(definition);
        var previousCulture = CultureInfo.CurrentCulture;
        ScenarioManifest second;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            second = materializer.Materialize(definition with { Seed = 42 });
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }

        second.Metadata.Seed.Should().Be(42);
        (second with { Metadata = first.Metadata }).Should().BeEquivalentTo(first,
            options => options.WithStrictOrdering());
        // Fixed v1 vectors prevent accidental cross-run ID procedure changes.
        first.Catalog.Attendees[0].Id.Value.Should().Be(Guid.Parse("c65390b2-c5a1-fceb-0025-2d5add3f63e8"));
        first.Catalog.FestivalDays[0].Id.Value.Should().Be(Guid.Parse("5af9e77b-e376-c521-5222-554ebcc56c53"));
        first.Catalog.Zones.Single(zone => zone.Name == "GOOD").Id.Value.Should()
            .Be(Guid.Parse("7cf1d6c8-0366-b07c-664a-474854d903f9"));
        first.Setup.CompletedRequests[0].Id.Value.Should().Be(Guid.Parse("50c2ebe4-f283-fb11-a033-d2d20c3a226f"));
        first.DailyPlans[0].Requests[0].SimulationRequestId.Should().Be(Guid.Parse("95afa3dd-1029-bec3-5eec-46b11672089a"));
    }

    [Fact]
    public void Materialize_UsesExplicitDayAndRequestOrderWhilePreservingMemberOrderAndEligibility()
    {
        var definition = MinimalRecoveryScenarioDefinition.Create();
        definition = definition with
        {
            AttendeeCount = 4,
            Days =
            [
                definition.Days[1] with
                {
                    Requests =
                    [
                        new(30, [2, 1], new RequestEligibility(false)),
                        new(20, [3, 4], new RequestEligibility(true))
                    ]
                },
                definition.Days[0] with { Requests = [new(10, [1, 2], new RequestEligibility(true))] }
            ]
        };

        var manifest = new ScenarioMaterializer().Materialize(definition);
        var reordered = new ScenarioMaterializer().Materialize(definition with
        {
            Topology = definition.Topology.Reverse().ToImmutableArray(),
            Days = definition.Days.Reverse().Select(day => day with
            {
                Requests = day.Requests.Reverse().ToImmutableArray(),
                OccupiedBlockKeys = day.OccupiedBlockKeys.Reverse().ToImmutableArray()
            }).ToImmutableArray()
        });

        reordered.Should().BeEquivalentTo(manifest, options => options.WithStrictOrdering());
        manifest.DailyPlans.Select(day => day.DayNumber).Should().Equal(1, 2);
        manifest.DailyPlans.SelectMany(day => day.Requests).Select(request => request.Sequence)
            .Should().Equal(10, 20, 30);
        var requests = manifest.DailyPlans[1].Requests;
        requests[1].Members.Select(member => member.Code.Value).Should().Equal("SIM-002", "SIM-001");
        requests[1].Eligibility.AllowsFrontStanding.Should().BeFalse();
        requests[0].RequestedAt.Should().Be(new DateTimeOffset(2026, 8, 16, 14, 0, 0, TimeSpan.Zero));
        requests[1].RequestedAt.Should().Be(new DateTimeOffset(2026, 8, 16, 14, 2, 0, TimeSpan.Zero));
        requests[1].AssignedAt.Should().Be(requests[1].RequestedAt.AddMinutes(1));
    }

    [Fact]
    public void Materialize_SeparatesSupportingOccupancyFromEvaluatedAttendeesAndWorkload()
    {
        var manifest = new ScenarioMaterializer().Materialize(MinimalRecoveryScenarioDefinition.Create());
        var evaluatedIds = manifest.Catalog.Attendees.Select(attendee => attendee.Id).ToArray();

        manifest.Setup.SupportingAttendees.Should().HaveCount(4)
            .And.NotContain(attendee => evaluatedIds.Contains(attendee.Id));
        manifest.Setup.CompletedRequests.Should().HaveCount(2);
        manifest.Setup.Assignments.Should().HaveCount(4).And.OnlyContain(assignment =>
            assignment.FestivalDayId == manifest.DailyPlans[0].FestivalDayId
            && assignment.ExperienceQuality != ExperienceQuality.Bad
            && !evaluatedIds.Contains(assignment.AttendeeId));
        manifest.Setup.CompletedRequests.Select(request => request.RequestedAt).Should().Equal(
            new DateTimeOffset(2026, 8, 15, 13, 1, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 15, 13, 2, 0, TimeSpan.Zero));
        foreach (var request in manifest.Setup.CompletedRequests)
        {
            request.CompletedAt.Should().Be(request.RequestedAt.AddSeconds(10));
            manifest.Setup.Assignments.Where(assignment => assignment.AssignmentRequestId == request.Id)
                .Should().HaveCount(request.AttendeeCodes.Length);
        }
        manifest.DailyPlans.SelectMany(day => day.Requests).Should().HaveCount(2)
            .And.OnlyContain(request => request.Members.All(member => evaluatedIds.Contains(member.Id)));
    }

    [Fact]
    public void Materialize_DerivesAllBusinessDatesFromDefinition()
    {
        var definition = MinimalRecoveryScenarioDefinition.Create();
        var materializer = new ScenarioMaterializer();
        var original = materializer.Materialize(definition);
        var shifted = materializer.Materialize(definition with { FirstDate = definition.FirstDate.AddDays(7) });

        shifted.Catalog.FestivalDays.Select(day => day.Date).Should().Equal(
            original.Catalog.FestivalDays.Select(day => day.Date.AddDays(7)));
        shifted.Setup.CompletedRequests.Select(request => request.RequestedAt).Should().Equal(
            original.Setup.CompletedRequests.Select(request => request.RequestedAt.AddDays(7)));
        shifted.Setup.Assignments.Select(assignment => assignment.AssignedAt).Should().Equal(
            original.Setup.Assignments.Select(assignment => assignment.AssignedAt.AddDays(7)));
        shifted.DailyPlans.SelectMany(day => day.Requests).Select(request => request.RequestedAt).Should().Equal(
            original.DailyPlans.SelectMany(day => day.Requests).Select(request => request.RequestedAt.AddDays(7)));
        shifted.DailyPlans.SelectMany(day => day.Requests).Select(request => request.AssignedAt).Should().Equal(
            original.DailyPlans.SelectMany(day => day.Requests).Select(request => request.AssignedAt.AddDays(7)));
    }

    [Fact]
    public void Materialize_RejectsUnsupportedGeneratorVersion()
    {
        var definition = MinimalRecoveryScenarioDefinition.Create() with { GeneratorVersion = "v2" };

        var materialize = () => new ScenarioMaterializer().Materialize(definition);

        materialize.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Materialize_RejectsUnsupportedConcurrencyLevel(int concurrencyLevel)
    {
        var definition = MinimalRecoveryScenarioDefinition.Create() with { ConcurrencyLevel = concurrencyLevel };

        var materialize = () => new ScenarioMaterializer().Materialize(definition);

        materialize.Should().Throw<NotSupportedException>();
    }
}
