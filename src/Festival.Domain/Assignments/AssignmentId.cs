namespace Festival.Domain.Assignments;

public readonly record struct AssignmentId(Guid Value)
{
    public static AssignmentId New() => new(Guid.NewGuid());

    public static AssignmentId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Assignment id cannot be empty.",
                nameof(value));
        }

        return new AssignmentId(value);
    }

    public override string ToString() => Value.ToString();
}
