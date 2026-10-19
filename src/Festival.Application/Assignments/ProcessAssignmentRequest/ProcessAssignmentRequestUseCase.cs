using Festival.Application.Assignments.Ports;
using Festival.Application.Zones.Ports;
using Festival.Domain.Assignments;
using Festival.Domain.Zones;

namespace Festival.Application.Assignments.ProcessAssignmentRequest;

public sealed class ProcessAssignmentRequestUseCase
{
    public const string AttendeeNotFoundCode = "ATTENDEE_NOT_FOUND";

    public const string NoContiguousSpotsAvailableCode =
        "NO_CONTIGUOUS_SPOTS_AVAILABLE";

    private readonly IAttendeeCodeResolver attendeeCodeResolver;
    private readonly IAvailableSpotProvider availableSpotProvider;
    private readonly IZoneCatalogProvider zoneCatalogProvider;
    private readonly IFairnessHistoryProvider fairnessHistoryProvider;
    private readonly IAssignmentRequestRepository assignmentRequestRepository;
    private readonly IAssignmentRepository assignmentRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly AssignmentEngine assignmentEngine;

    public ProcessAssignmentRequestUseCase(
        IAttendeeCodeResolver attendeeCodeResolver,
        IAvailableSpotProvider availableSpotProvider,
        IZoneCatalogProvider zoneCatalogProvider,
        IFairnessHistoryProvider fairnessHistoryProvider,
        IAssignmentRequestRepository assignmentRequestRepository,
        IAssignmentRepository assignmentRepository,
        IUnitOfWork unitOfWork,
        AssignmentEngine assignmentEngine)
    {
        this.attendeeCodeResolver = attendeeCodeResolver
            ?? throw new ArgumentNullException(nameof(attendeeCodeResolver));
        this.availableSpotProvider = availableSpotProvider
            ?? throw new ArgumentNullException(nameof(availableSpotProvider));
        this.zoneCatalogProvider = zoneCatalogProvider
            ?? throw new ArgumentNullException(nameof(zoneCatalogProvider));
        this.fairnessHistoryProvider = fairnessHistoryProvider
            ?? throw new ArgumentNullException(nameof(fairnessHistoryProvider));
        this.assignmentRequestRepository = assignmentRequestRepository
            ?? throw new ArgumentNullException(nameof(assignmentRequestRepository));
        this.assignmentRepository = assignmentRepository
            ?? throw new ArgumentNullException(nameof(assignmentRepository));
        this.unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
        this.assignmentEngine = assignmentEngine
            ?? throw new ArgumentNullException(nameof(assignmentEngine));
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
            command.RequestedAt,
            command.Eligibility);

        var attendeeIds = await attendeeCodeResolver
            .ResolveAttendeeIdsAsync(
                command.AttendeeCodes,
                cancellationToken);

        ArgumentNullException.ThrowIfNull(attendeeIds);

        // AssignmentRequest already guarantees distinct normalized attendee codes.
        if (attendeeIds.Count != assignmentRequest.RequestedAttendeeCodes.Count)
        {
            assignmentRequest.Reject(
                AssignmentRequestRejection.Create(
                    AttendeeNotFoundCode,
                    "One or more requested attendees could not be resolved."),
                command.AssignedAt);

            await assignmentRequestRepository.AddAsync(
                assignmentRequest,
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ProcessAssignmentRequestResult.Rejected(assignmentRequest);
        }

        var assignmentGroup = AssignmentGroup.Create(
            assignmentRequest.Id,
            assignmentRequest.FestivalDayId,
            attendeeIds);

        var zones = await zoneCatalogProvider.GetZonesAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(zones);

        var availableSpots = await availableSpotProvider
            .GetAvailableSpotsAsync(
                assignmentRequest.FestivalDayId,
                cancellationToken);

        ArgumentNullException.ThrowIfNull(availableSpots);

        var histories = await fairnessHistoryProvider.GetHistoriesAsync(
            assignmentGroup.AttendeeIds, cancellationToken);
        ArgumentNullException.ThrowIfNull(histories);

        var eligibleZones = ZoneEligibilityPolicy.Filter(assignmentRequest, zones);
        var groupScore = GroupRotationScore.Calculate(
            assignmentGroup.AttendeeIds.Select(id => histories[id]));
        var targetQuality = TargetQualityPolicy.Calculate(groupScore);
        var inventory = RemainingInventoryCalculator.Calculate(zones, availableSpots);
        var selection = AssignmentSelectionPolicy.Select(
            targetQuality, inventory, eligibleZones, availableSpots, assignmentGroup.GroupSize);

        if (selection is not null)
        {
            var assignmentEngineResult = assignmentEngine.Assign(
                assignmentGroup, selection, command.AssignedAt);
            assignmentRequest.Complete(command.AssignedAt);

            await assignmentRequestRepository.AddAsync(
                assignmentRequest,
                cancellationToken);

            await assignmentRepository.AddAsync(
                assignmentEngineResult.Assignments,
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ProcessAssignmentRequestResult.Assigned(
                assignmentRequest,
                assignmentEngineResult.Assignments);
        }

        assignmentRequest.Reject(
            AssignmentRequestRejection.Create(
                NoContiguousSpotsAvailableCode,
                "No contiguous spots are available for the requested assignment group."),
            command.AssignedAt);

        await assignmentRequestRepository.AddAsync(
            assignmentRequest,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ProcessAssignmentRequestResult.Rejected(assignmentRequest);
    }
}
