using Festival.Domain.Zones;

namespace Festival.Domain.Spots;

public sealed class Spot
{
    public SpotCode Code { get; }

    public ZoneId ZoneId { get; }

    public RowCode RowCode { get; }

    public SpotNumber Number { get; }

    private Spot(
        SpotCode code,
        ZoneId zoneId,
        RowCode rowCode,
        SpotNumber number)
    {
        Code = code;
        ZoneId = zoneId;
        RowCode = rowCode;
        Number = number;
    }

    public static Spot Create(
        SpotCode code,
        ZoneId zoneId,
        RowCode rowCode,
        SpotNumber number)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(rowCode);

        if (zoneId == default)
        {
            throw new ArgumentException(
                "Zone id is required.",
                nameof(zoneId));
        }

        if (number == default)
        {
            throw new ArgumentException(
                "Spot number is required.",
                nameof(number));
        }

        return new Spot(
            code,
            zoneId,
            rowCode,
            number);
    }
}