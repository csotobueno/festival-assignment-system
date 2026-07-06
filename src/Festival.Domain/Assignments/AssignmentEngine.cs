using Festival.Domain.Spots;

namespace Festival.Domain.Assignments;

public sealed class AssignmentEngine
{
    public AssignmentEngineResult Assign(
        AssignmentGroup assignmentGroup,
        IEnumerable<Spot> availableSpots,
        DateTimeOffset assignedAt)
    {
        ArgumentNullException.ThrowIfNull(assignmentGroup);
        ArgumentNullException.ThrowIfNull(availableSpots);

        var spots = availableSpots.ToArray();

        if (spots.Any(spot => spot is null))
        {
            throw new ArgumentException(
                "Available spots cannot contain null values.",
                nameof(availableSpots));
        }

        var block = FindFirstContiguousBlock(
            spots,
            assignmentGroup.GroupSize.Value);

        if (block is null)
        {
            return AssignmentEngineResult.Unassigned();
        }

        var assignments = assignmentGroup.AttendeeIds
            .Zip(block)
            .Select(pair => Assignment.Create(
                AssignmentId.New(),
                assignmentGroup.AssignmentRequestId,
                assignmentGroup.FestivalDayId,
                pair.First,
                pair.Second.Code,
                pair.Second.ZoneId,
                pair.Second.RowCode,
                pair.Second.Number,
                assignedAt))
            .ToArray();

        assignmentGroup.EnsureValidResult(assignments);

        return AssignmentEngineResult.Assigned(assignments);
    }

    private static IReadOnlyList<Spot>? FindFirstContiguousBlock(
        IEnumerable<Spot> spots,
        int requiredSize)
    {
        var groupedSpots = spots
            .GroupBy(spot => new
            {
                spot.ZoneId,
                spot.RowCode
            });

        foreach (var group in groupedSpots)
        {
            var ordered = group
                .OrderBy(spot => spot.Number.Value)
                .ToArray();

            for (var index = 0; index <= ordered.Length - requiredSize; index++)
            {
                var candidate = ordered
                    .Skip(index)
                    .Take(requiredSize)
                    .ToArray();

                if (IsConsecutive(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static bool IsConsecutive(IReadOnlyList<Spot> spots)
    {
        for (var index = 1; index < spots.Count; index++)
        {
            var previous = spots[index - 1].Number.Value;
            var current = spots[index].Number.Value;

            if (current != previous + 1)
            {
                return false;
            }
        }

        return true;
    }
}