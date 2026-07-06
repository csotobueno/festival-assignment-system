using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Assignments;

public sealed class AssignmentEngineTests
{
    [Fact]
    public void Assign_ShouldAssignOneAttendee_WhenSpotIsAvailable()
    {
        var group = CreateGroup(1);
        var spot = CreateSpot(CreateZoneId(1), "A", 10);

        var result = CreateEngine().Assign(
            group,
            [spot],
            AssignedAt);

        var assignment = Assert.Single(result.Assignments);

        Assert.True(result.IsAssigned);
        Assert.NotEqual(default, assignment.Id);
        Assert.Equal(
            group.AssignmentRequestId,
            assignment.AssignmentRequestId);
        Assert.Equal(group.FestivalDayId, assignment.FestivalDayId);
        Assert.Equal(group.AttendeeIds[0], assignment.AttendeeId);
        Assert.Equal(spot.Code, assignment.SpotCode);
        Assert.Equal(spot.ZoneId, assignment.ZoneId);
        Assert.Equal(spot.RowCode, assignment.RowCode);
        Assert.Equal(spot.Number, assignment.SpotNumber);
    }

    [Fact]
    public void Assign_ShouldCreateOneAssignmentPerAttendee()
    {
        var group = CreateGroup(3);
        var zoneId = CreateZoneId(1);

        var result = CreateEngine().Assign(
            group,
            [
                CreateSpot(zoneId, "A", 10),
                CreateSpot(zoneId, "A", 11),
                CreateSpot(zoneId, "A", 12)
            ],
            AssignedAt);

        Assert.True(result.IsAssigned);
        Assert.Equal(3, result.Assignments.Count);
        Assert.Equal(
            group.AttendeeIds,
            result.Assignments
                .Select(assignment => assignment.AttendeeId)
                .ToArray());
        Assert.Equal(
            3,
            result.Assignments
                .Select(assignment => assignment.Id)
                .Distinct()
                .Count());
    }

    [Fact]
    public void Assign_ShouldChooseSpotsInSameZone()
    {
        var group = CreateGroup(2);
        var firstZoneId = CreateZoneId(1);
        var secondZoneId = CreateZoneId(2);

        var result = CreateEngine().Assign(
            group,
            [
                CreateSpot(firstZoneId, "A", 10),
                CreateSpot(secondZoneId, "A", 10),
                CreateSpot(secondZoneId, "A", 11)
            ],
            AssignedAt);

        Assert.True(result.IsAssigned);
        Assert.All(
            result.Assignments,
            assignment => Assert.Equal(
                secondZoneId,
                assignment.ZoneId));
    }

    [Fact]
    public void Assign_ShouldChooseSpotsInSameRow()
    {
        var group = CreateGroup(2);
        var zoneId = CreateZoneId(1);

        var result = CreateEngine().Assign(
            group,
            [
                CreateSpot(zoneId, "A", 10),
                CreateSpot(zoneId, "B", 10),
                CreateSpot(zoneId, "B", 11)
            ],
            AssignedAt);

        Assert.True(result.IsAssigned);
        Assert.All(
            result.Assignments,
            assignment => Assert.Equal(
                RowCode.Create("B"),
                assignment.RowCode));
    }

    [Fact]
    public void Assign_ShouldChooseConsecutiveSpotNumbers()
    {
        var group = CreateGroup(2);
        var zoneId = CreateZoneId(1);

        var result = CreateEngine().Assign(
            group,
            [
                CreateSpot(zoneId, "A", 10),
                CreateSpot(zoneId, "A", 11),
                CreateSpot(zoneId, "A", 12)
            ],
            AssignedAt);

        Assert.Equal(
            [10, 11],
            result.Assignments
                .Select(assignment => assignment.SpotNumber.Value)
                .ToArray());
    }

    [Fact]
    public void Assign_ShouldIgnoreNonConsecutiveSpots()
    {
        var group = CreateGroup(2);
        var zoneId = CreateZoneId(1);

        var result = CreateEngine().Assign(
            group,
            [
                CreateSpot(zoneId, "A", 10),
                CreateSpot(zoneId, "A", 12),
                CreateSpot(zoneId, "A", 13)
            ],
            AssignedAt);

        Assert.True(result.IsAssigned);
        Assert.Equal(
            [12, 13],
            result.Assignments
                .Select(assignment => assignment.SpotNumber.Value)
                .ToArray());
    }

