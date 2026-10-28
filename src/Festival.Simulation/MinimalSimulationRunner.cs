using Festival.Application;
using Festival.Application.Assignments.Ports;
using Festival.Application.Assignments.Persistence;
using Festival.Application.Assignments.ProcessAssignmentRequest;
using Festival.Domain.Assignments;
using Festival.Infrastructure;
using Festival.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Festival.Simulation;

public static class MinimalSimulationRunner
{
    public static async Task<SimulationResult> RunAsync(
        ScenarioManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (manifest.Metadata.ExecutionMode != SimulationExecutionMode.Sequential
            || manifest.Metadata.ConcurrencyLevel != 1)
            throw new NotSupportedException("Only sequential execution with concurrency level 1 is supported.");

        await using var container = new PostgreSqlBuilder("postgres:16.3-alpine")
            .WithDatabase("festival_simulation")
            .WithUsername("festival_simulation")
            .WithPassword("festival_simulation_password")
            .Build();
        // Setup failures propagate; they are not assignment attempts.
        await container.StartAsync(cancellationToken);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:FestivalDatabase"] = container.GetConnectionString()
            }).Build();
        var registrations = new ServiceCollection();
        registrations.AddApplication().AddPostgreSqlPersistence(configuration);
        await using var services = registrations.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        await using (var setup = services.CreateAsyncScope())
        {
            var context = setup.ServiceProvider.GetRequiredService<FestivalDbContext>();
            await context.Database.MigrateAsync(cancellationToken);
            context.Attendees.AddRange(manifest.Catalog.Attendees);
            context.Attendees.AddRange(manifest.Setup.SupportingAttendees);
            context.FestivalDays.AddRange(manifest.Catalog.FestivalDays);
            context.Zones.AddRange(manifest.Catalog.Zones);
            context.Spots.AddRange(manifest.Catalog.Spots);
            var repository = setup.ServiceProvider.GetRequiredService<IAssignmentRequestRepository>();
            foreach (var supporting in manifest.Setup.CompletedRequests)
            {
                var request = AssignmentRequest.Create(supporting.Id, supporting.FestivalDayId,
                    supporting.AttendeeCodes, supporting.RequestedAt, supporting.Eligibility);
                request.Complete(supporting.CompletedAt);
                await repository.AddAsync(request, cancellationToken);
            }
            context.Assignments.AddRange(manifest.Setup.Assignments);
            await context.SaveChangesAsync(cancellationToken);
        }

        var attempts = new List<SimulationAttemptRecord>();
        var openingStates = new List<SimulationDayOpeningState>();
        foreach (var day in manifest.DailyPlans.OrderBy(day => day.DayNumber))
        {
            // Read persisted topology and real day-specific availability before execution.
            // There are no catalog or occupancy writes between FestivalDays.
            await using (var opening = services.CreateAsyncScope())
            {
                var context = opening.ServiceProvider.GetRequiredService<FestivalDbContext>();
                var openingZones = await context.Zones.AsNoTracking().OrderBy(zone => zone.Id)
                    .ToArrayAsync(cancellationToken);
                var openingSpots = await context.Spots.AsNoTracking().OrderBy(spot => spot.Code)
                    .ToArrayAsync(cancellationToken);
                var availableSpots = await opening.ServiceProvider.GetRequiredService<IAvailableSpotProvider>()
                    .GetAvailableSpotsAsync(day.FestivalDayId, cancellationToken);
                openingStates.Add(new SimulationDayOpeningState(day.FestivalDayId,
                    Array.AsReadOnly(openingZones), Array.AsReadOnly(openingSpots), availableSpots));
            }

            foreach (var request in day.Requests.OrderBy(request => request.Sequence))
            {
                var command = new ProcessAssignmentRequestCommand(day.FestivalDayId,
                    request.Members.Select(attendee => attendee.Code), request.RequestedAt,
                    request.AssignedAt, request.Eligibility);
                attempts.Add(await ExecuteAttemptAsync(services, command, request.Sequence,
                    request.SimulationRequestId, cancellationToken));
            }
        }

        // Reconcile in a fresh read scope; never infer persisted quality from returned outputs.
        await using var observation = services.CreateAsyncScope();
        var database = observation.ServiceProvider.GetRequiredService<FestivalDbContext>();
        var requests = await database.Database.SqlQueryRaw<PersistedSimulationRequest>(
            """
            SELECT "AssignmentRequestId", "FestivalDayId", "Status", "RequestedAt"
            FROM "AssignmentRequests" ORDER BY "RequestedAt", "AssignmentRequestId"
            """).ToArrayAsync(cancellationToken);
        var assignments = await database.Assignments.AsNoTracking()
            .OrderBy(assignment => assignment.AssignedAt).ThenBy(assignment => assignment.AttendeeId)
            .ToArrayAsync(cancellationToken);
        var zones = await database.Zones.AsNoTracking().OrderBy(zone => zone.Id)
            .ToArrayAsync(cancellationToken);
        var spots = await database.Spots.AsNoTracking().OrderBy(spot => spot.Code)
            .ToArrayAsync(cancellationToken);

        return new SimulationResult(attempts.AsReadOnly(), Array.AsReadOnly(requests),
            Array.AsReadOnly(assignments), Array.AsReadOnly(zones), Array.AsReadOnly(spots),
            openingStates.AsReadOnly());
    }

    private static async Task<SimulationAttemptRecord> ExecuteAttemptAsync(
        ServiceProvider services, ProcessAssignmentRequestCommand command, int sequence,
        Guid simulationRequestId, CancellationToken cancellationToken)
    {
        ProcessAssignmentRequestResult result;
        try
        {
            // Each request gets its own context, repositories and Unit of Work.
            await using var requestScope = services.CreateAsyncScope();
            result = await requestScope.ServiceProvider
                .GetRequiredService<ProcessAssignmentRequestUseCase>()
                .ExecuteAsync(command, cancellationToken);
        }
        catch (AssignmentPersistenceConflictException exception)
        {
            return Record(SimulationAttemptOutcome.KnownConcurrencyConflict, null, exception.ToString());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Do not misclassify arbitrary ArgumentExceptions as invalid input.
            // This code-defined scenario has no external-input exclusion boundary.
            return Record(SimulationAttemptOutcome.UnexpectedTechnicalFailure, null, exception.ToString());
        }

        // Confirm durable request status and the exact returned Assignment identities
        // before marking this attempt Successful or DomainRejected.
        await using var observation = services.CreateAsyncScope();
        var database = observation.ServiceProvider.GetRequiredService<FestivalDbContext>();
        var requestId = result.AssignmentRequestId.Value;
        var persisted = await database.Database.SqlQuery<PersistedSimulationRequest>($"""
            SELECT "AssignmentRequestId", "FestivalDayId", "Status", "RequestedAt"
            FROM "AssignmentRequests" WHERE "AssignmentRequestId" = {requestId}
            """).SingleOrDefaultAsync(cancellationToken);
        var assignmentIds = await database.Assignments.AsNoTracking()
            .Where(assignment => assignment.AssignmentRequestId == result.AssignmentRequestId)
            .Select(assignment => assignment.Id).ToArrayAsync(cancellationToken);
        var matches = persisted is not null
            && persisted.FestivalDayId == command.FestivalDayId.Value
            && persisted.Status == result.Status.ToString()
            && assignmentIds.ToHashSet().SetEquals(result.Assignments.Select(item => item.AssignmentId));
        var outcome = matches && result.IsAssigned && assignmentIds.Length == command.AttendeeCodes.Count
            ? SimulationAttemptOutcome.Successful
            : matches && result.IsRejected && assignmentIds.Length == 0
                ? SimulationAttemptOutcome.DomainRejected
                : SimulationAttemptOutcome.UnexpectedTechnicalFailure;
        return Record(outcome, persisted is null ? null : result.AssignmentRequestId,
            outcome == SimulationAttemptOutcome.UnexpectedTechnicalFailure
                ? "Returned outcome did not reconcile with persisted request/Assignments."
                : result.RejectionCode);

        SimulationAttemptRecord Record(SimulationAttemptOutcome outcome,
            AssignmentRequestId? persistedId, string? diagnostic) =>
            new(command.FestivalDayId, simulationRequestId, sequence, command.AttendeeCodes.Count,
                outcome, persistedId, diagnostic);
    }
}
