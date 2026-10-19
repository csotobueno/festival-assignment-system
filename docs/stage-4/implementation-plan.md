# Stage 4 Implementation Plan

## Purpose

This document defines the implementation sequence for Stage 4.

The goal is to convert the fairness model and online assignment strategy into working software through small, verifiable increments.

The Stage 4 MVP should validate whether the system can produce reasonably equitable assignments across the five festival days while preserving the flexibility of daily individual or group requests.

The plan follows a Lean principle:

> Implement the smallest useful behavior first, validate it with concrete scenarios, and add complexity only when evidence shows that it is necessary.

Stage 4 should produce a reliable online baseline that can later be evaluated quantitatively in Stage 5.
This is an initial guide, not a completed specification. The numbered steps are
work areas that can be grouped into small issues; they are not a mandatory
one-issue-per-step sequence. Pure score calculations and the Domain selection
pipeline, Application preparation, engine cutover and persisted immutable
Assignment ExperienceQuality are implemented. Full Stage 4 exit validation and
Stage 5 simulation remain separate work.

## Agreed Application Cutover Delivery Sequence

The [Stage 4 Application Cutover Boundary](../architecture/stage-4-application-cutover-boundary.md)
was delivered in two phases: stable dependencies first, then coordinated
activation. The delivered increments are:

1. Current Zone catalog access through IZoneCatalogProvider, now consumed by
   Application; PostgreSQL and the configured InMemory demo catalog are registered.
2. Assignment quality transition and neutral bootstrap decision.
3. Application composition using existing fairness, eligibility and inventory
   policies without duplicating their formulas.
4. Coordinated selection integration, engine materialization, mandatory Assignment
   quality, required text persistence/migration, explicit rehydration, batched
   recorded-history reads and removal of legacy placement.

Empty history remains the neutral bootstrap. There is one production placement
path and no nullable/default quality, fake backfill or current-Zone historical
reconstruction. The numbered work areas below do not require independently
activating coupled contracts.

---

# Implementation Principles

The implementation should follow these principles:

1. Keep each increment small and independently testable.
2. Preserve separation between:
   - assignment history;
   - RotationScore;
   - Experience Quality;
   - eligibility;
   - physical feasibility;
   - inventory;
   - fairness-aware selection;
   - persistence.
3. Prefer pure domain/application logic before infrastructure integration.
4. Convert validated fairness scenarios into automated tests.
5. Preserve deterministic sequential behavior.
6. Reuse Stage 2 and Stage 3 infrastructure where possible.
7. Protect all existing domain and database invariants.
8. Treat score weights and quality rules as hypotheses.
9. Do not predict future demand.
10. Do not implement operational workflows before they are justified by organization feedback.
11. Add complexity only in response to a concrete observed limitation.

---

# Decisions at the Point of Use

Resolve only the decision needed by the next dependent increment. Record the
provisional rule and a concrete expected result before implementing it. If a
business decision is unresolved, pause that part and continue independent work.
Do not silently choose business semantics inside code.

