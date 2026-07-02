using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;

namespace Festival.Domain.Tests.Assignments;

public sealed class AssignmentGroupTests
{
    [Fact]
    public void Create_ShouldReturnAssignmentGroup_WhenDataIsValid()
    {
        var assignmentRequestId = AssignmentRequestId.New();
        var festivalDayId = FestivalDayId.New();
        var attendeeIds = CreateAttendeeIds(3);

        var group = AssignmentGroup.Create(
            assignmentRequestId,
            festivalDayId,
            attendeeIds);

        Assert.Equal(assignmentRequestId, group.AssignmentRequestId);
        Assert.Equal(festivalDayId, group.FestivalDayId);
        Assert.Equal(attendeeIds, group.AttendeeIds);
        Assert.Equal(GroupSize.Create(3), group.GroupSize);
    }

    [Fact]
    public void Create_ShouldAllowOneAttendee()
    {
        var group = CreateGroup(CreateAttendeeIds(1));

        Assert.Single(group.AttendeeIds);
        Assert.Equal(1, group.GroupSize.Value);
    }

    [Fact]
    public void Create_ShouldAllowTenAttendees()
    {
        var group = CreateGroup(CreateAttendeeIds(10));

        Assert.Equal(10, group.AttendeeIds.Count);
        Assert.Equal(10, group.GroupSize.Value);
    }

    [Fact]
    public void Create_ShouldThrow_WhenNoAttendeesAreProvided()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateGroup([]));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenMoreThanTenAttendeesAreProvided()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateGroup(CreateAttendeeIds(11)));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenAttendeeIdsAreDuplicated()
    {
        var attendeeId = CreateAttendeeIds(1)[0];

        var exception = Assert.Throws<ArgumentException>(
            () => CreateGroup([attendeeId, attendeeId]));

        Assert.Equal("attendeeIds", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenAttendeeIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateGroup([default]));

        Assert.Equal("attendeeIds", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenAssignmentRequestIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => AssignmentGroup.Create(
                default,
                FestivalDayId.New(),
                CreateAttendeeIds(1)));

        Assert.Equal("assignmentRequestId", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenFestivalDayIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => AssignmentGroup.Create(
                AssignmentRequestId.New(),
                default,
                CreateAttendeeIds(1)));

        Assert.Equal("festivalDayId", exception.ParamName);
    }

    [Fact]
    public void AttendeeIds_ShouldNotExposeMutableCollection()
    {
        var attendeeIds = CreateAttendeeIds(2).ToList();
        var originalFirstAttendeeId = attendeeIds[0];
        var group = CreateGroup(attendeeIds);

        attendeeIds[0] = CreateAttendeeIds(1)[0];

        Assert.Equal(originalFirstAttendeeId, group.AttendeeIds[0]);
        Assert.Throws<NotSupportedException>(
            () => ((IList<AttendeeId>)group.AttendeeIds)
                .Add(CreateAttendeeIds(1)[0]));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void GroupSize_ShouldMatchActualAttendeeCount(int attendeeCount)
    {
        var group = CreateGroup(CreateAttendeeIds(attendeeCount));

        Assert.Equal(group.AttendeeIds.Count, group.GroupSize.Value);
    }

    private static AssignmentGroup CreateGroup(
        IEnumerable<AttendeeId> attendeeIds)
    {
        return AssignmentGroup.Create(
            AssignmentRequestId.New(),
            FestivalDayId.New(),
            attendeeIds);
    }

    private static AttendeeId[] CreateAttendeeIds(int count)
    {
        return Enumerable
            .Range(1, count)
            .Select(number => AttendeeId.Create(
                new Guid(number, 0, 0, new byte[8])))
            .ToArray();
    }
}
