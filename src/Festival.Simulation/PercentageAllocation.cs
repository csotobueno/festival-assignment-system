namespace Festival.Simulation;

internal static class PercentageAllocation
{
    internal static void Validate(IReadOnlyList<decimal> percentages)
    {
        if (percentages.Count == 0 || percentages.Any(value => value < 0 || value > 1)
            || percentages.Sum() != 1m)
            throw new ArgumentException("Profile percentages must be nonnegative decimal ratios totaling 1.");
    }

    // Hamilton / largest remainder; equal remainders favor the declared band order.
    internal static int[] Allocate(int count, IReadOnlyList<decimal> percentages)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));
        Validate(percentages);
        var quotas = percentages.Select(value => count * value).ToArray();
        var result = quotas.Select(value => (int)decimal.Floor(value)).ToArray();
        var remaining = count - result.Sum();
        foreach (var index in Enumerable.Range(0, result.Length)
                     .OrderByDescending(index => quotas[index] - result[index]).ThenBy(index => index)
                     .Take(remaining))
            result[index]++;
        return result;
    }
}
