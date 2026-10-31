using System.Collections.Immutable;
using Festival.Domain.Assignments;

namespace Festival.Simulation;

// Consumes already reconstructed, ordered persisted evidence. No attempts,
// calendar-day weighting, database reads or classification rules are involved.
public static class AttendeeRecoveryAnalyzer
{
    public static AttendeeRecoveryRecord Analyze(AttendeePathRecord path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var episodes = ImmutableArray.CreateBuilder<RecoveryEpisode>();
        var startIndex = -1;
        for (var index = 0; index < path.Entries.Length; index++)
        {
            var entry = path.Entries[index];
            if (entry.ExperienceQuality == ExperienceQuality.Bad)
            {
                if (startIndex < 0)
                    startIndex = index;
            }
            else if (startIndex >= 0
                && entry.ExperienceQuality is ExperienceQuality.Medium or ExperienceQuality.Good)
            {
                // Excludes the opening Bad, includes the recovery Assignment.
                episodes.Add(new RecoveryEpisode(path.AttendeeId, path.Entries[startIndex],
                    entry, index - startIndex));
                startIndex = -1;
            }
        }

        if (startIndex >= 0)
            episodes.Add(new RecoveryEpisode(path.AttendeeId, path.Entries[startIndex], null, null));

        return new AttendeeRecoveryRecord(path.AttendeeId, episodes.ToImmutable());
    }
}
