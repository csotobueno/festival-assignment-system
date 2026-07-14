using Festival.Application;
using Festival.Application.Assignments.Ports;
using Festival.Application.Assignments.ProcessAssignmentRequest;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Infrastructure.Assignments.InMemory;
using Festival.Infrastructure.Assignments.InMemory.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Festival.Infrastructure.Tests.Assignments.InMemory;

public sealed class InMemoryAssignmentFlowTests
{
    private static readonly DateTimeOffset RequestedAt =
        new(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(-5));

    private static readonly DateTimeOffset AssignedAt =
        new(2026, 7, 10, 9, 1, 0, TimeSpan.FromHours(-5));

    [Fact]
    public async Task ExecuteAsync_ShouldCompleteAssignmentFlow_WithSeededInMemoryAdapters()
    {
        using var serviceProvider = BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = ResolveContext(scope.ServiceProvider);

        var result = await context.UseCase.ExecuteAsync(
            CreateCommand(1, 2, 3));

        Assert.Equal(AssignmentRequestStatus.Completed, result.Status);
        Assert.True(result.IsAssigned);
        Assert.False(result.IsRejected);
        Assert.Equal(3, result.Assignments.Count);
        Assert.Single(
            result.Assignments
                .Select(assignment => assignment.ZoneId)
                .Distinct());
        Assert.Single(
            result.Assignments
                .Select(assignment => assignment.RowCode)
                .Distinct());
        AssertConsecutive(
            result.Assignments
                .Select(assignment => assignment.SpotNumber.Value)
                .ToArray());

        var storedRequest = Assert.Single(
            context.AssignmentRequestRepository.AssignmentRequests);

        Assert.Equal(AssignmentRequestStatus.Completed, storedRequest.Status);
        Assert.Equal(result.AssignmentRequestId, storedRequest.Id);
        Assert.Equal(3, context.AssignmentRepository.Assignments.Count);
        Assert.All(
            context.AssignmentRepository.Assignments,
            assignment => Assert.Equal(
                result.AssignmentRequestId,
                assignment.AssignmentRequestId));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectAssignmentFlow_WhenSeededRowsCannotFitGroup()
    {
        using var serviceProvider = BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = ResolveContext(scope.ServiceProvider);

        var result = await context.UseCase.ExecuteAsync(
            CreateCommand(1, 2, 3, 4, 5));

        Assert.Equal(AssignmentRequestStatus.Rejected, result.Status);
        Assert.True(result.IsRejected);
        Assert.False(result.IsAssigned);
        Assert.Equal(
            ProcessAssignmentRequestUseCase.NoContiguousSpotsAvailableCode,
            result.RejectionCode);
        Assert.Empty(result.Assignments);

        var storedRequest = Assert.Single(
            context.AssignmentRequestRepository.AssignmentRequests);

        Assert.Equal(AssignmentRequestStatus.Rejected, storedRequest.Status);
        Assert.Equal(result.AssignmentRequestId, storedRequest.Id);
        Assert.Equal(
            ProcessAssignmentRequestUseCase.NoContiguousSpotsAvailableCode,
            storedRequest.Rejection?.Code);
        Assert.Empty(context.AssignmentRepository.Assignments);
    }

    [Fact]
    public void SeedData_ShouldContainExpectedDeterministicLayout()
    {
        Assert.Equal(10, InMemoryAssignmentSeedData.Attendees.Count);
        Assert.Equal(
            new DateOnly(2026, 7, 10),
            InMemoryAssignmentSeedData.FestivalDay.Date);
        Assert.Equal(2, InMemoryAssignmentSeedData.Zones.Count);
        Assert.Equal(
            ["Zone A", "Zone B"],
            InMemoryAssignmentSeedData.Zones.Select(zone => zone.Name));
        Assert.Equal(16, InMemoryAssignmentSeedData.Spots.Count);
        Assert.Equal(16, InMemoryAssignmentSeedData.AvailableSpots.Count);
        Assert.All(
            InMemoryAssignmentSeedData.AvailableSpots,
            entry => Assert.Equal(
                InMemoryAssignmentSeedData.FestivalDayId,
                entry.FestivalDayId));
        Assert.Equal(
            Enumerable.Range(1, 10).Select(number => $"ATT-{number:000}"),
            InMemoryAssignmentSeedData.Attendees
                .Select(attendee => attendee.Code.Value));

        foreach (var zone in InMemoryAssignmentSeedData.Zones)
        {
            var zoneSpots = InMemoryAssignmentSeedData.Spots
                .Where(spot => spot.ZoneId == zone.Id)
                .ToArray();

            Assert.Equal(8, zoneSpots.Length);
            Assert.Equal(
                ["A", "B"],
                zoneSpots
                    .Select(spot => spot.RowCode.Value)
                    .Distinct()
                    .OrderBy(value => value)
                    .ToArray());

            foreach (var rowCode in new[] { "A", "B" })
            {
                Assert.Equal(
                    [1, 2, 3, 4],
                    zoneSpots
                        .Where(spot => spot.RowCode.Value == rowCode)
                        .Select(spot => spot.Number.Value)
                        .OrderBy(value => value)
                        .ToArray());
                Assert.Equal(
                    Enumerable.Range(1, 4).Select(number =>
                        $"{zone.Name.ToUpperInvariant().Replace(' ', '-')}-{rowCode}-{number:000}"),
                    zoneSpots
                        .Where(spot => spot.RowCode.Value == rowCode)
                        .Select(spot => spot.Code.Value)
                        .OrderBy(value => value));
            }
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddApplication();
        services.AddInMemoryInfrastructure();

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }

    private static TestContext ResolveContext(
        IServiceProvider serviceProvider)
    {
        return new TestContext(
            serviceProvider.GetRequiredService<ProcessAssignmentRequestUseCase>(),
            Assert.IsType<InMemoryAssignmentRequestRepository>(
                serviceProvider.GetRequiredService<IAssignmentRequestRepository>()),
            Assert.IsType<InMemoryAssignmentRepository>(
                serviceProvider.GetRequiredService<IAssignmentRepository>()));
    }

    private static ProcessAssignmentRequestCommand CreateCommand(
        params int[] attendeeNumbers)
    {
        return new ProcessAssignmentRequestCommand(
            InMemoryAssignmentSeedData.FestivalDayId,
            attendeeNumbers.Select(number =>
                AttendeeCode.Create($"ATT-{number:000}")),
            RequestedAt,
            AssignedAt);
    }

    private static void AssertConsecutive(
        IReadOnlyList<int> spotNumbers)
    {
        var ordered = spotNumbers.OrderBy(number => number).ToArray();

        for (var index = 1; index < ordered.Length; index++)
        {
            Assert.Equal(ordered[index - 1] + 1, ordered[index]);
        }
    }

    private sealed record TestContext(
        ProcessAssignmentRequestUseCase UseCase,
        InMemoryAssignmentRequestRepository AssignmentRequestRepository,
        InMemoryAssignmentRepository AssignmentRepository);
}
