using Festival.Domain.Attendees;
using Festival.Infrastructure.Assignments.InMemory;

namespace Festival.Infrastructure.Tests.Assignments.InMemory;

public sealed class InMemoryAttendeeCodeResolverTests
{
    [Fact]
    public async Task ResolveAttendeeIdsAsync_ShouldResolveAttendeesByCode()
    {
        var attendee = CreateAttendee(1);
        var resolver = new InMemoryAttendeeCodeResolver([attendee]);

        var attendeeIds = await resolver.ResolveAttendeeIdsAsync(
            [attendee.Code]);

        var attendeeId = Assert.Single(attendeeIds);

        Assert.Equal(attendee.Id, attendeeId);
    }

    [Fact]
    public async Task ResolveAttendeeIdsAsync_ShouldReturnOnlyAttendeesMatchingRequestedCodes()
    {
        var firstAttendee = CreateAttendee(1);
        var secondAttendee = CreateAttendee(2);
        var resolver = new InMemoryAttendeeCodeResolver(
            [firstAttendee, secondAttendee]);

        var attendeeIds = await resolver.ResolveAttendeeIdsAsync(
            [secondAttendee.Code]);

        var attendeeId = Assert.Single(attendeeIds);

        Assert.Equal(secondAttendee.Id, attendeeId);
    }

    [Fact]
    public async Task ResolveAttendeeIdsAsync_ShouldIgnoreUnknownAttendeeCodes()
    {
        var attendee = CreateAttendee(1);
        var resolver = new InMemoryAttendeeCodeResolver([attendee]);

        var attendeeIds = await resolver.ResolveAttendeeIdsAsync(
            [
                attendee.Code,
                AttendeeCode.Create("UNKNOWN")
            ]);

        var attendeeId = Assert.Single(attendeeIds);

        Assert.Equal(attendee.Id, attendeeId);
    }

    private static Attendee CreateAttendee(int number)
    {
        return Attendee.Create(
            AttendeeId.Create(
                Guid.Parse($"10000000-0000-0000-0000-{number:000000000000}")),
            AttendeeCode.Create($"ATT-{number:000}"),
            $"Attendee {number}");
    }
}
