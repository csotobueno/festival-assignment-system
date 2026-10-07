using Festival.Domain.Assignments;
using Festival.Domain.Zones;

namespace Festival.Domain.Spots;

public sealed class FeasibleSpotBlock
{
    public IReadOnlyList<Spot> Spots { get; }

    public ZoneId ZoneId => Spots[0].ZoneId;

    public RowCode RowCode => Spots[0].RowCode;

    private FeasibleSpotBlock(Spot[] spots)
    {
        Spots = Array.AsReadOnly(spots);
    }

    public static FeasibleSpotBlock Create(IEnumerable<Spot> spots)
    {
        ArgumentNullException.ThrowIfNull(spots);
        var items = spots.ToArray();

        if (items.Any(spot => spot is null))
        {
            throw new ArgumentException("Spots cannot contain null values.", nameof(spots));
        }

        GroupSize.Create(items.Length);

        if (items.Select(spot => spot.ZoneId).Distinct().Count() != 1
            || items.Select(spot => spot.RowCode).Distinct().Count() != 1)
        {
            throw new ArgumentException("A feasible block must belong to one zone and row.", nameof(spots));
        }

        if (items.Select(spot => spot.Code).Distinct().Count() != items.Length)
        {
            throw new ArgumentException("A feasible block cannot contain duplicate spot codes.", nameof(spots));
        }

        var ordered = items.OrderBy(spot => spot.Number.Value).ToArray();
        for (var index = 1; index < ordered.Length; index++)
        {
            if ((long)ordered[index].Number.Value != (long)ordered[index - 1].Number.Value + 1)
            {
                throw new ArgumentException("A feasible block must contain consecutive spot numbers.", nameof(spots));
            }
        }

        return new FeasibleSpotBlock(ordered);
    }
}
