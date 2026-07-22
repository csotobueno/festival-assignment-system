using Festival.Domain.Attendees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Festival.Infrastructure.Persistence.Configurations;

internal sealed class AttendeeConfiguration
    : IEntityTypeConfiguration<Attendee>
{
    public void Configure(EntityTypeBuilder<Attendee> builder)
    {
        builder.ToTable("Attendees");

        builder.HasKey(attendee => attendee.Id);

        builder.Property(attendee => attendee.Id)
            .HasColumnName("AttendeeId")
            .HasConversion(
                id => id.Value,
                value => AttendeeId.Create(value));

        builder.Property(attendee => attendee.Code)
            .HasColumnName("AttendeeCode")
            .HasConversion(
                code => code.Value,
                value => AttendeeCode.Create(value))
            .HasMaxLength(PersistenceLengths.AttendeeCode)
            .IsRequired();

        builder.Property(attendee => attendee.Name)
            .HasMaxLength(PersistenceLengths.AttendeeName)
            .IsRequired();

        builder.HasIndex(attendee => attendee.Code)
            .IsUnique();
    }
}
