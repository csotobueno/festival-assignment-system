namespace Festival.Domain.Attendees;

public readonly record struct AttendeeId(Guid Value)
{
    public static AttendeeId New() => new(Guid.NewGuid());

    public static AttendeeId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Attendee id cannot be empty.",
                nameof(value));
        }

        return new AttendeeId(value);
    }

    public override string ToString() => Value.ToString();
}