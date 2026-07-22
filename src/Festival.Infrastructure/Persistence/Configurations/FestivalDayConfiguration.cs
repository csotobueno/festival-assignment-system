using Festival.Domain.FestivalDays;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Festival.Infrastructure.Persistence.Configurations;

internal sealed class FestivalDayConfiguration
    : IEntityTypeConfiguration<FestivalDay>
{
    public void Configure(EntityTypeBuilder<FestivalDay> builder)
    {
        builder.ToTable("FestivalDays");

        builder.HasKey(festivalDay => festivalDay.Id);

        builder.Property(festivalDay => festivalDay.Id)
            .HasColumnName("FestivalDayId")
            .HasConversion(
                id => id.Value,
                value => FestivalDayId.Create(value));

        builder.Property(festivalDay => festivalDay.Date)
            .IsRequired();

        builder.HasIndex(festivalDay => festivalDay.Date)
            .IsUnique();

        builder.OwnsOne(
            festivalDay => festivalDay.AssignmentWindow,
            assignmentWindow =>
            {
                assignmentWindow.Property(window => window.Start)
                    .HasColumnName("AssignmentWindowStart")
                    .IsRequired();

                assignmentWindow.Property(window => window.End)
                    .HasColumnName("AssignmentWindowEnd")
                    .IsRequired();
            });

        builder.Navigation(festivalDay => festivalDay.AssignmentWindow)
            .IsRequired();
    }
}
