using Festival.Infrastructure.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.IntegrationTests.Persistence;

public sealed class FestivalDayPersistenceTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    [Fact]
    public async Task FestivalDay_ShouldRoundTripWithOwnedAssignmentWindow()
    {
        await using var context = Fixture.CreateDbContext();
        var festivalDay = IntegrationTestData.CreateFestivalDay();

        context.FestivalDays.Add(festivalDay);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persisted = await context.FestivalDays.SingleAsync();

        persisted.Id.Should().Be(IntegrationTestData.FestivalDayId);
        persisted.Date.Should().Be(new DateOnly(2026, 8, 15));
        persisted.AssignmentWindow.Should().NotBeNull();
        persisted.AssignmentWindow.Start.Should().Be(new TimeOnly(9, 0));
        persisted.AssignmentWindow.End.Should().Be(new TimeOnly(18, 0));
        persisted.IsAssignmentWindowOpen(
                new DateTime(2026, 8, 15, 12, 0, 0))
            .Should()
            .BeTrue();
        persisted.IsAssignmentWindowOpen(
                new DateTime(2026, 8, 15, 18, 0, 0))
            .Should()
            .BeFalse();
    }
}
