namespace Festival.Domain.Assignments;

public static class ExperienceQualityExtensions
{
    /// <summary>
    /// Returns the contribution to fairness history; higher positive accumulated
    /// values represent greater recovery need.
    /// </summary>
    public static int GetFairnessContribution(this ExperienceQuality quality)
    {
        return quality switch
        {
            ExperienceQuality.Good => -1,
            ExperienceQuality.Medium => 0,
            ExperienceQuality.Bad => 1,
            _ => throw new ArgumentOutOfRangeException(
                nameof(quality), quality, "Experience quality must be Good, Medium, or Bad.")
        };
    }
}
