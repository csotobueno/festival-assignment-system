using Festival.Domain.Assignments;
using Festival.Domain.Attendees;

namespace Festival.Infrastructure.Persistence.Models;

internal sealed class AssignmentRequestAttendeeRow
{
    public AssignmentRequestId AssignmentRequestId { get; set; }

    public int Position { get; set; }

    public AttendeeCode AttendeeCode { get; set; } = null!;

    public AssignmentRequestRow AssignmentRequest { get; set; } = null!;
}