using Festival.Application;
using Festival.Application.Assignments.ProcessAssignmentRequest;
using Festival.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Festival.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void ServiceProvider_ShouldResolveProcessAssignmentRequestUseCase()
    {
        var services = new ServiceCollection();

        services.AddApplication();
        services.AddInMemoryInfrastructure();

        using var serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        using var scope = serviceProvider.CreateScope();

        var useCase = scope.ServiceProvider
            .GetRequiredService<ProcessAssignmentRequestUseCase>();

        Assert.NotNull(useCase);
    }
}
