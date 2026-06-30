namespace Festival.Domain.Spots;

public sealed record SpotCode
{
    public string Value { get; }

    private SpotCode(string value)
    {
        Value = value;
    }

    public static SpotCode Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Spot code cannot be empty.",
                nameof(value));
        }

        var normalizedValue = value.Trim().ToUpperInvariant();

        return new SpotCode(normalizedValue);
    }

    public override string ToString() => Value;
}