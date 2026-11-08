using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using Festival.Application;
using Festival.Application.Assignments.Ports;
using Festival.Application.Zones.Ports;
using Festival.Domain.Assignments;
using Festival.Infrastructure;
using Festival.Infrastructure.Assignments.PostgreSql;
using Festival.Infrastructure.Persistence;
using Festival.Infrastructure.Zones.PostgreSql;
using Festival.Simulation;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Festival.Simulation.Tests;

public sealed class S8PostgreSqlConcurrencyTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(16)]
    public async Task RunAsync_UsesRealPersistenceRetainsHistoryAndExportsCompatibleEvidence(int level)
    {
        var definition = S8ConcurrentExecutionTests.SmallDefinition(7, 6);
        definition = definition with { Days = [definition.Days[0], new(2, [], [])] };
        var source = new ScenarioMaterializer().Materialize(definition);
        var manifest = S8ConcurrentExecution.Create(source, level, 2);
        var startedAt = DateTimeOffset.UtcNow;
        var result = await MinimalSimulationRunner.RunAsync(manifest);

        result.Attempts.Should().HaveCount(14).And.OnlyContain(attempt => attempt.Outcome
            == SimulationAttemptOutcome.Successful || attempt.Outcome == SimulationAttemptOutcome.DomainRejected
            || attempt.Outcome == SimulationAttemptOutcome.KnownConcurrencyConflict);
        foreach (var deadlock in result.Attempts.Where(attempt => attempt.DatabaseSqlState == "40P01"))
        {
            // Natural interleavings need not produce a fixed number of deadlocks.
            deadlock.Outcome.Should().Be(SimulationAttemptOutcome.KnownConcurrencyConflict);
            deadlock.DiagnosticCode.Should().Be("DeadlockDetected");
            deadlock.DiagnosticType.Should().NotBeNullOrWhiteSpace();
            deadlock.PersistedRequestId.Should().BeNull();
            deadlock.SelectedExperienceQuality.Should().BeNull();
        }
        AssertPersistedInvariants(manifest, result.Requests, result.Assignments, result.Attempts);
        result.Attempts.Should().OnlyContain(attempt => attempt.ProcessingStartedOffset.HasValue
            && attempt.Duration >= TimeSpan.Zero && attempt.StartedAt <= attempt.CompletedAt);
        result.DailyCheckpoints.Should().HaveCount(2);
        result.OpeningStates.Should().OnlyContain(state => state.AvailableSpots.Count == 6);
        result.AttendeePaths.Sum(path => path.PathLength).Should().Be(result.Assignments.Count);
        result.Attempts.Select(attempt => attempt.BaselineSequence).Should().Equal(Enumerable.Range(1, 14));
        foreach (var day in manifest.DailyPlans)
        {
            var attempts = result.Attempts.Where(attempt => attempt.FestivalDayId == day.FestivalDayId).ToArray();
            if (level == 1)
            {
                attempts.Select(attempt => attempt.Outcome).Should().Equal(
                    Enumerable.Repeat(SimulationAttemptOutcome.Successful, 3)
                        .Concat(Enumerable.Repeat(SimulationAttemptOutcome.DomainRejected, 4)));
                attempts.Should().OnlyContain(attempt => attempt.WindowNumber == null && attempt.WindowPosition == null);
            }
            else
            {
                for (var index = 0; index < attempts.Length; index++)
                {
                    attempts[index].WindowNumber.Should().Be(index / level + 1);
                    attempts[index].WindowPosition.Should().Be(index % level + 1);
                }
            }
            foreach (var window in attempts.Where(attempt => attempt.WindowNumber.HasValue).GroupBy(attempt => attempt.WindowNumber))
            {
                var next = attempts.Where(attempt => attempt.WindowNumber == window.Key + 1).ToArray();
                if (next.Length != 0)
                    next.Min(attempt => attempt.ProcessingStartedOffset!.Value).Should().BeGreaterThanOrEqualTo(
                        window.Max(attempt => attempt.ProcessingStartedOffset!.Value + attempt.Duration));
            }
        }

        var root = Path.Combine(Path.GetTempPath(), $"festival-s8-evidence-{Guid.NewGuid():N}");
        try
        {
            var metadata = SimulationRunMetadata.Create("small-s8-test", manifest, startedAt, DateTimeOffset.UtcNow);
            var directory = await new SimulationEvidenceExporter().ExportAsync(new(definition, manifest, result, metadata, root));
            Directory.GetFiles(directory).Select(Path.GetFileName).Should().BeEquivalentTo(SimulationEvidenceExporterTests.FileNames);
            using var runJson = SimulationEvidenceExporterTests.Read(directory, "run-metadata.json");
            runJson.RootElement.GetProperty("repetition").GetInt32().Should().Be(2);
            runJson.RootElement.GetProperty("concurrencyLevel").GetInt32().Should().Be(level);
            runJson.RootElement.GetProperty("executionMode").GetString().Should().Be(level == 1 ? "Sequential" : "Concurrent");
            runJson.RootElement.GetProperty("executionEnvironment").GetProperty("processorCount").GetInt32().Should().BeGreaterThan(0);
            runJson.RootElement.GetProperty("executionEnvironment").GetProperty("postgreSqlImage").GetString().Should().Be("postgres:16.3-alpine");
            using var input = SimulationEvidenceExporterTests.Read(directory, "definition.json");
            input.RootElement.GetProperty("scenarioId").GetString().Should().Be(S8ConcurrentExecution.WorkloadSourceScenarioId);
            input.RootElement.GetProperty("executionMode").GetString().Should().Be("Sequential");
            using var attemptJson = SimulationEvidenceExporterTests.Read(directory, "attempts.json");
            attemptJson.RootElement.GetArrayLength().Should().Be(14);
            attemptJson.RootElement[0].TryGetProperty("windowNumber", out _).Should().Be(level != 1);
            attemptJson.RootElement[0].GetProperty("processingStartedOffset").ValueKind.Should().Be(JsonValueKind.String);
            using var manifestJson = SimulationEvidenceExporterTests.Read(directory, "manifest.json");
            manifestJson.RootElement.GetProperty("metadata").GetProperty("repetition").GetInt32().Should().Be(2);
            var mismatched = metadata with { Repetition = 3, RunId = "mismatch" };
            var export = async () => await new SimulationEvidenceExporter().ExportAsync(new(definition, manifest, result, mismatched, root));
            await export.Should().ThrowAsync<ArgumentException>();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteDayAsync_ControlledCommitFixtureProvesIndependentScopesAndCompleteConflictRollback()
    {
        // Test-only pre-save coordination makes the rollback assertion deterministic,
        // matching Stage 3's controlled fixture. The actual runner has ONLY its
        // pre-use-case gate; it never coordinates inventory reads or saves.
        var manifest = S8ConcurrentExecutionTests.SmallManifest(4);
        await using var container = new PostgreSqlBuilder(MinimalSimulationRunner.PostgreSqlImage).Build();
        await container.StartAsync();
        var registrations = new ServiceCollection();
        registrations.AddApplication().AddPostgreSqlPersistence(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:FestivalDatabase"] = container.GetConnectionString() }).Build());
        var observations = new ConcurrentBag<FestivalDbContext>();
        var commits = new CommitFixture(4);
        registrations.AddScoped(provider =>
        {
            var context = provider.GetRequiredService<FestivalDbContext>();
            provider.GetRequiredService<FestivalDbContext>().Should().BeSameAs(context);
            observations.Add(context);
            return new RequestEvaluationObservation(new PostgreSqlZoneCatalogProvider(context), new PostgreSqlAvailableSpotProvider(context));
        });
        registrations.AddScoped<IZoneCatalogProvider>(provider => provider.GetRequiredService<RequestEvaluationObservation>());
        registrations.AddScoped<IAvailableSpotProvider>(provider => provider.GetRequiredService<RequestEvaluationObservation>());
        registrations.AddScoped<IUnitOfWork>(provider => new CoordinatedCommit(
            provider.GetRequiredService<FestivalDbContext>(), commits));
        await using var services = registrations.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await using (var setup = services.CreateAsyncScope())
        {
            var context = setup.ServiceProvider.GetRequiredService<FestivalDbContext>();
            await context.Database.MigrateAsync();
            context.Attendees.AddRange(manifest.Catalog.Attendees);
            context.FestivalDays.AddRange(manifest.Catalog.FestivalDays);
            context.Zones.AddRange(manifest.Catalog.Zones);
            context.Spots.AddRange(manifest.Catalog.Spots);
            await context.SaveChangesAsync();
        }
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var day = manifest.DailyPlans[0];
        var origin = Stopwatch.GetTimestamp();
        var attempts = await ConcurrentSimulationExecutor.ExecuteDayAsync(day, new(SimulationExecutionMode.Concurrent, 4),
            (request, position, ready, token) => MinimalSimulationRunner.ExecuteAttemptAsync(services,
                manifest.Metadata.ScenarioId, day.FestivalDayId, request, token, ready, position, origin), timeout.Token);

        observations.Should().HaveCount(4);
        observations.Select(context => context.ContextId.InstanceId).Distinct().Should().HaveCount(4);
        commits.Contexts.Select(context => context.ContextId.InstanceId).Should().BeEquivalentTo(
            observations.Select(context => context.ContextId.InstanceId));
        commits.Requests.Should().HaveCount(4).And.OnlyHaveUniqueItems();
        attempts.Count(attempt => attempt.Outcome == SimulationAttemptOutcome.Successful).Should().Be(1);
        var conflicts = attempts.Where(attempt => attempt.Outcome == SimulationAttemptOutcome.KnownConcurrencyConflict).ToArray();
        conflicts.Should().HaveCount(3).And.OnlyContain(attempt => attempt.DiagnosticCode == "SpotAlreadyAssigned"
            && attempt.PersistedRequestId == null && attempt.SelectedExperienceQuality == null
            && attempt.DatabaseSqlState == "23505");
        // All four real use-case intervals overlap in this controlled fixture.
        attempts.Max(attempt => attempt.ProcessingStartedOffset!.Value).Should().BeLessThan(
            attempts.Min(attempt => attempt.ProcessingStartedOffset!.Value + attempt.Duration));
        await using var read = services.CreateAsyncScope();
        var database = read.ServiceProvider.GetRequiredService<FestivalDbContext>();
        var persistedRequests = await database.Database.SqlQueryRaw<PersistedSimulationRequest>(
            """
            SELECT "AssignmentRequestId", "FestivalDayId", "Status", "RequestedAt" FROM "AssignmentRequests"
            """).ToArrayAsync();
        var assignments = await database.Assignments.AsNoTracking().ToArrayAsync();
        persistedRequests.Should().ContainSingle().Which.Status.Should().Be("Completed");
        assignments.Should().HaveCount(2);
        var winner = attempts.Single(attempt => attempt.Outcome == SimulationAttemptOutcome.Successful).PersistedRequestId!.Value.Value;
        commits.Requests.Where(id => id != winner).Should().NotIntersectWith(persistedRequests.Select(request => request.AssignmentRequestId));
        assignments.Should().OnlyContain(assignment => assignment.AssignmentRequestId.Value == winner);
        AssertPersistedInvariants(manifest, persistedRequests, assignments, attempts);
        // Dispose occurred before returning each attempt, including conflict contexts.
        foreach (var context in observations)
        {
            var accessDisposedContext = () => context.Database.GetDbConnection();
            accessDisposedContext.Should().Throw<ObjectDisposedException>();
        }
    }

    private static void AssertPersistedInvariants(ScenarioManifest manifest,
        IReadOnlyList<PersistedSimulationRequest> requests, IReadOnlyList<Assignment> assignments,
        IReadOnlyList<SimulationAttemptRecord> attempts)
    {
        assignments.Select(assignment => (assignment.FestivalDayId, assignment.SpotCode)).Should().OnlyHaveUniqueItems();
        assignments.Select(assignment => (assignment.FestivalDayId, assignment.AttendeeId)).Should().OnlyHaveUniqueItems();
        foreach (var conflict in attempts.Where(attempt => attempt.Outcome == SimulationAttemptOutcome.KnownConcurrencyConflict))
        {
            conflict.PersistedRequestId.Should().BeNull();
            assignments.Should().NotContain(assignment => assignment.FestivalDayId == conflict.FestivalDayId
                && conflict.AttendeeIds.Contains(assignment.AttendeeId));
        }
        foreach (var request in requests)
        {
            var graph = assignments.Where(assignment => assignment.AssignmentRequestId.Value == request.AssignmentRequestId).ToArray();
            var attempt = attempts.Single(attempt => attempt.PersistedRequestId?.Value == request.AssignmentRequestId);
            attempt.Outcome.Should().Be(request.Status == "Rejected"
                ? SimulationAttemptOutcome.DomainRejected : SimulationAttemptOutcome.Successful);
            var plan = manifest.DailyPlans.Single(day => day.FestivalDayId.Value == request.FestivalDayId).Requests
                .Single(plan => plan.SimulationRequestId == attempt.SimulationRequestId);
            // PostgreSQL timestamps have microsecond precision; v2 inputs retain ticks.
            request.RequestedAt.Should().BeCloseTo(plan.RequestedAt, TimeSpan.FromMicroseconds(1));
            if (request.Status == "Rejected") graph.Should().BeEmpty();
            else
            {
                request.Status.Should().Be("Completed");
                graph.Select(assignment => assignment.AttendeeId).Should().BeEquivalentTo(plan.Members.Select(member => member.Id));
                graph.Select(assignment => assignment.ZoneId).Distinct().Should().ContainSingle();
                graph.Select(assignment => assignment.RowCode).Distinct().Should().ContainSingle();
                var numbers = graph.Select(assignment => assignment.SpotNumber.Value).Order().ToArray();
                numbers.Should().Equal(Enumerable.Range(numbers[0], plan.Members.Length));
                graph.Should().OnlyContain(assignment => assignment.FestivalDayId.Value == request.FestivalDayId);
            }
        }
        assignments.Should().OnlyContain(assignment => requests.Any(request => request.AssignmentRequestId == assignment.AssignmentRequestId.Value));
    }

    private sealed class CommitFixture(int count)
    {
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource winnerCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int ready;
        public ConcurrentBag<FestivalDbContext> Contexts { get; } = [];
        public ConcurrentBag<Guid> Requests { get; } = [];

        public async Task<int> SaveAsync(FestivalDbContext context, CancellationToken token)
        {
            Contexts.Add(context);
            Requests.Add(context.ChangeTracker.Entries<Assignment>().First().Entity.AssignmentRequestId.Value);
            var position = Interlocked.Increment(ref ready);
            if (position == count) release.SetResult();
            await release.Task.WaitAsync(token);
            // Commit the elected winner first. Losers still use their real staged
            // graphs, now guaranteed to hit the committed uniqueness constraint
            // rather than depending on random multi-row insert/deadlock order.
            if (position != 1) await winnerCompleted.Task.WaitAsync(token);
            try { return await new EfCoreUnitOfWork(context).SaveChangesAsync(token); }
            finally { if (position == 1) winnerCompleted.TrySetResult(); }
        }
    }

    private sealed class CoordinatedCommit(FestivalDbContext context, CommitFixture fixture) : IUnitOfWork
    {
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await fixture.SaveAsync(context, cancellationToken);
        }
    }
}
