namespace Festival.Domain.Assignments;

/// <summary>
/// Location eligibility shared by all attendees in one AssignmentRequest.
/// Eligibility does not alter business-defined ExperienceQuality.
/// </summary>
public sealed record RequestEligibility
{
    public bool AllowsFrontStanding { get; }

    public RequestEligibility(bool allowsFrontStanding)
    {
        AllowsFrontStanding = allowsFrontStanding;
    }
}
