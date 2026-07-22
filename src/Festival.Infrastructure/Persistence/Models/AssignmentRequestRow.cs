using Festival.Domain.Assignments;
using Festival.Domain.FestivalDays;

namespace Festival.Infrastructure.Persistence.Models;

internal sealed class AssignmentRequestRow
{
    public AssignmentRequestId AssignmentRequestId { get; set; }

    public FestivalDayId FestivalDayId { get; set; }

    public DateTimeOffset RequestedAt { get; set; }

    public AssignmentRequestStatus Status { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public string? RejectionCode { get; set; }

    public string? RejectionMessage { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureMessage { get; set; }

    public List<AssignmentRequestAttendeeRow> Attendees { get; set; } = [];
}