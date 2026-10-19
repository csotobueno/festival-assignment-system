using System.Collections.ObjectModel;
using Festival.Application.Assignments.Ports;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.Assignments.PostgreSql;

public sealed class PostgreSqlFairnessHistoryProvider : IFairnessHistoryProvider
{
    private readonly FestivalDbContext dbContext;

    public PostgreSqlFairnessHistoryProvider(FestivalDbContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyDictionary<AttendeeId, FairnessHistory>> GetHistoriesAsync(
        IReadOnlyCollection<AttendeeId> attendeeIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attendeeIds);
        cancellationToken.ThrowIfCancellationRequested();

        var requestedIds = attendeeIds.Distinct().ToArray();
        if (requestedIds.Any(id => id == default))
        {
            throw new ArgumentException("Attendee ids cannot be empty.", nameof(attendeeIds));
        }

        if (requestedIds.Length == 0)
        {
            return new ReadOnlyDictionary<AttendeeId, FairnessHistory>(
                new Dictionary<AttendeeId, FairnessHistory>());
        }

        var outcomes = await dbContext.Assignments
            .AsNoTracking()
            .Where(assignment => requestedIds.Contains(assignment.AttendeeId))
            .OrderBy(assignment => assignment.AssignedAt)
            .ThenBy(assignment => assignment.Id)
            .Select(assignment => new { assignment.AttendeeId, assignment.ExperienceQuality })
            .ToArrayAsync(cancellationToken);

        var outcomesByAttendee = outcomes.ToLookup(
            outcome => outcome.AttendeeId, outcome => outcome.ExperienceQuality);

        return new ReadOnlyDictionary<AttendeeId, FairnessHistory>(
            requestedIds.ToDictionary(id => id, id => FairnessHistory.Create(outcomesByAttendee[id])));
    }
}
