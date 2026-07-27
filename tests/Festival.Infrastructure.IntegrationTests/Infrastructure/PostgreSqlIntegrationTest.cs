namespace Festival.Infrastructure.IntegrationTests.Infrastructure;

[Collection(PostgreSqlIntegrationCollection.Name)]
public abstract class PostgreSqlIntegrationTest(
    PostgreSqlContainerFixture fixture)
    : IAsyncLifetime
{
    protected PostgreSqlContainerFixture Fixture { get; } = fixture;

    public Task InitializeAsync() => Fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
