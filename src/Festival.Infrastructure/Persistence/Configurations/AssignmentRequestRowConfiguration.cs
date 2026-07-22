using Festival.Domain.Assignments;
using Festival.Domain.FestivalDays;
using Festival.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Festival.Infrastructure.Persistence.Configurations;

internal sealed class AssignmentRequestRowConfiguration
    : IEntityTypeConfiguration<AssignmentRequestRow>
{
    public void Configure(
        EntityTypeBuilder<AssignmentRequestRow> builder)
    {
        builder.ToTable("AssignmentRequests");

        builder.HasKey(row => row.AssignmentRequestId);

        builder.Property(row => row.AssignmentRequestId)
            .HasColumnName("AssignmentRequestId")
            .HasConversion(
                id => id.Value,
                value => AssignmentRequestId.Create(value));

        builder.Property(row => row.FestivalDayId)
            .HasConversion(
                id => id.Value,
                value => FestivalDayId.Create(value));

        builder.Property(row => row.RequestedAt)
            .IsRequired();

        builder.Property(row => row.Status)
            .HasConversion<string>()
            .HasMaxLength(PersistenceLengths.AssignmentRequestStatus)
            .IsRequired();

        builder.Property(row => row.ResolvedAt)
            .IsRequired(false);

        builder.Property(row => row.RejectionCode)
            .HasMaxLength(PersistenceLengths.OutcomeCode)
            .IsRequired(false);

        builder.Property(row => row.RejectionMessage)
            .HasMaxLength(PersistenceLengths.OutcomeMessage)
            .IsRequired(false);

        builder.Property(row => row.FailureCode)
            .HasMaxLength(PersistenceLengths.OutcomeCode)
            .IsRequired(false);

        builder.Property(row => row.FailureMessage)
            .HasMaxLength(PersistenceLengths.OutcomeMessage)
            .IsRequired(false);

        builder.HasOne<FestivalDay>()
            .WithMany()
            .HasForeignKey(row => row.FestivalDayId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(row => row.Attendees)
            .WithOne(row => row.AssignmentRequest)
            .HasForeignKey(row => row.AssignmentRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(row => new
        {
            row.FestivalDayId,
            row.RequestedAt
        });
    }
}
