using System.Collections.Immutable;
using System.Diagnostics;
using Festival.Application;
using Festival.Application.Assignments.Ports;
using Festival.Application.Assignments.Persistence;
using Festival.Application.Assignments.ProcessAssignmentRequest;
using Festival.Application.Zones.Ports;
using Festival.Domain.Assignments;
using Festival.Domain.FestivalDays;
using Festival.Infrastructure;
using Festival.Infrastructure.Assignments.PostgreSql;
using Festival.Infrastructure.Persistence;
using Festival.Infrastructure.Zones.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Npgsql;

namespace Festival.Simulation;

public static class MinimalSimulationRunner
{
    internal const string PostgreSqlImage = "postgres:16.3-alpine";

    public static async Task<SimulationResult> RunAsync(
        ScenarioManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var execution = new SimulationExecutionConfiguration(manifest.Metadata.ExecutionMode,
            manifest.Metadata.ConcurrencyLevel, manifest.Metadata.Repetition, manifest.Metadata.ArrivalSchedule);

        await using var container = new PostgreSqlBuilder(PostgreSqlImage)
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
        registrations.AddScoped(provider => new RequestEvaluationObservation(
            new PostgreSqlZoneCatalogProvider(provider.GetRequiredService<FestivalDbContext>()),
            new PostgreSqlAvailableSpotProvider(provider.GetRequiredService<FestivalDbContext>())));
        registrations.AddScoped<IZoneCatalogProvider>(provider =>
            provider.GetRequiredService<RequestEvaluationObservation>());
        registrations.AddScoped<IAvailableSpotProvider>(provider =>
            provider.GetRequiredService<RequestEvaluationObservation>());
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
        var checkpoints = new List<DailySimulationCheckpoint>();
        long? timingOrigin = execution.ExecutionMode != SimulationExecutionMode.Sequential || execution.Repetition.HasValue
            ? Stopwatch.GetTimestamp() : null;
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

            var dayAttemptStart = attempts.Count;
            if (execution.ExecutionMode == SimulationExecutionMode.ArrivalDriven)
            {
                attempts.AddRange(await ArrivalSimulationExecutor.ExecuteDayAsync(day, execution.ArrivalSchedule!, timingOrigin!.Value,
                    (request, scheduled, token) => ExecuteAttemptAsync(services, manifest.Metadata.ScenarioId,
                        day.FestivalDayId, request, token, timingOrigin: timingOrigin, scheduledArrivalOffset: scheduled), cancellationToken));
            }
            else if (execution.ExecutionMode == SimulationExecutionMode.Concurrent)
            {
                attempts.AddRange(await ConcurrentSimulationExecutor.ExecuteDayAsync(day, execution,
                    (request, position, ready, token) => ExecuteAttemptAsync(services, manifest.Metadata.ScenarioId,
                        day.FestivalDayId, request, token, ready, position, timingOrigin), cancellationToken));
            }
            else
            {
                // Retain the existing sequential path, including S5 dispatch order.
                foreach (var request in day.Requests.OrderBy(request => request.ExecutionSequence))
                {
                    attempts.Add(await ExecuteAttemptAsync(services, manifest.Metadata.ScenarioId,
                        day.FestivalDayId, request, cancellationToken, timingOrigin: timingOrigin));
                }
            }

            checkpoints.Add(await CaptureDailyCheckpointAsync(services, manifest.Metadata.ScenarioId,
                day.FestivalDayId, attempts.Skip(dayAttemptStart).ToArray(), cancellationToken));
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
        var attendeePaths = await AttendeePathReconstructor.ReconstructAsync(database, manifest, cancellationToken);
        var recoveryObservations = attendeePaths.Select(AttendeeRecoveryAnalyzer.Analyze).ToImmutableArray();
        var metrics = SimulationMetricsCalculator.Calculate(attendeePaths, recoveryObservations);

        return new SimulationResult(attempts.AsReadOnly(), checkpoints.AsReadOnly(), attendeePaths,
            recoveryObservations, metrics, Array.AsReadOnly(requests),
            Array.AsReadOnly(assignments), Array.AsReadOnly(zones), Array.AsReadOnly(spots),
            openingStates.AsReadOnly());
    }

