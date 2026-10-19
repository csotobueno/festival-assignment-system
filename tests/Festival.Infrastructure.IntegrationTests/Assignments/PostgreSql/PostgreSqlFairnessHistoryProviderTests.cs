using System.Data.Common;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Infrastructure.Assignments.PostgreSql;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using Festival.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Festival.Infrastructure.IntegrationTests.Assignments.PostgreSql;

public sealed class PostgreSqlFairnessHistoryProviderTests(PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    private static readonly AttendeeId ThirdAttendeeId = AttendeeId.Create(
        Guid.Parse("30000000-0000-0000-0000-000000000003"));

    [Fact]
    public async Task GetHistoriesAsync_ShouldReturnEmptyHistoryForEachRequestedAttendeeWithoutOutcomes()
    {
        await using var context = Fixture.CreateDbContext();
        var provider = new PostgreSqlFairnessHistoryProvider(context);

        var histories = await provider.GetHistoriesAsync(
            [IntegrationTestData.FirstAttendeeId, IntegrationTestData.SecondAttendeeId]);

        histories.Keys.Should().BeEquivalentTo(new[]
            { IntegrationTestData.FirstAttendeeId, IntegrationTestData.SecondAttendeeId });
        histories.Values.Should().OnlyContain(history => history.Experiences.Count == 0);
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task GetHistoriesAsync_ShouldBatchRecordedChronologicalHistoriesWithTechnicalTiesAndEmptyMembers()
    {
        await SeedRecordedOutcomesAsync();
        var counter = new QueryCounter();
        var options = new DbContextOptionsBuilder<FestivalDbContext>()
            .UseNpgsql(Fixture.ConnectionString).AddInterceptors(counter).Options;
        await using var context = new FestivalDbContext(options);
        var provider = new PostgreSqlFairnessHistoryProvider(context);

        var histories = await provider.GetHistoriesAsync(
            [ThirdAttendeeId, IntegrationTestData.SecondAttendeeId, IntegrationTestData.FirstAttendeeId]);

        histories[IntegrationTestData.FirstAttendeeId].Experiences.Should().Equal(
            ExperienceQuality.Bad, ExperienceQuality.Medium, ExperienceQuality.Good);
        histories[IntegrationTestData.SecondAttendeeId].Experiences.Should().Equal(ExperienceQuality.Good);
        histories[ThirdAttendeeId].Experiences.Should().BeEmpty();
        histories.Should().HaveCount(3);
        counter.ReadCount.Should().Be(1, "all requested members are read in one batch without Zone queries");
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task GetHistoriesAsync_ShouldReturnNoHistoriesForEmptyRequestWithoutQuerying()
    {
        var counter = new QueryCounter();
        var options = new DbContextOptionsBuilder<FestivalDbContext>()
            .UseNpgsql(Fixture.ConnectionString).AddInterceptors(counter).Options;
        await using var context = new FestivalDbContext(options);

        var histories = await new PostgreSqlFairnessHistoryProvider(context).GetHistoriesAsync([]);

        histories.Should().BeEmpty();
        counter.ReadCount.Should().Be(0);
    }

    [Fact]
    public async Task GetHistoriesAsync_ShouldHonorCancellation()
    {
        await using var context = Fixture.CreateDbContext();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var read = () => new PostgreSqlFairnessHistoryProvider(context).GetHistoriesAsync(
            [IntegrationTestData.FirstAttendeeId], cancellation.Token);

        await read.Should().ThrowAsync<OperationCanceledException>();
    }

    private async Task SeedRecordedOutcomesAsync()
    {
        await using var context = Fixture.CreateDbContext();
        context.AddRange(IntegrationTestData.CreateZone(), IntegrationTestData.CreateSpot(),
            IntegrationTestData.CreateSpot("FR-A-002", "A", 2),
            IntegrationTestData.CreateAttendee(),
            IntegrationTestData.CreateAttendee(IntegrationTestData.SecondAttendeeId, "ATT-002"),
            IntegrationTestData.CreateAttendee(ThirdAttendeeId, "ATT-003"));
        var qualities = new[] { ExperienceQuality.Bad, ExperienceQuality.Medium, ExperienceQuality.Good };
        foreach (var index in new[] { 2, 0, 1 })
        {
            var dayId = FestivalDayId.Create(Guid.Parse($"10000000-0000-0000-0000-{index + 1:000000000000}"));
            var requestId = AssignmentRequestId.Create(Guid.Parse($"40000000-0000-0000-0000-{index + 1:000000000000}"));
            var assignedAt = IntegrationTestData.AssignedAt.AddDays(index == 2 ? 1 : 0);
            context.Add(IntegrationTestData.CreateFestivalDay(dayId, new DateOnly(2026, 8, 15 + index)));
            var request = IntegrationTestData.CreateRequestRow(
                AssignmentRequestStatus.Completed, requestId, dayId,
                index == 1
                    ? [AttendeeCode.Create("ATT-001"), AttendeeCode.Create("ATT-002")]
                    : [AttendeeCode.Create("ATT-001")]);
            context.Add(request);
            var spot = IntegrationTestData.CreateSpot();
            context.Add(Assignment.Create(
                AssignmentId.Create(Guid.Parse($"50000000-0000-0000-0000-{index + 1:000000000000}")),
                requestId, dayId, IntegrationTestData.FirstAttendeeId,
                spot.Code, spot.ZoneId, spot.RowCode, spot.Number, qualities[index], assignedAt));
            if (index == 1)
            {
                var secondSpot = IntegrationTestData.CreateSpot("FR-A-002", "A", 2);
                context.Add(Assignment.Create(
                    AssignmentId.Create(Guid.Parse("50000000-0000-0000-0000-000000000004")),
                    requestId, dayId, IntegrationTestData.SecondAttendeeId,
                    secondSpot.Code, secondSpot.ZoneId, secondSpot.RowCode, secondSpot.Number,
                    ExperienceQuality.Good, assignedAt));
            }
        }
        await context.SaveChangesAsync();
        // Recorded Good and Bad outcomes differ from the current Medium Zone.
        await context.Database.ExecuteSqlRawAsync("UPDATE \"Zones\" SET \"ZoneType\" = 'UpperRight'");
    }

    private sealed class QueryCounter : DbCommandInterceptor
    {
        public int ReadCount { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return ValueTask.FromResult(result);
        }
    }
}
