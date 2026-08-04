namespace Festival.Application.Assignments.Persistence;

public sealed class AssignmentPersistenceConflictException : Exception
{
    public AssignmentPersistenceConflict Conflict { get; }

    public AssignmentPersistenceConflictException(
        AssignmentPersistenceConflict conflict,
        Exception innerException)
        : base(CreateMessage(conflict), innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        Conflict = conflict;
    }

    private static string CreateMessage(
        AssignmentPersistenceConflict conflict)
    {
        return conflict switch
        {
            AssignmentPersistenceConflict.SpotAlreadyAssigned =>
                "The assignment could not be persisted because the spot is already assigned for the festival day.",
            AssignmentPersistenceConflict.AttendeeAlreadyAssigned =>
                "The assignment could not be persisted because the attendee is already assigned for the festival day.",
            AssignmentPersistenceConflict.DuplicateRequestAssignment =>
                "The assignment could not be persisted because the request contains a duplicate attendee assignment.",
            _ => throw new ArgumentOutOfRangeException(
                nameof(conflict),
                conflict,
                "The assignment persistence conflict is not supported.")
        };
    }
}
