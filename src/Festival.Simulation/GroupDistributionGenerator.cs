using System.Collections.Immutable;

namespace Festival.Simulation;

public static class GroupDistributionGenerator
{
    public static ImmutableArray<int> Generate(int dailyRequesterCount,
        ImmutableArray<GroupSizeBand> profile, DeterministicRandom random)
    {
        if (dailyRequesterCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(dailyRequesterCount));
        ArgumentNullException.ThrowIfNull(random);
        Validate(profile);

        var mean = profile.Sum(band => (band.MinSize + band.MaxSize) / 2m * band.Percentage);
        var count = Math.Clamp((int)decimal.Round(dailyRequesterCount / mean, 0,
            MidpointRounding.AwayFromZero), (dailyRequesterCount - 1) / 10 + 1, dailyRequesterCount);
        var allocation = PercentageAllocation.Allocate(count, profile.Select(band => band.Percentage).ToArray());
        var sizes = profile.SelectMany((band, index) => Enumerable.Range(0, allocation[index])
            .Select(_ => random.NextInt(band.MinSize, band.MaxSize + 1))).ToArray();
        random.Shuffle(sizes);

        var difference = dailyRequesterCount - sizes.Sum();
        // First reconcile only inside each group's original band: band counts stay intact.
        for (var index = 0; index < sizes.Length && difference != 0; index++)
        {
            var band = profile[BandOf(sizes[index], profile)];
            var adjustment = Math.Clamp(difference, band.MinSize - sizes[index], band.MaxSize - sizes[index]);
            sizes[index] += adjustment;
            difference -= adjustment;
        }

        // Fixed-size/very small workloads can make those counts infeasible. Move one
        // member at a time, minimizing the incremental L1 band-count deviation. A size
        // outside the declared bands counts as an extra off-profile bucket. Ties use
        // shuffled group order. Each iteration reduces the finite membership difference.
        var counts = new int[profile.Length + 1];
        foreach (var size in sizes)
            counts[Bucket(size)]++;
        var direction = Math.Sign(difference);
        for (var step = 0; step < Math.Abs(difference); step++)
        {
            var selected = -1;
            var bestCost = decimal.MaxValue;
            for (var index = 0; index < sizes.Length; index++)
            {
                var next = sizes[index] + direction;
                if (next is < 1 or > 10)
                    continue;
                var from = Bucket(sizes[index]);
                var to = Bucket(next);
                var cost = from == to ? 0m
                    : Deviation(from, -1) + Deviation(to, 1);
                if (cost < bestCost)
                {
                    selected = index;
                    bestCost = cost;
                }
            }
            counts[Bucket(sizes[selected])]--;
            sizes[selected] += direction;
            counts[Bucket(sizes[selected])]++;
        }
        random.Shuffle(sizes);
        return sizes.ToImmutableArray();

        int Bucket(int size)
        {
            var band = BandOf(size, profile);
            return band < 0 ? profile.Length : band;
        }

        decimal Deviation(int band, int change)
        {
            var target = band == profile.Length ? 0 : count * profile[band].Percentage;
            return Math.Abs(counts[band] + change - target) - Math.Abs(counts[band] - target);
        }
    }

    internal static void Validate(ImmutableArray<GroupSizeBand> profile)
    {
        if (profile.IsDefaultOrEmpty || profile.Any(band => band is null
                || band.MinSize < 1 || band.MaxSize > 10 || band.MinSize > band.MaxSize))
            throw new ArgumentException("Group bands must have ordered size limits within 1..10.", nameof(profile));
        PercentageAllocation.Validate(profile.Select(band => band.Percentage).ToArray());
        if (Enumerable.Range(1, 10).Any(size => profile.Count(band =>
                size >= band.MinSize && size <= band.MaxSize) > 1))
            throw new ArgumentException("Group-size bands must not overlap.", nameof(profile));
    }

    private static int BandOf(int size, ImmutableArray<GroupSizeBand> profile)
    {
        for (var index = 0; index < profile.Length; index++)
            if (size >= profile[index].MinSize && size <= profile[index].MaxSize)
                return index;
        return -1;
    }
}
