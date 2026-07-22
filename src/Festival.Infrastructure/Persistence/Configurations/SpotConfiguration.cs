using Festival.Domain.Spots;
using Festival.Domain.Zones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Festival.Infrastructure.Persistence.Configurations;

internal sealed class SpotConfiguration : IEntityTypeConfiguration<Spot>
{
    public void Configure(EntityTypeBuilder<Spot> builder)
    {
        builder.ToTable("Spots");

        builder.HasKey(spot => spot.Code);

        builder.Property(spot => spot.Code)
            .HasColumnName("SpotCode")
            .HasConversion(
                code => code.Value,
                value => SpotCode.Create(value));

        builder.Property(spot => spot.ZoneId)
            .HasConversion(
                id => id.Value,
                value => ZoneId.Create(value));

        builder.Property(spot => spot.RowCode)
            .HasConversion(
                rowCode => rowCode.Value,
                value => RowCode.Create(value));

        builder.Property(spot => spot.Number)
            .HasColumnName("SpotNumber")
            .HasConversion(
                number => number.Value,
                value => SpotNumber.Create(value));

        builder.HasOne<Zone>()
            .WithMany()
            .HasForeignKey(spot => spot.ZoneId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(spot => new
            {
                spot.ZoneId,
                spot.RowCode,
                spot.Number
            })
            .IsUnique();
    }
}
