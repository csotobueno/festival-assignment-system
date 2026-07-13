using Festival.Application.Assignments.Ports;
using Festival.Infrastructure.Assignments.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Festival.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInMemoryInfrastructure(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<
            IAttendeeCodeResolver,
            InMemoryAttendeeCodeResolver>();
        services.AddSingleton<
            IAvailableSpotProvider,
            InMemoryAvailableSpotProvider>();
        services.AddSingleton<
            IAssignmentRequestRepository,
            InMemoryAssignmentRequestRepository>();
        services.AddSingleton<
            IAssignmentRepository,
            InMemoryAssignmentRepository>();

        return services;
    }
}
