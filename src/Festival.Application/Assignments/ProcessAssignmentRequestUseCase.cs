using Festival.Domain.Assignments;

namespace Festival.Application.Assignments;

public sealed class ProcessAssignmentRequestUseCase
{
    public const string NoContiguousSpotsAvailableCode =
        "NO_CONTIGUOUS_SPOTS_AVAILABLE";

    private readonly IAttendeeCodeResolver attendeeCodeResolver;
    private readonly IAvailableSpotProvider availableSpotProvider;
    private readonly IAssignmentRequestStore assignmentRequestStore;
    private readonly IAssignmentStore assignmentStore;
    private readonly AssignmentEngine assignmentEngine;

    public ProcessAssignmentRequestUseCase(
        IAttendeeCodeResolver attendeeCodeResolver,
        IAvailableSpotProvider availableSpotProvider,
        IAssignmentRequestStore assignmentRequestStore,
        IAssignmentStore assignmentStore)
    {
        this.attendeeCodeResolver = attendeeCodeResolver
            ?? throw new ArgumentNullException(nameof(attendeeCodeResolver));
        this.availableSpotProvider = availableSpotProvider
            ?? throw new ArgumentNullException(nameof(availableSpotProvider));
        this.assignmentRequestStore = assignmentRequestStore
            ?? throw new ArgumentNullException(nameof(assignmentRequestStore));
        this.assignmentStore = assignmentStore
            ?? throw new ArgumentNullException(nameof(assignmentStore));
        assignmentEngine = new AssignmentEngine();
    }

    public async Task<ProcessAssignmentRequestResult> ExecuteAsync(
        ProcessAssignmentRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var assignmentRequest = AssignmentRequest.Create(
            AssignmentRequestId.New(),
            command.FestivalDayId,
            command.AttendeeCodes,
            command.RequestedAt);

        var attendeeIds = await attendeeCodeResolver
            .ResolveAttendeeIdsAsync(
                command.AttendeeCodes,
                cancellationToken);

        ArgumentNullException.ThrowIfNull(attendeeIds);

        var assignmentGroup = AssignmentGroup.Create(
            assignmentRequest.Id,
            assignmentRequest.FestivalDayId,
            attendeeIds);

        var availableSpots = await availableSpotProvider
            .GetAvailableSpotsAsync(
                assignmentRequest.FestivalDayId,
                cancellationToken);

        ArgumentNullException.ThrowIfNull(availableSpots);

        var assignmentEngineResult = assignmentEngine.Assign(
            assignmentGroup,
            availableSpots,
            command.AssignedAt);

        if (assignmentEngineResult.IsAssigned)
        {
            assignmentRequest.Complete(command.AssignedAt);

            await assignmentRequestStore.SaveAsync(
                assignmentRequest,
                cancellationToken);

            await assignmentStore.SaveAsync(
                assignmentEngineResult.Assignments,
                cancellationToken);

            return ProcessAssignmentRequestResult.Assigned(
                assignmentRequest,
                assignmentEngineResult.Assignments);
        }

        assignmentRequest.Reject(
            AssignmentRequestRejection.Create(
                NoContiguousSpotsAvailableCode,
                "No contiguous spots are available for the requested assignment group."),
            command.AssignedAt);

        await assignmentRequestStore.SaveAsync(
            assignmentRequest,
            cancellationToken);

        return ProcessAssignmentRequestResult.Rejected(assignmentRequest);
    }
}
