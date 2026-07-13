using Festival.Application.Assignments.ProcessAssignmentRequest;
using Festival.Domain.Assignments;
using Microsoft.Extensions.DependencyInjection;

namespace Festival.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<AssignmentEngine>();
        services.AddScoped<ProcessAssignmentRequestUseCase>();

        return services;
    }
}
