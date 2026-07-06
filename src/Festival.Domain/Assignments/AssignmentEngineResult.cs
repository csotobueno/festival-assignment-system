namespace Festival.Domain.Assignments;

public sealed class AssignmentEngineResult
{
    public bool IsAssigned { get; }

    public IReadOnlyList<Assignment> Assignments { get; }

    private AssignmentEngineResult(
        bool isAssigned,
        IReadOnlyList<Assignment> assignments)
    {
        IsAssigned = isAssigned;
        Assignments = assignments;
    }

    public static AssignmentEngineResult Assigned(
        IEnumerable<Assignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(assignments);

        var items = assignments.ToArray();

        if (items.Any(assignment => assignment is null))
        {
            throw new ArgumentException(
                "Assignments cannot contain null values.",
                nameof(assignments));
        }

        if (items.Length == 0)
        {
            throw new ArgumentException(
                "Assigned result must contain at least one assignment.",
                nameof(assignments));
        }

        return new AssignmentEngineResult(
            true,
            Array.AsReadOnly(items));
    }

    public static AssignmentEngineResult Unassigned()
    {
        return new AssignmentEngineResult(
            false,
            Array.AsReadOnly(Array.Empty<Assignment>()));
    }
}