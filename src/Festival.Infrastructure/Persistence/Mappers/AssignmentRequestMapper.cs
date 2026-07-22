using Festival.Domain.Assignments;
using Festival.Infrastructure.Persistence.Models;

namespace Festival.Infrastructure.Persistence.Mappers;

public class AssignmentRequestMapper
{
    internal static AssignmentRequestRow ToRow(
        AssignmentRequest request)
    {
        var row = new AssignmentRequestRow
        {
            AssignmentRequestId = request.Id,
            FestivalDayId = request.FestivalDayId,
            RequestedAt = request.RequestedAt,
            Status = request.Status,
            ResolvedAt = request.ResolvedAt,
            RejectionCode = request.Rejection?.Code,
            RejectionMessage = request.Rejection?.Message,
            FailureCode = request.Failure?.Code,
            FailureMessage = request.Failure?.Message
        };

        row.Attendees = request.RequestedAttendeeCodes
            .Select((code, position) =>
                new AssignmentRequestAttendeeRow
                {
                    AssignmentRequestId = request.Id,
                    Position = position,
                    AttendeeCode = code,
                    AssignmentRequest = row
                })
            .ToList();

        return row;
    }

    internal static AssignmentRequest ToDomain(
        AssignmentRequestRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        ValidateAttendeeRows(row);

        var attendeeCodes = row.Attendees
            .OrderBy(attendee => attendee.Position)
            .Select(attendee => attendee.AttendeeCode)
            .ToArray();

        var rejection = CreateRejection(row);
        var failure = CreateFailure(row);

        return AssignmentRequest.Rehydrate(
            row.AssignmentRequestId,
            row.FestivalDayId,
            attendeeCodes,
            row.RequestedAt,
            row.Status,
            row.ResolvedAt,
            rejection,
            failure);
    }

    private static void ValidateAttendeeRows(
        AssignmentRequestRow row)
    {
        if (row.Attendees.Any(attendee => attendee.Position < 0))
        {
            throw new InvalidOperationException(
                "Assignment request attendee positions cannot be negative.");
        }

        if (row.Attendees
            .GroupBy(attendee => attendee.Position)
            .Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException(
                "Assignment request attendee positions must be unique.");
        }
    }

    private static AssignmentRequestRejection? CreateRejection(
        AssignmentRequestRow row)
    {
        if (row.RejectionCode is null && row.RejectionMessage is null)
        {
            return null;
        }

        if (row.RejectionCode is null || row.RejectionMessage is null)
        {
            throw new InvalidOperationException(
                "Assignment request rejection data must include both code and message.");
        }

        return AssignmentRequestRejection.Create(
            row.RejectionCode,
            row.RejectionMessage);
    }

    private static AssignmentRequestFailure? CreateFailure(
        AssignmentRequestRow row)
    {
        if (row.FailureCode is null && row.FailureMessage is null)
        {
            return null;
        }

        if (row.FailureCode is null || row.FailureMessage is null)
        {
            throw new InvalidOperationException(
                "Assignment request failure data must include both code and message.");
        }

        return AssignmentRequestFailure.Create(
            row.FailureCode,
            row.FailureMessage);
    }
}
