using System.Data;
using Festival.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Festival.Infrastructure.Tests.Persistence;

public sealed class FestivalDbContextRegistrationTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=festival";

    [Fact]
    public void AddPostgreSqlPersistence_ShouldResolveNpgsqlContextWithoutConnecting()
    {
        using var serviceProvider = CreateServiceProvider(ConnectionString);
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<FestivalDbContext>();

        context.Database.ProviderName.Should().Be(
            "Npgsql.EntityFrameworkCore.PostgreSQL");
        context.Database.GetDbConnection().State.Should().Be(
            ConnectionState.Closed);
    }

    [Fact]
    public void AddPostgreSqlPersistence_ShouldRegisterContextAsScoped()
    {
        using var serviceProvider = CreateServiceProvider(ConnectionString);

        FestivalDbContext firstContext;

        using (var firstScope = serviceProvider.CreateScope())
        {
            firstContext = firstScope.ServiceProvider
                .GetRequiredService<FestivalDbContext>();
            var contextResolvedAgain = firstScope.ServiceProvider
                .GetRequiredService<FestivalDbContext>();

            contextResolvedAgain.Should().BeSameAs(firstContext);
        }

        using var secondScope = serviceProvider.CreateScope();
        var secondContext = secondScope.ServiceProvider
            .GetRequiredService<FestivalDbContext>();

        secondContext.Should().NotBeSameAs(firstContext);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddPostgreSqlPersistence_ShouldFailWhenConnectionStringIsMissingOrBlank(
        string? connectionString)
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(connectionString);

        var registration = () =>
            services.AddPostgreSqlPersistence(configuration);

        registration.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "*ConnectionStrings:FestivalDatabase*required*PostgreSQL*");
    }

    private static ServiceProvider CreateServiceProvider(
        string connectionString)
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(connectionString);

        services.AddPostgreSqlPersistence(configuration);

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }

    private static IConfiguration CreateConfiguration(
        string? connectionString)
    {
        var values = new Dictionary<string, string?>();

        if (connectionString is not null)
        {
            values["ConnectionStrings:FestivalDatabase"] = connectionString;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
