namespace Festival.Domain.Zones;

public readonly record struct ZoneId(Guid Value)
{
    public static ZoneId New() => new(Guid.NewGuid());

    public static ZoneId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Zone id cannot be empty.",
                nameof(value));
        }

        return new ZoneId(value);
    }

    public override string ToString() => Value.ToString();
}