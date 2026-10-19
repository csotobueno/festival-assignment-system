using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Tests.Assignments;

public sealed class AssignmentEngineTests
{
    private static readonly DateTimeOffset AssignedAt =
        new(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(-5));

    [Theory]
    [InlineData(ZoneType.FrontStanding, ExperienceQuality.Good)]
    [InlineData(ZoneType.MiddleLeft, ExperienceQuality.Medium)]
    [InlineData(ZoneType.UpperLeft, ExperienceQuality.Bad)]
    public void Assign_ShouldMaterializeExactSelectionInMemberOrder(
        ZoneType type, ExperienceQuality quality)
    {
        var group = CreateGroup(3);
        var selection = CreateSelection(3, type, quality);

        var result = new AssignmentEngine().Assign(group, selection, AssignedAt);

        Assert.True(result.IsAssigned);
        Assert.Equal(group.AttendeeIds, result.Assignments.Select(assignment => assignment.AttendeeId));
        Assert.Equal(selection.Block.Spots.Select(spot => spot.Code),
            result.Assignments.Select(assignment => assignment.SpotCode));
        Assert.Equal(3, result.Assignments.Select(assignment => assignment.Id).Distinct().Count());
        foreach (var (assignment, spot) in result.Assignments.Zip(selection.Block.Spots))
        {
            Assert.Equal(group.AssignmentRequestId, assignment.AssignmentRequestId);
            Assert.Equal(group.FestivalDayId, assignment.FestivalDayId);
            Assert.Equal(spot.ZoneId, assignment.ZoneId);
            Assert.Equal(spot.RowCode, assignment.RowCode);
            Assert.Equal(spot.Number, assignment.SpotNumber);
            Assert.Equal(quality, assignment.ExperienceQuality);
            Assert.Equal(AssignedAt, assignment.AssignedAt);
        }
        group.EnsureValidResult(result.Assignments);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Assign_ShouldRejectSelectedBlockWhoseSizeDiffersFromGroup(int blockSize)
    {
        var group = CreateGroup(2);
        var selection = CreateSelection(blockSize, ZoneType.MiddleLeft, ExperienceQuality.Medium);

        var assign = () => new AssignmentEngine().Assign(group, selection, AssignedAt);

        Assert.Equal("selection", Assert.Throws<ArgumentException>(assign).ParamName);
    }

    [Fact]
    public void Assign_ShouldRequireGroup()
    {
        var selection = CreateSelection(1, ZoneType.MiddleLeft, ExperienceQuality.Medium);
        var assign = () => new AssignmentEngine().Assign(null!, selection, AssignedAt);
        Assert.Equal("assignmentGroup", Assert.Throws<ArgumentNullException>(assign).ParamName);
    }

    [Fact]
    public void Assign_ShouldRequireSelection()
    {
        var assign = () => new AssignmentEngine().Assign(CreateGroup(1), null!, AssignedAt);
        Assert.Equal("selection", Assert.Throws<ArgumentNullException>(assign).ParamName);
    }

    private static AssignmentGroup CreateGroup(int count) => AssignmentGroup.Create(
        AssignmentRequestId.New(), FestivalDayId.New(),
        Enumerable.Range(1, count).Select(i => AttendeeId.Create(new Guid(i, 0, 0, new byte[8]))));

    private static AssignmentSelection CreateSelection(int count, ZoneType type, ExperienceQuality quality)
    {
        var zone = Zone.Create(ZoneId.New(), "Selected Zone", type);
        var spots = Enumerable.Range(30, count).Reverse().Select(number => Spot.Create(
            SpotCode.Create($"SELECTED-B-{number}"), zone.Id,
            RowCode.Create("B"), SpotNumber.Create(number)));
        return AssignmentSelection.Create(zone, FeasibleSpotBlock.Create(spots), quality);
    }
}
