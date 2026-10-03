namespace Festival.Domain.Assignments;

/// <summary>
/// Business-defined assignment experience classification, unchanged by request eligibility.
/// </summary>
public enum ExperienceQuality
{
    /// <summary>A favorable assignment experience.</summary>
    Good,

    /// <summary>A neutral assignment experience.</summary>
    Medium,

    /// <summary>An unfavorable assignment experience.</summary>
    Bad
}
