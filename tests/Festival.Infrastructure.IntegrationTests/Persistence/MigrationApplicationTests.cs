using Festival.Infrastructure.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Festival.Infrastructure.IntegrationTests.Persistence;

public sealed class MigrationApplicationTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    private static readonly string[] ExpectedApplicationTables =
    [
        "AssignmentRequestAttendees",
        "AssignmentRequests",
        "Assignments",
        "Attendees",
        "FestivalDays",
        "Spots",
        "Zones"
    ];

    [Fact]
    public async Task Migrations_ShouldCreateExpectedPhysicalSchema()
    {
        await using var context = Fixture.CreateDbContext();

        await context.Database.MigrateAsync();

        var pendingMigrations =
            await context.Database.GetPendingMigrationsAsync();
        var physicalTables = await LoadApplicationTableNamesAsync();

        pendingMigrations.Should().BeEmpty();
        physicalTables.Should().Equal(ExpectedApplicationTables);
    }

    private async Task<string[]> LoadApplicationTableNamesAsync()
    {
        await using var connection =
            new NpgsqlConnection(Fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT tablename
            FROM pg_catalog.pg_tables
            WHERE schemaname = 'public'
              AND tablename <> '__EFMigrationsHistory'
            ORDER BY tablename;
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        var tables = new List<string>();

        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables.ToArray();
    }
}
