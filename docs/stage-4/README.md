# Stage 4 — Online Assignment Engine & Fairness

## Status and Purpose

**Pure Stage 4 Domain selection is implemented; Application integration is pending.**

The agreed [Stage 4 Application Cutover Boundary](../architecture/stage-4-application-cutover-boundary.md)
uses a **two-step cutover**: prepare stable dependencies while legacy engine
placement remains temporarily active, then transition the use case, engine and
mandatory Assignment.ExperienceQuality together. There is no permanent dual path.
The [transition and bootstrap decision](../architecture/stage-4-application-cutover-boundary.md#assignment-experiencequality-transition-and-bootstrap)
is agreed; its implementation remains pending.

The technical MVP tests whether a reliable system can assign locations to
Attendees across several festival days while preserving fairness and flexible
daily groups. Stage 4 builds the minimum executable online baseline. Stage 5
measures its results and limitations across five-day simulation scenarios and
produces evidence for a proposal to the festival organization.

The complete operational system is outside this MVP. Operational and business
considerations will be refined with the organization after that evidence is
available. The [project operating model](../project-operating-model.md) defines
the overall scope.

These documents are a versioned starting hypothesis. They do not claim that all
selection policies are settled or already implemented. Resolve each open decision
when its increment needs it, record a concrete scenario and expected behavior,
and add complexity only when evidence justifies it. Final calibration is not a
prerequisite for beginning implementation.

## Reading Order and Document Responsibilities

| Order | Document | Main responsibility |
| --- | --- | --- |
| 1 | [Fairness Definition v1](fairness-definition-v1.md) | Business principles, individual/group/global fairness and evaluation dimensions. |
| 2 | [RotationScore v1](rotation-score-v1.md) | Initial formula, signals, group average and calculated reference scenarios. |
| 3 | [Assignment Strategy v1](assignment-strategy-v1.md) | Decision flow, agreed Zone Evaluation Strategy, availability and feasibility boundaries. |
| 4 | [Implementation Plan](implementation-plan.md) | Increments, decisions needed by each increment, validation and exit criteria. |
| 5 | [Trade-offs and Open Questions](trade-offs-and-open-questions.md) | Accepted limitations, their reasons, and later evaluation or organization questions. |
| 6 | [Stage 4 Application Cutover Boundary](../architecture/stage-4-application-cutover-boundary.md) | Agreed two-phase integration, ownership, delivery sequence and separate risks. |

Keep detailed definitions in their responsible document and link to them from
other files. The [glossary](../glossary.md) defines shared terms; the
[critical invariants](../critical-invariants.md) remain mandatory. Earlier stage
documents preserve their historical decisions. The
[Stage 3 closure](../stages/stage-3-closure.md) describes the implemented
persistence foundation on which this proposal builds.

## Assignment Flow

This is the target Stage 4 flow, not the currently active Application flow.
Today ProcessAssignmentRequestUseCase resolves attendees, creates the group,
loads available Spots and calls the legacy AssignmentEngine. Application input
preparation, selection-policy invocation, engine cutover and persisted Assignment
ExperienceQuality are not yet integrated. External callers continue supplying
business-request data; Application will prepare AssignmentSelection internally.

1. Receive an AssignmentRequest with one to ten Attendees and resolve its members.
2. Load each Attendee's reliable recorded Assignment.ExperienceQuality history;
   use an empty history when no such outcomes exist.
3. Calculate individual RotationScores. For a group, use their arithmetic mean
   as GroupRotationScore; individual histories remain separate.
4. Derive a [Target Quality v1](assignment-strategy-v1.md#3-determine-target-quality) reference: `Good` or `Medium`; `Bad` is only a possible actual outcome.
5. Apply organization policies and `ZoneEligibilityPolicy` to obtain eligible Zones;
   exclusions never change ExperienceQuality.
6. Use TargetQuality and Zone-level raw availability to order eligible Zones via
   the agreed [Zone Evaluation Strategy v1](assignment-strategy-v1.md#11-zone-evaluation-strategy-v1).
7. Use `FeasibleZoneCandidateFinder` to call `FeasibleSpotBlockFinder` per ordered
   Zone; return the first feasible Zone together with all its blocks.
8. Select one block through the implemented [Block Selection Policy v1](assignment-strategy-v1.md#block-selection-policy-v1).
   Steps 6–8 are composed by `AssignmentSelectionPolicy`, which returns the
   selected Zone, block and actual ExperienceQuality. Engine integration remains pending.
9. Persist the request outcome and all Assignments atomically. Successful outcomes
   become history for subsequent requests.

```text
prepared decision inputs:
TargetQuality + RemainingInventoryState + eligible Zones + available Spots + GroupSize
        ↓
AssignmentSelectionPolicy
        ↓
AssignmentSelection
        ├── Zone
        ├── FeasibleSpotBlock (Block)
        └── ExperienceQuality
```

Internally: `ZoneEvaluationPolicy` → `FeasibleZoneCandidateFinder` (using
`FeasibleSpotBlockFinder` per Zone) → `BlockSelectionPolicy`. Actual quality
comes from `ZoneExperienceQualityPolicy.GetQuality(selectedZone.Type)`, independently
of TargetQuality. The immutable result requires the block's ZoneId and current
Zone quality to match. The pure selection returns `null` when no placement exists;
it creates no Assignment or request rejection. Its caller prepares all inputs
from the same decision snapshot; engine and application integration remain pending.

Good targets use Good → Medium → Bad. Medium targets use Good → Medium → Bad
when GoodRemaining > MediumRemaining, otherwise Medium → Good → Bad (equality
means Medium first). Within a quality, use AvailableSpotCount descending, then
a stable technical tie-break such as ZoneId ascending for an exact count tie.
Target Quality is neither an entitlement nor a ceiling. Bad is an actual
last-resort result, never a target. CurrentGlobalAssignmentState is deferred.

If no complete eligible and feasible block exists, reject the request without
Assignments. Lack of contiguous capacity retains
`CONTIGUOUS_SPOTS_NOT_AVAILABLE`. A changed group requires a new request; the
original request does not remain pending.

## Application Cutover

**Phase 1 — Prepare Stable Dependencies:** current Zone read access is implemented
and the quality transition/bootstrap decision is agreed. Prepare independent
Application input composition using supplied histories and other dependencies
that do not require persisted quality. GroupRotationScore already computes
individual scores from member histories. Keep legacy placement active; do not
require fairness-history queries against the old schema.

**Phase 2 — Coordinated Final Cutover:** the use case invokes
AssignmentSelectionPolicy; null rejects the request, while a selection is
materialized by AssignmentEngine from its Block and ExperienceQuality. Transition
caller, engine API, mandatory Assignment quality, persistence/schema migration,
explicit rehydration and recorded-history reads together; remove legacy helpers
and retain one active placement path. Preserve the Stage 3 UnitOfWork boundary
and request-status semantics.

Fairness history begins with reliable quality recording. No reliable outcomes
means `FairnessHistory = []` and `RotationScore = 0`, the intentional neutral
bootstrap. The first successful Stage 4 Assignment records actual selection
quality and becomes trustworthy history for future requests. Earlier quality is
never reconstructed or fabricated.

The [architecture decision](../architecture/stage-4-application-cutover-boundary.md#implementation-sequence)
defines the revised preparation/activation sequence. InMemory remains lightweight
test/demo support without required production parity. Incomplete attendee
resolution is now rejected before group creation; see the [resolved validation gap](../architecture/stage-4-application-cutover-boundary.md#attendee-resolution-completeness--resolved).
Pre-save snapshot consistency remains a separate concern; this cutover does not
implement its fix or global assigned-outcome state.

## Baseline and Boundaries

- **Online processing:** daily group composition can change. Decisions use known
  current state, without forecasting or reserving capacity for unknown requests.
  Arrival order can affect results.
- **Recovery need:** RotationScore uses historical experience. A new Attendee has
  score zero; absences introduce no synthetic assignments. Inventory and global
  state remain outside the score; global assigned-outcome influence is deferred.
- **Group average:** scores `2, 4, 1, 3, 0` produce GroupRotationScore `2`. This
  guides quality and Zone selection while preserving the complete group.
- **Experience Quality:** the agreed [seven ZoneTypes and global mapping](assignment-strategy-v1.md#agreed-zone-taxonomy-and-global-mapping)
  derive `Good`, `Medium` or `Bad` through `ZoneExperienceQualityPolicy`.
  Request eligibility changes participation only; historical recorded quality
  remains stable. ZoneType, the quality policy and actual quality in
  AssignmentSelection are implemented; historical quality recording remains pending.
- **Zone model evolution:** mandatory `Zone.Type` replaces `IsFrontStanding`;
  eligibility uses `Zone.Type == ZoneType.FrontStanding`.
  `ZoneCode` remains deferred until a concrete integration or identifier need.
- **Availability:** ZoneAvailabilityState (ZoneId, ExperienceQuality,
  AvailableSpotCount) is implemented as the primary inventory source in the
  immutable RemainingInventoryState snapshot. Quality totals derive from these
  entries; the calculator omits zero-availability Zones. Raw counts do not prove
  group feasibility. `ZoneEvaluationPolicy.Order(targetQuality, inventory, eligibleZones)`
  now orders participating Zones deterministically. `FeasibleZoneCandidateFinder`
  composes ordered traversal with physical feasibility and returns the first
  feasible Zone with all its blocks. AssignmentSelectionPolicy composes ordering,
  feasibility and block selection; engine integration remains pending.
- **Block selection:** v1 is implemented as
  `BlockSelectionPolicy.Select(candidate, availableSpots)`. Rank already feasible
  blocks by LargestRemainingContiguousRun descending in the affected Row after
  simulated removal, then RowCode ascending and starting SpotNumber ascending.
  Use current availability, including existing gaps; an empty remaining Row scores
  zero. This local capacity heuristic does not change Zone priority or add a
  fairness rule; the official fairness-rule count remains 19.
- **Invariants:** same Zone, same Row, consecutive SpotNumbers, complete groups,
  daily uniqueness and final Assignments remain protected. Fairness cannot
  override eligibility or physical feasibility.
- **Determinism:** identical relevant input state produces the same decision.
  This does not determine which concurrent transaction wins; Stage 3 persistence
  and conflict guarantees remain in force.
- **Starting data:** no production history needs backfill. Disposable local/test
  databases may be recreated, and the first persistent Stage 4 environment may
  start with no Assignments, following the [pre-cutover data policy](../architecture/stage-4-application-cutover-boundary.md#pre-cutover-data-policy).
  Deterministic test fixtures may provide explicit scenarios; they do not justify
  fabricated historical quality, fixture-ID-specific migrations or absence records.

## Implementation Readiness

The initial score formula, neutral state and group average are sufficiently
specified to begin pure calculations using already-classified histories.
Location classification need not block that work.

The [implementation decision table](implementation-plan.md#decisions-at-the-point-of-use)
records the agreed Zone-quality mapping, Target Quality and Zone Evaluation
ordering, plus the implemented Zone-level availability prerequisite and ordering
policy. Block Selection Policy v1 is implemented; global
assigned-outcome influence remains deferred. An unresolved business rule pauses
its dependent increment; independent work can continue. No operational rule should
be invented merely to complete the design.

## Scope

Stage 4 includes Experience Quality, history, RotationScore and GroupRotationScore,
Target Quality, minimum eligibility rules, complete candidate generation,
Zone-level availability and agreed Zone ordering, deterministic selection,
atomic persistence integration, and deterministic unit/integration scenarios.

It excludes festival-wide optimization, daily batch or hybrid allocation,
future-demand prediction and reservation, weighted randomness, complex preference
ranking, internal group-dispersion optimization, automatic group restructuring,
retrospective reassignment, and complete operational/UI/administrative workflows.
Global assigned-outcome influence remains deferred. Block Selection Policy v1
is implemented separately; full engine integration remains pending.
Final parameter calibration and quantitative fairness evaluation belong to
Stage 5. Detailed reasons are in the
[trade-offs document](trade-offs-and-open-questions.md).

Accessibility, reserved areas and preferences are possible policy inputs. Only
concretely defined rules needed by an MVP scenario should be implemented; their
mention does not authorize building every operational policy.

## Exit and Stage 5 Handoff

Stage 4 ends with an executable, reproducible baseline whose provisional rules
are explicit, whose outcomes preserve invariants and persist atomically, and
whose histories can be measured in Stage 5. The
[exit criteria](implementation-plan.md#stage-4-exit-criteria) include a small
five-day presimulation against documented scenarios. This detects obvious
failures; it does not prove sufficient fairness or production readiness.

Stage 5 establishes provisional measurable classification criteria before
judging results. It evaluates path balance, recovery, access to Good experiences,
group effects, arrival order, inventory utilization, physical capacity and
parameter sensitivity. It should distinguish algorithmic limitations from
physical or operational ones and record evidence even when the outcome is
unfavorable.

The resulting proposal to the organization will include that evidence,
limitations and [open operational questions](trade-offs-and-open-questions.md#questions-for-festival-organization),
such as request workflows, accessibility, reservations, supported preferences
and assisted registration. Organizational validation is a later step, not a
claim made by the Stage 4 implementation.
