using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;
using Festival.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Festival.Infrastructure.Persistence.Configurations;

internal sealed class AssignmentConfiguration
    : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id)
            .HasColumnName("AssignmentId")
            .HasConversion(
                id => id.Value,
                value => AssignmentId.Create(value));

        builder.Property(assignment => assignment.AssignmentRequestId)
            .HasConversion(
                id => id.Value,
                value => AssignmentRequestId.Create(value));

        builder.Property(assignment => assignment.FestivalDayId)
            .HasConversion(
                id => id.Value,
                value => FestivalDayId.Create(value));

        builder.Property(assignment => assignment.AttendeeId)
            .HasConversion(
                id => id.Value,
                value => AttendeeId.Create(value));

        builder.Property(assignment => assignment.SpotCode)
            .HasConversion(
                code => code.Value,
                value => SpotCode.Create(value));

        builder.Property(assignment => assignment.ZoneId)
            .HasConversion(
                id => id.Value,
                value => ZoneId.Create(value));

        builder.Property(assignment => assignment.RowCode)
            .HasConversion(
                rowCode => rowCode.Value,
                value => RowCode.Create(value));

        builder.Property(assignment => assignment.SpotNumber)
            .HasConversion(
                number => number.Value,
                value => SpotNumber.Create(value));

        builder.Property(assignment => assignment.AssignedAt)
            .IsRequired();

        builder.HasOne<AssignmentRequestRow>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AssignmentRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FestivalDay>()
            .WithMany()
            .HasForeignKey(assignment => assignment.FestivalDayId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Attendee>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AttendeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Spot>()
            .WithMany()
            .HasForeignKey(assignment => assignment.SpotCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(assignment => new
            {
                assignment.FestivalDayId,
                assignment.SpotCode
            })
            .IsUnique();

        builder.HasIndex(assignment => new
            {
                assignment.FestivalDayId,
                assignment.AttendeeId
            })
            .IsUnique();

        builder.HasIndex(assignment => new
            {
                assignment.AssignmentRequestId,
                assignment.AttendeeId
            })
            .IsUnique();
    }
}
