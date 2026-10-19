# Stage 4 Application Cutover Boundary

## Decision and Current Status

**Agreed strategy: Two-step cutover.** Prepare stable dependencies while the
current placement path remains active, then transition the Application caller
and Assignment Engine together. This document records the agreed integration
boundary; it does not claim that the cutover is implemented.

| Status | Boundary |
| --- | --- |
| IMPLEMENTED | Pure Stage 4 Domain selection: Zone ordering, physical feasibility, block selection and AssignmentSelection with actual ExperienceQuality. |
| IMPLEMENTED | Current Zone catalog read access through IZoneCatalogProvider and its PostgreSQL adapter; not yet consumed by Application orchestration. |
| NOT YET INTEGRATED | Application preparation of Stage 4 inputs, AssignmentSelectionPolicy invocation from the use case, AssignmentEngine cutover and persisted Assignment.ExperienceQuality. |
| TEMPORARILY ACTIVE | Legacy AssignmentEngine placement from available Spots. |
| FINAL TARGET | AssignmentSelectionPolicy selects placement; AssignmentEngine only materializes the selection. |

The currently active flow is:

```text
ProcessAssignmentRequestCommand
        ↓
ProcessAssignmentRequestUseCase
        ↓
AssignmentRequest → resolve AttendeeIds → AssignmentGroup
        ↓
load available Spots
        ↓
AssignmentEngine.Assign(assignmentGroup, availableSpots, assignedAt)
        ↓
legacy contiguous-block placement
        ↓
Assignment[] → repositories → UnitOfWork
```

The separate implemented Domain pipeline consumes prepared TargetQuality,
RemainingInventoryState, eligible Zones, available Spots and GroupSize through
AssignmentSelectionPolicy. Its AssignmentSelection contains Zone, Block and
actual ExperienceQuality. It creates no Assignment.

## Why the Caller and Engine Must Transition Together

The current engine requires available Spots; its final contract will require an
AssignmentSelection. ProcessAssignmentRequestUseCase cannot yet produce that
selection: current Zone read access is available, but it lacks attendee
fairness-history read access, TargetQuality preparation and
RemainingInventoryState preparation. Assignment also does not yet record
historical ExperienceQuality.

Changing the engine first would break its current caller. The agreed solution is
to prepare dependencies first and perform the coordinated caller/engine cutover
later. It is not a temporary external caller contract or a permanent pair of
placement strategies.

## Phase 1 — Prepare Stable Dependencies

During this phase, ProcessAssignmentRequestUseCase continues to use legacy
AssignmentEngine placement until the required Stage 4 inputs can be produced
safely. Preparing a capability does not mean activating Stage 4 in the current
Application flow.

### Current Zone Read Access

Implemented as `IZoneCatalogProvider.GetZonesAsync(CancellationToken)`, returning
`IReadOnlyList<Zone>`. The scoped `PostgreSqlZoneCatalogProvider` reads all current
Zones from `FestivalDbContext.Zones` through the existing mapping, without
tracking, and propagates cancellation. Results are read-only and may be empty.
ZoneId ascending is technical ordering only; business priority remains with
Stage 4 policies. The catalog includes FrontStanding, has no FestivalDay
relationship and adds no eligibility, quality classification or inventory logic.

Application already has IAvailableSpotProvider, but Spots carry only ZoneId,
not Zone metadata. The new port is available for future ZoneEligibilityPolicy
and RemainingInventoryCalculator input preparation; the use case does not yet
consume it. InMemory composition does not require the unused port today, so no
InMemory adapter is added; its existing test/demo flow remains unchanged.

Application must obtain Zones through Application ports, with Infrastructure
implementing data access. It must not read FestivalDbContext directly. This is
input preparation before selection, not a post-selection Zone lookup to recover
quality. The previously discarded lookup approach is not reintroduced.

### Assignment ExperienceQuality Transition

The target invariant is:

```text
every valid historical Assignment → recorded immutable ExperienceQuality
```

ExperienceQuality is the actual outcome at assignment time. Historical reads
must not reconstruct it through Assignment.ZoneId → current Zone → current
ZoneExperienceQualityPolicy, because classification may evolve.

Phase 1 prepares the transition and persistence strategy. Activating mandatory
quality must be coordinated with the active Assignment creation path at final
cutover; it must not independently break the legacy caller. Nullable quality is
not the target model. Default Medium, fabricated historical quality, current
Zone reclassification and post-selection Zone lookup are rejected transition
solutions. The implementation must coordinate mandatory quality with creation
and persistence rather than silently filling missing outcomes.

### Fairness History Read Access and Input Preparation

Application currently lacks the read boundary for:

```text
AttendeeId
        ↓
previous recorded Assignment outcomes
        ↓
ordered Assignment.ExperienceQuality values
        ↓
FairnessHistory per attendee
        ↓
GroupRotationScore.Calculate(memberHistories)
        ↓
TargetQualityPolicy.Calculate(groupScore)
        ↓
TargetQuality
```

The future query must read recorded Assignment.ExperienceQuality, not current
Zone classification, and supply experiences from oldest to most recent as
required by FairnessHistory. This capability depends on the quality transition;
the current schema is not an already-classified history source.

GroupRotationScore already calculates individual RotationScores internally.
Application orchestrates existing pure Domain policies without duplicating
their formulas. Scores remain derived values; Application does not need to
persist RotationScore or introduce scoring storage. No fairness rule changes.

### Eligibility and Inventory Preparation

The eventual Application preparation is:

