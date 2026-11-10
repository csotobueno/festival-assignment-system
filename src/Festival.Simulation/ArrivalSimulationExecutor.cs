using System.Collections.Immutable;
using System.Diagnostics;

namespace Festival.Simulation;

internal sealed record ScheduledSimulationRequest(DailyRequestPlan Request, TimeSpan Offset);

internal static class ArrivalSimulationExecutor
{
    internal static ImmutableArray<ScheduledSimulationRequest> CreateSchedule(DailyPlan day, ArrivalSchedule schedule)
    {
        var requests = day.Requests.OrderBy(request => request.Sequence).ToArray();
        if (requests.Any(request => request.Sequence <= 0 || request.ExecutionSequence != request.Sequence)
            || requests.Select(request => request.Sequence).Distinct().Count() != requests.Length)
            throw new ArgumentException("Arrival scheduling preserves unique baseline dispatch positions.", nameof(day));
        return requests.Select((request, ordinal) => new ScheduledSimulationRequest(request, schedule.OffsetAt(ordinal)))
            .ToImmutableArray();
    }

    internal static async Task<IReadOnlyList<SimulationAttemptRecord>> ExecuteDayAsync(DailyPlan day,
        ArrivalSchedule schedule, long timingOrigin,
        Func<DailyRequestPlan, TimeSpan, CancellationToken, Task<SimulationAttemptRecord>> executeRequest,
        CancellationToken cancellationToken = default)
    {
        var planned = CreateSchedule(day, schedule);
        var dayOrigin = Stopwatch.GetElapsedTime(timingOrigin);
        var tasks = new List<Task<SimulationAttemptRecord>>(planned.Length);
        try
        {
            foreach (var entry in planned)
            {
                var due = dayOrigin + entry.Offset;
                // Absolute deadlines prevent request duration or timer overshoot
                // from becoming accumulated inter-arrival drift.
                var remaining = due - Stopwatch.GetElapsedTime(timingOrigin);
                while (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining, cancellationToken);
                    remaining = due - Stopwatch.GetElapsedTime(timingOrigin);
                }
                cancellationToken.ThrowIfCancellationRequested();
                // Scope preparation / synchronous portions of ExecuteAsync must not
                // block the arrival producer. No readiness gate or worker throttle.
                tasks.Add(Task.Run(() => executeRequest(entry.Request, due, cancellationToken), CancellationToken.None));
            }
        }
        finally
        {
            // Drain owned requests even on cancellation/preparation failure before
            // disposing the run's provider/container. Never start the next day early.
            await Task.WhenAll(tasks);
        }
        return tasks.Select(task => task.Result).ToArray();
    }
}
