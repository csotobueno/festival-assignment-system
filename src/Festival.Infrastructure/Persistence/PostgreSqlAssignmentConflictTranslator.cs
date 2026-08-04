using Festival.Application.Assignments.Persistence;
using Npgsql;

namespace Festival.Infrastructure.Persistence;

internal static class PostgreSqlAssignmentConflictTranslator
{
    private const string UniqueViolation = "23505";

    internal const string SpotAlreadyAssignedConstraint =
        "IX_Assignments_FestivalDayId_SpotCode";

    internal const string AttendeeAlreadyAssignedConstraint =
        "IX_Assignments_FestivalDayId_AttendeeId";

    internal const string DuplicateRequestAssignmentConstraint =
        "IX_Assignments_AssignmentRequestId_AttendeeId";

    internal static bool TryTranslate(
        Exception exception,
        out AssignmentPersistenceConflict conflict)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var postgresException = FindPostgresException(exception);

        if (postgresException?.SqlState != UniqueViolation)
        {
            conflict = default;
            return false;
        }

        return TryMapConstraint(postgresException.ConstraintName, out conflict);
    }

    private static PostgresException? FindPostgresException(
        Exception exception)
    {
        for (Exception? current = exception;
             current is not null;
             current = current.InnerException)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }
        }

        return null;
    }

    private static bool TryMapConstraint(
        string? constraintName,
        out AssignmentPersistenceConflict conflict)
    {
        switch (constraintName)
        {
            case SpotAlreadyAssignedConstraint:
                conflict = AssignmentPersistenceConflict.SpotAlreadyAssigned;
                return true;
            case AttendeeAlreadyAssignedConstraint:
                conflict = AssignmentPersistenceConflict.AttendeeAlreadyAssigned;
                return true;
            case DuplicateRequestAssignmentConstraint:
                conflict = AssignmentPersistenceConflict.DuplicateRequestAssignment;
                return true;
            default:
                conflict = default;
                return false;
        }
    }
}
