using Festival.Domain.FestivalDays;

namespace Festival.Domain.Tests.FestivalDays;

public sealed class FestivalDayTests
{
    [Fact]
    public void Create_ShouldReturnFestivalDay_WhenDataIsValid()
    {
        var id = FestivalDayId.New();
        var date = new DateOnly(2026, 7, 10);
        var window = AssignmentWindow.Create(
            new TimeOnly(9, 0),
            new TimeOnly(18, 0));

        var festivalDay = FestivalDay.Create(
            id,
            date,
            window);

        Assert.Equal(id, festivalDay.Id);
        Assert.Equal(date, festivalDay.Date);
        Assert.Equal(window, festivalDay.AssignmentWindow);
    }

    [Fact]
    public void Create_ShouldThrow_WhenIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => FestivalDay.Create(
                default,
                new DateOnly(2026, 7, 10),
                AssignmentWindow.Create(
                    new TimeOnly(9, 0),
                    new TimeOnly(18, 0))));

        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void Create_ShouldThrow_WhenWindowIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => FestivalDay.Create(
                FestivalDayId.New(),
                new DateOnly(2026, 7, 10),
                null!));

        Assert.Equal("assignmentWindow", exception.ParamName);
    }

    [Fact]
    public void IsAssignmentWindowOpen_ShouldReturnTrue_WhenDateAndTimeAreValid()
    {
        var festivalDay = CreateFestivalDay();

        var result = festivalDay.IsAssignmentWindowOpen(
            new DateTime(2026, 7, 10, 12, 0, 0));

        Assert.True(result);
    }

    [Fact]
    public void IsAssignmentWindowOpen_ShouldReturnFalse_WhenDateIsDifferent()
    {
        var festivalDay = CreateFestivalDay();

        var result = festivalDay.IsAssignmentWindowOpen(
            new DateTime(2026, 7, 11, 12, 0, 0));

        Assert.False(result);
    }

    [Fact]
    public void IsAssignmentWindowOpen_ShouldReturnFalse_WhenTimeIsOutsideWindow()
    {
        var festivalDay = CreateFestivalDay();

        var result = festivalDay.IsAssignmentWindowOpen(
            new DateTime(2026, 7, 10, 18, 0, 0));

        Assert.False(result);
    }

    private static FestivalDay CreateFestivalDay()
    {
        return FestivalDay.Create(
            FestivalDayId.New(),
            new DateOnly(2026, 7, 10),
            AssignmentWindow.Create(
                new TimeOnly(9, 0),
                new TimeOnly(18, 0)));
    }
}