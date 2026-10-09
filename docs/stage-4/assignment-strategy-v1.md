# Assignment Strategy v1

## Purpose

This document defines the first online fairness-aware assignment strategy for Stage 4.

Its purpose is to describe **how the system transforms a daily assignment request into a valid assignment decision** using:

- attendee or group assignment history;
- RotationScore;
- target experience quality;
- organization policies;
- attendee eligibility;
- physical feasibility;
- current inventory;
- Zone-level availability;
- deterministic selection.

This guide records the agreed, not yet implemented Zone Evaluation Strategy v1.
Block selection remains deferred. Other selection examples express intent;
where they say “may” or “reasonable”, the corresponding implementation increment
must establish a concrete provisional rule and expected result. The
[decision table](implementation-plan.md#decisions-at-the-point-of-use) identifies
those points without requiring a complete policy before implementation starts.

The strategy intentionally operates with the information known at the moment of the request.

It does not attempt to globally optimize all festival assignments in advance.

---

## MVP Objective

The Stage 4 strategy should help validate whether the system can:

> Produce reasonably equitable assignments across the five festival days while preserving the flexibility of daily individual or group requests.

The strategy should protect:

- individual fairness;
- group fairness when applicable;
- incremental global fairness;

while remaining simple enough to implement, explain, test, and simulate.

---

# Core Principle

Each request is processed online using known current state. The
[overview](README.md#assignment-flow) summarizes the flow; the
[candidate decision outline](#26-candidate-decision-outline) below is its detailed
sequence. Unknown future requests do not participate in the decision.

---

# Strategy Responsibilities

Assignment Strategy v1 is responsible for:

- receiving the current assignment request;
- resolving the attendee or group context;
- obtaining relevant assignment history;
- consuming RotationScore;
- deriving a reasonable target quality;
- considering only eligible and feasible candidates;
- evaluating current inventory;
- ordering eligible Zones using Target Quality and Zone-level availability;
- selecting a valid assignment deterministically;
- preserving existing assignment invariants;
- producing a result that becomes part of the state used by later requests.

This document describes the complete workflow, not the responsibilities of one
Domain service. Application resolves attendees, obtains history and decision
inputs through ports, coordinates the request outcome and commits through the
Unit of Work. Domain evaluates rules and selects valid assignments from supplied
inputs. Infrastructure implements data access; the API remains the composition
root. The Domain engine must not query EF Core or persist outcomes directly.

The strategy should not attempt to solve future operational workflows that have not yet been validated with the festival organization.

---

# Decision Inputs

The strategy may require information such as:

- FestivalDay;
- request attendees;
- attendee assignment history;
- RotationScore;
- group size;
- available Spots;
- Zone and Row information;
- experience-quality classification;
- current assignments;
- organization-reserved capacity;
- attendee eligibility constraints;
- allowed attendee preferences;
- accessibility requirements when applicable.

The exact ports, interfaces, and classes used to obtain this information are implementation details and are intentionally not fixed by this document.

---

# 1. Resolve Request Context

A request may contain:

```text
1 attendee
```

or:

```text
multiple attendees
```

For an individual request, fairness is based on the attendee's own history.

For a group request:

- every member keeps their own historical path;
- individual RotationScores are calculated;
- the group fairness state is approximated using the average score;
- the group is treated as one assignment unit for the current day.

Group membership may change between festival days.

The MVP does not assume a permanent group identity.

---

# 2. Load Historical Fairness State

The strategy should obtain the real assignment history of each attendee.

History should contain previous actual experiences such as:

```text
Good → Medium → Bad
```

Absences do not create artificial history.

The strategy then consumes:

```text
RotationScore
```

for the attendee or:

```text
GroupRotationScore
```

for a group request.

RotationScore represents recovery need.

It does not directly select a Zone or Spot.

---

# 3. Determine Target Quality

Target Quality v1 expresses a reasonable quality objective derived only from
current recovery need. The same policy consumes either an individual
`RotationScore` or the arithmetic-mean `GroupRotationScore`:

```text
score >= 1.00 → Target Quality = Good
score < 1.00  → Target Quality = Medium
```

The boundary is inclusive: exactly `1.00` produces `Good`. There is no separate
group threshold; Target Quality consumes the resulting group score without
reassessing individual histories or internal dispersion.

The agreed threshold is a Stage 4 baseline hypothesis selected from representative
histories, not by dividing the theoretical score range. The
[threshold decision and alternatives](trade-offs-and-open-questions.md#target-quality-v1-threshold-decision)
record the rationale; Stage 5 must validate and calibrate it.

The domain implementation exposes `TargetQualityPolicy.Calculate(decimal rotationScore)`
and returns the separate `TargetQuality` enum (`Good` or `Medium`). The policy owns
the fixed `1.00m` threshold in its private `GoodThreshold` constant. Both
`RotationScore.Calculate` and `GroupRotationScore.Calculate` return the decimal
input accepted by this same API.

The conceptual flow keeps measurement, objective and selection separate:

```text
RotationScore / GroupRotationScore → current recovery need
        ↓
Target Quality → reasonable quality objective
        ↓
RequestEligibility → ZoneEligibilityPolicy → eligible Zones
        +
TargetQuality + Zone-level availability
        ↓
Zone Evaluation Strategy → ordered Zones
        ↓
FeasibleSpotBlockFinder → selected Zone with 1..N feasible blocks
        ↓
Block selection (deferred) → actual ExperienceQuality
```

Target Quality is a reference for the later assignment strategy. It is neither
an entitlement, a guaranteed outcome, a maximum allowed quality, nor a direct
Zone or Spot selection. Actual `ExperienceQuality` describes the experience of
a valid assignment; the target expresses an objective before that selection.

Only `Good` and `Medium` are intentional targets in v1. Low or negative scores
mean lower recovery need, never an obligation to assign `Bad` after favorable
history. The system must not manufacture unfavorable experiences for compensation.
`Bad` remains a valid actual Experience Quality when real constraints make it
unavoidable.

A `Medium` target may result in `Good`, including when current inventory makes
an eligible, feasible `Good` candidate a more reasonable choice than consuming
`Medium`. A `Good` target may degrade to `Medium` or `Bad` when eligibility,
physical feasibility or available inventory prevents the preferred outcome.

Target derivation does not include inventory or global state. The agreed
[Zone Evaluation Strategy](#11-zone-evaluation-strategy-v1) defines inventory-aware
better-than-target and degradation behavior. CurrentGlobalAssignmentState is
deferred and is not an input to v1.
Decisions use only information available at that moment; Target Quality does
not reserve capacity for unknown future requests.

---

# 4. Organization Policies

Organization-defined restrictions are applied before assignment selection.

Examples may include:

- reserved volunteer capacity;
- accessibility-specific capacity;
- operationally unavailable areas;
- temporary location exclusions.

A Spot excluded by organization policy is not a valid assignment candidate.

Fairness must not override organization policy.

Only policies concretely required by the MVP should be implemented during Stage 4.

---

# 5. Attendee Eligibility

Eligibility determines which location options are valid for the request.

### Implemented Request-Level Zone Eligibility v1

`new RequestEligibility(bool allowsFrontStanding)` is an immutable domain value
with a get-only `AllowsFrontStanding` property. `true` allows Front Standing to
participate; `false` excludes it for the complete AssignmentRequest. Individual
and group requests use one shared value, regardless of attendee count. Both
boolean values are valid; no default decision is imposed.

The current rule only covers Front Standing. It does not identify concrete Zones
or Spots, classify quality, check availability, or establish physical feasibility.
Per-attendee eligibility, intersections and arbitrary multi-zone exclusions are
deferred until justified by concrete requirements.

`AssignmentRequest.Eligibility` now owns one mandatory, get-only
`RequestEligibility`, shared by every request member and stable through outcome
transitions. Creation and rehydration explicitly require it. Application receives
it through `ProcessAssignmentRequestCommand.Eligibility` and propagates it into
the request without deriving it from fairness or attendee count.

Persistence stores `AllowsFrontStanding` as a required Boolean on
`AssignmentRequests`; rehydration reconstructs the original value for both
`true` and `false`. The migration backfills pre-eligibility rows with `true` to
preserve their opportunity space, then removes the temporary database default.
Unrelated historical fixtures also explicitly use `true` as a compatibility
baseline, not a business default.

Zone-level filtering is now implemented by the pure deterministic API
`ZoneEligibilityPolicy.Filter(AssignmentRequest request, IEnumerable<Zone> availableZones)`.
It reads only `request.Eligibility.AllowsFrontStanding`: `true` retains all supplied
Zones; `false` excludes Zones whose get-only `Type` is `ZoneType.FrontStanding`.
The result is a read-only list preserving input order and the original Zone
objects. Other ZoneTypes remain unchanged, including when Front Standing is
absent. Individual and group requests use the same rule.

`Zone.Create(id, name, type)` explicitly requires one of the seven agreed
ZoneTypes and rejects undefined enum values. `Zone.Type` replaces the temporary
`Zone.IsFrontStanding` Boolean. Display names never determine type or eligibility.
Persistence stores the required type using EF Core's string enum conversion in
`Zones.ZoneType`; constructor binding rehydrates the exact enum value.

The `IntroduceZoneType` migration changes schema only: it removes the Boolean
and introduces required text without a database default. No real Zone data needs
migration before the first persistent deployment; development databases containing
experimental old Zone rows may need recreation. After that deployment, future
schema changes must use real data-migration strategies.

The existing deterministic seed/fixture identities explicitly use `MiddleLeft`
for `20000000-0000-0000-0000-000000000001` and `UpperLeft` for
`20000000-0000-0000-0000-000000000002`. These types describe test/catalog state
only; they are neither migration rules nor venue business rules. Existing catalog
IDs, names, Zone count and Spots are preserved in seed setup.

Filtering changes participation only; Experience Quality and fairness scores
remain unchanged. The policy is not yet wired into the Spot-based engine or
providers: Spot filtering, candidate feasibility and selection remain later
tasks. No HTTP transport contract is changed.

### Later Eligibility Concerns

Examples may include:
- accessibility compatibility;
- allowed location categories.

An ineligible location is not a fairness alternative.

Conceptually:

```text
Available Spots
      ↓
Eligibility
      ↓
Eligible Spots
```

For a group, the complete candidate must satisfy the eligibility requirements of the request.

---

# 6. Eligibility-Aware Experience Quality

### Agreed Zone Taxonomy and Global Mapping

Zone Experience Quality v1 is an agreed global business classification. The
intended `ZoneType` taxonomy identifies the stable physical/business area of a
Zone; its classification is the same for every request.

| ZoneType | ExperienceQuality |
| --- | --- |
| `FrontStanding` | `Good` |
| `MiddleLeft` | `Medium` |
| `MiddleCenter` | `Good` |
| `MiddleRight` | `Medium` |
| `UpperLeft` | `Bad` |
| `UpperCenter` | `Medium` |
| `UpperRight` | `Bad` |

This is the Stage 4 v1 baseline. The taxonomy and mapping are decided;
`ZoneType` and `ZoneExperienceQualityPolicy.GetQuality(ZoneType zoneType)` are
implemented. The pure static policy returns the agreed `ExperienceQuality` and
rejects undefined ZoneTypes with `ArgumentOutOfRangeException`; quality remains
derived rather than stored on Zone.

### Quality Ownership

The intended model is:

```text
Zone.Type → ZoneType
               ↓
ZoneExperienceQualityPolicy
               ↓
ExperienceQuality → Good / Medium / Bad
```

`ZoneType` expresses what area the Zone represents. The business policy derives
its quality through `ZoneType → ExperienceQuality`; quality is not mutable,
independent state stored directly on Zone. This keeps venue meaning separate
from business classification and avoids contradictory type/quality combinations.

`ZoneCode` is not required yet: `ZoneType` provides enough stable domain semantics
for current Stage 4 rules. Defer a code until external integration, import/export,
organization-provided configuration, UI/API stable business identifiers or
mapping external data to Zones creates a concrete requirement.

### Eligibility and Target Quality Are Separate

`RequestEligibility` determines which Zones may participate;
`ZoneExperienceQualityPolicy` determines their global quality. The request-owned
`AllowsFrontStanding` rule applies equally to individual and group requests:

```text
AllowsFrontStanding = true  → FrontStanding may participate
AllowsFrontStanding = false → FrontStanding does not participate

FrontStanding excluded → MiddleLeft remains Medium
                        MiddleCenter remains Good
```

Exclusions never promote or demote the remaining Zones. Availability and physical
feasibility also do not change their business classification.

`TargetQuality` is the fairness-derived desired quality reference, whereas
`ExperienceQuality` describes a real Zone, candidate or assignment. For example,
`RotationScore >= 1.00` produces `TargetQuality = Good`. The later strategy will
seek an eligible, feasible Zone with suitable actual quality. If no suitable
`Good` option exists, later degradation may evaluate `Medium` and eventually
`Bad`; this policy decision defines no selection or degradation algorithm.
`RotationScore` and `GroupRotationScore` remain independent from Zone eligibility.

### Historical Quality Stability

The actual `ExperienceQuality` recorded for a completed assignment is the fact
consumed by `FairnessHistory` and `RotationScore`. Past assignments must not be
reclassified automatically when the business mapping changes. For example:

```text
assignment made when UpperCenter = Medium
→ recorded historical ExperienceQuality remains Medium

even if a later business policy classifies UpperCenter as Good
```

Deriving current Zone quality through a policy does not mean recalculating past
quality from today's policy. Recording historical quality remains a separate
pending implementation step.

### Implemented Zone Model Evolution

`Zone.Type` now owns the mandatory `ZoneType`; `Zone.IsFrontStanding` has been
removed from the domain model. `ZoneEligibilityPolicy` identifies Front Standing
through `Zone.Type == ZoneType.FrontStanding`. The global
`ZoneExperienceQualityPolicy.GetQuality` implements the agreed quality mapping.
Candidate integration and historical quality recording remain separate pending
tasks. `ZoneCode` remains deferred.

---

# 7. Physical Feasibility

After eligibility filtering, the strategy determines which complete assignments are physically possible.

Existing domain invariants remain authoritative.

For an individual request:

```text
candidate = valid available Spot
```

For a group:

```text
candidate = complete valid contiguous block
```

Relevant constraints include:

- same FestivalDay;
- same Zone;
- same Row;
- sufficient number of Spots;
- contiguous Spots for the MVP;
- Spot availability;
- complete group assignment;
- existing Attendee uniqueness;
- existing Spot uniqueness.

Fairness must never bypass physical feasibility.

The pure domain API `FeasibleSpotBlockFinder.Find(Zone zone,
IEnumerable<Spot> availableSpots, GroupSize groupSize)` now finds every complete
contiguous window inside the supplied Zone, preserving overlapping alternatives.
It groups by existing RowCode and orders rows ordinally by their normalized value,
then Spots numerically by SpotNumber. It uses the existing same-Zone, same-Row,
consecutive-number rule. Each immutable `FeasibleSpotBlock` exposes a read-only
ordered `Spots` collection and derives `ZoneId` and `RowCode` from it; its validated
`Create(spots)` factory also requires a size in the existing GroupSize range.

Empty availability returns no blocks. Null inputs/elements, uninitialized
GroupSize, foreign Zone membership, duplicate SpotCodes and duplicate physical
positions are rejected. Availability and eligibility are supplied by the caller.
The finder does not rank Zones or blocks, reserve Spots, or bind Attendees.
Fragmentation evaluation and selection remain separate deferred responsibilities;
the existing AssignmentEngine is unchanged.

---

# 8. Candidate Generation

Order eligible Zones before generating physical blocks. The later Assignment
flow calls `FeasibleSpotBlockFinder` separately for each ordered Zone:

```text
ordered Zone → FeasibleSpotBlockFinder → 0..N feasible blocks
```

If there is no complete block for the current `GroupSize`, continue to the next
Zone. The first Zone with one or more feasible blocks becomes the selected Zone
for the next decision step. Do not generate all candidates across all Zones
before ordering. The finder verifies physical feasibility; it does not choose a
Zone or select among its blocks.

A group of five cannot use three contiguous Good Spots; evaluation continues
until a complete block is found, including Medium and then Bad as applicable.
Multiple feasible blocks in the selected Zone remain a separate, deferred
block-selection concern. No first-block selection, fragmentation scoring, edge
or center preference, random selection or future-capacity optimization is defined.

---

# 9. Current Inventory State

The online assignment strategy should consider the inventory known at the moment of the request.

Inventory v1 is implemented as the immutable `RemainingInventoryState`, with
non-negative raw currently available Spot counts:

```text
GoodRemaining   = Spots in Good Zones
MediumRemaining = Spots in Medium Zones
BadRemaining    = Spots in Bad Zones
```

`RemainingInventoryCalculator.Calculate(IEnumerable<Zone> zones,
IEnumerable<Spot> availableSpots)` counts each supplied available Spot exactly
once. It resolves `Spot.ZoneId` in the supplied catalog and classifies `Zone.Type`
through `ZoneExperienceQualityPolicy.GetQuality`. Multiple Zones with the same
quality contribute to one count. Empty availability returns three zero counts;
input order does not affect the result.

Null collections/elements, duplicate Zone IDs, unknown Zone references, duplicate
SpotCodes and duplicate physical positions `(ZoneId, RowCode, SpotNumber)` are
rejected explicitly. Availability is supplied by the caller; the calculator does
not load catalog Spots or apply request eligibility. A later caller can supply
an eligible snapshot when needed.

Raw remaining Spot count ≠ contiguous group-feasible capacity. Five remaining
Good Spots may be fragmented across Rows or Zones; this snapshot does not prove
that a group of five fits. `FeasibleSpotBlockFinder` remains responsible for
physical feasibility and is not called by the inventory calculator. GroupSize,
fragmentation metrics, fairness and selection rules are outside inventory v1.

### Next Prerequisite: Zone-Level Availability

The implemented aggregate alone is insufficient for same-quality Zone ordering.
The next implementation prerequisite is to evolve toward Zone-level entries as
the primary availability source:

```text
ZoneAvailabilityState
- ZoneId
- ExperienceQuality
- AvailableSpotCount
```

`AvailableSpotCount` means raw currently available Spots in that Zone. It does
not mean contiguous capacity, number of feasible groups, largest possible group,
fragmentation quality or number of feasible Spot blocks:

```text
AvailableSpotCount ≠ group-feasible capacity
```

Conceptually derive `GoodRemaining`, `MediumRemaining` and `BadRemaining` by
summing Zone-level entries by quality, rather than maintaining an independent
source of truth. Zone Evaluation orders only already eligible Zones using the
supplied availability snapshot. Current availability uses current Zone classification through the
quality policy; historical assignments use quality recorded at assignment time.

This model evolution is agreed design, not implemented here. Existing
`RemainingInventoryState` and `RemainingInventoryCalculator` remain unchanged.
Inventory is decision context and never part of RotationScore.

---

# 10. Current Global Assignment State

`CurrentGlobalAssignmentState` is not used in Zone Evaluation Strategy v1 and
remains deferred. Stage 5 may show that remaining inventory alone is insufficient
and justify incorporating recorded assigned-outcome state in a future iteration.
A likely future model, not an implemented contract, is:

```text
ZoneAssignmentState
- ZoneId
- recorded ExperienceQuality
- AssignedCount
```

Global Good/Medium/Bad assigned totals may then be derived from recorded outcomes.
Later ZoneType or quality-policy changes must not reinterpret historical
assignments. Global fairness is an outcome to measure, not an additional v1
Zone-ordering input. Any future influence needs a concrete scenario and an
explicit population/time scope before implementation.

---

# 11. Zone Evaluation Strategy v1

The future policy orders already eligible Zones only. Its conceptual contract is:

```text
TargetQuality + ZoneAvailabilityState[] + eligible Zones
        ↓
ZoneEvaluationPolicy.Order(...) → IReadOnlyList<Zone>
        ↓
ordered eligible Zones
        ↓
FeasibleSpotBlockFinder
```

The exact production API is not fixed or implemented in this documentation task.
Eligibility precedes ordering:

```text
RequestEligibility → ZoneEligibilityPolicy → eligible Zones
        ↓
Zone Evaluation Strategy
```

Eligibility never reclassifies quality: a Medium Zone stays Medium even if all
Good Zones were excluded.

### Quality Order

| TargetQuality | Remaining raw capacity condition | Quality evaluation order |
| --- | --- | --- |
| Good | Any | Good → Medium → Bad |
| Medium | GoodRemaining > MediumRemaining | Good → Medium → Bad |
| Medium | GoodRemaining <= MediumRemaining (including equality) | Medium → Good → Bad |

For a Good target, evaluate every eligible Good Zone before degrading to Medium,
and every eligible Medium Zone before Bad. For a Medium target, target quality
remains the default preference, while clearly more abundant Good raw capacity
may be consumed first. Equality means Medium first. There is no percentage,
ratio or arbitrary additional threshold; Stage 5 will measure this simple rule.

A quality with zero remaining raw Spot capacity may be omitted. For example,
`GoodRemaining = 0`, `MediumRemaining = 40`, `BadRemaining = 20` permits a Good
target to begin with Medium → Bad. Positive raw capacity does not prove a
physically feasible block exists.

### Same-Quality Zone Order

Within each quality, order eligible Zones by `AvailableSpotCount` descending.
For example, MiddleCenter (Good, 80 available) precedes FrontStanding (Good,
30 available). This uses the existing snapshot and tends to balance consumption
between equivalent-quality Zones as availability changes, avoiding a fixed Zone
being systematically consumed first.

For exact same-quality/count ties, use a stable technical tie-break such as
`ZoneId` ascending. ZoneId ordering is a technical deterministic tie-break only,
not a business preference or the primary Zone priority. No permanent ZoneType
ranking such as FrontStanding before MiddleCenter is defined.

The same state snapshot produces the same order, reproducible for tests and
Stage 5 simulation. No randomness, weighted randomness, persistent round-robin
cursor or future-demand prediction is used.

### Physical-Feasibility Boundary

Visit Zones in this order and call `FeasibleSpotBlockFinder` for each. Skip Zones
with zero blocks for `GroupSize`. The first Zone with 1..N blocks wins for the
next decision step, even if a later Zone might preserve capacity better. This
strategy does not select among blocks; that decision remains deferred.

---

# 12. No Artificial Degradation

Low recovery need does not require a Bad assignment. A Medium target may receive
Good under the exact capacity comparison above, or when no earlier Medium Zone
has a feasible block. Target Quality is not a ceiling. Unknown future need does
not justify speculative reservation.

---

# 13. Degradation When Target Quality Is Unavailable

Bad is never a Target Quality in v1, but is an acceptable actual result when no
eligible Good or Medium Zone has a feasible complete block. The MVP deliberately
prefers a complete lower-quality assignment over rejecting an otherwise
physically assignable group.

A Good-target request is rejected for lack of a block only after no physically
feasible block exists in any eligible Good, Medium or Bad Zone. The same complete
search applies to a Medium target using its quality order. Groups are never
split or restructured automatically; after rejection the user may retry with
another group configuration through a new request.

Stage 5 may evaluate whether degradation should stop before Bad in some scenarios.

---

# 14. Better-Than-Target Assignments

For a Medium target, `GoodRemaining > MediumRemaining` means Good → Medium → Bad;
otherwise Medium → Good → Bad, including equality. Better-than-target behavior
uses raw current inventory, with no global assigned-outcome state in v1.
Physical feasibility remains a separate check in each ordered Zone.

---

# 15. Fairness Is Not Future Reservation

Stage 4 does not reserve favorable capacity for unknown future attendees.

Therefore:

```text
current Good available
+
current request can validly receive it
```

should not automatically become:

```text
save Good because someone with higher future need may arrive
```

Future attendees are unknown.

The current request is evaluated using current information.

If later requests experience lower availability, that effect becomes part of Stage 5 measurement.

---

# 16. Request Arrival Order

Because assignments are online and final, request arrival order may affect the five-day outcome.

An earlier request may consume capacity that would have benefited a later attendee with greater recovery need.

Stage 4 accepts this limitation.

The MVP does not introduce:

- batching;
- speculative reservation;
- delayed assignment;
- future-demand prediction.

Stage 5 should measure how strongly arrival order affects fairness.

If the impact is unacceptable, hybrid or batch allocation can be evaluated later.

---

# 17. Group Assignment Strategy

For a group request:

1. calculate each member's RotationScore;
2. calculate GroupRotationScore;
3. determine the group's target quality;
4. apply organization policy and eligibility;
5. order eligible Zones using Target Quality and Zone-level availability;
6. find feasible blocks per ordered Zone, stopping at the first with blocks;
7. select a complete block through a separate policy (deferred);
8. never split the group automatically merely to improve fairness.

The MVP preserves group integrity.

---

# 18. Group Configuration Failure

A requested group configuration may have no complete feasible assignment.

The Stage 4 engine should not automatically restructure the group.

Conceptually:

```text
requested group
      ↓
no feasible complete block
      ↓
Rejected, no Assignments
```

The existing lack-of-contiguity outcome is
`CONTIGUOUS_SPOTS_NOT_AVAILABLE`. The AssignmentRequest is rejected and does not
remain pending. The user can submit a new request with a changed group; the
original request is not resumed or modified. See the
[domain decision](../domain-blueprint-v1.md#11-contiguous-availability-decision).
Any distinct reason required by a new eligibility policy must be defined with
that policy, without changing the terminal nature of rejection.

The exact interaction workflow is an operational concern and does not need to be fully implemented for the technical fairness MVP unless required.

---

# 19. Group Fairness Trade-Off

GroupRotationScore uses the average member score.

Example:

```text
Member A = +2
Member B = -2

GroupRotationScore = 0
```

The strategy accepts that this can hide internal fairness differences.

No variance-based or max-need group model is introduced in Stage 4.

Stage 5 should measure the impact first.

---

# 20. Deterministic Selection

The Stage 4 baseline remains deterministic.

Given the same relevant:

- request;
- history;
- eligibility;
- feasible candidates;
- inventory;
- Zone-level availability;

the strategy should produce the same decision.

This supports:

- automated testing;
- debugging;
- reproducible simulation;
- explainability.

---

# 21. Tie-Breaking

RotationScore v1 does not use a numerical equivalence tolerance. Same-quality
Zones use available raw Spot count descending, then a stable technical tie-break
such as ZoneId ascending for an exact count tie. This creates no ZoneType business
ranking. Block-selection tie-breaking remains deferred.

---

# 22. Sequential Determinism vs Concurrency

Deterministic business logic and concurrent persistence are separate concerns.

For a sequentially evaluated state:

```text
same input snapshot
→ same assignment decision
```

However, two concurrent requests may evaluate overlapping available Spots before either transaction commits.

Stage 4 does not guarantee:

```text
the request with the higher RotationScore
always wins the database race
```

or:

```text
the first logical request
always commits first
```

Stage 3 database constraints remain authoritative for concurrency safety.

They protect correctness, not fairness ordering between concurrent transactions.

---

# 23. Persistence Boundary

The final assignment should preserve the atomic persistence behavior established in Stage 3.

Conceptually:

```text
assignment decision
        ↓
AssignmentRequest state
+
Assignments
        ↓
single atomic commit
```

Stage 4 must not weaken:

- rollback guarantees;
- uniqueness constraints;
- stable conflict translation;
- transaction boundaries.

---

# 24. Concurrency Failure

A candidate may appear available during evaluation and become unavailable before persistence because another concurrent transaction commits first.

If the database rejects the assignment:

- existing Stage 3 conflict behavior remains applicable;
- fairness must not bypass the global constraint;
- complex automatic retry or re-optimization should not be added without evidence that the MVP requires it.

Retry policy is not part of Fairness Definition v1.

---

# 25. Experience History Update

Once an assignment succeeds:

```text
selected quality
        ↓
persisted assignment experience
        ↓
future attendee history
```

That experience becomes part of later RotationScore calculations.

Each successful online decision therefore updates both:

- inventory;
- fairness history.

This creates the incremental evolution of the system across the five festival days.

---

# 26. Candidate Decision Outline

The Stage 4 decision flow is:

```text
1. Receive daily AssignmentRequest.

2. Resolve attendees and group configuration.

3. Load real previous assignment history.

4. Calculate individual RotationScores.

5. Calculate GroupRotationScore when applicable.

6. Determine reasonable Target Quality.

7. Apply organization policies.

8. Apply attendee/group eligibility.

9. Derive current Zone quality and Zone-level raw availability.

10. Order eligible Zones using TargetQuality and ZoneAvailabilityState entries.

11. Call FeasibleSpotBlockFinder for each ordered Zone until one has 1..N blocks.

12. Reject without Assignments if no eligible Zone has a feasible block.

13. Select a block inside the selected Zone (separate deferred decision).

14. Persist the complete decision atomically.

15. Return the assignment result.
```

This flow defines business behavior.

It does not prescribe a class structure.

---

# 27. Reference Decision Examples

## Example A — Recovery Need and Available Quality

```text
History:
Bad → Bad → Medium

RotationScore:
high recovery need

Available:
Good
Medium
Bad
```

Expected behavior:

Prefer a `Good` assignment when feasible.

---

## Example B — Target Quality Unavailable

```text
Target = Good

Available feasible:
Medium
Bad
```

Expected behavior:

Prefer `Medium`.

Do not violate feasibility to obtain `Good`.

---

## Example C — Better Than Target Is Reasonable

```text
Target = Medium

Inventory:
GoodRemaining = 80
MediumRemaining = 30
```

Expected behavior:

Evaluate Good first; select the first Good Zone with a feasible block.

The engine should not degrade the request merely to match the target exactly.

---

## Example D — Low Recovery Need Does Not Mean Punishment

```text
History:
Good → Good → Medium

Target:
lower recovery need

Inventory:
Good capacity abundant
```

Expected behavior:

For a Medium target, Good is evaluated first when GoodRemaining > MediumRemaining;
otherwise it follows Medium. A feasible Good outcome remains allowed.

---

## Example E — Group Feasibility

```text
Group size = 4

Good:
2 contiguous Spots

Medium:
4 contiguous Spots
```

Expected behavior:

`Good` is not a complete candidate.

Continue to Medium Zones; the first with feasible blocks is selected.
Choosing among its blocks remains deferred.

---

## Example F — Eligibility Changes Participation Only

```text
Attendee excludes Front Standing.
```

Expected behavior:

Front Standing is removed before fairness selection.

`MiddleCenter` remains `Good`; `MiddleLeft` remains `Medium`. Excluding
`FrontStanding` does not promote or demote either Zone.

---

## Example G — Unknown Future Demand

```text
Current request:
neutral recovery need

Good available now

No other request known
```

Expected behavior:

Do not deliberately withhold `Good` only because a higher-need attendee might request later.

---

# 28. Strategy Baseline

The baseline makes one deterministic online decision at a time using historical
recovery need, globally classified Experience Quality and Zone-level availability.
Global assigned-outcome state is deferred.
The [reference examples](#27-reference-decision-examples) guide provisional rules;
Stage 5 evaluates their resulting fairness. This is not a globally optimal
allocation algorithm.

---

# 29. What Assignment Strategy v1 Does Not Do

The baseline does not:

- optimize all FestivalDays globally;
- batch all daily requests;
- predict future requests;
- reserve capacity for unknown attendees;
- automatically restructure groups;
- perform complex preference ranking;
- use weighted randomness;
- calculate a mathematically optimal global solution;
- guarantee identical outcomes under concurrent transaction races;
- guarantee a specific fairness percentage.

These are possible future evolutions if Stage 5 or organization feedback justifies them.

---

# 30. Lean Design Constraints

Do not introduce, without concrete evidence:

- batch schedulers;
- global optimization;
- prediction models;
- dynamic fairness learning;
- complex inventory forecasting;
- multiple fairness strategies;
- internal group-dispersion metrics;
- automatic group negotiation;
- speculative operational abstractions.

Preferred evolution:

```text
implement online baseline
        ↓
simulate five festival days
        ↓
measure fairness
        ↓
identify concrete limitation
        ↓
make smallest justified change
```

---

# Validation Requirements

Assignment Strategy v1 is considered implemented when:

- daily individual requests can be processed;
- daily group requests can be processed;
- historical assignments affect RotationScore;
- RotationScore influences target quality;
- eligibility is applied before selection;
- physical feasibility is applied before selection;
- same Zone and same Row group invariants are respected;
- only complete group candidates are considered;
- current inventory influences the final decision;
- eligible Zones follow the agreed quality/count/tie ordering;
- Zone-level entries are the primary availability source;
- feasibility is checked per ordered Zone;
- block selection is resolved separately before full engine completion;
- better-than-target assignments are possible when reasonable;
- artificial degradation is avoided;
- unknown future demand is not used for speculative reservation;
- deterministic sequential behavior is preserved;
- persistence remains atomic;
- Stage 3 concurrency protections remain green;
- reference scenarios are covered by automated tests.

---

# Stage 5 Validation Questions

Stage 5 should measure:

1. Does the online strategy produce sufficiently balanced five-day paths?
2. How much does request arrival order affect results?
3. How often does the engine fail to recover unfavorable histories?
4. How often do attendees never receive a `Good` experience?
5. Does inventory-aware selection improve capacity usage?
6. Is global assigned-outcome state needed to improve fairness?
7. Does the strategy assign too many favorable experiences early?
8. Does it leave favorable capacity unused or underused?
9. How strongly do group requests affect fairness?
10. How strongly does group size affect feasible quality?
11. How much fairness is physically impossible due to venue configuration?
12. Would a daily batch or hybrid model materially improve outcomes?

The detailed [Zone Evaluation questions](trade-offs-and-open-questions.md#zone-evaluation-strategy-v1)
cover degradation rates, Medium-to-Good outcomes, the raw-capacity comparison,
same-quality usage balance, fragmentation, stopping before Bad, group
restructuring, global outcome state and unacceptable arrival-order effects.

Until these questions are measured, Assignment Strategy v1 remains a **Lean online fairness baseline**, not a final operational allocation policy.
