using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Festival.Infrastructure.Persistence.Configurations;

internal sealed class AssignmentRequestAttendeeRowConfiguration
    : IEntityTypeConfiguration<AssignmentRequestAttendeeRow>
{
    public void Configure(
        EntityTypeBuilder<AssignmentRequestAttendeeRow> builder)
    {
        builder.ToTable("AssignmentRequestAttendees");

        builder.HasKey(row => new
        {
            row.AssignmentRequestId,
            row.Position
        });

        builder.Property(row => row.AssignmentRequestId)
            .HasConversion(
                id => id.Value,
                value => AssignmentRequestId.Create(value));

        builder.Property(row => row.Position)
            .IsRequired();

        builder.Property(row => row.AttendeeCode)
            .HasConversion(
                code => code.Value,
                value => AttendeeCode.Create(value))
            .IsRequired();

        builder.HasOne(row => row.AssignmentRequest)
            .WithMany(request => request.Attendees)
            .HasForeignKey(row => row.AssignmentRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(row => new
            {
                row.AssignmentRequestId,
                row.AttendeeCode
            })
            .IsUnique();
    }
}
