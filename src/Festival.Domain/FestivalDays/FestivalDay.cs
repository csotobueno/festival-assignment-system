namespace Festival.Domain.FestivalDays;

public sealed class FestivalDay
{
    public FestivalDayId Id { get; }

    public DateOnly Date { get; }

    public AssignmentWindow AssignmentWindow { get; }

    private FestivalDay(
        FestivalDayId id,
        DateOnly date,
        AssignmentWindow assignmentWindow)
    {
        Id = id;
        Date = date;
        AssignmentWindow = assignmentWindow;
    }

    public static FestivalDay Create(
        FestivalDayId id,
        DateOnly date,
        AssignmentWindow assignmentWindow)
    {
        if (id == default)
        {
            throw new ArgumentException(
                "Festival day id is required.",
                nameof(id));
        }

        ArgumentNullException.ThrowIfNull(assignmentWindow);

        return new FestivalDay(
            id,
            date,
            assignmentWindow);
    }

    public bool IsAssignmentWindowOpen(DateTime localDateTime)
    {
        return DateOnly.FromDateTime(localDateTime) == Date
            && AssignmentWindow.Contains(
                TimeOnly.FromDateTime(localDateTime));
    }
}