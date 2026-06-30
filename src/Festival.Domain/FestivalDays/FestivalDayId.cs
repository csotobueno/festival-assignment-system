namespace Festival.Domain.FestivalDays;

public readonly record struct FestivalDayId(Guid Value)
{
    public static FestivalDayId New() => new(Guid.NewGuid());

    public static FestivalDayId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Festival day id cannot be empty.",
                nameof(value));
        }

        return new FestivalDayId(value);
    }

    public override string ToString() => Value.ToString();
}