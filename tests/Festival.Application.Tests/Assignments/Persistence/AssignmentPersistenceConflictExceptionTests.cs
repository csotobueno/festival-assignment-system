using Festival.Application.Assignments.Persistence;

namespace Festival.Application.Tests.Assignments.Persistence;

public sealed class AssignmentPersistenceConflictExceptionTests
{
    [Theory]
    [InlineData(
        AssignmentPersistenceConflict.SpotAlreadyAssigned,
        "The assignment could not be persisted because the spot is already assigned for the festival day.")]
    [InlineData(
        AssignmentPersistenceConflict.AttendeeAlreadyAssigned,
        "The assignment could not be persisted because the attendee is already assigned for the festival day.")]
    [InlineData(
        AssignmentPersistenceConflict.DuplicateRequestAssignment,
        "The assignment could not be persisted because the request contains a duplicate attendee assignment.")]
    public void Constructor_ShouldExposeStableConflictAndPreserveInnerException(
        AssignmentPersistenceConflict conflict,
        string expectedMessage)
    {
        var persistenceException = new InvalidOperationException(
            "Provider-specific details.");

        var exception = new AssignmentPersistenceConflictException(
            conflict,
            persistenceException);

        Assert.Equal(conflict, exception.Conflict);
        Assert.Equal(expectedMessage, exception.Message);
        Assert.Same(persistenceException, exception.InnerException);
        Assert.DoesNotContain("PostgreSQL", exception.Message);
        Assert.DoesNotContain("23505", exception.Message);
        Assert.DoesNotContain("IX_", exception.Message);
    }
}
