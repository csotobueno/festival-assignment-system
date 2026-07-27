namespace Festival.Infrastructure.IntegrationTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlIntegrationCollection
    : ICollectionFixture<PostgreSqlContainerFixture>
{
    public const string Name = "PostgreSQL integration";
}
