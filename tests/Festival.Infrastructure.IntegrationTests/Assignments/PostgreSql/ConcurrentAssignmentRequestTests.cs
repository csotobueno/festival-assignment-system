using Festival.Application;
using Festival.Application.Assignments.Persistence;
using Festival.Application.Assignments.Ports;
using Festival.Application.Assignments.ProcessAssignmentRequest;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.Spots;
using Festival.Infrastructure.Assignments.PostgreSql;
using Festival.Infrastructure.IntegrationTests.Infrastructure;
using Festival.Infrastructure.Persistence;
using Festival.Infrastructure.Persistence.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Primitives;
using Npgsql;

namespace Festival.Infrastructure.IntegrationTests.Assignments.PostgreSql;

public sealed class ConcurrentAssignmentRequestTests(
    PostgreSqlContainerFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    private const string UniqueViolation = "23505";
    private const string FirstSpotCode = "FR-A-001";
    private const string SecondSpotCode = "FR-A-002";
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task ExecuteAsync_ConcurrentRequestsForSameSpot_ShouldCommitOneCompleteGraphAndConflictTheOther()
    {
        await SeedSpotCompetitionAsync();

        var coordinator = new AsyncPreSaveCoordinator(2);
        await using var serviceProvider = CreateServiceProvider(
            coordinator,
            useSelectedSpotProvider: false);
        using var timeout = new CancellationTokenSource(TestTimeout);

        ConcurrentExecution[] executions;

        {
            await using var scopeA = serviceProvider.CreateAsyncScope();
            await using var scopeB = serviceProvider.CreateAsyncScope();

            AssertIndependentScopes(scopeA, scopeB);

            var taskA = ExecuteAsync(
                scopeA.ServiceProvider,
                CreateCommand("ATT-001"),
                coordinator,
                timeout.Token);
            var taskB = ExecuteAsync(
                scopeB.ServiceProvider,
                CreateCommand("ATT-002"),
                coordinator,
                timeout.Token);

            executions = await Task.WhenAll(taskA, taskB);
        }

        var (succeeded, conflicted) = AssertOneSuccessAndOneConflict(
            executions,
            AssignmentPersistenceConflict.SpotAlreadyAssigned,
            PostgreSqlAssignmentConflictTranslator.SpotAlreadyAssignedConstraint);

        succeeded.Result.Assignments.Should().ContainSingle()
            .Which.SpotCode.Value.Should().Be(FirstSpotCode);

        await AssertSpotCompetitionDurableStateAsync(
            succeeded,
            conflicted);
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrentRequestsForSameAttendee_ShouldCommitOneCompleteGraphAndConflictTheOther()
    {
        await SeedAttendeeCompetitionAsync();

        var coordinator = new AsyncPreSaveCoordinator(2);
        await using var serviceProvider = CreateServiceProvider(
            coordinator,
            useSelectedSpotProvider: true);
        using var timeout = new CancellationTokenSource(TestTimeout);

        ConcurrentExecution[] executions;

        {
            await using var scopeA = serviceProvider.CreateAsyncScope();
            await using var scopeB = serviceProvider.CreateAsyncScope();

            AssertIndependentScopes(scopeA, scopeB);
            SelectSpot(scopeA, FirstSpotCode);
            SelectSpot(scopeB, SecondSpotCode);

            var taskA = ExecuteAsync(
                scopeA.ServiceProvider,
                CreateCommand("ATT-001"),
                coordinator,
                timeout.Token);
            var taskB = ExecuteAsync(
                scopeB.ServiceProvider,
                CreateCommand("ATT-001"),
                coordinator,
                timeout.Token);

            executions = await Task.WhenAll(taskA, taskB);
        }

        var (succeeded, conflicted) = AssertOneSuccessAndOneConflict(
            executions,
            AssignmentPersistenceConflict.AttendeeAlreadyAssigned,
            PostgreSqlAssignmentConflictTranslator.AttendeeAlreadyAssignedConstraint);

        succeeded.Result.Assignments.Should().ContainSingle()
            .Which.SpotCode.Value.Should().BeOneOf(
                FirstSpotCode,
                SecondSpotCode);

        await AssertAttendeeCompetitionDurableStateAsync(
            succeeded,
            conflicted);
    }

    private ServiceProvider CreateServiceProvider(
        AsyncPreSaveCoordinator coordinator,
        bool useSelectedSpotProvider)
    {
        var services = new ServiceCollection();
        var configuration = new TestConfiguration(
            Fixture.ConnectionString);

        services.AddApplication();
        services.AddPostgreSqlPersistence(configuration);
        services.AddSingleton(coordinator);

        services.RemoveAll<IUnitOfWork>();
        services.AddScoped<EfCoreUnitOfWork>();
        services.AddScoped<CoordinatedUnitOfWork>();
        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<CoordinatedUnitOfWork>());

        if (useSelectedSpotProvider)
        {
            services.RemoveAll<IAvailableSpotProvider>();
            services.AddScoped<PostgreSqlAvailableSpotProvider>();
            services.AddScoped<ScopedSpotSelection>();
            services.AddScoped<IAvailableSpotProvider>(serviceProvider =>
                new SelectedPostgreSqlAvailableSpotProvider(
                    serviceProvider.GetRequiredService<
                        PostgreSqlAvailableSpotProvider>(),
                    serviceProvider.GetRequiredService<ScopedSpotSelection>()));
        }

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }

    private static void AssertIndependentScopes(
        AsyncServiceScope scopeA,
        AsyncServiceScope scopeB)
    {
        var contextA = scopeA.ServiceProvider
            .GetRequiredService<FestivalDbContext>();
        var contextB = scopeB.ServiceProvider
            .GetRequiredService<FestivalDbContext>();

        scopeA.ServiceProvider.GetRequiredService<FestivalDbContext>()
            .Should().BeSameAs(contextA);
        scopeB.ServiceProvider.GetRequiredService<FestivalDbContext>()
            .Should().BeSameAs(contextB);
        contextA.Should().NotBeSameAs(contextB);

        var unitOfWorkA = scopeA.ServiceProvider
            .GetRequiredService<IUnitOfWork>();
        var unitOfWorkB = scopeB.ServiceProvider
            .GetRequiredService<IUnitOfWork>();

        unitOfWorkA.Should().NotBeSameAs(unitOfWorkB);
    }

    private static void SelectSpot(
        AsyncServiceScope scope,
        string spotCode)
    {
        scope.ServiceProvider
            .GetRequiredService<ScopedSpotSelection>()
            .Select(SpotCode.Create(spotCode));
    }

    private static async Task<ConcurrentExecution> ExecuteAsync(
        IServiceProvider serviceProvider,
        ProcessAssignmentRequestCommand command,
        AsyncPreSaveCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        var useCase = serviceProvider
            .GetRequiredService<ProcessAssignmentRequestUseCase>();
        var unitOfWork = serviceProvider
            .GetRequiredService<CoordinatedUnitOfWork>();

        try
        {
            var result = await useCase.ExecuteAsync(
                command,
                cancellationToken);

            if (result.Status != AssignmentRequestStatus.Completed)
            {
                throw new InvalidOperationException(
                    "The concurrent request did not prepare a completed assignment graph.");
            }

            return new SucceededExecution(
                RequirePendingGraph(unitOfWork),
                result);
        }
        catch (AssignmentPersistenceConflictException exception)
        {
            return new ConflictedExecution(
                RequirePendingGraph(unitOfWork),
                exception);
        }
        catch (Exception exception)
        {
            coordinator.Abort(exception);
            throw;
        }
    }

    private static PendingGraphSnapshot RequirePendingGraph(
        CoordinatedUnitOfWork unitOfWork)
    {
        return unitOfWork.PendingGraph
            ?? throw new InvalidOperationException(
                "The request failed before reaching the coordinated persistence boundary.");
    }

    private static (
        SucceededExecution Succeeded,
        ConflictedExecution Conflicted)
        AssertOneSuccessAndOneConflict(
            IReadOnlyCollection<ConcurrentExecution> executions,
            AssignmentPersistenceConflict expectedConflict,
            string expectedConstraint)
    {
        executions.Should().HaveCount(2);

        var succeededExecutions = executions
            .OfType<SucceededExecution>()
            .ToArray();
        var conflictedExecutions = executions
            .OfType<ConflictedExecution>()
            .ToArray();

        succeededExecutions.Should().ContainSingle();
        conflictedExecutions.Should().ContainSingle();

        var succeeded = succeededExecutions.Single();
        var conflicted = conflictedExecutions.Single();

        succeeded.RequestId.Should().NotBe(conflicted.RequestId);
        succeeded.Result.Status.Should().Be(AssignmentRequestStatus.Completed);
        succeeded.Result.IsAssigned.Should().BeTrue();
        AssertCompletePendingGraph(succeeded.PendingGraph);
        AssertCompletePendingGraph(conflicted.PendingGraph);
        AssertTranslatedConflict(
            conflicted.Exception,
            expectedConflict,
            expectedConstraint);

        return (succeeded, conflicted);
    }

    private static void AssertCompletePendingGraph(
        PendingGraphSnapshot graph)
    {
        graph.AssignmentRequestCount.Should().Be(1);
        graph.AssignmentRequestAttendeeCount.Should().Be(1);
        graph.AssignmentCount.Should().Be(1);
        graph.AllEntriesAdded.Should().BeTrue();
    }

    private static void AssertTranslatedConflict(
        AssignmentPersistenceConflictException exception,
        AssignmentPersistenceConflict expectedConflict,
        string expectedConstraint)
    {
        exception.Conflict.Should().Be(expectedConflict);
        exception.InnerException.Should().BeOfType<DbUpdateException>();

        var postgresException = FindPostgresException(exception);
        postgresException.Should().NotBeNull();
        postgresException!.SqlState.Should().Be(UniqueViolation);
        postgresException.ConstraintName.Should().Be(expectedConstraint);
    }

    private static PostgresException? FindPostgresException(
        Exception exception)
    {
        for (Exception? current = exception;
             current is not null;
             current = current.InnerException)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }
        }

        return null;
    }

    private async Task AssertSpotCompetitionDurableStateAsync(
        SucceededExecution succeeded,
        ConflictedExecution conflicted)
    {
        await using var context = Fixture.CreateDbContext();

        var assignments = await context.Assignments.ToArrayAsync();
        assignments.Should().ContainSingle();
        assignments[0].FestivalDayId.Should().Be(
            IntegrationTestData.FestivalDayId);
        assignments[0].SpotCode.Value.Should().Be(FirstSpotCode);
        assignments[0].AssignmentRequestId.Should().Be(succeeded.RequestId);

        await AssertOnlyWinningRequestGraphIsDurableAsync(
            context,
            succeeded.RequestId,
            conflicted.RequestId);
    }

    private async Task AssertAttendeeCompetitionDurableStateAsync(
        SucceededExecution succeeded,
        ConflictedExecution conflicted)
    {
        await using var context = Fixture.CreateDbContext();

        var assignments = await context.Assignments.ToArrayAsync();
        assignments.Should().ContainSingle();
        assignments[0].FestivalDayId.Should().Be(
            IntegrationTestData.FestivalDayId);
        assignments[0].AttendeeId.Should().Be(
            IntegrationTestData.FirstAttendeeId);
        assignments[0].SpotCode.Value.Should().BeOneOf(
            FirstSpotCode,
            SecondSpotCode);
        assignments[0].AssignmentRequestId.Should().Be(succeeded.RequestId);

        await AssertOnlyWinningRequestGraphIsDurableAsync(
            context,
            succeeded.RequestId,
            conflicted.RequestId);
    }

    private static async Task AssertOnlyWinningRequestGraphIsDurableAsync(
        FestivalDbContext context,
        AssignmentRequestId winningRequestId,
        AssignmentRequestId losingRequestId)
    {
        var durableRequest = await context.AssignmentRequests
            .Include(request => request.Attendees)
            .SingleAsync();

        durableRequest.AssignmentRequestId.Should().Be(winningRequestId);
        durableRequest.Status.Should().Be(AssignmentRequestStatus.Completed);
        durableRequest.Attendees.Should().ContainSingle();

        (await context.AssignmentRequestAttendees.CountAsync(row =>
            row.AssignmentRequestId == winningRequestId)).Should().Be(1);
        (await context.Assignments.CountAsync(assignment =>
            assignment.AssignmentRequestId == winningRequestId)).Should().Be(1);

        (await context.AssignmentRequests.CountAsync(row =>
            row.AssignmentRequestId == losingRequestId)).Should().Be(0);
        (await context.AssignmentRequestAttendees.CountAsync(row =>
            row.AssignmentRequestId == losingRequestId)).Should().Be(0);
        (await context.Assignments.CountAsync(assignment =>
            assignment.AssignmentRequestId == losingRequestId)).Should().Be(0);

        (await context.AssignmentRequests.CountAsync()).Should().Be(1);
        (await context.AssignmentRequestAttendees.CountAsync()).Should().Be(1);
        (await context.Assignments.CountAsync()).Should().Be(1);
    }

    private async Task SeedSpotCompetitionAsync()
    {
        await using var context = Fixture.CreateDbContext();
        context.AddRange(
            IntegrationTestData.CreateFestivalDay(),
            IntegrationTestData.CreateZone(),
            IntegrationTestData.CreateSpot(),
            IntegrationTestData.CreateAttendee(),
            IntegrationTestData.CreateAttendee(
                IntegrationTestData.SecondAttendeeId,
                "ATT-002",
                "Grace Hopper"));
        await context.SaveChangesAsync();
    }

    private async Task SeedAttendeeCompetitionAsync()
    {
        await using var context = Fixture.CreateDbContext();
        context.AddRange(
            IntegrationTestData.CreateFestivalDay(),
            IntegrationTestData.CreateZone(),
            IntegrationTestData.CreateSpot(),
            IntegrationTestData.CreateSpot(
                SecondSpotCode,
                "A",
                2),
            IntegrationTestData.CreateAttendee());
        await context.SaveChangesAsync();
    }

    private static ProcessAssignmentRequestCommand CreateCommand(
        string attendeeCode)
    {
        return new ProcessAssignmentRequestCommand(
            IntegrationTestData.FestivalDayId,
            [AttendeeCode.Create(attendeeCode)],
            IntegrationTestData.RequestedAt,
            IntegrationTestData.AssignedAt);
    }

    private abstract record ConcurrentExecution(
        PendingGraphSnapshot PendingGraph)
    {
        internal AssignmentRequestId RequestId => PendingGraph.RequestId;
    }

    private sealed record SucceededExecution(
        PendingGraphSnapshot PendingGraph,
        ProcessAssignmentRequestResult Result)
        : ConcurrentExecution(PendingGraph);

    private sealed record ConflictedExecution(
        PendingGraphSnapshot PendingGraph,
        AssignmentPersistenceConflictException Exception)
        : ConcurrentExecution(PendingGraph);

    private sealed record PendingGraphSnapshot(
        AssignmentRequestId RequestId,
        int AssignmentRequestCount,
        int AssignmentRequestAttendeeCount,
        int AssignmentCount,
        bool AllEntriesAdded);

    private sealed class CoordinatedUnitOfWork(
        EfCoreUnitOfWork inner,
        FestivalDbContext context,
        AsyncPreSaveCoordinator coordinator)
        : IUnitOfWork
    {
        internal PendingGraphSnapshot? PendingGraph { get; private set; }

        public async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            var requestEntries = context.ChangeTracker
                .Entries<AssignmentRequestRow>()
                .Where(entry => entry.State == EntityState.Added)
                .ToArray();
            var attendeeEntries = context.ChangeTracker
                .Entries<AssignmentRequestAttendeeRow>()
                .Where(entry => entry.State == EntityState.Added)
                .ToArray();
            var assignmentEntries = context.ChangeTracker
                .Entries<Assignment>()
                .Where(entry => entry.State == EntityState.Added)
                .ToArray();

            if (requestEntries.Length != 1)
            {
                throw new InvalidOperationException(
                    "Exactly one pending AssignmentRequest was expected before save.");
            }

            PendingGraph = new PendingGraphSnapshot(
                requestEntries[0].Entity.AssignmentRequestId,
                requestEntries.Length,
                attendeeEntries.Length,
                assignmentEntries.Length,
                context.ChangeTracker.Entries().All(entry =>
                    entry.State == EntityState.Added));

            await coordinator.SignalAndWaitAsync(cancellationToken);

            return await inner.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class AsyncPreSaveCoordinator(int participantCount)
    {
        private readonly TaskCompletionSource<bool> release = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int arrivals;

        internal async Task SignalAndWaitAsync(
            CancellationToken cancellationToken)
        {
            var arrival = Interlocked.Increment(ref arrivals);

            if (arrival > participantCount)
            {
                throw new InvalidOperationException(
                    $"The coordinator accepts exactly {participantCount} participants.");
            }

            if (arrival == participantCount)
            {
                release.TrySetResult(true);
            }

            await release.Task.WaitAsync(cancellationToken);
        }

        internal void Abort(Exception exception)
        {
            release.TrySetException(exception);
        }
    }

    private sealed class ScopedSpotSelection
    {
        internal SpotCode? SpotCode { get; private set; }

        internal void Select(SpotCode spotCode)
        {
            SpotCode = spotCode
                ?? throw new ArgumentNullException(nameof(spotCode));
        }
    }

    private sealed class SelectedPostgreSqlAvailableSpotProvider(
        PostgreSqlAvailableSpotProvider inner,
        ScopedSpotSelection selection)
        : IAvailableSpotProvider
    {
        public async Task<IReadOnlyList<Spot>> GetAvailableSpotsAsync(
            Festival.Domain.FestivalDays.FestivalDayId festivalDayId,
            CancellationToken cancellationToken = default)
        {
            var availableSpots = await inner.GetAvailableSpotsAsync(
                festivalDayId,
                cancellationToken);

            var selectedSpot = selection.SpotCode
                ?? throw new InvalidOperationException(
                    "A Spot must be selected before executing the request.");
            var filteredSpots = availableSpots
                .Where(spot => spot.Code == selectedSpot)
                .ToArray();

            return Array.AsReadOnly(filteredSpots);
        }
    }

    private sealed class TestConfiguration : IConfigurationSection
    {
        private readonly IReadOnlyDictionary<string, string?> values;

        internal TestConfiguration(string connectionString)
            : this(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:FestivalDatabase"] = connectionString
                },
                string.Empty)
        {
        }

        private TestConfiguration(
            IReadOnlyDictionary<string, string?> values,
            string path)
        {
            this.values = values;
            Path = path;
            Key = path.Split(':').LastOrDefault() ?? string.Empty;
        }

        public string? this[string key]
        {
            get => values.GetValueOrDefault(CombinePath(key));
            set => throw new NotSupportedException();
        }

        public string Key { get; }

        public string Path { get; }

        public string? Value
        {
            get => values.GetValueOrDefault(Path);
            set => throw new NotSupportedException();
        }

        public IEnumerable<IConfigurationSection> GetChildren() => [];

        public IChangeToken GetReloadToken()
        {
            return new CancellationChangeToken(CancellationToken.None);
        }

        public IConfigurationSection GetSection(string key)
        {
            return new TestConfiguration(values, CombinePath(key));
        }

        private string CombinePath(string key)
        {
            return string.IsNullOrEmpty(Path)
                ? key
                : $"{Path}:{key}";
        }
    }
}
