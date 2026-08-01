using Festival.Domain.Assignments;
using Festival.Infrastructure.Assignments.PostgreSql;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using Festival.Infrastructure.Persistence.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.IntegrationTests.Assignments.PostgreSql;

public sealed class PostgreSqlAssignmentRequestRepositoryTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    [Fact]
    public async Task AddAsync_ShouldStageCompleteGraphWithoutMakingItDurable()
    {
        await using var context = Fixture.CreateDbContext();
        context.FestivalDays.Add(IntegrationTestData.CreateFestivalDay());
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository =
            new PostgreSqlAssignmentRequestRepository(context);
        var request = IntegrationTestData.CreateRequest();

        await repository.AddAsync(request);

        context.ChangeTracker.Entries<AssignmentRequestRow>()
            .Should()
            .ContainSingle()
            .Which.State.Should().Be(EntityState.Added);
        context.ChangeTracker.Entries<AssignmentRequestAttendeeRow>()
            .Should()
            .HaveCount(3)
            .And.OnlyContain(entry => entry.State == EntityState.Added);
        context.Model.FindEntityType(typeof(AssignmentRequest))
            .Should()
            .BeNull();
        context.ChangeTracker.Entries<AssignmentRequestAttendeeRow>()
            .OrderBy(entry => entry.Entity.Position)
            .Select(entry => new
            {
                entry.Entity.Position,
                Code = entry.Entity.AttendeeCode.Value
            })
            .Should()
            .Equal(
                new { Position = 0, Code = "ATT-001" },
                new { Position = 1, Code = "ATT-002" },
                new { Position = 2, Code = "ATT-003" });

        await using (var beforeCommit = Fixture.CreateDbContext())
        {
            (await beforeCommit.AssignmentRequests.CountAsync())
                .Should()
                .Be(0);
            (await beforeCommit.AssignmentRequestAttendees.CountAsync())
                .Should()
                .Be(0);
        }

        await context.SaveChangesAsync();

        await using var afterCommit = Fixture.CreateDbContext();
        (await afterCommit.AssignmentRequests.CountAsync()).Should().Be(1);
        (await afterCommit.AssignmentRequestAttendees.CountAsync())
            .Should()
            .Be(3);
    }

    [Theory]
    [InlineData(AssignmentRequestStatus.Received)]
    [InlineData(AssignmentRequestStatus.Completed)]
    [InlineData(AssignmentRequestStatus.Rejected)]
    [InlineData(AssignmentRequestStatus.Failed)]
    public async Task AddAsync_ShouldStageSupportedRequestStatuses(
        AssignmentRequestStatus status)
    {
        await using var context = Fixture.CreateDbContext();
        var repository =
            new PostgreSqlAssignmentRequestRepository(context);
        var request = IntegrationTestData.CreateRequest(status);

        await repository.AddAsync(request);

        var entry = context.ChangeTracker
            .Entries<AssignmentRequestRow>()
            .Should()
            .ContainSingle()
            .Which;
        entry.State.Should().Be(EntityState.Added);
        entry.Entity.Status.Should().Be(status);
    }
}
