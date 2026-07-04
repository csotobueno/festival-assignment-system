using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

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

    [Fact]
    public void EnsureValidResult_ShouldNotThrow_WhenResultIsValid()
    {
        var attendeeIds = CreateAttendeeIds(3);
        var group = CreateGroup(attendeeIds);
        var zoneId = ZoneId.New();
        var rowCode = RowCode.Create("A");

        var assignments = new[]
        {
            CreateAssignment(group, attendeeIds[0], zoneId, rowCode, 10),
            CreateAssignment(group, attendeeIds[1], zoneId, rowCode, 11),
            CreateAssignment(group, attendeeIds[2], zoneId, rowCode, 12)
        };

        var exception = Record.Exception(
            () => group.EnsureValidResult(assignments));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureValidResult_ShouldThrow_WhenAssignmentsIsNull()
    {
        var group = CreateGroup(CreateAttendeeIds(2));

        var exception = Assert.Throws<ArgumentNullException>(
            () => group.EnsureValidResult(null!));

        Assert.Equal("assignments", exception.ParamName);
    }

    [Fact]
    public void EnsureValidResult_ShouldThrow_WhenAssignmentsContainNullElement()
    {
        var attendeeIds = CreateAttendeeIds(2);
        var group = CreateGroup(attendeeIds);
        var zoneId = ZoneId.New();
        var rowCode = RowCode.Create("A");

        var assignments = new[]
        {
            CreateAssignment(group, attendeeIds[0], zoneId, rowCode, 10),
            null!
        };

        var exception = Assert.Throws<ArgumentException>(
            () => group.EnsureValidResult(assignments));

        Assert.Equal("assignments", exception.ParamName);
    }

    [Fact]
    public void EnsureValidResult_ShouldThrow_WhenResultIsIncomplete()
    {
        var attendeeIds = CreateAttendeeIds(3);
        var group = CreateGroup(attendeeIds);
        var zoneId = ZoneId.New();
        var rowCode = RowCode.Create("A");

        var assignments = new[]
        {
            CreateAssignment(group, attendeeIds[0], zoneId, rowCode, 10),
            CreateAssignment(group, attendeeIds[1], zoneId, rowCode, 11)
        };

        var exception = Assert.Throws<ArgumentException>(
            () => group.EnsureValidResult(assignments));

        Assert.Equal("assignments", exception.ParamName);
    }

    [Fact]
    public void EnsureValidResult_ShouldThrow_WhenResultHasExtraAssignment()
    {
        var attendeeIds = CreateAttendeeIds(2);
        var group = CreateGroup(attendeeIds);
        var zoneId = ZoneId.New();
        var rowCode = RowCode.Create("A");
        var extraAttendeeId = CreateAttendeeId(999);

        var assignments = new[]
        {
            CreateAssignment(group, attendeeIds[0], zoneId, rowCode, 10),
            CreateAssignment(group, attendeeIds[1], zoneId, rowCode, 11),
            CreateAssignment(group, extraAttendeeId, zoneId, rowCode, 12)
        };

        var exception = Assert.Throws<ArgumentException>(
            () => group.EnsureValidResult(assignments));

        Assert.Equal("assignments", exception.ParamName);
    }

    [Fact]
    public void EnsureValidResult_ShouldThrow_WhenAssignmentBelongsToAnotherAssignmentRequest()
    {
        var attendeeIds = CreateAttendeeIds(2);
        var group = CreateGroup(attendeeIds);
        var zoneId = ZoneId.New();
        var rowCode = RowCode.Create("A");

        var assignments = new[]
        {
            CreateAssignment(group, attendeeIds[0], zoneId, rowCode, 10),
            Assignment.Create(
                AssignmentId.New(),
                AssignmentRequestId.New(),
                group.FestivalDayId,
                attendeeIds[1],
                SpotCode.Create($"Z-{rowCode}-011"),
                zoneId,
                rowCode,
                SpotNumber.Create(11),
                AssignedAt)
        };

        var exception = Assert.Throws<ArgumentException>(
            () => group.EnsureValidResult(assignments));

        Assert.Equal("assignments", exception.ParamName);
    }

    [Fact]
    public void EnsureValidResult_ShouldThrow_WhenAssignmentBelongsToAnotherFestivalDay()
    {
        var attendeeIds = CreateAttendeeIds(2);
        var group = CreateGroup(attendeeIds);
        var zoneId = ZoneId.New();
        var rowCode = RowCode.Create("A");

        var assignments = new[]
        {
            CreateAssignment(group, attendeeIds[0], zoneId, rowCode, 10),
            Assignment.Create(
                AssignmentId.New(),
                group.AssignmentRequestId,
                FestivalDayId.New(),
                attendeeIds[1],
                SpotCode.Create($"Z-{rowCode}-011"),
                zoneId,
                rowCode,
                SpotNumber.Create(11),
                AssignedAt)
        };

        var exception = Assert.Throws<ArgumentException>(
            () => group.EnsureValidResult(assignments));

        Assert.Equal("assignments", exception.ParamName);
    }

    [Fact]
    public void EnsureValidResult_ShouldThrow_WhenAssignmentIsForAttendeeOutsideGroup()
    {
        var attendeeIds = CreateAttendeeIds(2);
        var group = CreateGroup(attendeeIds);
        var zoneId = ZoneId.New();
        var rowCode = RowCode.Create("A");
        var outsiderId = CreateAttendeeId(999);

        var assignments = new[]
        {
            CreateAssignment(group, attendeeIds[0], zoneId, rowCode, 10),
            CreateAssignment(group, outsiderId, zoneId, rowCode, 11)
        };

        var exception = Assert.Throws<ArgumentException>(
            () => group.EnsureValidResult(assignments));

        Assert.Equal("assignments", exception.ParamName);
    }

    [Fact]
    public void EnsureValidResult_ShouldThrow_WhenAttendeeHasDuplicatedAssignment()
    {
        var attendeeIds = CreateAttendeeIds(2);
        var group = CreateGroup(attendeeIds);
        var zoneId = ZoneId.New();
        var rowCode = RowCode.Create("A");

        var assignments = new[]
        {
            CreateAssignment(group, attendeeIds[0], zoneId, rowCode, 10),
            CreateAssignment(group, attendeeIds[0], zoneId, rowCode, 11)
        };

        var exception = Assert.Throws<ArgumentException>(
            () => group.EnsureValidResult(assignments));

        Assert.Equal("assignments", exception.ParamName);
    }

    [Fact]
    public void EnsureValidResult_ShouldThrow_WhenAssignmentsBelongToDifferentZones()
    {
        var attendeeIds = CreateAttendeeIds(2);
        var group = CreateGroup(attendeeIds);
        var rowCode = RowCode.Create("A");

        var assignments = new[]
        {
            CreateAssignment(group, attendeeIds[0], ZoneId.New(), rowCode, 10),
            CreateAssignment(group, attendeeIds[1], ZoneId.New(), rowCode, 11)
        };

        var exception = Assert.Throws<ArgumentException>(
            () => group.EnsureValidResult(assignments));

        Assert.Equal("assignments", exception.ParamName);
    }

    [Fact]
    public void EnsureValidResult_ShouldThrow_WhenAssignmentsBelongToDifferentRows()
    {
        var attendeeIds = CreateAttendeeIds(2);
        var group = CreateGroup(attendeeIds);
        var zoneId = ZoneId.New();

        var assignments = new[]
        {
            CreateAssignment(group, attendeeIds[0], zoneId, RowCode.Create("A"), 10),
            CreateAssignment(group, attendeeIds[1], zoneId, RowCode.Create("B"), 11)
        };

        var exception = Assert.Throws<ArgumentException>(
            () => group.EnsureValidResult(assignments));

        Assert.Equal("assignments", exception.ParamName);
    }

    [Fact]
    public void EnsureValidResult_ShouldThrow_WhenSpotNumbersAreNotConsecutive()
    {
        var attendeeIds = CreateAttendeeIds(2);
        var group = CreateGroup(attendeeIds);
        var zoneId = ZoneId.New();
        var rowCode = RowCode.Create("A");

        var assignments = new[]
        {
            CreateAssignment(group, attendeeIds[0], zoneId, rowCode, 10),
            CreateAssignment(group, attendeeIds[1], zoneId, rowCode, 12)
        };

        var exception = Assert.Throws<ArgumentException>(
            () => group.EnsureValidResult(assignments));

        Assert.Equal("assignments", exception.ParamName);
    }

    [Fact]
    public void EnsureValidResult_ShouldNotThrow_WhenAssignmentsAreOutOfOrder()
    {
        var attendeeIds = CreateAttendeeIds(3);
        var group = CreateGroup(attendeeIds);
        var zoneId = ZoneId.New();
        var rowCode = RowCode.Create("A");

        var assignments = new[]
        {
            CreateAssignment(group, attendeeIds[2], zoneId, rowCode, 12),
            CreateAssignment(group, attendeeIds[0], zoneId, rowCode, 10),
            CreateAssignment(group, attendeeIds[1], zoneId, rowCode, 11)
        };

        var exception = Record.Exception(
            () => group.EnsureValidResult(assignments));

        Assert.Null(exception);
    }

    private static readonly DateTimeOffset AssignedAt =
        new(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(-5));

    private static AssignmentGroup CreateGroup(
        IEnumerable<AttendeeId> attendeeIds)
    {
        return AssignmentGroup.Create(
            AssignmentRequestId.New(),
            FestivalDayId.New(),
            attendeeIds);
    }

    private static Assignment CreateAssignment(
        AssignmentGroup group,
        AttendeeId attendeeId,
        ZoneId zoneId,
        RowCode rowCode,
        int spotNumber)
    {
        return Assignment.Create(
            AssignmentId.New(),
            group.AssignmentRequestId,
            group.FestivalDayId,
            attendeeId,
            SpotCode.Create($"Z-{rowCode}-{spotNumber:000}"),
            zoneId,
            rowCode,
            SpotNumber.Create(spotNumber),
            AssignedAt);
    }

    private static AttendeeId[] CreateAttendeeIds(int count)
    {
        return Enumerable
            .Range(1, count)
            .Select(number => CreateAttendeeId(number))
            .ToArray();
    }

    private static AttendeeId CreateAttendeeId(int number)
    {
        return AttendeeId.Create(new Guid(number, 0, 0, new byte[8]));
    }
}
