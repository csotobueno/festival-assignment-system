using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;
using Festival.Infrastructure.Persistence.Mappers;
using Festival.Infrastructure.Persistence.Models;

namespace Festival.Infrastructure.IntegrationTests.Infrastructure;

internal static class IntegrationTestData
{
    internal static readonly FestivalDayId FestivalDayId =
        FestivalDayId.Create(
            Guid.Parse("10000000-0000-0000-0000-000000000001"));

    internal static readonly FestivalDayId SecondFestivalDayId =
        FestivalDayId.Create(
            Guid.Parse("10000000-0000-0000-0000-000000000002"));

    internal static readonly ZoneId ZoneId =
        ZoneId.Create(
            Guid.Parse("20000000-0000-0000-0000-000000000001"));

    internal static readonly AttendeeId FirstAttendeeId =
        AttendeeId.Create(
            Guid.Parse("30000000-0000-0000-0000-000000000001"));

    internal static readonly AttendeeId SecondAttendeeId =
        AttendeeId.Create(
            Guid.Parse("30000000-0000-0000-0000-000000000002"));

    internal static readonly AssignmentRequestId RequestId =
        AssignmentRequestId.Create(
            Guid.Parse("40000000-0000-0000-0000-000000000001"));

    internal static readonly AssignmentRequestId SecondRequestId =
        AssignmentRequestId.Create(
            Guid.Parse("40000000-0000-0000-0000-000000000002"));

    internal static readonly DateTimeOffset RequestedAt =
        new(2026, 8, 15, 14, 0, 0, TimeSpan.Zero);

    internal static readonly DateTimeOffset ResolvedAt =
        new(2026, 8, 15, 14, 5, 0, TimeSpan.Zero);

    internal static readonly DateTimeOffset AssignedAt =
        new(2026, 8, 15, 14, 6, 0, TimeSpan.Zero);

    internal static FestivalDay CreateFestivalDay(
        FestivalDayId? id = null,
        DateOnly? date = null)
    {
        return FestivalDay.Create(
            id ?? FestivalDayId,
            date ?? new DateOnly(2026, 8, 15),
            AssignmentWindow.Create(
                new TimeOnly(9, 0),
                new TimeOnly(18, 0)));
    }

    internal static Attendee CreateAttendee(
        AttendeeId? id = null,
        string code = "ATT-001",
        string name = "Ada Lovelace")
    {
        return Attendee.Create(
            id ?? FirstAttendeeId,
            AttendeeCode.Create(code),
            name);
    }

    internal static Zone CreateZone()
    {
        return Zone.Create(ZoneId, "Front");
    }

    internal static Spot CreateSpot(
        string code = "FR-A-001",
        string rowCode = "A",
        int number = 1)
    {
        return Spot.Create(
            SpotCode.Create(code),
            ZoneId,
            RowCode.Create(rowCode),
            SpotNumber.Create(number));
    }

    internal static AssignmentRequest CreateRequest(
        AssignmentRequestStatus status = AssignmentRequestStatus.Received,
        AssignmentRequestId? id = null,
        FestivalDayId? festivalDayId = null,
        IReadOnlyCollection<AttendeeCode>? attendeeCodes = null)
    {
        var request = AssignmentRequest.Create(
            id ?? RequestId,
            festivalDayId ?? FestivalDayId,
            attendeeCodes ??
            [
                AttendeeCode.Create("ATT-001"),
                AttendeeCode.Create("ATT-002"),
                AttendeeCode.Create("ATT-003")
            ],
            RequestedAt);

        switch (status)
        {
            case AssignmentRequestStatus.Completed:
                request.Complete(ResolvedAt);
                break;
            case AssignmentRequestStatus.Rejected:
                request.Reject(
                    AssignmentRequestRejection.Create(
                        "NO_CONTIGUOUS_SPOTS",
                        "No contiguous spots were available."),
                    ResolvedAt);
                break;
            case AssignmentRequestStatus.Failed:
                request.Fail(
                    AssignmentRequestFailure.Create(
                        "DATABASE_FAILURE",
                        "The assignment could not be persisted."),
                    ResolvedAt);
                break;
        }

        return request;
    }

    internal static AssignmentRequestRow CreateRequestRow(
        AssignmentRequestStatus status = AssignmentRequestStatus.Received,
        AssignmentRequestId? id = null,
        FestivalDayId? festivalDayId = null,
        IReadOnlyCollection<AttendeeCode>? attendeeCodes = null)
    {
        return AssignmentRequestMapper.ToRow(
            CreateRequest(status, id, festivalDayId, attendeeCodes));
    }

    internal static Assignment CreateAssignment(
        Guid id,
        AssignmentRequestId? requestId = null,
        FestivalDayId? festivalDayId = null,
        AttendeeId? attendeeId = null,
        string spotCode = "FR-A-001",
        string rowCode = "A",
        int spotNumber = 1)
    {
        return Assignment.Create(
            AssignmentId.Create(id),
            requestId ?? RequestId,
            festivalDayId ?? FestivalDayId,
            attendeeId ?? FirstAttendeeId,
            SpotCode.Create(spotCode),
            ZoneId,
            RowCode.Create(rowCode),
            SpotNumber.Create(spotNumber),
            AssignedAt);
    }
}
