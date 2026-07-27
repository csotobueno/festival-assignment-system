using FluentAssertions;
using Npgsql;

namespace Festival.Infrastructure.IntegrationTests.Infrastructure;

internal static class PostgreSqlExceptionAssertions
{
    internal static async Task<PostgresException> ShouldFailWithPostgresAsync(
        Func<Task> action,
        string sqlState,
        string constraintName)
    {
        var exception = await FluentActions
            .Awaiting(action)
            .Should()
            .ThrowAsync<Exception>();

        var postgresException = FindPostgresException(exception.Which);

        postgresException.Should().NotBeNull();
        postgresException!.SqlState.Should().Be(sqlState);
        postgresException.ConstraintName.Should().Be(constraintName);

        return postgresException;
    }

    private static PostgresException? FindPostgresException(
        Exception exception)
    {
        for (var current = exception;
             current is not null;
             current = current.InnerException!)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }
        }

        return null;
    }
}
