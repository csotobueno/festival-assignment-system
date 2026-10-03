using Festival.Infrastructure.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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

    [Fact]
    public async Task EligibilityMigration_ShouldBackfillExistingRequestsWithoutKeepingADefault()
    {
        await using var context = Fixture.CreateDbContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20260722174748_InitialCreate");

        try
        {
            context.FestivalDays.Add(IntegrationTestData.CreateFestivalDay());
            await context.SaveChangesAsync();
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AssignmentRequests"
                    ("AssignmentRequestId", "FestivalDayId", "RequestedAt", "Status")
                VALUES ({IntegrationTestData.RequestId.Value},
                        {IntegrationTestData.FestivalDayId.Value},
                        {IntegrationTestData.RequestedAt}, 'Received');
                """);

            await migrator.MigrateAsync();

            var row = await context.AssignmentRequests.SingleAsync();
            row.AllowsFrontStanding.Should().BeTrue();

            await using var connection = new NpgsqlConnection(Fixture.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("""
                SELECT is_nullable = 'NO' AND column_default IS NULL
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'AssignmentRequests'
                  AND column_name = 'AllowsFrontStanding';
                """, connection);

            (await command.ExecuteScalarAsync()).Should().Be(true);
        }
        finally
        {
            // Keep the shared isolated test database at the latest schema even
            // when a migration assertion fails.
            await migrator.MigrateAsync();
        }
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
