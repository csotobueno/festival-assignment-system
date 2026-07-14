using Festival.Application.Assignments.Ports;
using Festival.Infrastructure.Assignments.InMemory;
using Festival.Infrastructure.Assignments.InMemory.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Festival.Infrastructure;

public static class DependencyInjection
{
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
}
