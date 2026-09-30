namespace Festival.Domain.Assignments;

public static class RotationScore
{
    private const decimal HistoricalDeficitWeight = 1.00m;
    private const decimal RecentRecoveryNeedWeight = 0.50m;
    private const decimal GoodExperienceDeficitWeight = 0.25m;

    /// <summary>
    /// Returns current recovery need using the Stage 4 baseline weights.
    /// Higher scores indicate greater need; no history produces zero.
    /// These weights are provisional until evaluated in Stage 5.
    /// </summary>
    public static decimal Calculate(FairnessHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        return HistoricalDeficitWeight * HistoricalDeficit.Calculate(history)
            + RecentRecoveryNeedWeight * RecentRecoveryNeed.Calculate(history)
            + GoodExperienceDeficitWeight * GoodExperienceDeficit.Calculate(history);
    }
}
