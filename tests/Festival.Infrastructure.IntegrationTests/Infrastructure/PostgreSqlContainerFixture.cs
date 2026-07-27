using Festival.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Festival.Infrastructure.IntegrationTests.Infrastructure;

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container =
        new PostgreSqlBuilder("postgres:16.3-alpine")
            .WithDatabase("festival_integration_tests")
            .WithUsername("festival_test")
            .WithPassword("festival_test_password")
            .Build();

    public string ConnectionString => container.GetConnectionString();

    public FestivalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FestivalDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new FestivalDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateDbContext();

        // The collection runs serially. Truncating all application tables
        // before every test gives order-independent isolation while preserving
        // the migration history and the physical schema under test.
        await context.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                "AssignmentRequestAttendees",
                "Assignments",
                "AssignmentRequests",
                "Attendees",
                "Spots",
                "FestivalDays",
                "Zones"
            CASCADE;
            """);
    }

    public async Task DisposeAsync()
    {
        await container.DisposeAsync();
    }
}
