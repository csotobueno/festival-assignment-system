namespace Festival.Domain.FestivalDays;

public sealed record AssignmentWindow
{
    public TimeOnly Start { get; }

    public TimeOnly End { get; }

    private AssignmentWindow(
        TimeOnly start,
        TimeOnly end)
    {
        if (start >= end)
        {
            throw new ArgumentException(
                "Assignment window start must be earlier than its end.");
        }

        Start = start;
        End = end;
    }

    public static AssignmentWindow Create(
        TimeOnly start,
        TimeOnly end)
    {
        return new AssignmentWindow(start, end);
    }

    public bool Contains(TimeOnly time)
    {
        return time >= Start && time < End;
    }
}
