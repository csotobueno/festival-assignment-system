namespace Festival.Domain.Spots;

public readonly record struct SpotNumber
{
    public int Value { get; }

    private SpotNumber(int value)
    {
        Value = value;
    }

    public static SpotNumber Create(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Spot number must be greater than zero.");
        }

        return new SpotNumber(value);
    }

    public override string ToString() => Value.ToString();
}