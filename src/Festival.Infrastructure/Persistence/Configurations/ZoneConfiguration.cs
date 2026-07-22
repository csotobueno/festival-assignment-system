using Festival.Domain.Zones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Festival.Infrastructure.Persistence.Configurations;

internal sealed class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> builder)
    {
        builder.ToTable("Zones");

        builder.HasKey(zone => zone.Id);

        builder.Property(zone => zone.Id)
            .HasColumnName("ZoneId")
            .HasConversion(
                id => id.Value,
                value => ZoneId.Create(value));

        builder.Property(zone => zone.Name)
            .HasMaxLength(PersistenceLengths.Name)
            .IsRequired();
    }
}
