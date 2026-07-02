namespace Festival.Domain.Assignments;

public readonly record struct GroupSize
{
    public const int Minimum = 1;
    public const int Maximum = 10;
    public int Value { get; }

    private GroupSize(int value)
    {
        Value = value;
    }

    public static GroupSize Create(int value)
    {
        if (value is < Minimum or > Maximum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Group size must be between {Minimum} and {Maximum}.");
        }

        return new GroupSize(value);
    }

    public override string ToString() => Value.ToString();
}
