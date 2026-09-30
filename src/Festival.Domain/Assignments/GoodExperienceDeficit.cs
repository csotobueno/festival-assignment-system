namespace Festival.Domain.Assignments;

public static class GoodExperienceDeficit
{
    /// <summary>
    /// Returns +1 when an attendee has participated but never received Good,
    /// or zero when history is empty or contains at least one Good experience.
    /// </summary>
    public static int Calculate(FairnessHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        return history.Experiences.Count > 0
            && !history.Experiences.Contains(ExperienceQuality.Good)
                ? 1
                : 0;
    }
}
