using System.Collections.Immutable;

namespace Festival.Simulation;

public static class ArrivalDistributionGenerator
{
    public static ImmutableArray<DailyRequestPlan> Generate(IReadOnlyList<DailyRequestPlan> requests,
        ImmutableArray<ArrivalTimeBand> profile, DateOnly date, DeterministicRandom random)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(random);
        Validate(profile);
        if (requests.Select(request => request.SimulationRequestId).Distinct().Count() != requests.Count)
            throw new ArgumentException("Arrival groups require unique simulation request IDs.", nameof(requests));
        var counts = PercentageAllocation.Allocate(requests.Count,
            profile.Select(band => band.Percentage).ToArray());
        var shuffled = requests.ToArray();
        random.Shuffle(shuffled);
        var next = 0;
        for (var band = 0; band < profile.Length; band++)
        {
            // v2 interprets profile clock times as UTC business time, not local wall time.
            var start = new DateTimeOffset(date.ToDateTime(profile[band].Start), TimeSpan.Zero);
            var ticks = profile[band].End.Ticks - profile[band].Start.Ticks;
            for (var index = 0; index < counts[band]; index++)
            {
                var requestedAt = start.AddTicks(random.NextInt64(ticks));
                shuffled[next] = shuffled[next] with { RequestedAt = requestedAt, AssignedAt = requestedAt };
                next++;
            }
        }
        return shuffled.OrderBy(request => request.RequestedAt)
            .ThenBy(request => request.SimulationRequestId).ToImmutableArray();
    }

    internal static void Validate(ImmutableArray<ArrivalTimeBand> profile)
    {
        if (profile.IsDefaultOrEmpty || profile.Any(band => band is null || band.Start >= band.End))
            throw new ArgumentException("Arrival bands must have Start < End within a day.", nameof(profile));
        PercentageAllocation.Validate(profile.Select(band => band.Percentage).ToArray());
        for (var index = 1; index < profile.Length; index++)
            if (profile[index].Start < profile[index - 1].End)
                throw new ArgumentException("Arrival bands must be ordered and non-overlapping.", nameof(profile));
    }
}
