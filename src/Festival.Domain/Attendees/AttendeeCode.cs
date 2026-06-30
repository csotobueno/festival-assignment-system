namespace Festival.Domain.Attendees;

public sealed record AttendeeCode
{
    public string Value { get; }

    private AttendeeCode(string value)
    {
        Value = value;
    }

    public static AttendeeCode Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Attendee code cannot be empty.",
                nameof(value));
        }

        var normalizedValue = value.Trim().ToUpperInvariant();

        return new AttendeeCode(normalizedValue);
    }

    public override string ToString() => Value;
}