# Stage 4 Application Cutover Boundary

## Decision and Current Status

**Implemented: coordinated Stage 4 cutover.** Stable dependencies were prepared
first, then the Application caller, engine, required Assignment quality,
persistence/rehydration and recorded-history reads were activated together.

| Status | Boundary |
| --- | --- |
| IMPLEMENTED | Application prepares fairness, eligibility and inventory inputs and invokes AssignmentSelectionPolicy. |
| IMPLEMENTED | Current Zone catalog and batched recorded fairness histories are read through Application ports. |
| IMPLEMENTED | AssignmentEngine materializes the selected block and actual quality; Assignment.ExperienceQuality is required, immutable and persisted. |
| IMPLEMENTED | Empty FairnessHistory supplies the neutral bootstrap. |
| REMOVED | Legacy first-contiguous-block placement, FindFirstContiguousBlock and IsConsecutive. |

There is one production placement path:
`AssignmentSelectionPolicy → AssignmentSelection → AssignmentEngine`.
External callers still supply business-request data; Application prepares the
selection internally. The [runtime flow](#application-ownership-and-target-end-state)
below includes the existing complete-attendee-resolution guard and one atomic
write boundary.

## Why the Caller and Engine Must Transition Together

The previous engine selected from available Spots, while the new contract
consumes an AssignmentSelection. Changing the engine alone would have broken
its caller; activating required quality alone would have left the old creation
path unable to supply an actual selected outcome. Both contracts, persistence,
rehydration and history reads now use the coordinated Stage 4 boundary.
No compatibility placement overload or external selection contract remains.

## Phase 1 — Prepare Stable Dependencies

**Delivered.** Current Zone access and the transition/bootstrap decision were
prepared before activating the coordinated runtime flow.

### Current Zone Read Access

Implemented as `IZoneCatalogProvider.GetZonesAsync(CancellationToken)`, returning
`IReadOnlyList<Zone>`. The scoped `PostgreSqlZoneCatalogProvider` reads all current
Zones from `FestivalDbContext.Zones` through the existing mapping, without
tracking, and propagates cancellation. Results are read-only and may be empty.
ZoneId ascending is technical ordering only; business priority remains with
Stage 4 policies. The catalog includes FrontStanding, has no FestivalDay
relationship and adds no eligibility, quality classification or inventory logic.

ProcessAssignmentRequestUseCase now consumes this port for ZoneEligibilityPolicy
and RemainingInventoryCalculator input preparation. PostgreSQL registers the
adapter as scoped. InMemory composition now requires the port too and supplies
the deterministic configured Zones through InMemoryZoneCatalogProvider.

Application must obtain Zones through Application ports, with Infrastructure
implementing data access. It must not read FestivalDbContext directly. This is
input preparation before selection, not a post-selection Zone lookup to recover
quality. The previously discarded lookup approach is not reintroduced.

### Transition and Fairness Input Preparation

The [transition and bootstrap decision](#assignment-experiencequality-transition-and-bootstrap)
is implemented. `IFairnessHistoryProvider.GetHistoriesAsync(IReadOnlyCollection<AttendeeId>,
CancellationToken)` returns an `IReadOnlyDictionary<AttendeeId, FairnessHistory>`
with an entry for every requested attendee, including empty histories.

PostgreSqlFairnessHistoryProvider uses one no-tracking query over persisted
Assignments for the requested IDs. It reads only recorded quality, ordered by
AssignedAt ascending then AssignmentId ascending for exact timestamp ties.
The latter is a technical tie-break, not a fairness preference. Empty requests
return an empty dictionary without a query; cancellation is propagated.
No current Zone lookup, absence entry or synthetic outcome is involved.

Application passes member histories to GroupRotationScore, which already
calculates individual RotationScores internally, then TargetQualityPolicy.
No formula is duplicated and no score or target is persisted.

### Eligibility and Inventory Preparation

The implemented Application preparation is:

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

**Implemented architecture decision.** Stage 4
fairness history begins when reliable `Assignment.ExperienceQuality` recording
begins. Earlier assignments are neither reconstructed nor assigned synthetic
quality. Empty history is the intentional neutral bootstrap state.

### Target Assignment Invariant and Historical Stability

The coordinated Stage 4 cutover enforces:

```text
every valid Assignment → one explicit immutable ExperienceQuality
```

ExperienceQuality is the actual outcome at assignment time. It is required at
creation, persisted with the Assignment, restored explicitly during rehydration
and used as historical fairness input. It is never nullable, defaulted or inferred
from ZoneId. The one creation API requires explicit quality. EF constructor
binding restores the required persisted value and validates the enum; it does
not reclassify the current Zone. Required quality was activated with Application
selection rather than added independently to the previous legacy caller.

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
introduce fixture-ID-specific migration logic. `RecordAssignmentExperienceQuality`
adds required text `Assignments.ExperienceQuality` without a default or backfill,
following existing enum-name persistence conventions. Applying it to a disposable
experimental database with old Assignments requires recreating that database.
Integration tests use isolated empty PostgreSQL databases, not local data resets.

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

**Implemented as one coherent boundary:** Application selection integration,
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

The engine responsibility is now:

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

The previous placement helpers and their algorithm-specific engine tests have
been removed/replaced with exact-selection materialization tests. The engine
checks block size against GroupSize before zipping, preserves member order and
retains AssignmentGroup.EnsureValidResult. Null selection is handled by Application
using the existing `NO_CONTIGUOUS_SPOTS_AVAILABLE` rejection code: no complete
feasible block exists in any eligible Zone. Rejected requests are staged and saved
once without invoking the engine or adding Assignments.

### Application Ownership and Target End-State

AssignmentSelection preparation belongs inside Application orchestration.
External callers continue to supply business-request data. The rejected shape
is an external caller constructing AssignmentSelection and passing it to
ProcessAssignmentRequestUseCase as a temporary integration contract.

The active runtime flow is:

```text
ProcessAssignmentRequestCommand
        ↓
AssignmentRequest
        ↓
resolve AttendeeIds
        ↓
validate complete resolution (otherwise ATTENDEE_NOT_FOUND)
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

All four cutover increments are delivered:

1. Current Zone catalog read access.
2. Assignment quality transition and neutral bootstrap decision.
3. Application composition using existing Domain policies.
4. Coordinated activation of selection, engine materialization, required quality,
   persistence/rehydration and recorded-history reads; legacy placement removed.

Stage 4 integration is implemented; Stage 5 simulation and final fairness
measurement remain separate work. No new fairness rule or selection policy was
introduced by this cutover.

## Support Policy and Separate Concerns

### InMemory Support

InMemory adapters provide lightweight test/demo support, not a second production
implementation or required full production parity. Extend them only for a
concrete test or demo scenario. Do not require new InMemory capabilities solely
to mirror PostgreSQL dynamic availability, fairness-history queries, locking,
transactions or production concurrency semantics.
InMemoryZoneCatalogProvider exposes configured seed Zones, and
InMemoryFairnessHistoryProvider reads recorded outcomes from the existing
InMemoryAssignmentRepository in deterministic chronological order. Both are
required by the current test/demo composition. Empty histories remain empty;
there is no historical backfill, new transaction behavior, assignment-aware
Spot availability, locking or production-equivalent migration path.

### Attendee Resolution Completeness — Resolved

ProcessAssignmentRequestUseCase now checks resolved AttendeeId count against the
request's distinct normalized AttendeeCodes before creating AssignmentGroup.
Incomplete resolution rejects the whole request with `ATTENDEE_NOT_FOUND`,
persists that rejection through the existing UnitOfWork and skips Spot loading
and engine execution. A three-person request cannot silently become a two-person
group. Resolver behavior and the Stage 4 cutover remain unchanged.

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
