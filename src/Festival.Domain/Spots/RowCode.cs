namespace Festival.Domain.Spots;

public sealed record RowCode
{
    public string Value { get; }

    private RowCode(string value)
    {
        Value = value;
    }

    public static RowCode Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Row code cannot be empty.",
                nameof(value));
        }

        return new RowCode(value.Trim().ToUpperInvariant());
    }

    public override string ToString() => Value;
}