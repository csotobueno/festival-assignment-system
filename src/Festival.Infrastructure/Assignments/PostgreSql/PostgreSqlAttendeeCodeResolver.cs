using Festival.Application.Assignments.Ports;
using Festival.Domain.Attendees;
using Festival.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.Assignments.PostgreSql;

public sealed class PostgreSqlAttendeeCodeResolver
    : IAttendeeCodeResolver
{
    private readonly FestivalDbContext dbContext;

    public PostgreSqlAttendeeCodeResolver(FestivalDbContext dbContext)
    {
        this.dbContext = dbContext
            ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<AttendeeId>> ResolveAttendeeIdsAsync(
        IEnumerable<AttendeeCode> attendeeCodes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attendeeCodes);
        cancellationToken.ThrowIfCancellationRequested();

        var requestedCodes = attendeeCodes.ToArray();

        if (requestedCodes.Any(code => code is null))
        {
            throw new ArgumentException(
                "Attendee codes cannot contain null values.",
                nameof(attendeeCodes));
        }

        if (requestedCodes.Length == 0)
        {
            return Array.AsReadOnly(Array.Empty<AttendeeId>());
        }

        var resolvedAttendees = await dbContext.Attendees
            .AsNoTracking()
            .Where(attendee => requestedCodes.Contains(attendee.Code))
            .Select(attendee => new
            {
                attendee.Code,
                attendee.Id
            })
            .ToListAsync(cancellationToken);

        var attendeeIdsByCode = resolvedAttendees.ToDictionary(
            attendee => attendee.Code,
            attendee => attendee.Id);
        var attendeeIds = requestedCodes
            .Where(attendeeIdsByCode.ContainsKey)
            .Select(code => attendeeIdsByCode[code])
            .ToArray();

        return Array.AsReadOnly(attendeeIds);
    }
}
