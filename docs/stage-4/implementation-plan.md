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
one-issue-per-step sequence. The score can be implemented with classified test
histories while venue-quality decisions are still open.

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
| Location-to-quality mapping, eligibility versus availability, and member-specific quality in mixed groups | Steps 2 and 12 | A small venue example, explicit classifications and the quality stored for each member. |
| Score-to-Target Quality policy (agreed) | Step 9 implementation | [Target Quality v1](assignment-strategy-v1.md#3-determine-target-quality): score >= 1.00 → Good; score < 1.00 → Medium, for individuals and groups. Calibration belongs to Stage 5. |
| Minimum implemented eligibility rules | Step 10 | A concrete allowed/excluded candidate case; undefined organization policies remain deferred. |
| Inventory and global-state scope and selection influence | Steps 13–16 | An exact decision using known current state, including when a better-than-target option is reasonable. Define counting units and whether state is daily or festival-wide. |
| Deterministic candidate ordering | Step 18 | A tied-candidate case with one reproducible result. |
| Persisted experience representation | Steps 20–22 | Quality and the complete successful outcome saved within the existing atomic boundary. No legacy-data backfill is needed. |

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

Quality represents the attendee's experience, not an absolute Zone ranking.

### Goal

Provide a stable representation that can be consumed by assignment history and RotationScore.

### Validation

Unit tests should verify:

- the three quality values;
- their fairness contribution;
- deterministic behavior.

---

## Step 2 — Define Eligibility-Aware Quality Rules

Define the minimum rule required to interpret quality within the attendee's valid opportunity space.

For example:

```text
Front Standing excluded
```

must not automatically mean:

```text
attendee cannot receive Good
```

The best remaining valid option may still represent `Good`.

### Goal

Avoid creating fairness deficits from options the attendee was never eligible to receive.

### Lean Constraint

Do not implement complex preference ranking.

Only implement the minimum quality rules required by the MVP venue model.
Resolve the [quality questions in the strategy](assignment-strategy-v1.md#6-eligibility-aware-experience-quality)
before implementing classification. This does not block Steps 3–8 using
already-classified histories.

---

## Step 3 — Represent Assignment History for Fairness

Provide the minimum ordered information required to calculate an attendee's fairness state.

History contains real previous assignments only.

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
historical quality records or decide mixed-eligibility quality classification.

---

## Step 9 — Implement the Agreed Target Quality Policy v1

The [policy is defined](assignment-strategy-v1.md#3-determine-target-quality).
Implementation in code remains a subsequent task, using the same rule for
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
feasibility, inventory and global state remain selection concerns.

### Validation

Future implementation tests must cover negative and zero scores (`Medium`),
scores below `1.00` (`Medium`), exactly `1.00` (`Good`) and above it (`Good`),
with the same policy for individuals and groups. Preserve the
[representative histories and boundary case](trade-offs-and-open-questions.md#target-quality-v1-threshold-decision)
as reference cases. Verify that the policy never targets `Bad`; do not implement
final assignment selection as part of this step.

---

## Step 10 — Implement Minimum Eligibility Rules

Implement only eligibility concepts concretely required by the MVP.

Possible initial cases:

- front-standing opt-out;
- accessibility compatibility;
- organization-reserved capacity.

Eligibility must be applied before fairness-aware selection.

### Goal

Ensure invalid options never reach final selection.

### Lean Constraint

If a rule has not yet been defined sufficiently by the business model, document it instead of inventing behavior.

---

## Step 11 — Generate Feasible Candidate Blocks

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

Each valid candidate should receive its relative experience-quality classification:

```text
Good
Medium
Bad
```

Quality must respect the request's eligible opportunity space.

### Goal

Allow candidate selection to reason about both fairness need and available experience quality.

---

## Step 13 — Introduce Current Inventory State

Provide the minimum information required to describe currently available capacity.

Possible initial metrics:

```text
Good capacity remaining
Medium capacity remaining
Bad capacity remaining
Total remaining capacity
```

The implementation should remain minimal.

### Goal

Allow the strategy to recognize:

- scarce favorable capacity;
- abundant favorable capacity;
- quality distributions that make a better-than-target assignment reasonable.

### Important Boundary

Inventory State must not modify RotationScore.
Define whether counts describe Spots or feasible blocks and how relative quality
is interpreted for the current request. Candidate blocks can overlap; counting
them does not necessarily count independently usable capacity.

---

## Step 14 — Introduce Current Global Assignment State

Provide the minimum aggregate information required to describe assignments already made.

Possible initial metrics:

```text
Good assignments made
Medium assignments made
Bad assignments made
```

Additional metrics should only be added if a concrete decision requires them.

### Goal

Support incremental global fairness.

The system should be able to ask:

> Would this current decision unnecessarily deteriorate the fairness distribution observed so far?

### Lean Constraint

Do not build a complete global optimization model.
First document a scenario with explicit current histories, candidates and
inventory where global state justifies a different selection. Specify the
expected decision and minimum information needed to explain it. Aggregate
Good/Medium/Bad counts alone do not demonstrate fair distribution among people.
Resolve what “unnecessarily deteriorate” means for that scenario before adding
state or implementing its decision rule. This is a Stage 4 design task, not a
reason to predict future demand.

---

## Step 15 — Implement Online Fairness-Aware Selection

Combine:

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

to select the best reasonable candidate now.

### Required Behavior

The strategy should:

- prefer recovery when justified;
- respect eligibility and feasibility;
- avoid artificial degradation;
- allow better-than-target assignments;
- use only known current state;
- avoid speculative reservation for future requests.

### Goal

Create the first complete Stage 4 online allocation baseline.

---

## Step 16 — Implement Better-Than-Target Behavior

Explicitly support cases such as:

```text
Target = Medium

Good capacity abundant
Medium capacity scarce
```

where `Good` may be the more reasonable final assignment.

### Goal

Prevent Target Quality from becoming an artificial ceiling.

---

## Step 17 — Implement Degradation When Target Is Unavailable

If target quality is unavailable:

```text
Good
→ Medium
→ Bad
```

may be evaluated in decreasing quality order, subject to eligibility and feasibility.

Before final degradation, the strategy should consider whether a better-quality candidate is reasonably available under the current inventory/global state.

### Goal

Preserve graceful behavior under scarcity.

---

## Step 18 — Add Deterministic Candidate Tie-Breaking

RotationScore v1 does not use score tolerance.

When the decision rules result in an exact tie, use a stable deterministic rule.

Examples:

```text
stable location ordering
```

or another explicit deterministic candidate order.

### Goal

Guarantee reproducibility.

---

## Step 19 — Integrate with ProcessAssignmentRequestUseCase

Integrate the Stage 4 strategy with the existing application flow.

Reuse existing ports whenever responsibilities still match.

Introduce a new abstraction only when Stage 4 creates a genuinely new responsibility.

### Goal

Evolve the existing assignment process without weakening current application semantics.

---

## Step 20 — Integrate Persisted History

Use previous real assignments as input to RotationScore.

Infrastructure should provide the required history through an application/domain-facing abstraction.

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

Once an assignment is completed, the quality interpreted for that assignment should be available for future history evaluation.

### Goal

Avoid reclassifying old experiences every time current venue rules or preferences change.

The exact persistence representation should be chosen during implementation with the smallest justified change.

The database is empty at Stage 4 preparation; there are no historical assignments
to migrate or reclassify. Record quality for new Assignments as part of the
atomic successful outcome. Implement this together with persisted-history
integration when needed, rather than assuming existing records contain quality.

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

Lack of a contiguous block retains `CONTIGUOUS_SPOTS_NOT_AVAILABLE`, as defined
in the [existing domain decision](../domain-blueprint-v1.md#11-contiguous-availability-decision).
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
→ feasibility
→ inventory
→ global state
→ selection
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
- actual global-state metrics;
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
- global-state evaluation;
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
- eligibility-aware quality is defined;
- RotationScore v1 is implemented;
- reference RotationScore scenarios pass;
- GroupRotationScore works;
- Target Quality v1 is implemented;
- eligibility precedes assignment selection;
- physical feasibility precedes assignment selection;
- same Zone and same Row group constraints are respected;
- complete candidate blocks are generated;
- Current Inventory State influences the decision;
- Current Global Assignment State influences the decision;
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