namespace Festival.Domain.Assignments;

/// <summary>
/// An attendee's already-classified, real completed assignment experiences.
/// Absences have no entry, and recorded quality remains unchanged.
/// </summary>
public sealed class FairnessHistory
{
    /// <summary>Experiences ordered from oldest to most recent.</summary>
    public IReadOnlyList<ExperienceQuality> Experiences { get; }

    private FairnessHistory(IReadOnlyList<ExperienceQuality> experiences)
    {
        Experiences = experiences;
    }

    /// <summary>
    /// Copies experiences supplied in chronological order, oldest first.
    /// The caller supplies only real completed assignments; no previous
    /// assignments are represented by an empty sequence.
    /// </summary>
    public static FairnessHistory Create(IEnumerable<ExperienceQuality> experiences)
    {
        ArgumentNullException.ThrowIfNull(experiences);

        var qualities = experiences.ToArray();

        if (qualities.Any(quality => !Enum.IsDefined(quality)))
        {
            throw new ArgumentException(
                "Experiences cannot contain undefined experience quality values.",
                nameof(experiences));
        }

        return new FairnessHistory(Array.AsReadOnly(qualities));
    }
}