    private static async Task<DailySimulationCheckpoint> CaptureDailyCheckpointAsync(
        ServiceProvider services, string scenarioId, FestivalDayId dayId,
        IReadOnlyList<SimulationAttemptRecord> dailyAttempts, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var zones = await scope.ServiceProvider.GetRequiredService<IZoneCatalogProvider>()
            .GetZonesAsync(cancellationToken);
        var available = await scope.ServiceProvider.GetRequiredService<IAvailableSpotProvider>()
            .GetAvailableSpotsAsync(dayId, cancellationToken);
        var inventory = RemainingInventoryCalculator.Calculate(zones, available);
        return new DailySimulationCheckpoint(scenarioId, dayId, dailyAttempts.Count,
            dailyAttempts.Count(attempt => attempt.Outcome == SimulationAttemptOutcome.Successful),
            dailyAttempts.Count(attempt => attempt.Outcome == SimulationAttemptOutcome.DomainRejected),
            inventory.GoodRemaining, inventory.MediumRemaining, inventory.BadRemaining, available.Count);
    }

    internal static async Task<SimulationAttemptRecord> ExecuteAttemptAsync(
        ServiceProvider services, string scenarioId, FestivalDayId dayId,
        DailyRequestPlan plan, CancellationToken cancellationToken = default,
        Func<Task>? waitForRelease = null, SimulationWindowPosition? windowPosition = null,
        long? timingOrigin = null, TimeSpan? scheduledArrivalOffset = null)
    {
        var command = new ProcessAssignmentRequestCommand(dayId,
            plan.Members.Select(attendee => attendee.Code), plan.RequestedAt, plan.AssignedAt, plan.Eligibility);
        ProcessAssignmentRequestResult? result = null;
        Exception? failure = null;
        DateTimeOffset startedAt;
        DateTimeOffset completedAt;
        TimeSpan duration;
        int? eligibleCount;
        TimeSpan? processingStartedOffset = null;
        string? databaseSqlState = null;
        // Scope preparation and disposal are outside the measured use-case boundary.
        await using (var requestScope = services.CreateAsyncScope())
        {
            var useCase = requestScope.ServiceProvider.GetRequiredService<ProcessAssignmentRequestUseCase>();
            var evaluation = requestScope.ServiceProvider.GetRequiredService<RequestEvaluationObservation>();
            // All request services are scoped/resolved, but no business read has
            // occurred. The window releases only when every member reaches here.
            if (waitForRelease is not null) await waitForRelease();
            cancellationToken.ThrowIfCancellationRequested();
            startedAt = DateTimeOffset.UtcNow;
            var timestamp = Stopwatch.GetTimestamp();
            if (timingOrigin.HasValue)
                processingStartedOffset = Stopwatch.GetElapsedTime(timingOrigin.Value, timestamp);
            try
            {
                result = await useCase.ExecuteAsync(command, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failure = exception;
                databaseSqlState = FindDatabaseSqlState(exception);
            }
            duration = Stopwatch.GetElapsedTime(timestamp);
            completedAt = DateTimeOffset.UtcNow;
            eligibleCount = evaluation.GetEligibleAvailableSpotCount(command, plan.SimulationRequestId);
        }

        if (failure is AssignmentPersistenceConflictException conflict)
        {
            return Record(SimulationAttemptOutcome.KnownConcurrencyConflict, null,
                conflict.Conflict.ToString(), conflict.GetType().FullName, null, null, conflict.Message);
        }
        if (failure is not null)
        {
            // PostgreSQL aborts a deadlock victim's transaction. The failed scope
            // is already disposed; retain the explicit provider code without retry.
            // Other provider/processing failures remain unexpected.
            var deadlock = databaseSqlState == PostgresErrorCodes.DeadlockDetected;
            return Record(deadlock ? SimulationAttemptOutcome.KnownConcurrencyConflict
                    : SimulationAttemptOutcome.UnexpectedTechnicalFailure, null,
                deadlock ? nameof(PostgresErrorCodes.DeadlockDetected) : null,
                failure.GetType().FullName, null, null, failure.Message);
        }

        // The real selection result supplies block evidence; other rejections are unknown.
        bool? blockFound = result!.IsAssigned ? true
            : result.RejectionCode == ProcessAssignmentRequestUseCase.NoContiguousSpotsAvailableCode ? false : null;
        AssignmentRequestId? confirmedRequestId = null;
        try
        {
            // Reconcile after timing in a fresh read scope. Quality comes only from
            // durable Assignments, never scenario intent or current Zone quality.
            await using var observation = services.CreateAsyncScope();
            var database = observation.ServiceProvider.GetRequiredService<FestivalDbContext>();
            var requestId = result.AssignmentRequestId.Value;
            var persisted = await database.Database.SqlQuery<PersistedSimulationRequest>($"""
                SELECT "AssignmentRequestId", "FestivalDayId", "Status", "RequestedAt"
                FROM "AssignmentRequests" WHERE "AssignmentRequestId" = {requestId}
                """).SingleOrDefaultAsync(cancellationToken);
            confirmedRequestId = persisted is null ? null : result.AssignmentRequestId;
            var assignments = await database.Assignments.AsNoTracking()
                .Where(assignment => assignment.AssignmentRequestId == result.AssignmentRequestId)
                .Select(assignment => new { assignment.Id, assignment.AttendeeId, assignment.ExperienceQuality })
                .ToArrayAsync(cancellationToken);
            var qualities = assignments.Select(assignment => assignment.ExperienceQuality).Distinct().ToArray();
            var matches = persisted is not null
                && persisted.FestivalDayId == command.FestivalDayId.Value
                && persisted.Status == result.Status.ToString()
                && assignments.Select(assignment => assignment.Id).ToHashSet()
                    .SetEquals(result.Assignments.Select(item => item.AssignmentId));
            var outcome = matches && result.IsAssigned && assignments.Length == command.AttendeeCodes.Count
                && qualities.Length == 1
                && assignments.Select(assignment => assignment.AttendeeId).ToHashSet()
                    .SetEquals(plan.Members.Select(member => member.Id))
                ? SimulationAttemptOutcome.Successful
                : matches && result.IsRejected && assignments.Length == 0
                    ? SimulationAttemptOutcome.DomainRejected
                    : SimulationAttemptOutcome.UnexpectedTechnicalFailure;
            return Record(outcome, confirmedRequestId,
                outcome == SimulationAttemptOutcome.UnexpectedTechnicalFailure ? "PERSISTED_OUTCOME_MISMATCH" : result.RejectionCode,
                null, blockFound, outcome == SimulationAttemptOutcome.Successful ? qualities[0] : null,
                outcome == SimulationAttemptOutcome.UnexpectedTechnicalFailure
                    ? "Returned outcome did not reconcile with persisted request/Assignments."
                    : result.RejectionMessage);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            databaseSqlState = FindDatabaseSqlState(exception);
            return Record(SimulationAttemptOutcome.UnexpectedTechnicalFailure, confirmedRequestId,
                "RECONCILIATION_FAILED", exception.GetType().FullName, blockFound, null, exception.Message);
        }

        SimulationAttemptRecord Record(SimulationAttemptOutcome outcome,
            AssignmentRequestId? persistedId, string? diagnosticCode, string? diagnosticType,
            bool? feasibleBlockFound, ExperienceQuality? selectedQuality, string? diagnostic) =>
            new(scenarioId, dayId, plan.SimulationRequestId, plan.ExecutionSequence, command.AttendeeCodes.Count,
                plan.Members.Select(member => member.Id).ToImmutableArray(), startedAt, completedAt, duration,
                outcome, persistedId, diagnosticCode, diagnosticType, eligibleCount, feasibleBlockFound,
                selectedQuality, diagnostic)
            {
                BaselineSequence = plan.Sequence,
                WindowNumber = windowPosition?.Number,
                WindowPosition = windowPosition?.Position,
                ProcessingStartedOffset = processingStartedOffset,
                ScheduledArrivalOffset = scheduledArrivalOffset,
                DatabaseSqlState = databaseSqlState
            };
    }

    private static string? FindDatabaseSqlState(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is PostgresException postgres) return postgres.SqlState;
        return null;
    }
}
