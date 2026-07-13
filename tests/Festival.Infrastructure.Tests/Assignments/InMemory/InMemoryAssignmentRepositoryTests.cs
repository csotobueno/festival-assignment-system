using Festival.Domain.Assignments;
using Festival.Domain.Attendees;
using Festival.Domain.FestivalDays;
using Festival.Domain.Spots;
using Festival.Domain.Zones;
using Festival.Infrastructure.Assignments.InMemory;

namespace Festival.Infrastructure.Tests.Assignments.InMemory;

public sealed class InMemoryAssignmentRepositoryTests
{
    private static readonly DateTimeOffset AssignedAt =
        new(2026, 7, 10, 9, 1, 0, TimeSpan.FromHours(-5));

    [Fact]
    public async Task SaveAsync_ShouldSaveAssignmentsInMemory()
    {
        var assignment = CreateAssignment(1);
        var repository = new InMemoryAssignmentRepository();

        await repository.SaveAsync([assignment]);

        var savedAssignment = Assert.Single(repository.Assignments);

        Assert.Equal(assignment, savedAssignment);
    }

    [Fact]
    public async Task SaveAsync_ShouldPreserveSavedAssignmentData()
    {
        var assignment = CreateAssignment(1);
        var repository = new InMemoryAssignmentRepository();

        await repository.SaveAsync([assignment]);

        var savedAssignment = Assert.Single(repository.Assignments);

        Assert.Equal(assignment.Id, savedAssignment.Id);
        Assert.Equal(
            assignment.AssignmentRequestId,
            savedAssignment.AssignmentRequestId);
        Assert.Equal(assignment.FestivalDayId, savedAssignment.FestivalDayId);
        Assert.Equal(assignment.AttendeeId, savedAssignment.AttendeeId);
        Assert.Equal(assignment.SpotCode, savedAssignment.SpotCode);
        Assert.Equal(assignment.ZoneId, savedAssignment.ZoneId);
        Assert.Equal(assignment.RowCode, savedAssignment.RowCode);
        Assert.Equal(assignment.SpotNumber, savedAssignment.SpotNumber);
        Assert.Equal(assignment.AssignedAt, savedAssignment.AssignedAt);
    }

    private static Assignment CreateAssignment(int number)
    {
        return Assignment.Create(
            AssignmentId.Create(
                Guid.Parse($"60000000-0000-0000-0000-{number:000000000000}")),
            AssignmentRequestId.Create(
                Guid.Parse("70000000-0000-0000-0000-000000000001")),
            FestivalDayId.Create(
                Guid.Parse("80000000-0000-0000-0000-000000000001")),
            AttendeeId.Create(
                Guid.Parse($"90000000-0000-0000-0000-{number:000000000000}")),
            SpotCode.Create($"SPOT-{number:000}"),
            ZoneId.Create(
                Guid.Parse("a0000000-0000-0000-0000-000000000001")),
            RowCode.Create("A"),
            SpotNumber.Create(number),
            AssignedAt);
    }
}
