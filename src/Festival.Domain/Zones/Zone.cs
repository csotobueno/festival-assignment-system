namespace Festival.Domain.Zones;

public sealed class Zone
{
    public ZoneId Id { get; }

    public string Name { get; }

    public ZoneType Type { get; }

    private Zone(
        ZoneId id,
        string name,
        ZoneType type)
    {
        Id = id;
        Name = name;
        Type = type;
    }

    public static Zone Create(
        ZoneId id,
        string? name,
        ZoneType type)
    {
        if (id == default)
        {
            throw new ArgumentException(
                "Zone id is required.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Zone name cannot be empty.",
                nameof(name));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type), type, "Zone type must be a defined ZoneType.");
        }

        return new Zone(
            id,
            name.Trim(),
            type);
    }
}
