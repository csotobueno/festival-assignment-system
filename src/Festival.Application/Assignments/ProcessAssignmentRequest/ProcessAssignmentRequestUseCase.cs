using Festival.Application.Assignments.Ports;
using Festival.Domain.Assignments;

namespace Festival.Application.Assignments.ProcessAssignmentRequest;

public sealed class ProcessAssignmentRequestUseCase
{
    public const string NoContiguousSpotsAvailableCode =
        "NO_CONTIGUOUS_SPOTS_AVAILABLE";

    private readonly IAttendeeCodeResolver attendeeCodeResolver;
    private readonly IAvailableSpotProvider availableSpotProvider;
    private readonly IAssignmentRequestRepository assignmentRequestRepository;
    private readonly IAssignmentRepository assignmentRepository;
    private readonly AssignmentEngine assignmentEngine;

    public ProcessAssignmentRequestUseCase(
        IAttendeeCodeResolver attendeeCodeResolver,
        IAvailableSpotProvider availableSpotProvider,
        IAssignmentRequestRepository assignmentRequestRepository,
        IAssignmentRepository assignmentRepository)
    {
        this.attendeeCodeResolver = attendeeCodeResolver
            ?? throw new ArgumentNullException(nameof(attendeeCodeResolver));
        this.availableSpotProvider = availableSpotProvider
            ?? throw new ArgumentNullException(nameof(availableSpotProvider));
        this.assignmentRequestRepository = assignmentRequestRepository
            ?? throw new ArgumentNullException(nameof(assignmentRequestRepository));
        this.assignmentRepository = assignmentRepository
            ?? throw new ArgumentNullException(nameof(assignmentRepository));
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

            await assignmentRequestRepository.SaveAsync(
                assignmentRequest,
                cancellationToken);

            await assignmentRepository.SaveAsync(
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

        await assignmentRequestRepository.SaveAsync(
            assignmentRequest,
            cancellationToken);

        return ProcessAssignmentRequestResult.Rejected(assignmentRequest);
    }
}
