using System.Collections.ObjectModel;
using Festival.Application.Assignments.Ports;
using Festival.Domain.Assignments;
using Festival.Domain.Attendees;

namespace Festival.Infrastructure.Assignments.InMemory;

public sealed class InMemoryFairnessHistoryProvider : IFairnessHistoryProvider
{
    private readonly InMemoryAssignmentRepository repository;

    public InMemoryFairnessHistoryProvider(InMemoryAssignmentRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<IReadOnlyDictionary<AttendeeId, FairnessHistory>> GetHistoriesAsync(
        IReadOnlyCollection<AttendeeId> attendeeIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attendeeIds);
        cancellationToken.ThrowIfCancellationRequested();
        if (attendeeIds.Any(id => id == default))
        {
            throw new ArgumentException("Attendee ids cannot be empty.", nameof(attendeeIds));
        }

        var outcomesByAttendee = repository.Assignments
            .OrderBy(assignment => assignment.AssignedAt)
            .ThenBy(assignment => assignment.Id.Value)
            .ToLookup(assignment => assignment.AttendeeId, assignment => assignment.ExperienceQuality);
        IReadOnlyDictionary<AttendeeId, FairnessHistory> histories =
            new ReadOnlyDictionary<AttendeeId, FairnessHistory>(attendeeIds.Distinct()
                .ToDictionary(id => id, id => FairnessHistory.Create(outcomesByAttendee[id])));
        return Task.FromResult(histories);
    }
}
