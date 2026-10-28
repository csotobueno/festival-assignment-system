using Festival.Application;
using Festival.Application.Assignments.Persistence;
using Festival.Application.Assignments.Ports;
using Festival.Application.Zones.Ports;
using Festival.Domain.Attendees;
using Festival.Infrastructure;
using Festival.Infrastructure.Assignments.InMemory;
using Festival.Infrastructure.Zones.InMemory;
using Festival.Simulation;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class SimulationAttemptRecordingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAttemptAsync_RecordsTypedCommitFailureWithoutClaimingDurableQuality(bool knownConflict)
    {
        var manifest = new ScenarioMaterializer().Materialize(MinimalRecoveryScenarioDefinition.Create());
        Exception failure = knownConflict
            ? new AssignmentPersistenceConflictException(AssignmentPersistenceConflict.SpotAlreadyAssigned,
                new InvalidOperationException("Commit conflict"))
            : new InvalidOperationException("Unexpected commit failure");
        var unitOfWork = new FailingUnitOfWork(failure);
        var registrations = CreateRegistrations(manifest);
        registrations.AddScoped<IUnitOfWork>(_ => unitOfWork);
        await using var services = registrations.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        var day = manifest.DailyPlans[0];

        var attempt = await MinimalSimulationRunner.ExecuteAttemptAsync(services, manifest.Metadata.ScenarioId,
            day.FestivalDayId, day.Requests[0]);

        attempt.Outcome.Should().Be(knownConflict
            ? SimulationAttemptOutcome.KnownConcurrencyConflict
            : SimulationAttemptOutcome.UnexpectedTechnicalFailure);
        attempt.DiagnosticCode.Should().Be(knownConflict ? "SpotAlreadyAssigned" : null);
        attempt.DiagnosticType.Should().Be(failure.GetType().FullName);
        attempt.Diagnostic.Should().Be(failure.Message);
        attempt.PersistedRequestId.Should().BeNull();
        attempt.SelectedExperienceQuality.Should().BeNull();
        attempt.FeasibleContiguousBlockFound.Should().BeNull();
        attempt.EligibleAvailableSpotCount.Should().Be(6);
        attempt.ScenarioId.Should().Be(manifest.Metadata.ScenarioId);
        attempt.SimulationRequestId.Should().Be(day.Requests[0].SimulationRequestId);
        attempt.AttendeeIds.Should().Equal(day.Requests[0].Members.Select(member => member.Id));
        attempt.StartedAt.Should().BeOnOrBefore(attempt.CompletedAt);
        attempt.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        unitOfWork.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAttemptAsync_LeavesUnobservedCapacityUnknownAfterEarlyProcessingFailure()
    {
        var manifest = new ScenarioMaterializer().Materialize(MinimalRecoveryScenarioDefinition.Create());
        var registrations = CreateRegistrations(manifest);
        registrations.AddScoped<IAttendeeCodeResolver, FailingAttendeeCodeResolver>();
        await using var services = registrations.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        var day = manifest.DailyPlans[0];

        var attempt = await MinimalSimulationRunner.ExecuteAttemptAsync(services, manifest.Metadata.ScenarioId,
            day.FestivalDayId, day.Requests[0]);

        attempt.Outcome.Should().Be(SimulationAttemptOutcome.UnexpectedTechnicalFailure);
        attempt.DiagnosticType.Should().Be(typeof(ArgumentException).FullName);
        attempt.EligibleAvailableSpotCount.Should().BeNull();
        attempt.FeasibleContiguousBlockFound.Should().BeNull();
        attempt.SelectedExperienceQuality.Should().BeNull();
        attempt.PersistedRequestId.Should().BeNull();
        attempt.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    // Unit-only adapters exercise the real use case's exception boundary. The runner's
    // executable composition remains exclusively PostgreSQL/Testcontainers.
    private static ServiceCollection CreateRegistrations(ScenarioManifest manifest)
    {
        var registrations = new ServiceCollection();
        registrations.AddApplication().AddInMemoryInfrastructure();
        registrations.AddScoped<IAttendeeCodeResolver>(_ => new InMemoryAttendeeCodeResolver(manifest.Catalog.Attendees));
        registrations.AddScoped(_ => new RequestEvaluationObservation(
            new InMemoryZoneCatalogProvider(manifest.Catalog.Zones),
            new InMemoryAvailableSpotProvider(manifest.Catalog.Spots.Select(spot =>
                (manifest.DailyPlans[0].FestivalDayId, spot)))));
        registrations.AddScoped<IZoneCatalogProvider>(provider => provider.GetRequiredService<RequestEvaluationObservation>());
        registrations.AddScoped<IAvailableSpotProvider>(provider => provider.GetRequiredService<RequestEvaluationObservation>());
        return registrations;
    }

    private sealed class FailingUnitOfWork(Exception failure) : IUnitOfWork
    {
        public int CallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromException<int>(failure);
        }
    }

    private sealed class FailingAttendeeCodeResolver : IAttendeeCodeResolver
    {
        public Task<IReadOnlyList<AttendeeId>> ResolveAttendeeIdsAsync(
            IEnumerable<AttendeeCode> attendeeCodes, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<AttendeeId>>(new ArgumentException("Unexpected resolver failure"));
    }
}
