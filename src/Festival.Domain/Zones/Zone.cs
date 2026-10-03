namespace Festival.Domain.Zones;

public sealed class Zone
{
    public ZoneId Id { get; }

    public string Name { get; }

    public bool IsFrontStanding { get; }

    private Zone(
        ZoneId id,
        string name,
        bool isFrontStanding)
    {
        Id = id;
        Name = name;
        IsFrontStanding = isFrontStanding;
    }

    public static Zone Create(
        ZoneId id,
        string? name,
        bool isFrontStanding)
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

        return new Zone(
            id,
            name.Trim(),
            isFrontStanding);
    }
}