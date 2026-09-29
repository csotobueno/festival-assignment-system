namespace Festival.Domain.Assignments;

public static class RecentRecoveryNeed
{
    /// <summary>
    /// Returns the recovery need from the latest real assignment experience,
    /// or zero when there is no history. The v1 values are Good = -1,
    /// Medium = 0, and Bad = +1. This rule is independent of the fairness
    /// contributions used for historical accumulated balance.
    /// </summary>
    public static int Calculate(FairnessHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        if (history.Experiences.Count == 0)
        {
            return 0;
        }

        return history.Experiences[^1] switch
        {
            ExperienceQuality.Good => -1,
            ExperienceQuality.Medium => 0,
            ExperienceQuality.Bad => 1,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
