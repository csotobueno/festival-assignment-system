namespace Festival.Domain.Assignments;

public static class GroupRotationScore
{
    /// <summary>
    /// Returns the arithmetic mean of individual RotationScores for the current
    /// request's members. Each member contributes one independent history,
    /// including an empty history for a new attendee.
    /// </summary>
    public static decimal Calculate(IEnumerable<FairnessHistory> memberHistories)
    {
        ArgumentNullException.ThrowIfNull(memberHistories);

        var histories = memberHistories.ToArray();

        if (histories.Any(history => history is null))
        {
            throw new ArgumentException(
                "Member histories cannot contain null values.",
                nameof(memberHistories));
        }

        var groupSize = GroupSize.Create(histories.Length);

        return histories.Sum(history => RotationScore.Calculate(history)) / groupSize.Value;
    }
}
