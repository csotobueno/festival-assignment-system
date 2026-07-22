namespace Festival.Domain.FestivalDays;

public sealed class FestivalDay
{
    public FestivalDayId Id { get; }

    public DateOnly Date { get; }

    public AssignmentWindow AssignmentWindow { get; private set; } = null!;

    private FestivalDay(
        FestivalDayId id,
        DateOnly date)
    {
        if (id == default)
        {
            throw new ArgumentException(
                "Festival day ID is required.",
                nameof(id));
        }

        Id = id;
        Date = date;
    }

    private FestivalDay(
        FestivalDayId id,
        DateOnly date,
        AssignmentWindow assignmentWindow)
        : this(id, date)
    {
        AssignmentWindow = assignmentWindow
            ?? throw new ArgumentNullException(nameof(assignmentWindow));
    }

    public static FestivalDay Create(
        FestivalDayId id,
        DateOnly date,
        AssignmentWindow assignmentWindow)
    {
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
