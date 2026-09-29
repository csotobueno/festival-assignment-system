namespace Festival.Domain.Assignments;

public static class HistoricalDeficit
{
    /// <summary>
    /// Returns the accumulated experience balance: positive for unfavorable
    /// experience, negative for favorable experience, and zero for neutral balance.
    /// </summary>
    public static int Calculate(FairnessHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        return history.Experiences.Sum(quality => quality.GetFairnessContribution());
    }
}
