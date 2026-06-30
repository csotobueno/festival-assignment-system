using Festival.Domain.FestivalDays;

namespace Festival.Domain.Tests.FestivalDays;

public sealed class AssignmentWindowTests
{
    [Fact]
    public void Create_ShouldReturnWindow_WhenRangeIsValid()
    {
        var window = AssignmentWindow.Create(
            new TimeOnly(9, 0),
            new TimeOnly(18, 0));

        Assert.Equal(new TimeOnly(9, 0), window.Start);
        Assert.Equal(new TimeOnly(18, 0), window.End);
    }

    [Theory]
    [InlineData(9, 0, 9, 0)]
    [InlineData(18, 0, 9, 0)]
    public void Create_ShouldThrow_WhenStartIsNotEarlierThanEnd(
        int startHour,
        int startMinute,
        int endHour,
        int endMinute)
    {
        Assert.Throws<ArgumentException>(
            () => AssignmentWindow.Create(
                new TimeOnly(startHour, startMinute),
                new TimeOnly(endHour, endMinute)));
    }

    [Fact]
    public void Contains_ShouldReturnTrue_WhenTimeIsInsideWindow()
    {
        var window = AssignmentWindow.Create(
            new TimeOnly(9, 0),
            new TimeOnly(18, 0));

        Assert.True(window.Contains(new TimeOnly(12, 0)));
    }

    [Fact]
    public void Contains_ShouldIncludeStart()
    {
        var window = AssignmentWindow.Create(
            new TimeOnly(9, 0),
            new TimeOnly(18, 0));

        Assert.True(window.Contains(new TimeOnly(9, 0)));
    }

    [Fact]
    public void Contains_ShouldExcludeEnd()
    {
        var window = AssignmentWindow.Create(
            new TimeOnly(9, 0),
            new TimeOnly(18, 0));

        Assert.False(window.Contains(new TimeOnly(18, 0)));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_WhenTimeIsOutsideWindow()
    {
        var window = AssignmentWindow.Create(
            new TimeOnly(9, 0),
            new TimeOnly(18, 0));

        Assert.False(window.Contains(new TimeOnly(8, 59)));
    }
}