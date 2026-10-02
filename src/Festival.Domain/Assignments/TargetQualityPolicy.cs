namespace Festival.Domain.Assignments;

public static class TargetQualityPolicy
{
    private const decimal GoodThreshold = 1.00m;

    /// <summary>
    /// Returns the target for an individual or group score using the inclusive
    /// Stage 4 baseline threshold, pending evaluation in Stage 5.
    /// </summary>
    public static TargetQuality Calculate(decimal rotationScore)
    {
        return rotationScore >= GoodThreshold ? TargetQuality.Good : TargetQuality.Medium;
    }
}
