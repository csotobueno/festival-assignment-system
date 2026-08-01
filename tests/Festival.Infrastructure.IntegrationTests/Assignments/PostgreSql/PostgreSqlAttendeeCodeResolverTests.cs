using Festival.Domain.Attendees;
using Festival.Infrastructure.Assignments.PostgreSql;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.IntegrationTests.Assignments.PostgreSql;

public sealed class PostgreSqlAttendeeCodeResolverTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    [Fact]
    public async Task ResolveAttendeeIdsAsync_ShouldResolveExistingCodesInRequestedOrderAndOmitUnknownCodes()
    {
        await using var context = Fixture.CreateDbContext();
        var first = IntegrationTestData.CreateAttendee(
            IntegrationTestData.FirstAttendeeId,
            "ATT-001",
            "Ada Lovelace");
        var second = IntegrationTestData.CreateAttendee(
            IntegrationTestData.SecondAttendeeId,
            "ATT-003",
            "Grace Hopper");
        context.Attendees.AddRange(first, second);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var resolver = new PostgreSqlAttendeeCodeResolver(context);

        var resolved = await resolver.ResolveAttendeeIdsAsync(
        [
            AttendeeCode.Create("ATT-003"),
            AttendeeCode.Create("ATT-001"),
            AttendeeCode.Create("ATT-999")
        ]);

        resolved.Should().Equal(
            IntegrationTestData.SecondAttendeeId,
            IntegrationTestData.FirstAttendeeId);
        context.ChangeTracker.Entries().Should().BeEmpty();

        await using var verificationContext = Fixture.CreateDbContext();
        (await verificationContext.Attendees.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task ResolveAttendeeIdsAsync_ShouldReturnEmptyResultForEmptyInput()
    {
        await using var context = Fixture.CreateDbContext();
        var resolver = new PostgreSqlAttendeeCodeResolver(context);

        var resolved = await resolver.ResolveAttendeeIdsAsync([]);

        resolved.Should().BeEmpty();
        context.ChangeTracker.Entries().Should().BeEmpty();
    }
}