    [Fact]
    public void Assign_ShouldReturnUnassigned_WhenNoContiguousBlockExists()
    {
        var group = CreateGroup(2);
        var zoneId = CreateZoneId(1);

        var result = CreateEngine().Assign(
            group,
            [
                CreateSpot(zoneId, "A", 10),
                CreateSpot(zoneId, "A", 12)
            ],
            AssignedAt);

        Assert.False(result.IsAssigned);
        Assert.Empty(result.Assignments);
    }

    [Fact]
    public void Assign_ShouldReturnUnassigned_WhenNotEnoughSpotsExist()
    {
        var group = CreateGroup(3);

        var result = CreateEngine().Assign(
            group,
            [
                CreateSpot(CreateZoneId(1), "A", 10),
                CreateSpot(CreateZoneId(1), "A", 11)
            ],
            AssignedAt);

        Assert.False(result.IsAssigned);
        Assert.Empty(result.Assignments);
    }

    [Fact]
    public void Assign_ShouldReturnSameSpots_RegardlessOfInputOrder()
    {
        var group = CreateGroup(2);
        var zoneId = CreateZoneId(1);
        var firstSpot = CreateSpot(zoneId, "A", 10);
        var secondSpot = CreateSpot(zoneId, "A", 11);
        var thirdSpot = CreateSpot(zoneId, "A", 12);

        var firstResult = CreateEngine().Assign(
            group,
            [thirdSpot, firstSpot, secondSpot],
            AssignedAt);

        var secondResult = CreateEngine().Assign(
            group,
            [secondSpot, thirdSpot, firstSpot],
            AssignedAt);

        Assert.Equal(
            firstResult.Assignments
                .Select(assignment => assignment.SpotCode),
            secondResult.Assignments
                .Select(assignment => assignment.SpotCode));
    }

    [Fact]
    public void Assign_ShouldThrow_WhenAssignmentGroupIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => CreateEngine().Assign(
                null!,
                [],
                AssignedAt));

        Assert.Equal("assignmentGroup", exception.ParamName);
    }

    [Fact]
    public void Assign_ShouldThrow_WhenAvailableSpotsIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => CreateEngine().Assign(
                CreateGroup(1),
                null!,
                AssignedAt));

        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Fact]
    public void Assign_ShouldThrow_WhenAvailableSpotsContainsNull()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateEngine().Assign(
                CreateGroup(1),
                [null!],
                AssignedAt));

        Assert.Equal("availableSpots", exception.ParamName);
    }

    [Fact]
    public void Assign_ShouldPreserveAssignedAt()
    {
        var result = CreateEngine().Assign(
            CreateGroup(1),
            [CreateSpot(CreateZoneId(1), "A", 10)],
            AssignedAt);

        Assert.Equal(
            AssignedAt,
            Assert.Single(result.Assignments).AssignedAt);
    }

    [Fact]
    public void Assign_ShouldProduceResultAcceptedByAssignmentGroup()
    {
        var group = CreateGroup(3);
        var zoneId = CreateZoneId(1);
        var result = CreateEngine().Assign(
            group,
            [
                CreateSpot(zoneId, "A", 12),
                CreateSpot(zoneId, "A", 10),
                CreateSpot(zoneId, "A", 11)
            ],
            AssignedAt);

        var exception = Record.Exception(
            () => group.EnsureValidResult(result.Assignments));

        Assert.True(result.IsAssigned);
        Assert.Null(exception);
    }

    private static readonly DateTimeOffset AssignedAt =
        new(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(-5));

    private static AssignmentEngine CreateEngine()
    {
        return new AssignmentEngine();
    }

    private static AssignmentGroup CreateGroup(int attendeeCount)
    {
        var attendeeIds = Enumerable
            .Range(1, attendeeCount)
            .Select(number => AttendeeId.Create(
                new Guid(number, 0, 0, new byte[8])));

        return AssignmentGroup.Create(
            AssignmentRequestId.New(),
            FestivalDayId.New(),
            attendeeIds);
    }

    private static Spot CreateSpot(
        ZoneId zoneId,
        string rowCode,
        int spotNumber)
    {
        return Spot.Create(
            SpotCode.Create(
                $"{zoneId.Value:N}-{rowCode}-{spotNumber:000}"),
            zoneId,
            RowCode.Create(rowCode),
            SpotNumber.Create(spotNumber));
    }

    private static ZoneId CreateZoneId(int number)
    {
        return ZoneId.Create(
            new Guid(number, 0, 0, new byte[8]));
    }
}
