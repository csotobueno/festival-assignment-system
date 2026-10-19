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
| DECIDED, NOT YET ACTIVATED | Assignment ExperienceQuality transition and neutral fairness bootstrap, documented below. |
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

### Transition and Fairness Input Preparation

The [Assignment quality transition and bootstrap decision](#assignment-experiencequality-transition-and-bootstrap)
is defined below. Phase 1 may prepare Application input composition using
supplied histories and the existing pure Domain policies, plus the persistence
and rehydration design. It keeps legacy placement active and does not activate
mandatory quality or a fairness-history query against the current schema.

GroupRotationScore already calculates individual RotationScores internally.
Application will prepare FairnessHistory per attendee, invoke GroupRotationScore
and then TargetQualityPolicy without duplicating their formulas. Scores remain
derived, not stored. The persisted-history read boundary is still missing; it
must be implemented and activated with recorded quality in Phase 2.

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

## Assignment ExperienceQuality Transition and Bootstrap

**Agreed architecture decision; implementation remains pending.** Stage 4
fairness history begins when reliable `Assignment.ExperienceQuality` recording
begins. Earlier assignments are neither reconstructed nor assigned synthetic
quality. Empty history is the intentional neutral bootstrap state.

### Target Assignment Invariant and Historical Stability

At the coordinated Stage 4 cutover, the final invariant becomes:

```text
every valid Assignment → one explicit immutable ExperienceQuality
```

ExperienceQuality is the actual outcome at assignment time. It is required at
creation, persisted with the Assignment, restored explicitly during rehydration
and used as historical fairness input. It is never nullable, defaulted or inferred
from ZoneId. The active Application caller and legacy engine use available Spots
rather than AssignmentSelection today, so mandatory quality must not activate
independently before Application can produce that selection.

```text
quality at assignment time → stored on Assignment → remains historically stable
```

For example, an Assignment recorded as Medium when UpperCenter was Medium stays
Medium even if a later policy classifies UpperCenter as Good. The rejected
reconstruction is:

```text
historical Assignment.ZoneId → current Zone → current ZoneExperienceQualityPolicy
                            → historical quality
```

The current Zone policy classifies current selections; it cannot reconstruct
historical outcomes.

### Neutral Bootstrap and First Reliable Assignment

When an attendee has no reliable recorded fairness experiences, use the existing
[neutral score semantics](../stage-4/rotation-score-v1.md#neutral-state):

```text
FairnessHistory = []
HistoricalDeficit = 0
RecentRecoveryNeed = 0
GoodExperienceDeficit = 0
RotationScore = 0
```

A new attendee and an attendee with no reliable Stage 4 history both start
neutral. No special bonus or penalty is added. For a group, each such member
contributes zero to the existing arithmetic mean; other members retain their
own reliable histories.

The first successful Stage 4 assignment follows the normal selection path:

```text
empty FairnessHistory → RotationScore = 0
        ↓
GroupRotationScore → TargetQualityPolicy → TargetQuality
        ↓
AssignmentSelectionPolicy (with prepared eligibility, inventory and availability)
        ↓
AssignmentSelection.ExperienceQuality
        ↓
Assignment.ExperienceQuality → atomic successful commit
        ↓
first trustworthy fairness-history entry for future requests
```

When every member starts neutral, GroupRotationScore is zero and the existing
TargetQualityPolicy produces Medium. This is a target, not a fabricated Medium
experience: the stored quality comes from the actual selection.

Fairness-history reads activate with recording at cutover and initially return
empty histories when no outcomes exist. After successful commits, they supply
only reliable recorded `Assignment.ExperienceQuality` values from previous
assignments, ordered oldest to most recent, to FairnessHistory. Absence generates
no synthetic experience; pre-Stage-4 quality is never inferred. Existing
FairnessHistory, RotationScore, GroupRotationScore and TargetQualityPolicy logic
remains unchanged.

### Pre-Cutover Data Policy

The project is pre-production; no real production history requires preservation
or backfill. Disposable local/test Assignments without recorded quality do not
justify a mixed legacy/new historical model. Local experimental databases may
be recreated, and the first persistent Stage 4 environment may start with no
Assignments. Do not fabricate quality to preserve those disposable records or
introduce fixture-ID-specific migration logic. The concrete schema migration
and explicit rehydration changes belong to the coordinated implementation.

### Rejected Transition Approaches

- **Nullable ExperienceQuality:** weakens the final invariant and creates two
  classes of valid historical Assignment; it is not a transition strategy.
- **Default Medium or artificial Good/Bad history:** fabricates an outcome,
  including when used as a migration default or fake backfill.
- **Historical reconstruction from current Zone:** cannot preserve quality at
  assignment time after classification changes.
- **Temporary Zone lookup in AssignmentEngine:** do not add a Zone provider to
  the legacy engine solely to calculate quality.
- **Temporary dual Assignment creation APIs:** do not retain one production
  creation path requiring quality and another omitting it. Activate the required
  quality contract with the single Stage 4 placement path.

## Phase 2 — Coordinated Final Cutover

Implement and activate one coherent boundary: Application selection integration,
the engine API, mandatory Assignment quality, persistence/schema migration,
explicit quality rehydration, recorded fairness-history read support and removal
of legacy placement. History reads are not a prerequisite against the old schema;
they start with the neutral bootstrap above and use recorded outcomes thereafter.

At this cutover, ProcessAssignmentRequestUseCase invokes:

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

Migrate the engine API, activate mandatory quality in Assignment creation,
persistence and rehydration, and wire recorded-history reads into Application.
Stop the old placement path and remove FindFirstContiguousBlock and IsConsecutive
when no longer referenced. Retain no permanent compatibility overload or parallel
placement architecture.

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

The revised delivery sequence separates independent preparation from activation:

1. Expose Current Zone Read Access — implemented; orchestration remains pending.
2. Define Assignment ExperienceQuality transition and neutral bootstrap — decided
   in this document; recording and history-read implementation remain pending.
3. Prepare independent Application input composition and other dependencies that
   can use supplied histories without persisted quality. Keep the legacy caller
   active; do not make old-schema fairness-history reads a delivery prerequisite.
4. Perform the coordinated Stage 4 activation: ProcessAssignmentRequestUseCase
   produces AssignmentSelection; AssignmentEngine consumes it; Assignment requires
   quality; persistence/schema migration stores it; rehydration restores it;
   fairness-history support reads recorded outcomes; legacy placement is removed.

Steps 1–3 are Phase 1; step 4 is Phase 2. Its coupled changes belong to one coherent
implementation boundary. Do not force separate deliveries that require temporary
incompatible APIs, nullable quality or fabricated history. This corrects the
earlier sequence that placed history reads before required recorded quality.

## Support Policy and Separate Concerns

### InMemory Support

InMemory adapters provide lightweight test/demo support, not a second production
implementation or required full production parity. Extend them only for a
concrete test or demo scenario. Do not require new InMemory capabilities solely
to mirror PostgreSQL dynamic availability, fairness-history queries, locking,
transactions or production concurrency semantics.
No InMemory historical backfill or production-equivalent migration path is
required by this transition.

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