| Decision | Needed by | Minimum evidence to record |
| --- | --- | --- |
| Zone taxonomy and global quality mapping (agreed) | Steps 2, 10 and 12 implementation | [Zone Experience Quality v1](assignment-strategy-v1.md#agreed-zone-taxonomy-and-global-mapping): seven stable ZoneTypes classified globally through a policy; request eligibility changes participation only. |
| Score-to-Target Quality policy (agreed) | Step 9 implementation | [Target Quality v1](assignment-strategy-v1.md#3-determine-target-quality): score >= 1.00 → Good; score < 1.00 → Medium, for individuals and groups. Calibration belongs to Stage 5. |
| Minimum implemented eligibility rules | Step 10 | A concrete allowed/excluded candidate case; undefined organization policies remain deferred. |
| Inventory v1 counting scope (implemented) | Step 13 | Raw available Spot counts by quality in the caller-supplied snapshot; no GroupSize or eligibility filtering inside the calculator. See [Current Inventory State](assignment-strategy-v1.md#9-current-inventory-state). |
| Zone-level availability prerequisite (implemented) | Step 13, before Zone ordering | ZoneAvailabilityState entries are the primary source; derive quality totals from Zone-level entries. Raw counts do not prove feasibility. |
| Zone Evaluation Strategy v1 (ordering and first-feasible-Zone search implemented) | Steps 15–18 | [Exact quality/count/tie ordering](assignment-strategy-v1.md#11-zone-evaluation-strategy-v1); first feasible Zone wins. Global assigned-outcome state is deferred. |
| Global-state influence (deferred) | Future iteration after Stage 5 evidence | A concrete fairness failure showing remaining inventory is insufficient; then define minimum recorded outcome state and population/time scope. |
| Block Selection Policy v1 (implemented) | Before full engine integration | [Affected-Row remaining-run ranking](assignment-strategy-v1.md#block-selection-policy-v1): LargestRemainingContiguousRun descending, RowCode ascending, starting SpotNumber ascending. Zone selection remains upstream. |
| Assignment quality transition and neutral bootstrap (implemented) | Coordinated activation across Steps 19–22 | [Authoritative decision](../architecture/stage-4-application-cutover-boundary.md#assignment-experiencequality-transition-and-bootstrap): required immutable quality, explicit rehydration and batched recorded-history reads are active; empty history is neutral and no fake backfill is allowed. Quality is stored as required enum-name text without a default. |

Parameter sensitivity and final calibration belong to Stage 5. An initial
executable policy is still required in Stage 4. Final path-classification
thresholds need not block implementation; Stage 5 must record provisional
measurement criteria before evaluating its simulation results.

# Implementation Sequence

## Step 1 — Define Experience Quality v1

Introduce the minimum representation required to describe assignment experience quality.

Initial model:

```text
Good
Medium
Bad
```

Initial fairness contribution:

```text
Good   = -1
Medium =  0
Bad    = +1
```

Experience Quality represents the globally business-classified quality of the
Zone or actual assignment. ZoneType supplies stable venue meaning; a separate
policy derives its `Good`, `Medium` or `Bad` classification.

### Goal

Provide a stable representation that can be consumed by assignment history and RotationScore.

### Validation

Unit tests should verify:

- the three quality values;
- their fairness contribution;
- deterministic behavior.

---

## Step 2 — Implement the Agreed Zone Experience Quality v1 Policy

The [Zone taxonomy and global mapping](assignment-strategy-v1.md#agreed-zone-taxonomy-and-global-mapping)
are decided. The taxonomy, mandatory `Zone.Type`, eligibility migration and
`ZoneExperienceQualityPolicy.GetQuality(ZoneType zoneType)` (items 1–4 below)
are implemented. Quality is derived, never stored on Zone. Historical quality
recording (item 5) is now integrated at the coordinated cutover. The sequence is:

1. Introduce `ZoneType` with exactly `FrontStanding`, `MiddleLeft`, `MiddleCenter`,
   `MiddleRight`, `UpperLeft`, `UpperCenter` and `UpperRight`.
2. Give Zone a stable `Type`, migrating from the current `IsFrontStanding`
   Boolean to `Zone.Type == ZoneType.FrontStanding`. Remove the dedicated Boolean
   when the taxonomy is implemented; update persistence and layout data then.
3. Update `ZoneEligibilityPolicy` to use ZoneType while continuing to consume
   `AssignmentRequest.Eligibility`, preserving the request-level rule and order.
4. Implement `ZoneExperienceQualityPolicy: ZoneType → ExperienceQuality` with
   the agreed mapping. Derive quality through this policy rather than storing
   mutable independent ExperienceQuality on Zone.
5. Keep the actual historical ExperienceQuality recorded at assignment time
   stable when later business classification changes; persistence integration
   belongs to Steps 20–22.

`RequestEligibility` and its durable request state are already implemented.
`AllowsFrontStanding = false` excludes Front Standing for all request members;
it never changes the quality of remaining Zones. In particular, `MiddleLeft`
remains `Medium` and `MiddleCenter` remains `Good`.

### Goal

Separate stable venue meaning, global business quality, request participation and
the fairness-derived Target Quality reference. Do not define selection or
degradation as part of the taxonomy and mapping implementation.

### Validation

Unit tests cover all seven mappings and explicit rejection of undefined
ZoneTypes. The API consumes only ZoneType; eligibility and TargetQuality are
not classification inputs. Stable recorded historical quality after a later
policy change remains part of historical recording integration.

### Lean Constraint

`ZoneCode` is not required yet; defer it until a concrete external integration,
import/export, organization configuration, UI/API business identifier or
external-data mapping requirement appears. Per-attendee eligibility, arbitrary
exclusions and additional venue categories remain outside this v1 policy.
Steps 3–8 can continue to use already-classified histories.

---

## Step 3 — Represent Assignment History for Fairness

Provide the minimum ordered information required to calculate an attendee's fairness state.

History contains reliable recorded outcomes from previous assignments only.
With no reliable Stage 4 outcomes, empty FairnessHistory is the intentional
neutral bootstrap, not a requirement to reconstruct pre-cutover quality. See the
[transition decision](../architecture/stage-4-application-cutover-boundary.md#assignment-experiencequality-transition-and-bootstrap).

Example:

```text
Bad → Medium → Good
```

Absences do not create synthetic persisted assignments.

Historical quality should remain stable once recorded.

### Goal

Allow fairness calculations to consume history without depending directly on EF Core or PostgreSQL.

### Validation

Cover:

- no history;
- one assignment;
- multiple assignments;
- intermittent participation;
- stable chronological ordering.

---

## Step 4 — Implement Historical Deficit

Definition:

```text
HistoricalDeficit =
sum(quality contributions)
```

Examples:

```text
Bad → Bad → Medium = +2
```

```text
Good → Medium → Bad = 0
```

### Goal

Capture accumulated favorable or unfavorable experience.

---

## Step 5 — Implement Recent Recovery Need

Initial definition:

```text
last Bad    → +1
last Medium →  0
last Good   → -1
```

No history:

```text
0
```

### Goal

Capture immediate recovery need using the minimum recency model.

### Lean Constraint

Do not introduce trend, streak, or recent windows unless later evidence requires them.

---

## Step 6 — Implement Good-Experience Deficit

Initial behavior:

```text
no history       → 0
never had Good   → +1
already had Good → 0
```

### Goal

Represent the additional need of an attendee who has participated but never received a favorable experience.

---

## Step 7 — Implement RotationScore v1

Initial baseline:

```text
RotationScore =
    HistoricalDeficit
  + 0.50 × RecentRecoveryNeed
  + 0.25 × GoodExperienceDeficit
```

The calculation must be:

- deterministic;
- independent from persistence;
- independent from inventory;
- independent from global assignment state.

### Goal

Produce the first measurable recovery-need signal.

### Validation

Automate the [reference scenarios](rotation-score-v1.md#reference-scenarios) from RotationScore v1.

At minimum:

```text
Juan > Luis > Ana > Pedro
```

```text
B > A > C > D
```

```text
Ana > Gisela
```

---

## Step 8 — Implement Group RotationScore

For groups:

```text
GroupRotationScore =
average(MemberRotationScores)
```

A new attendee contributes:

```text
0
```

### Goal

Support daily group requests using the agreed MVP simplification.

### Validation

Test:

- all-neutral groups;
- mixed positive and negative histories;
- new members;
- strongly different member scores.

Internal group dispersion remains intentionally unoptimized.
Include the confirmed arithmetic example: `(2 + 4 + 1 + 3 + 0) / 5 = 2`.
The mean guides the group's target and selection; it does not replace individual
historical quality records or alter the global Zone Experience Quality policy.

---

## Step 9 — Implement the Agreed Target Quality Policy v1

The [policy is defined](assignment-strategy-v1.md#3-determine-target-quality).
It is implemented and used by Application, with the same rule for
`RotationScore` and `GroupRotationScore`:

```text
score >= 1.00 → Good
score < 1.00  → Medium
```

Exactly `1.00` targets `Good`. `Bad` is never an intentional v1 target.
The threshold is a fixed Stage 4 hypothesis; runtime configuration and further
target bands are outside this increment. Stage 5 owns validation and calibration.

Target Quality is a reference, not an entitlement, a maximum or a guaranteed
assignment. Better-than-target outcomes and degradation under real constraints
belong to later selection tasks.

### Goal

Separate measuring recovery need, deriving a quality objective and deciding
what to assign now. Consume only the individual or group score; eligibility,
feasibility and inventory remain selection concerns; global assigned-outcome
influence is deferred from Zone Evaluation v1.

### Validation

Domain tests cover negative and zero scores (`Medium`),
scores below `1.00` (`Medium`), exactly `1.00` (`Good`) and above it (`Good`),
with the same policy for individuals and groups. Preserve the
[representative histories and boundary case](trade-offs-and-open-questions.md#target-quality-v1-threshold-decision)
as reference cases. Verify that the policy never targets `Bad`; do not implement
final assignment selection as part of this step.

---

## Step 10 — Implement Minimum Eligibility Rules

Implemented v1: `ZoneEligibilityPolicy.Filter(request, availableZones)` reads
`AssignmentRequest.Eligibility` and excludes only Zones whose
`Type == ZoneType.FrontStanding` when `AllowsFrontStanding` is `false`. It
preserves Zone objects and input order for both individuals and groups, without
changing Experience Quality. All six non-Front ZoneTypes remain eligible.

`Zone.Create(id, name, type)` explicitly requires a valid ZoneType. The get-only
`Zone.Type` replaces `Zone.IsFrontStanding`; persistence stores its enum name in
the required `Zones.ZoneType` text column. Display text is never consulted.
The global `ZoneExperienceQualityPolicy.GetQuality` now derives quality from
ZoneType alone, without reinterpreting historical ExperienceQuality or
introducing `ZoneCode`.

Application now connects eligible Zones to prepared availability, inventory and
AssignmentSelectionPolicy. Available-spot provider behavior is unchanged; the
engine consumes the resulting selection without evaluating eligibility.

Implement only further eligibility concepts concretely required by the MVP.

Possible later cases:

- accessibility compatibility;
- organization-reserved capacity.

Eligibility must be applied before fairness-aware selection.

### Goal

Ensure invalid options never reach final selection.

### Lean Constraint

If a rule has not yet been defined sufficiently by the business model, document it instead of inventing behavior.

---

## Step 11 — Generate Feasible Candidate Blocks

The physical finder is implemented as `FeasibleSpotBlockFinder.Find(zone,
availableSpots, groupSize)`, returning all complete `FeasibleSpotBlock` windows
inside the supplied Zone. It preserves overlapping alternatives and keeps Rows
independent, ordered by RowCode then SpotNumber. It reuses the existing physical
invariants; tests verify compatibility with AssignmentGroup validation.
`FeasibleZoneCandidateFinder` now consumes ordered Zones and invokes the finder
per Zone until one returns blocks; it does not generate candidates across all
Zones first. Engine materialization is integrated.
The caller supplies availability. AssignmentSelectionPolicy now composes Zone
ordering, feasibility and Block Selection Policy v1; attendee binding and engine
integration are implemented.

Build on existing Stage 2 and Stage 3 invariants.

For an individual:

```text
candidate = one valid Spot
```

For a group:

```text
candidate = complete contiguous block
```

Relevant constraints include:

- same FestivalDay;
- same Zone;
- same Row;
- sufficient Spots;
- contiguity;
- availability;
- complete group assignment.

### Goal

Produce only complete and valid assignment candidates.

### Validation

Cover:

- valid individual;
- valid group;
- insufficient contiguous capacity;
- fragmented availability;
- multiple valid blocks.

---

## Step 12 — Determine Candidate Experience Quality

Using the implemented ZoneExperienceQualityPolicy, each valid candidate should
receive the globally business-defined classification of its ZoneType:

```text
Good
Medium
Bad
```

RequestEligibility determines whether the Zone participates; it does not alter
the policy result. Quality is derived from ZoneType, not the request's exclusions
or TargetQuality. AssignmentSelectionPolicy now resolves this actual quality in
AssignmentSelection; historical recording is implemented.

### Goal

Allow candidate selection to reason about both fairness need and available experience quality.

---

## Step 13 — Introduce Current Inventory State

Implemented pure domain API:

```text
RemainingInventoryCalculator.Calculate(zones, availableSpots)
→ RemainingInventoryState { ZoneAvailabilities: ZoneAvailabilityState[] }
→ derived GoodRemaining / MediumRemaining / BadRemaining
```

`ZoneAvailabilityState.Create(zoneId, experienceQuality, availableSpotCount)`
validates the required ZoneId, defined quality and non-negative raw count.
`RemainingInventoryState.Create(zoneAvailabilities)` copies distinct Zone entries
into an immutable snapshot; `ZoneAvailabilities` is exposed as an IReadOnlyList.
GoodRemaining/MediumRemaining/BadRemaining are derived by summing entries of
each quality, with no independently stored totals.
The calculator uses `Spot.ZoneId → Zone.Type → ZoneExperienceQualityPolicy` and
counts supplied Spots separately for each Zone. Zero-availability Zones are
omitted; empty availability returns no entries and derived zero totals. Tests
cover every ZoneType, mixed and independent same-quality Zones, supplied
availability, logical input-order independence, snapshot immutability,
fragmented raw capacity and malformed/duplicate inputs. No entry sequence order
or business priority is defined. Zone-level availability is implemented;
ordering influence is implemented in ZoneEvaluationPolicy and pure selection is
composed by AssignmentSelectionPolicy. Current availability uses current classification;
historical assignments retain recorded quality.

### Goal

Allow the strategy to recognize:

- scarce favorable capacity;
- abundant favorable capacity;
- quality distributions that make a better-than-target assignment reasonable.

### Important Boundary

Raw remaining Spot count ≠ contiguous group-feasible capacity. The calculator
does not accept GroupSize, call FeasibleSpotBlockFinder, calculate fragmentation,
filter request eligibility, modify RotationScore or select candidates. A caller
can supply an eligible Zone/Spot snapshot later; the inventory component only
describes its supplied snapshot and does not determine daily/festival scope.

---

## Step 14 — Introduce Current Global Assignment State

Deferred; not a prerequisite or input to Zone Evaluation Strategy v1. A future
iteration may add recorded outcome state if Stage 5 shows remaining inventory
alone is insufficient. A likely model is `ZoneAssignmentState` with ZoneId,
recorded ExperienceQuality and AssignedCount, deriving global quality totals
from recorded outcomes. This is not implemented or a fixed production API.
Before implementation, document the concrete failure, expected improvement,
minimum information and population/time scope. Preserve historical quality.

---

## Step 15 — Implement Zone Evaluation Strategy v1

Implemented pure, stateless domain API using Step 13's inventory snapshot:

```text
TargetQuality + RemainingInventoryState + eligible Zones
        ↓
ZoneEvaluationPolicy.Order(targetQuality, inventory, eligibleZones) → IReadOnlyList<Zone>
```

The policy implements the
[agreed strategy](assignment-strategy-v1.md#11-zone-evaluation-strategy-v1), without
CurrentGlobalAssignmentState. It matches eligible Zones to inventory by ZoneId,
omits absent entries and rejects current quality mismatches using the existing
quality policy. Results are read-only; empty participation returns empty.
Tests cover quality orders and equality, capacity priority, exact ZoneId ties,
participation, input permutations, malformed inputs and snapshot immutability.
RequestEligibility → ZoneEligibilityPolicy precedes ordering; eligibility never
changes quality. The separate pure composition is now implemented as
`FeasibleZoneCandidateFinder.Find(orderedZones, availableSpots, groupSize)`.
It validates collection structure before traversal, scopes Spots by ZoneId and
reuses FeasibleSpotBlockFinder. The first Zone with blocks returns immediately
as an immutable FeasibleZoneCandidate containing that Zone and every block in
existing finder order. No feasible Zone returns null. Engine materialization is integrated.

### Implemented Block Selection Policy v1

The separate [Block Selection Policy v1](assignment-strategy-v1.md#block-selection-policy-v1)
is implemented as `BlockSelectionPolicy.Select(candidate, availableSpots)`.
It consumes the selected FeasibleZoneCandidate and current available Spots. For
each feasible block, simulate its removal from its
affected Row and rank by LargestRemainingContiguousRun descending, then RowCode
ascending and starting SpotNumber ascending. Preserve current gaps; an empty
remaining Row scores zero. Compare these raw scores across Rows without
normalization. Do not reconsider Zone priority or reinterpret physical feasibility.

Domain unit tests cover the documented eight-Spot and different-Row examples,
existing gaps, empty remaining Rows, both exact technical tie-breaks, and logical
snapshot determinism. Candidate creation rejects mixed block sizes. Selection
validates the entire snapshot structure and every candidate Spot's current
identity/position before ranking. Add no general fragmentation score, future-demand
prediction, reservation, first-block default or random selection.

### Implemented Pure Assignment Selection

`AssignmentSelectionPolicy.Select(targetQuality, inventory, eligibleZones,
availableSpots, groupSize)` composes `ZoneEvaluationPolicy.Order` →
`FeasibleZoneCandidateFinder.Find` → `BlockSelectionPolicy.Select`, then resolves
actual quality with `ZoneExperienceQualityPolicy`. It consumes prepared inputs,
materializes availability once and returns an immutable AssignmentSelection with
Zone, Block and actual ExperienceQuality, or `null` when no placement exists.
The result factory enforces matching ZoneId and current Zone quality. Domain
tests cover target/actual differences, ordering, feasibility, block selection,
empty results, determinism and result invariants. No new fairness rule is added;
the official count remains 19. See the [pure composition boundary](assignment-strategy-v1.md#pure-assignment-selection-composition).
Engine, Application and recorded-quality persistence integration are implemented.

### Validation

Domain unit tests cover ordering and first-feasible-Zone composition, including
fragmented or insufficient availability, advancing to later Zones, authoritative
Zone order, all returned blocks, exact group sizes, Zone isolation, empty results,
malformed snapshots and read-only results. The ordering policy remains unchanged;
block selection is implemented separately; engine materialization is integrated.

---

## Step 16 — Implement Better-Than-Target Behavior

For TargetQuality.Medium:

```text
GoodRemaining > MediumRemaining → Good → Medium → Bad
otherwise (including equality) → Medium → Good → Bad
```

Use raw totals derived from the supplied Zone-level availability entries. Target quality is the default
preference; more abundant Good inventory can be evaluated first. Introduce no
percentage, ratio or additional threshold. Stage 5 measures this baseline.

---

## Step 17 — Implement Degradation When Target Is Unavailable

For TargetQuality.Good, evaluate Good → Medium → Bad. Exhaust eligible Zones at
each quality before degrading. Zero raw inventory qualities may be omitted;
positive counts never guarantee feasibility.

Bad is never a target, but is an acceptable actual last-resort result when no
Good or Medium Zone has a feasible block. Prefer a complete lower-quality
assignment over rejection. Reject for lack of a block only after every eligible
quality is exhausted. Never automatically split or restructure the group; a
changed configuration requires a new request. Stage 5 may evaluate stopping
before Bad or offering group restructuring.

---

## Step 18 — Add Deterministic Zone Tie-Breaking

Within each quality, order by AvailableSpotCount descending, then a stable
technical tie-break such as ZoneId ascending for exact count ties. ZoneId is
only technical determinism, not a business preference or primary priority.
There is no permanent ZoneType ranking, randomness, weighted randomness,
persistent round-robin cursor or future-demand prediction. RotationScore retains
its existing no-tolerance rule. Block Selection Policy v1 has separate, defined
tie-breaks and is implemented; engine materialization is integrated.

---

## Step 19 — Integrate with ProcessAssignmentRequestUseCase

Implemented: after the complete-resolution guard, ProcessAssignmentRequestUseCase
loads Zones, available Spots and batched fairness histories through Application
ports. GroupRotationScore (including individual score calculation),
TargetQualityPolicy, ZoneEligibilityPolicy and RemainingInventoryCalculator
prepare the existing AssignmentSelectionPolicy inputs. Null selection rejects
with `NO_CONTIGUOUS_SPOTS_AVAILABLE`; otherwise AssignmentEngine materializes the
exact selection, the request completes and one UnitOfWork save commits it.
Legacy placement is removed, with no fallback or temporary caller contract.

### Goal

Evolve the existing assignment process without weakening current application semantics.

---

## Step 20 — Integrate Persisted History

Use reliable recorded outcomes from previous assignments as input to RotationScore.

Infrastructure should provide the required history through an application/domain-facing abstraction.

Implemented as `IFairnessHistoryProvider.GetHistoriesAsync(attendeeIds,
cancellationToken)`, returning one FairnessHistory per requested attendee.
PostgreSqlFairnessHistoryProvider uses one no-tracking batch query over recorded
Assignment.ExperienceQuality, ordered by AssignedAt then AssignmentId for exact
timestamp ties, without Zone lookup. Missing outcomes return empty histories;
existing Domain logic produces RotationScore zero. Scores remain derived.
InMemory composition reads existing in-memory recorded outcomes for test/demo
support only. Provider tests protect chronological order, empty members,
cancellation, recorded quality and a single read for the batch.

### Goal

Ensure earlier festival-day experiences affect later assignment decisions.

### Validation

Example:

```text
Day 1 assignment
        ↓
persisted
        ↓
Day 2 request
        ↓
RotationScore
        ↓
different decision context
```

---

## Step 21 — Persist Historical Experience Quality

Ensure the historical fairness model remains reproducible.

Record the actual ExperienceQuality derived under the business policy at
assignment time for later FairnessHistory and RotationScore evaluation. Current
Zone quality is derived from ZoneType; recorded historical quality is a stable
fact, not recomputed from the current policy.

For example, an assignment recorded when `UpperCenter` was `Medium` remains
`Medium` in history even if a later policy maps UpperCenter to `Good`.
Historical recording and explicit EF constructor rehydration are implemented.

### Goal

Prevent automatic reinterpretation of past assignments after business-policy
or eligibility changes.

ExperienceQuality is required enum-name text on Assignments, with no schema default.

The [transition/bootstrap and pre-cutover data policy](../architecture/stage-4-application-cutover-boundary.md#assignment-experiencequality-transition-and-bootstrap)
is agreed: no production history needs backfill, and disposable local/test
databases may be recreated. Required immutable quality, its persistence,
explicit rehydration and recorded-history reads are active with selection and
the engine across Steps 19–22. selection.ExperienceQuality is recorded as part of
the atomic successful outcome. No nullable/default quality, fabricated legacy values,
current-Zone historical reconstruction or temporary engine Zone lookup is allowed.
Existing records must not be assumed to contain quality.

---

## Step 22 — Preserve Atomic Persistence

Continue using the Stage 3 atomic persistence boundary.

Conceptually:

```text
decision
   ↓
request state + assignments
   ↓
single commit
```

Do not weaken:

- rollback guarantees;
- uniqueness constraints;
- conflict translation;
- transaction boundaries.

---

## Step 23 — Verify Concurrency Regression

Run existing Stage 3 concurrency scenarios against the evolved assignment flow.

Important:

```text
deterministic strategy
≠
deterministic concurrent winner
```

The database remains authoritative for conflicting commits.

### Goal

Ensure fairness functionality does not regress persistence correctness.

### Lean Constraint

Do not introduce locks, queues, or sophisticated retry orchestration unless evidence shows the MVP requires them.

---

## Step 24 — Handle Infeasible Group Requests

If no complete valid block exists:

```text
requested group
→ no feasible candidate
→ Rejected, no Assignments
```

The engine should not automatically split or restructure the group.

### Goal

Keep user choice outside the assignment algorithm.

Application retains `NO_CONTIGUOUS_SPOTS_AVAILABLE` when no complete feasible
block exists in eligible Zones, preserving the behavior described by the
[historical domain decision](../domain-blueprint-v1.md#11-contiguous-availability-decision).
The original request is terminal; a changed group requires a new AssignmentRequest.
The future UI for doing so is outside this increment. If an implemented eligibility
policy needs a distinct rejection reason, define it in that policy's increment.

---

## Step 25 — Add End-to-End Reference Scenarios

Create scenarios exercising:

```text
history
→ RotationScore
→ Target Quality
→ eligibility
→ Zone-level availability
→ ordered eligible Zones
→ per-Zone feasibility
→ Block Selection Policy v1 (implemented)
→ persistence
```

Recommended initial cases:

- new attendee;
- attendee with recovery need;
- attendee with favorable history;
- attendee who never received Good;
- individual request;
- group request;
- preference exclusion;
- target unavailable;
- abundant Good capacity;
- scarce Good capacity;
- infeasible group;
- concurrent persistence conflict.

---

## Step 26 — Run a Small Five-Day Pre-Simulation

Before Stage 4 closure, execute a small controlled five-day simulation.

This is not the full Stage 5 evaluation.

Its goal is to detect obvious implementation or model failures.

Observe at least:

- persistent Bad paths;
- attendees never receiving Good;
- unused Good capacity;
- excessive early consumption of Good capacity;
- pathological group behavior;
- unexpected dependence on arrival order;
- unexpected score behavior.

### Goal

Validate that the baseline is executable and credible enough for formal Stage 5
measurement. Before running the presimulation, record its input, expected
reference behavior and invariant checks. Compare results against those explicit
expectations and record observed limitations. A negative path alone is not proof
of an implementation defect: capacity and arrival order may explain it.
This check does not certify a fairness percentage or final business acceptance.

---

## Step 27 — Update Documentation with Implemented Reality

At the end of Stage 4, update documentation with:

- actual formula parameters;
- actual target-quality rules;
- actual inventory metrics;
- deferred global assigned-outcome influence;
- implemented tie-breaking;
- accepted limitations;
- deviations from the original hypothesis.

Documentation should describe the implemented baseline, not only the original design.

---

# Testing Strategy

## Unit Tests

Primary targets:

- Experience Quality;
- HistoricalDeficit;
- RecentRecoveryNeed;
- GoodExperienceDeficit;
- RotationScore;
- GroupRotationScore;
- Target Quality;
- eligibility rules;
- candidate generation;
- inventory evaluation;
- Zone-level availability and Zone ordering;
- deterministic candidate selection.

---

## Application Tests

Validate:

- orchestration;
- correct responsibility ordering;
- history usage;
- online fairness-aware decision behavior;
- group rejection when infeasible;
- deterministic results.

---

## Integration Tests

Using PostgreSQL/Testcontainers where persistence matters:

- persisted history affects future requests;
- historical quality remains available;
- assignments remain atomic;
- database constraints remain authoritative;
- rollback remains correct;
- concurrency conflict translation remains stable.

---

## Regression Scenarios

Validated fairness examples should become durable regression tests.

These protect business intent while allowing later algorithm evolution.

---

# Suggested Issue Breakdown

Use the numbered [implementation sequence](#implementation-sequence) as the
single source for issue scope. Each issue should name its step or adjacent steps,
required decisions, concrete scenario and validation. Combine related steps when
that keeps the change small; do not maintain a second independently ordered list.

A practical starting increment is Experience Quality values plus classified
history and the score signals. Venue classification and selection can follow
when their required decisions are ready.

---

# Lean Decision Gate

After every meaningful increment, ask:

```text
Does the current implementation already satisfy
the validated scenario?
```

If yes:

```text
stop adding complexity
```

If no:

```text
identify concrete failure
        ↓
make smallest justified change
        ↓
add regression test
```

Avoid:

```text
possible future problem
        ↓
speculative architecture
        ↓
extra complexity
```

---

# Stage 4 Exit Criteria

Stage 4 can be considered complete when:

- Fairness Definition v1 is documented;
- Experience Quality v1 is implemented;
- ZoneType and the global ZoneExperienceQualityPolicy are implemented;
- eligibility changes Zone participation without redefining ExperienceQuality;
- RotationScore v1 is implemented;
- reference RotationScore scenarios pass;
- GroupRotationScore works;
- Target Quality v1 is implemented;
- eligibility precedes assignment selection;
- physical feasibility precedes assignment selection;
- same Zone and same Row group constraints are respected;
- complete candidate blocks are generated;
- Current Inventory State influences the decision;
- Zone-level availability drives the agreed quality/count/tie Zone order;
- CurrentGlobalAssignmentState remains deferred from v1;
- the first ordered Zone with feasible blocks is selected;
- the defined Block Selection Policy v1 is implemented before full engine completion;
- better-than-target assignments are supported;
- unnecessary artificial degradation is avoided;
- unknown future demand is not used for speculative reservation;
- persisted history affects future decisions;
- persistence remains atomic;
- concurrency regression tests remain green;
- the small five-day pre-simulation satisfies its explicit reference expectations and invariant checks;
- provisional selection rules, assumptions, observed limitations and any changed decisions are documented;
- the system is ready for Stage 5 measurement.

---

# Stage 5 Handoff

Completion means a reproducible provisional implementation ready for measurement,
not proof that its fairness is sufficient. Stage 5 will supply evidence and
limitations for a proposal to the organization, where operational and business
considerations will be further validated.

Stage 4 answers:

> Can we build a simple, reliable, deterministic online assignment engine that uses attendee history and current system state to preserve fairness across five festival days?

Stage 5 answers:

> How equitable are the resulting assignments, and where does measurable evidence justify changing the baseline?

Stage 5 should measure rather than assume:

- Balanced-path percentage;
- Acceptable-path percentage;
- Not-Balanced percentage;
- extreme negative paths;
- recovery behavior;
- effect of arrival order;
- effect of group configuration;
- effect of physical capacity;
- inventory utilization;
- RotationScore sensitivity;
- Target Quality behavior;
- need for batch, hybrid, randomness, or more advanced optimization.
