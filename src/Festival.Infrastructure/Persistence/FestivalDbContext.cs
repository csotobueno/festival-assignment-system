using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;
using Festival.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Festival.Infrastructure.Persistence;

public sealed class FestivalDbContext(
    DbContextOptions<FestivalDbContext> options)
    : DbContext(options)
{
    public DbSet<Attendee> Attendees => Set<Attendee>();

    public DbSet<FestivalDay> FestivalDays => Set<FestivalDay>();

    public DbSet<Zone> Zones => Set<Zone>();

    public DbSet<Spot> Spots => Set<Spot>();

    internal DbSet<AssignmentRequestRow> AssignmentRequests => Set<AssignmentRequestRow>();

    internal DbSet<AssignmentRequestAttendeeRow> AssignmentRequestAttendees => Set<AssignmentRequestAttendeeRow>();
    
    public DbSet<Assignment> Assignments => Set<Assignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FestivalDbContext).Assembly);
    }
}
