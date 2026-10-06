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
- current global assignment state;
- deterministic selection.

This is the initial Stage 4 strategy guide. Selection examples express intent;
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
- considering the global assignment state accumulated so far;
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
Eligibility + Physical Feasibility + Candidate Experience Quality
+ Current Inventory State + Current Global Assignment State
        ↓
Final Assignment Selection → actual ExperienceQuality
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

Target derivation does not include inventory or global state. Final selection
must consider eligible options, physical feasibility, candidate Experience
Quality, Current Inventory State and Current Global Assignment State. Concrete
better-than-target and degradation rules remain separate later selection tasks.
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

---

# 8. Candidate Generation

Only complete, eligible, and feasible assignment candidates should continue to selection.

Conceptually:

```text
Available Spots
      ↓
Organization Policy
      ↓
Eligibility
      ↓
Physical Feasibility
      ↓
Candidate Assignments
```

For groups, the strategy compares complete blocks rather than isolated Spots.

Example:

```text
Group size = 5

Good capacity:
3 contiguous Spots

Medium capacity:
5 contiguous Spots
```

The `Good` block is not a valid candidate.

The complete `Medium` block is.

---

# 9. Current Inventory State

The online assignment strategy should consider the inventory known at the moment of the request.

Relevant information may include:

```text
Good capacity remaining
Medium capacity remaining
Bad capacity remaining
Total unassigned capacity
Current assigned capacity
```

The exact inventory model should remain minimal for Stage 4.

Its purpose is to answer questions such as:

> Is favorable capacity currently scarce?

> Is there enough favorable capacity remaining that assigning a better-than-target experience would not unnecessarily damage the current global balance?

Inventory State is not part of RotationScore.

It is decision context.

---

# 10. Current Global Assignment State

The strategy should also consider the assignment distribution accumulated so far.

This may include aggregate information such as:

```text
Good assignments already made
Medium assignments already made
Bad assignments already made
```

or other minimal metrics justified during implementation.

The objective is not to calculate a future global optimum.

The objective is to avoid making a current decision that **unnecessarily deteriorates** the fairness state observed so far.

Global fairness in Stage 4 is therefore incremental.

Before implementing global-state influence, document one concrete scenario where
it changes the selected candidate, the expected outcome and why that outcome
preserves fairness. Define the population/time scope and minimum metric needed.
Simple quality totals may be useful inputs, but cannot alone show whether the
same attendees repeatedly receive favorable experiences. The
[implementation plan](implementation-plan.md#step-14--introduce-current-global-assignment-state)
requires scenario evidence before introducing additional state.

---

# 11. Online Fairness-Aware Selection

The final decision should combine:

```text
RotationScore
        +
Target Quality
        +
Eligible Candidates
        +
Physical Feasibility
        +
Current Inventory
        +
Current Global Assignment State
```

to select the best reasonable candidate available now.

Conceptually:

```text
fairness need
      +
current system state
      ↓
best reasonable assignment now
```

The strategy should protect the attendee or group's historical path while avoiding unnecessary damage to the current global distribution.

---

# 12. No Artificial Degradation

The strategy must not deliberately assign a worse option simply because the attendee currently has low recovery need.

Example:

```text
Target Quality = Medium
+
Good inventory is abundant
+
Good candidates are eligible and feasible
```

The system may assign:

```text
Good
```

if doing so does not meaningfully damage current global fairness.

The strategy should not waste favorable capacity merely to enforce a theoretical quality level.

---

# 13. Degradation When Target Quality Is Unavailable

If no valid candidate exists at the target quality, the strategy may evaluate lower-quality alternatives.

Conceptually:

```text
Target = Good

No valid Good candidate
        ↓
try Medium
        ↓
if unavailable
try Bad
```

However, before finalizing a lower-quality assignment, the strategy should evaluate the full current candidate and inventory state.

A better-quality candidate may still be chosen if:

- it is eligible;
- it is feasible;
- it is available;
- using it does not unnecessarily worsen the current global fairness state.

---

# 14. Better-Than-Target Assignments

The strategy may assign a better quality than the target when current system conditions justify it.

Example:

```text
Target = Medium

Available:
many Good
few Medium
enough total capacity
```

A `Good` assignment may be more reasonable than deliberately consuming scarce `Medium` capacity.

This is one reason Target Quality must remain separate from final Candidate Selection.

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
5. generate complete feasible blocks;
6. evaluate current inventory and global state;
7. select a complete block;
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
- global assignment state;

the strategy should produce the same decision.

This supports:

- automated testing;
- debugging;
- reproducible simulation;
- explainability.

---

# 21. Tie-Breaking

RotationScore v1 does not use a numerical equivalence tolerance.

If an actual selection reaches an exact tie after the relevant decision rules are applied, the strategy should use a simple deterministic tie-breaker.

Examples may include:

```text
stable candidate ordering
```

or:

```text
request ordering
```

where applicable.

The exact tie-break should be explicit in implementation and should not introduce hidden fairness semantics.

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

9. Generate complete physically feasible candidates.

10. Derive candidate Experience Quality from the global ZoneType policy.

11. Read Current Inventory State.

12. Read Current Global Assignment State.

13. Select the best reasonable candidate now.

14. Apply deterministic tie-breaking if required.

15. Persist the complete decision atomically.

16. Return the assignment result.
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
Good capacity abundant
Medium capacity scarce
```

Expected behavior:

A `Good` assignment may be selected.

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

`Good` remains a valid outcome when no current fairness reason requires preserving that capacity.

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

Select the valid `Medium` block if appropriate.

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
recovery need, globally classified Experience Quality, inventory and incremental
global state.
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
- current global assignment state can influence the final decision;
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
6. Does incremental global fairness avoid extreme negative paths?
7. Does the strategy assign too many favorable experiences early?
8. Does it leave favorable capacity unused or underused?
9. How strongly do group requests affect fairness?
10. How strongly does group size affect feasible quality?
11. How much fairness is physically impossible due to venue configuration?
12. Would a daily batch or hybrid model materially improve outcomes?

Until these questions are measured, Assignment Strategy v1 remains a **Lean online fairness baseline**, not a final operational allocation policy.
