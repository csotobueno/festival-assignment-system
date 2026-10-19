namespace Festival.Domain.Assignments;

public sealed class AssignmentEngine
{
    public AssignmentEngineResult Assign(
        AssignmentGroup assignmentGroup,
        AssignmentSelection selection,
        DateTimeOffset assignedAt)
    {
        ArgumentNullException.ThrowIfNull(assignmentGroup);
        ArgumentNullException.ThrowIfNull(selection);

        if (selection.Block.Spots.Count != assignmentGroup.GroupSize.Value)
        {
            throw new ArgumentException(
                "Selected block must contain exactly one spot per attendee.", nameof(selection));
        }

        var assignments = assignmentGroup.AttendeeIds
            .Zip(selection.Block.Spots)
            .Select(pair => Assignment.Create(
                AssignmentId.New(),
                assignmentGroup.AssignmentRequestId,
                assignmentGroup.FestivalDayId,
                pair.First,
                pair.Second.Code,
                pair.Second.ZoneId,
                pair.Second.RowCode,
                pair.Second.Number,
                selection.ExperienceQuality,
                assignedAt))
            .ToArray();

        assignmentGroup.EnsureValidResult(assignments);

        return AssignmentEngineResult.Assigned(assignments);
    }
}
