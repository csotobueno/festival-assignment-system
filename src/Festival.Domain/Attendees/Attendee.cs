namespace Festival.Domain.Attendees;

public sealed class Attendee
{
    public AttendeeId Id { get; }

    public AttendeeCode Code { get; }

    public string Name { get; }

    private Attendee(
        AttendeeId id,
        AttendeeCode code,
        string name)
    {
        Id = id;
        Code = code;
        Name = name;
    }

    public static Attendee Create(
        AttendeeId id,
        AttendeeCode code,
        string? name)
    {
        if (id == default)
        {
            throw new ArgumentException(
                "Attendee id is required.",
                nameof(id));
        }

        ArgumentNullException.ThrowIfNull(code);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Attendee name cannot be empty.",
                nameof(name));
        }

        return new Attendee(
            id,
            code,
            name.Trim());
    }
}