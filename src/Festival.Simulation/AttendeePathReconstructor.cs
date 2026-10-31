using System.Collections.Immutable;
using Festival.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Festival.Simulation;

public static class AttendeePathReconstructor
{
    public static async Task<ImmutableArray<AttendeePathRecord>> ReconstructAsync(
        FestivalDbContext database, ScenarioManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(manifest);
        var evaluatedAttendees = manifest.DailyPlans.SelectMany(day => day.Requests)
            .SelectMany(request => request.Members).Select(member => member.Id)
            .Distinct().OrderBy(id => id.Value).ToArray();
        if (evaluatedAttendees.Length == 0)
            return [];

        var evaluatedDays = manifest.DailyPlans.Select(day => day.FestivalDayId).Distinct().ToArray();
        // One read-only query for the workload population and evaluated period.
        // Dates come from persisted FestivalDays, never from manifest intent.
        var evidence = await (
            from assignment in database.Assignments.AsNoTracking()
            join day in database.FestivalDays.AsNoTracking() on assignment.FestivalDayId equals day.Id
            where evaluatedAttendees.Contains(assignment.AttendeeId)
                && evaluatedDays.Contains(assignment.FestivalDayId)
            orderby assignment.AttendeeId, assignment.AssignedAt, assignment.Id
            select new
            {
                assignment.AttendeeId,
                Entry = new AttendeePathEntry(assignment.Id, assignment.FestivalDayId,
                    day.Date, assignment.AssignedAt, assignment.ExperienceQuality)
            }).ToArrayAsync(cancellationToken);

        var byAttendee = evidence.ToLookup(item => item.AttendeeId, item => item.Entry);
        return evaluatedAttendees.Select(id => PathClassificationRubric.Evaluate(id, byAttendee[id]))
            .ToImmutableArray();
    }
}
