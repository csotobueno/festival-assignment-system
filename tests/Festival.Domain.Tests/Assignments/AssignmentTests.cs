using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Assignments;

public sealed class AssignmentTests
{
    [Fact]
    public void Create_ShouldReturnAssignment_WhenDataIsValid()
    {
        var id = AssignmentId.New();
        var assignmentRequestId = AssignmentRequestId.New();
        var festivalDayId = FestivalDayId.New();
        var attendeeId = AttendeeId.New();
        var spotCode = SpotCode.Create("FRONT-A-010");
        var zoneId = ZoneId.New();
        var rowCode = RowCode.Create("A");
        var spotNumber = SpotNumber.Create(10);
        var assignedAt = new DateTimeOffset(
            2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(-5));

        var assignment = Assignment.Create(
            id,
            assignmentRequestId,
            festivalDayId,
            attendeeId,
            spotCode,
            zoneId,
            rowCode,
            spotNumber,
            assignedAt);

        Assert.Equal(id, assignment.Id);
        Assert.Equal(assignmentRequestId, assignment.AssignmentRequestId);
        Assert.Equal(festivalDayId, assignment.FestivalDayId);
        Assert.Equal(attendeeId, assignment.AttendeeId);
        Assert.Equal(spotCode, assignment.SpotCode);
        Assert.Equal(zoneId, assignment.ZoneId);
        Assert.Equal(rowCode, assignment.RowCode);
        Assert.Equal(spotNumber, assignment.SpotNumber);
        Assert.Equal(assignedAt, assignment.AssignedAt);
    }

    [Fact]
    public void Create_ShouldThrow_WhenAssignmentIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Assignment.Create(
                default,
                AssignmentRequestId.New(),
                FestivalDayId.New(),
                AttendeeId.New(),
                SpotCode.Create("FRONT-A-010"),
                ZoneId.New(),
                RowCode.Create("A"),
                SpotNumber.Create(10),
                DateTimeOffset.UtcNow));

        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenAssignmentRequestIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Assignment.Create(
                AssignmentId.New(),
                default,
                FestivalDayId.New(),
                AttendeeId.New(),
                SpotCode.Create("FRONT-A-010"),
                ZoneId.New(),
                RowCode.Create("A"),
                SpotNumber.Create(10),
                DateTimeOffset.UtcNow));

        Assert.Equal("assignmentRequestId", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenFestivalDayIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Assignment.Create(
                AssignmentId.New(),
                AssignmentRequestId.New(),
                default,
                AttendeeId.New(),
                SpotCode.Create("FRONT-A-010"),
                ZoneId.New(),
                RowCode.Create("A"),
                SpotNumber.Create(10),
                DateTimeOffset.UtcNow));

        Assert.Equal("festivalDayId", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenAttendeeIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Assignment.Create(
                AssignmentId.New(),
                AssignmentRequestId.New(),
                FestivalDayId.New(),
                default,
                SpotCode.Create("FRONT-A-010"),
                ZoneId.New(),
                RowCode.Create("A"),
                SpotNumber.Create(10),
                DateTimeOffset.UtcNow));

        Assert.Equal("attendeeId", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenSpotCodeIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => Assignment.Create(
                AssignmentId.New(),
                AssignmentRequestId.New(),
                FestivalDayId.New(),
                AttendeeId.New(),
                null!,
                ZoneId.New(),
                RowCode.Create("A"),
                SpotNumber.Create(10),
                DateTimeOffset.UtcNow));

        Assert.Equal("spotCode", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenZoneIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Assignment.Create(
                AssignmentId.New(),
                AssignmentRequestId.New(),
                FestivalDayId.New(),
                AttendeeId.New(),
                SpotCode.Create("FRONT-A-010"),
                default,
                RowCode.Create("A"),
                SpotNumber.Create(10),
                DateTimeOffset.UtcNow));

        Assert.Equal("zoneId", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenRowCodeIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => Assignment.Create(
                AssignmentId.New(),
                AssignmentRequestId.New(),
                FestivalDayId.New(),
                AttendeeId.New(),
                SpotCode.Create("FRONT-A-010"),
                ZoneId.New(),
                null!,
                SpotNumber.Create(10),
                DateTimeOffset.UtcNow));

        Assert.Equal("rowCode", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenSpotNumberIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Assignment.Create(
                AssignmentId.New(),
                AssignmentRequestId.New(),
                FestivalDayId.New(),
                AttendeeId.New(),
                SpotCode.Create("FRONT-A-010"),
                ZoneId.New(),
                RowCode.Create("A"),
                default,
                DateTimeOffset.UtcNow));

        Assert.Equal("spotNumber", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldStoreAssignedAt()
    {
        var assignedAt = new DateTimeOffset(
            2026, 7, 10, 14, 30, 0, TimeSpan.FromHours(-5));

        var assignment = Assignment.Create(
            AssignmentId.New(),
            AssignmentRequestId.New(),
            FestivalDayId.New(),
            AttendeeId.New(),
            SpotCode.Create("FRONT-A-010"),
            ZoneId.New(),
            RowCode.Create("A"),
            SpotNumber.Create(10),
            assignedAt);

        Assert.Equal(assignedAt, assignment.AssignedAt);
    }
}
