using Festival.Domain.Spots;
using Festival.Domain.Zones;

namespace Festival.Domain.Assignments;

/// <summary>
/// Immutable selection of a Zone, one feasible block and the Zone's current quality.
/// It does not create an Assignment or record historical quality.
/// </summary>
public sealed class AssignmentSelection
{
    public Zone Zone { get; }

    public FeasibleSpotBlock Block { get; }

    public ExperienceQuality ExperienceQuality { get; }

    private AssignmentSelection(Zone zone, FeasibleSpotBlock block, ExperienceQuality experienceQuality)
    {
        Zone = zone;
        Block = block;
        ExperienceQuality = experienceQuality;
    }

    public static AssignmentSelection Create(
        Zone zone,
        FeasibleSpotBlock block,
        ExperienceQuality experienceQuality)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(block);

        if (block.ZoneId != zone.Id)
        {
            throw new ArgumentException("Selected block must belong to the selected zone.", nameof(block));
        }

        if (!Enum.IsDefined(experienceQuality))
        {
            throw new ArgumentOutOfRangeException(
                nameof(experienceQuality), experienceQuality, "Experience quality must be defined.");
        }

        if (experienceQuality != ZoneExperienceQualityPolicy.GetQuality(zone.Type))
        {
            throw new ArgumentException(
                "Experience quality must match the selected zone's current quality.", nameof(experienceQuality));
        }

        return new AssignmentSelection(zone, block, experienceQuality);
    }
}
