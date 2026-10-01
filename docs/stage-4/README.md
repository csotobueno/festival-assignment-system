# Stage 4 — Online Assignment Engine & Fairness

## Status and Purpose

**Initial implementation guide — Stage 4 is next, not completed.**

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
| 3 | [Assignment Strategy v1](assignment-strategy-v1.md) | Decision flow, candidate quality, inventory, global state and selection examples. |
| 4 | [Implementation Plan](implementation-plan.md) | Increments, decisions needed by each increment, validation and exit criteria. |
| 5 | [Trade-offs and Open Questions](trade-offs-and-open-questions.md) | Accepted limitations, their reasons, and later evaluation or organization questions. |

Keep detailed definitions in their responsible document and link to them from
other files. The [glossary](../glossary.md) defines shared terms; the
[critical invariants](../critical-invariants.md) remain mandatory. Earlier stage
documents preserve their historical decisions. The
[Stage 3 closure](../stages/stage-3-closure.md) describes the implemented
persistence foundation on which this proposal builds.

## Assignment Flow

1. Receive an AssignmentRequest with one to ten Attendees and resolve its members.
2. Load each Attendee's available history of real previous assignments.
3. Calculate individual RotationScores. For a group, use their arithmetic mean
   as GroupRotationScore; individual histories remain separate.
4. Derive a [Target Quality v1](assignment-strategy-v1.md#3-determine-target-quality) reference: `Good` or `Medium`; `Bad` is only a possible actual outcome.
5. Evaluate options that are eligible, physically feasible and currently available.
6. Select a complete candidate considering individual or group fairness and the
   global fairness state accumulated so far.
7. If the target quality is unavailable, evaluate other feasible options.
8. Before degrading the assignment, inspect current inventory and global state:
   a better-than-target option may be assigned when its use does not reasonably
   harm current global fairness.
9. Persist the request outcome and all Assignments atomically. Successful outcomes
   become history for subsequent requests.

Target Quality is a reference, not an entitlement or ceiling. The precise rules
for deriving it and combining inventory with global state will be made explicit
in the relevant implementation increments.

If no complete eligible and feasible block exists, reject the request without
Assignments. Lack of contiguous capacity retains
`CONTIGUOUS_SPOTS_NOT_AVAILABLE`. A changed group requires a new request; the
original request does not remain pending.

## Baseline and Boundaries

- **Online processing:** daily group composition can change. Decisions use known
  current state, without forecasting or reserving capacity for unknown requests.
  Arrival order can affect results.
- **Recovery need:** RotationScore uses historical experience. A new Attendee has
  score zero; absences introduce no synthetic assignments. Inventory and global
  state affect selection, not the score itself.
- **Group average:** scores `2, 4, 1, 3, 0` produce GroupRotationScore `2`. This
  guides quality and Zone selection while preserving the complete group.
- **Experience Quality:** `Good`, `Medium` and `Bad` are relative to eligible
  opportunities. Historical classification remains stable. The initial location
  mapping and handling of mixed group eligibility still require explicit rules.
- **Invariants:** same Zone, same Row, consecutive SpotNumbers, complete groups,
  daily uniqueness and final Assignments remain protected. Fairness cannot
  override eligibility or physical feasibility.
- **Determinism:** identical relevant input state produces the same decision.
  This does not determine which concurrent transaction wins; Stage 3 persistence
  and conflict guarantees remain in force.
- **Starting data:** the database is empty at Stage 4 preparation. There are no
  legacy Assignments to classify or migrate. New assignment experiences will be
  recorded when persisted-history integration is implemented. Deterministic test
  fixtures may provide synthetic scenarios, without inventing absence records.

## Implementation Readiness

The initial score formula, neutral state and group average are sufficiently
specified to begin pure calculations using already-classified histories.
Location classification need not block that work.

The [implementation decision table](implementation-plan.md#decisions-at-the-point-of-use)
identifies when to settle quality mapping, Target Quality, inventory/global-state
rules and deterministic tie-breaking. An unresolved business rule pauses its
dependent increment; independent work can continue. No operational rule should
be invented merely to complete the design.

## Scope

Stage 4 includes Experience Quality, history, RotationScore and GroupRotationScore,
Target Quality, minimum eligibility rules, complete candidate generation,
inventory and incremental global-state evaluation, deterministic selection,
atomic persistence integration, and deterministic unit/integration scenarios.

It excludes festival-wide optimization, daily batch or hybrid allocation,
future-demand prediction and reservation, weighted randomness, complex preference
ranking, internal group-dispersion optimization, automatic group restructuring,
retrospective reassignment, and complete operational/UI/administrative workflows.
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
