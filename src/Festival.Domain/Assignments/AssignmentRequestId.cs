namespace Festival.Domain.Assignments;

public readonly record struct AssignmentRequestId(Guid Value)
{
    public static AssignmentRequestId New()
        => new(Guid.NewGuid());

    public static AssignmentRequestId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Assignment request id cannot be empty.",
                nameof(value));
        }

        return new AssignmentRequestId(value);
    }

    public override string ToString() => Value.ToString();
}