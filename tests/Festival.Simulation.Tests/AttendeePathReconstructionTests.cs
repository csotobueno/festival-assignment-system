using System.Collections.Immutable;
using System.Data.Common;
using Festival.Application.Assignments.Ports;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Infrastructure;
using Festival.Infrastructure.Persistence;
using Festival.Simulation;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class AttendeePathReconstructionTests
{
    [Fact]
    public async Task ReconstructAsync_ReadsPersistedEvidenceForWorkloadInOneUntrackedOrderedQuery()
    {
        var definition = MinimalRecoveryScenarioDefinition.Create() with
        {
            AttendeeCount = 4,
            Days =
            [
                new(1, ["good", "medium"], [new(1, [1, 2, 3], new RequestEligibility(true))]),
                new(2, [], []),
                new(3, [], []),
                new(4, [], [])
            ]
        };
        var materialized = new ScenarioMaterializer().Materialize(definition);
        var manifest = materialized with
        {
            // Day 4 is outside the evaluated period; its persisted history must
            // not enter paths. Catalog dates deliberately differ from PostgreSQL.
            DailyPlans = materialized.DailyPlans.Take(3).ToImmutableArray(),
            Catalog = materialized.Catalog with
            {
                FestivalDays = materialized.Catalog.FestivalDays.Select(day =>
                    FestivalDay.Create(day.Id, day.Date.AddDays(10), day.AssignmentWindow)).ToImmutableArray()
            }
        };
        await using var container = new PostgreSqlBuilder("postgres:16.3-alpine").Build();
        await container.StartAsync();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:FestivalDatabase"] = container.GetConnectionString()
        }).Build();
        var registrations = new ServiceCollection();
        registrations.AddPostgreSqlPersistence(configuration);
        await using var services = registrations.BuildServiceProvider();
        var attendees = materialized.Catalog.Attendees;
        var days = materialized.Catalog.FestivalDays;
        var timestamp = new DateTimeOffset(2026, 8, 15, 14, 0, 0, TimeSpan.Zero);
        Assignment[] persisted;
        await using (var setup = services.CreateAsyncScope())
        {
            var database = setup.ServiceProvider.GetRequiredService<FestivalDbContext>();
            await database.Database.MigrateAsync();
            database.Attendees.AddRange(attendees);
            database.Attendees.AddRange(materialized.Setup.SupportingAttendees);
            database.FestivalDays.AddRange(days);
            database.Zones.AddRange(materialized.Catalog.Zones);
            database.Spots.AddRange(materialized.Catalog.Spots);
            var repository = setup.ServiceProvider.GetRequiredService<IAssignmentRequestRepository>();
            foreach (var supporting in materialized.Setup.CompletedRequests)
            {
                var request = AssignmentRequest.Create(supporting.Id, supporting.FestivalDayId,
                    supporting.AttendeeCodes, supporting.RequestedAt, supporting.Eligibility);
                request.Complete(supporting.CompletedAt);
                await repository.AddAsync(request);
            }
            database.Assignments.AddRange(materialized.Setup.Assignments);

            // Insert out of order. The oldest timestamp has the largest ID;
            // the tied timestamps must order by ID, independently of day/date.
            persisted =
            [
                await AddAssignmentAsync(attendees[0], days[1], 2, timestamp.AddHours(1), ExperienceQuality.Good),
                await AddAssignmentAsync(attendees[0], days[0], 3, timestamp, ExperienceQuality.Bad),
                await AddAssignmentAsync(attendees[0], days[2], 1, timestamp.AddHours(1), ExperienceQuality.Medium),
                await AddAssignmentAsync(attendees[0], days[3], 4, timestamp.AddDays(-1), ExperienceQuality.Bad),
                await AddAssignmentAsync(attendees[1], days[1], 5, timestamp, ExperienceQuality.Medium)
            ];
            await database.SaveChangesAsync();

            async Task<Assignment> AddAssignmentAsync(Attendee attendee, FestivalDay day,
                int id, DateTimeOffset assignedAt, ExperienceQuality quality)
            {
                var request = AssignmentRequest.Create(AssignmentRequestId.Create(
                        Guid.Parse($"10000000-0000-0000-0000-{id:D12}")), day.Id, [attendee.Code],
                    assignedAt.AddMinutes(-1), new RequestEligibility(true));
                request.Complete(assignedAt);
                await repository.AddAsync(request);
                // Separate Spots also respect unique occupancy for the singleton.
                var spot = materialized.Catalog.Spots[id == 5 ? 1 : 0];
                var assignment = Assignment.Create(AssignmentId.Create(
                        Guid.Parse($"00000000-0000-0000-0000-{id:D12}")), request.Id, day.Id, attendee.Id,
                    spot.Code, spot.ZoneId, spot.RowCode, spot.Number, quality, assignedAt);
                database.Assignments.Add(assignment);
                return assignment;
            }
        }

        var reads = new ReadCounter();
        var options = new DbContextOptionsBuilder<FestivalDbContext>()
            .UseNpgsql(container.GetConnectionString()).AddInterceptors(reads).Options;
        await using var observation = new FestivalDbContext(options);

        // No attempt/Application results are provided: persisted rows alone
        // determine real experiences, including their quality and dates.
        var paths = await AttendeePathReconstructor.ReconstructAsync(observation, manifest);

        reads.Count.Should().Be(1);
        observation.ChangeTracker.Entries().Should().BeEmpty();
        paths.Select(path => path.AttendeeId).Should().BeEquivalentTo(attendees.Take(3).Select(attendee => attendee.Id));
        paths.Should().NotContain(path => materialized.Setup.SupportingAttendees.Any(attendee => attendee.Id == path.AttendeeId));
        var path = paths.Single(path => path.AttendeeId == attendees[0].Id);
        var expected = new[] { persisted[1], persisted[2], persisted[0] };
        path.Entries.Should().Equal(expected.Select(assignment => new AttendeePathEntry(assignment.Id,
            assignment.FestivalDayId, days.Single(day => day.Id == assignment.FestivalDayId).Date,
            assignment.AssignedAt, assignment.ExperienceQuality)));
        path.Entries.Select(entry => entry.FestivalDayId).Should().Equal(days[0].Id, days[2].Id, days[1].Id);
        path.Entries.Select(entry => entry.ExperienceQuality).Should().Equal(
            ExperienceQuality.Bad, ExperienceQuality.Medium, ExperienceQuality.Good);
        path.DirectionScore.Should().Be(0.60m);
        path.Classification.Should().Be(PathClassification.Balanced);
        paths.Single(path => path.AttendeeId == attendees[1].Id).Classification
            .Should().Be(PathClassification.InsufficientEvidence);
        var empty = paths.Single(path => path.AttendeeId == attendees[2].Id);
        empty.PathLength.Should().Be(0);
        empty.Classification.Should().BeNull();

        // A catalog without evaluated workload creates no fairness population.
        var noWorkload = await AttendeePathReconstructor.ReconstructAsync(observation,
            manifest with { DailyPlans = [] });
        noWorkload.Should().BeEmpty();
        reads.Count.Should().Be(1);
    }

    private sealed class ReadCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return ValueTask.FromResult(result);
        }
    }
}
