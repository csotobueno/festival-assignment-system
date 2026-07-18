using Festival.Application.Assignments.Ports;
using Festival.Infrastructure.Assignments.InMemory;
using Festival.Infrastructure.Assignments.InMemory.Seed;
using Festival.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Festival.Infrastructure;

public static class DependencyInjection
{
    private const string FestivalDatabaseConnectionString = "FestivalDatabase";

    public static IServiceCollection AddInMemoryInfrastructure(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAttendeeCodeResolver>(
            _ => new InMemoryAttendeeCodeResolver(
                InMemoryAssignmentSeedData.Attendees));
        services.AddSingleton<IAvailableSpotProvider>(
            _ => new InMemoryAvailableSpotProvider(
                InMemoryAssignmentSeedData.AvailableSpots));
        services.AddSingleton<
            IAssignmentRequestRepository,
            InMemoryAssignmentRequestRepository>();
        services.AddSingleton<
            IAssignmentRepository,
            InMemoryAssignmentRepository>();

        return services;
    }

    public static IServiceCollection AddPostgreSqlPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(
            FestivalDatabaseConnectionString);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The connection string " +
                $"'ConnectionStrings:{FestivalDatabaseConnectionString}' " +
                "is required for PostgreSQL persistence.");
        }

        services.AddDbContext<FestivalDbContext>(
            options => options.UseNpgsql(connectionString),
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);

        return services;
    }
}
