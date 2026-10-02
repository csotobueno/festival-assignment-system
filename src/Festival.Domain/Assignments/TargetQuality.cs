namespace Festival.Domain.Assignments;

/// <summary>
/// Quality reference before assignment selection, distinct from actual ExperienceQuality.
/// It is neither an entitlement, a guaranteed outcome, nor a maximum allowed quality.
/// </summary>
public enum TargetQuality
{
    Good,
    Medium
}