```text
AssignmentRequest + current Zones
        ↓
ZoneEligibilityPolicy
        ↓
eligible Zones

Zones + availableSpots
        ↓
RemainingInventoryCalculator
        ↓
RemainingInventoryState
```

Eligibility filters Zones without changing ExperienceQuality. This decision
adds neither Spot-level eligibility nor quality reclassification.

Inventory uses the same logical current decision snapshot as the available
Spots used for selection. Raw availability is not physical feasibility;
contiguous-block discovery remains outside inventory calculation. The existing
calculator requires Zone metadata for every supplied Spot's ZoneId. This
cutover decision does not change inventory counting or selection rules.

## Phase 2 — Coordinated Final Cutover

Once Application can prepare all required inputs, ProcessAssignmentRequestUseCase
invokes:

```text
AssignmentSelectionPolicy.Select(
    targetQuality, inventory, eligibleZones, availableSpots, groupSize)
        ↓
AssignmentSelection?
```

Null means no current placement is possible and the request is rejected without
Assignments. A selection continues to materialization. There is no second
placement fallback. Current request-status semantics remain unchanged.

At the same cutover, change the engine responsibility to:

```text
AssignmentGroup + AssignmentSelection + AssignedAt
        ↓
AssignmentEngine
        ↓
Assignment[] with recorded ExperienceQuality
```

The engine consumes selection.Block and selection.ExperienceQuality. It no
longer decides placement, uses TargetQuality, looks up a Zone, recalculates
quality or infers it from ZoneId. AssignmentSelection already contains the final
actual outcome, transferred directly to Assignment.ExperienceQuality. TargetQuality
remains the decision reference; ExperienceQuality is the actual immutable outcome.

Migrate the engine API, activate mandatory quality in Assignment creation and
persistence, stop the old placement path and remove FindFirstContiguousBlock and
IsConsecutive when no longer referenced. Retain no permanent compatibility
overload or parallel placement architecture.

### Application Ownership and Target End-State

AssignmentSelection preparation belongs inside Application orchestration.
External callers continue to supply business-request data. The rejected shape
is an external caller constructing AssignmentSelection and passing it to
ProcessAssignmentRequestUseCase as a temporary integration contract.

The final target flow is:

```text
ProcessAssignmentRequestCommand
        ↓
AssignmentRequest
        ↓
resolve AttendeeIds
        ↓
AssignmentGroup
        ↓
load available Spots
load current Zones
load attendee fairness histories
        ↓
ZoneEligibilityPolicy
GroupRotationScore
TargetQualityPolicy
RemainingInventoryCalculator
        ↓
AssignmentSelectionPolicy
        ↓
AssignmentSelection?
        ├── null
        │     ↓
        │   reject request → request repository → UnitOfWork
        │
        └── selection
              ↓
        AssignmentEngine
              ↓
        Assignment[] with recorded ExperienceQuality
              ↓
        repositories
              ↓
        UnitOfWork
```

The final architecture has one active placement path:
AssignmentSelectionPolicy → AssignmentSelection → AssignmentEngine materialization.

## Implementation Sequence

The agreed delivery sequence is:

1. Expose Current Zone Read Access — implemented; orchestration remains pending.
2. Prepare Assignment ExperienceQuality transition and persistence strategy
   consistent with the cutover.
3. Read Attendee Fairness History.
4. Prepare Stage 4 Fairness Inputs in Application.
5. Prepare eligibility + inventory + AssignmentSelection inside
   ProcessAssignmentRequestUseCase.
6. Perform coordinated final cutover: ProcessAssignmentRequestUseCase +
   AssignmentEngine + mandatory Assignment.ExperienceQuality.
7. Remove legacy placement logic.

Steps 5–7 may be delivered in one coherent implementation issue if separating
them would require a temporary incompatible API. Step 2 prepares the transition;
it does not claim mandatory historical quality is already active. Do not reorder
this sequence without reporting a concrete repository constraint.

## Support Policy and Separate Concerns

### InMemory Support

InMemory adapters provide lightweight test/demo support, not a second production
implementation or required full production parity. Extend them only for a
concrete test or demo scenario. Do not require new InMemory capabilities solely
to mirror PostgreSQL dynamic availability, fairness-history queries, locking,
transactions or production concurrency semantics.

### Attendee Resolution Technical Debt

Current attendee resolvers can return fewer AttendeeIds than requested
AttendeeCodes. Application creates AssignmentGroup from those IDs without
verifying one-to-one resolution. A request for three attendees may resolve two
IDs and create GroupSize 2. This requires a separate validation issue; its
implementation is not part of the Stage 4 cutover.

### Snapshot and Concurrency Risk

Zones, available Spots and fairness histories may be read at different moments.
Sharing one scoped DbContext does not automatically make all pre-save reads one
explicit transactionally consistent snapshot. Evaluate this as a later
concurrency/integration concern when the Application flow is fully wired; this
decision introduces no transaction or locking changes.

Preserve the [Stage 3 atomic persistence boundary](../stage-3-persistence-model-and-transaction-boundary.md#12-persistence-contract-evaluation):
repositories stage the complete outcome and one UnitOfWork save confirms it.
Database integrity constraints remain the final protection for conflicting
writes; current rejection and failure semantics are unchanged.

### Deferred Global Assignment State and Fairness

CurrentGlobalAssignmentState and ZoneAssignmentState remain deferred, outside
the current Stage 4 v1 selection and cutover path. Recorded historical quality
may later unblock that separate work; it does not make it required now.

The official fairness-rule count remains **19**. This decision changes
Application integration architecture only, without changing fairness, Zone
evaluation, block selection or Stage 5 questions.
