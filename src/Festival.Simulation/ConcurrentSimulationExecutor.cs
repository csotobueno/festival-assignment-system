using System.Collections.Immutable;

namespace Festival.Simulation;

internal sealed record SimulationRequestWindow(int Number, ImmutableArray<DailyRequestPlan> Requests);
internal sealed record SimulationWindowPosition(int Number, int Position);

// Only dispatch coordination belongs here. Scope preparation and the real use case
// remain in the runner; no gate is placed inside business reads or persistence.
internal static class ConcurrentSimulationExecutor
{
    internal static ImmutableArray<SimulationRequestWindow> CreateWindows(DailyPlan day,
        SimulationExecutionConfiguration configuration)
    {
        if (configuration.ExecutionMode != SimulationExecutionMode.Concurrent)
            throw new ArgumentException("Concurrent windows require Concurrent mode.", nameof(configuration));
        var requests = day.Requests.OrderBy(request => request.Sequence).ToArray();
        if (requests.Any(request => request.Sequence <= 0 || request.ExecutionSequence != request.Sequence)
            || requests.Select(request => request.Sequence).Distinct().Count() != requests.Length)
            throw new ArgumentException("Concurrent execution preserves unique baseline dispatch positions.", nameof(day));
        return requests.Chunk(configuration.ConcurrencyLevel)
            .Select((window, index) => new SimulationRequestWindow(index + 1, window.ToImmutableArray()))
            .ToImmutableArray();
    }

    internal static async Task<IReadOnlyList<SimulationAttemptRecord>> ExecuteDayAsync(DailyPlan day,
        SimulationExecutionConfiguration configuration,
        Func<DailyRequestPlan, SimulationWindowPosition, Func<Task>, CancellationToken,
            Task<SimulationAttemptRecord>> executePreparedRequest,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SimulationAttemptRecord>();
        foreach (var window in CreateWindows(day, configuration))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var readyCount = 0;
            var tasks = window.Requests.Select((request, index) => ExecuteAsync(request, index + 1)).ToArray();
            // WhenAll drains every request, including disposal/reconciliation, even
            // on cancellation or preparation failure. No next window is started.
            results.AddRange(await Task.WhenAll(tasks));

            async Task<SimulationAttemptRecord> ExecuteAsync(DailyRequestPlan request, int position)
            {
                try
                {
                    return await executePreparedRequest(request, new(window.Number, position), ReadyAsync,
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    // A preparation/cancellation failure must release waiters too.
                    // It is infrastructure failure, never a synthetic conflict.
                    release.TrySetException(exception);
                    throw;
                }
            }

            Task ReadyAsync()
            {
                if (Interlocked.Increment(ref readyCount) == window.Requests.Length)
                    release.TrySetResult();
                return release.Task.WaitAsync(cancellationToken);
            }
        }
        return results.AsReadOnly();
    }
}
