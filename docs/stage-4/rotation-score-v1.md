# RotationScore v1

## Purpose

`RotationScore` estimates the attendee's **current recovery need** based on their real assignment history.

Its purpose is to provide a simple, deterministic, and explainable fairness signal that can help the online assignment strategy determine a reasonable target experience quality for the current request.

RotationScore does **not**:

- select a specific Zone or Spot;
- reserve capacity for future attendees;
- compare the current request against unknown future requests;
- include current inventory pressure;
- guarantee a specific assignment quality.

RotationScore v1 is intentionally minimal and should remain a measurable baseline that can be evaluated and calibrated in Stage 5.

---

## Relationship with Fairness Definition v1

`RotationScore` is derived from the principles defined in [Fairness Definition v1](./fairness-definition-v1.md).

Conceptually:

```text
Assignment History
        ↓
Fairness Signals
        ↓
RotationScore
        ↓
Recovery Need
        ↓
Target Experience Quality
```

A higher `RotationScore` means:

> The attendee currently has a greater recovery need.

A lower score means:

> The attendee currently has less recovery need based on the assignment history known so far.

The score represents **need**, not entitlement.

---

# Online Decision Boundary

RotationScore uses only the Attendee's real past assignments. It contains no
inventory pressure, global-state metric or prediction of future demand.

# RotationScore vs Assignment Decision

The score estimates recovery need. The
[Assignment Strategy](assignment-strategy-v1.md) combines it with Target Quality,
eligibility, feasibility, current inventory and global assignment state.
A low score does not require a Bad assignment; available Good capacity may still
be used under the selection policy.

---

# Design Goals

RotationScore v1 should be:

- deterministic;
- explainable;
- inexpensive to calculate;
- independent from persistence infrastructure;
- based only on real assignment history;
- sensitive to accumulated imbalance;
- sensitive to recent recovery need;
- able to recognize an attendee who has never received a favorable experience;
- simple enough to validate with concrete scenarios;
- easy to calibrate during Stage 5.

It should not attempt to perfectly rank every possible assignment path.

---

# Experience Quality Representation

For RotationScore v1, assignment history is represented using three experience-quality levels:

```text
Good   = -1
Medium =  0
Bad    = +1
```

The direction is intentional:

```text
positive contribution
→ greater recovery need

neutral contribution
→ neutral effect

negative contribution
→ favorable experience received
```

Therefore:

```text
higher RotationScore
→ greater recovery need
```

RotationScore consumes already-classified historical experiences.

The mechanism used to determine whether a real assignment was `Good`, `Medium`, or `Bad` belongs to the Experience Quality model.

---

# Eligibility-Aware Quality

Experience quality must be interpreted relative to the attendee's eligible opportunity space at the time of the assignment.

For example:

```text
Front Standing excluded
```

must not later become:

```text
Attendee never received Front Standing
→ fairness deficit
```

If the best eligible alternative for that attendee was assigned, that assignment may still represent `Good`.

Therefore RotationScore should consume the **experience quality recorded for that attendee's valid opportunity space**, not an absolute global venue ranking.

---

# Historical Quality Stability

Once an assignment becomes part of the attendee's fairness history, RotationScore should use the quality classification associated with that historical assignment.

The MVP should not continuously reinterpret old assignments based on later preference or policy changes.

This keeps historical evaluation stable and reproducible.

If organization feedback later requires historical reclassification, that should be treated as a separate policy decision.

---

# RotationScore Signals

RotationScore v1 uses three signals:

1. `HistoricalDeficit`
2. `RecentRecoveryNeed`
3. `GoodExperienceDeficit`

No additional signal should be introduced unless a concrete fairness scenario demonstrates that the current model is insufficient.

---

## 1. Historical Deficit

`HistoricalDeficit` represents the accumulated balance of the attendee's real assignment history.

Initial definition:

```text
HistoricalDeficit =
    sum(assignment quality contributions)
```

Example:

```text
Bad → Bad → Medium

+1 +1 +0 = +2
```

Therefore:

```text
HistoricalDeficit = +2
```

Another example:

```text
Good → Medium → Bad

-1 +0 +1 = 0
```

Therefore:

```text
HistoricalDeficit = 0
```

### Interpretation

Positive:

```text
accumulated unfavorable experience
```

Negative:

```text
accumulated favorable experience
```

Near zero:

```text
approximately neutral historical balance
```

### Why It Exists

Without `HistoricalDeficit`, the system cannot distinguish meaningful accumulated disadvantage from a neutral path.

This is the main structural signal in RotationScore v1.

---

## 2. Recent Recovery Need

`RecentRecoveryNeed` represents the immediate effect of the most recent real assignment.

Initial definition:

```text
last assignment = Bad    → +1
last assignment = Medium →  0
last assignment = Good   → -1
```

No history:

```text
RecentRecoveryNeed = 0
```

Example:

```text
Good → Medium → Bad
```

produces:

```text
RecentRecoveryNeed = +1
```

while:

```text
Good → Bad → Medium
```

produces:

```text
RecentRecoveryNeed = 0
```

Although the accumulated balance is identical, the first path has greater immediate recovery need.

### Why It Exists

Historical balance alone cannot represent direction.

The recent assignment provides a minimal way to distinguish deterioration from recovery.

### Known Simplification

RotationScore v1 considers only the latest real assignment.

It does not yet calculate:

- recent windows;
- trend;
- streak duration;
- recovery velocity.

If Stage 5 demonstrates that this simplification produces meaningful failures, the signal may evolve.

---

## 3. Good-Experience Deficit

`GoodExperienceDeficit` identifies whether the attendee has already received at least one favorable experience.

Initial definition:

```text
no history       → 0
never had Good   → +1
already had Good → 0
```

A new attendee remains neutral.

### Why It Exists

Consider:

```text
Medium → Medium → Medium
```

and:

```text
Bad → Medium → Good
```

Both may have approximately neutral accumulated balance.

However, the first attendee has never experienced a `Good` assignment.

Fairness Definition v1 considers that difference relevant.

---

# Baseline Formula

The first experimental formula is:

```text
RotationScore =
    HistoricalDeficit
  + 0.50 × RecentRecoveryNeed
  + 0.25 × GoodExperienceDeficit
```

Equivalent notation:

```text
RotationScore =
    H
  + 0.50R
  + 0.25G
```

where:

```text
H = HistoricalDeficit
R = RecentRecoveryNeed
G = GoodExperienceDeficit
```

---

# Weight Interpretation

The initial weights encode the following conceptual hierarchy:

```text
Historical Deficit
        >
Recent Recovery Need
        >
Good-Experience Deficit
```

This means:

- accumulated history is the strongest signal;
- recent experience can meaningfully modify immediate recovery need;
- never receiving `Good` is a complementary signal.

These values are not proven business constants.

They are baseline parameters derived from the fairness scenarios explored during Stage 4.

Stage 5 should evaluate their behavior and sensitivity.

---

# Neutral State

A new attendee has:

```text
HistoricalDeficit     = 0
RecentRecoveryNeed    = 0
GoodExperienceDeficit = 0
```

Therefore:

```text
RotationScore = 0
```

This preserves the Fairness Definition principle that a new attendee starts neutral.

---

# Absence

A festival day without participation does not modify RotationScore.

No synthetic assignment is created.

Example:

```text
Day 1: Bad
Day 2: absent
Day 3: Medium
```

Real fairness history:

```text
Bad → Medium
```

The absence introduces neither reward nor penalty.

---

# RotationScore and Target Quality

Higher recovery need suggests a stronger preference for Good; neutral need may
suggest Medium; lower need reduces the claim on scarce Good capacity.

The mapping belongs to the
[Target Quality policy](assignment-strategy-v1.md#3-determine-target-quality),
not the score formula. That policy must define an initial deterministic rule
when implemented. Its parameters may later be calibrated with evidence.

## Target Quality Is Not a Maximum

A Medium target can result in Good when current inventory and global fairness
justify it. Low recovery need does not require an unfavorable assignment.

## Target Quality Is Not a Guarantee

A Good target can result in Medium or Bad when eligible, feasible and available
options do not support it. The strategy defines fallback and better-than-target
behavior; the score alone never guarantees an assigned quality.

---

# Reference Scenarios

The following cases were validated during Stage 4 discovery and should become regression tests for RotationScore behavior.

---

## Scenario 1 — Clear Recovery Differences

### Juan

```text
Bad → Bad → Medium
```

Calculation:

```text
HistoricalDeficit     = +2
RecentRecoveryNeed    =  0
GoodExperienceDeficit = +1

RotationScore =
2 + 0 + 0.25
= 2.25
```

### Luis

```text
Good → Bad → Bad
```

Calculation:

```text
HistoricalDeficit     = +1
RecentRecoveryNeed    = +1
GoodExperienceDeficit = 0

RotationScore =
1 + 0.50
= 1.50
```

### Ana

```text
Medium → Medium → Medium
```

Calculation:

```text
HistoricalDeficit     = 0
RecentRecoveryNeed    = 0
GoodExperienceDeficit = +1

RotationScore = 0.25
```

### Pedro

```text
Good → Good → Medium
```

Calculation:

```text
HistoricalDeficit     = -2
RecentRecoveryNeed    = 0
GoodExperienceDeficit = 0

RotationScore = -2
```

Recovery-need ordering:

```text
Juan > Luis > Ana > Pedro
```

The baseline reproduces this ordering.

This does not mean these four requests must be processed together.

The scenario validates only how RotationScore represents their individual recovery needs.

---

## Scenario 2 — Direction Matters

### B

```text
Good → Bad → Bad
```

```text
RotationScore = 1.50
```

### A

```text
Bad → Bad → Good
```

```text
HistoricalDeficit     = +1
RecentRecoveryNeed    = -1
GoodExperienceDeficit = 0

RotationScore = 0.50
```

### C

```text
Medium → Medium → Medium
```

```text
RotationScore = 0.25
```

### D

```text
Bad → Medium → Good
```

```text
HistoricalDeficit     = 0
RecentRecoveryNeed    = -1
GoodExperienceDeficit = 0

RotationScore = -0.50
```

Recovery-need ordering:

```text
B > A > C > D
```

This demonstrates that recent recovery can modify historical need.

---

## Scenario 3 — Business Intuition Does Not Require Perfect Score Ordering

Consider:

### C

```text
Medium → Bad → Bad
```

```text
RotationScore = 2.75
```

### D

```text
Bad → Medium → Medium
```

```text
RotationScore = 1.25
```

### A

```text
Good → Medium → Bad
```

```text
RotationScore = 0.50
```

### B

```text
Good → Bad → Medium
```

```text
RotationScore = 0
```

The formula produces:

```text
C > D > A > B
```

During fairness discovery, the business distinction between `D` and `A` was considered relatively weak.

However, RotationScore v1 does not introduce special tolerance rules to reproduce every weak preference.

The MVP accepts the numerical ordering as long as the main fairness distinctions remain reasonable.

Stage 5 should determine whether this produces any meaningful negative outcome.

---

## Scenario 4 — No Good Experience vs Strong Recovery

### Ana

```text
Medium → Medium → Medium → Medium → Medium
```

```text
HistoricalDeficit     = 0
RecentRecoveryNeed    = 0
GoodExperienceDeficit = +1

RotationScore = 0.25
```

### Gisela

```text
Bad → Bad → Medium → Good → Good
```

```text
HistoricalDeficit     = 0
RecentRecoveryNeed    = -1
GoodExperienceDeficit = 0

RotationScore = -0.50
```

Recovery-need ordering:

```text
Ana > Gisela
```

Gisela experienced an initial deficit but has already received strong recovery.

Ana remains neutral but has never received a favorable experience.

---

## Scenario 5 — Large Deficits Are Not Erased by One Good Experience

Consider:

```text
Bad → Bad → Bad → Bad → Good
```

Calculation:

```text
HistoricalDeficit     = +3
RecentRecoveryNeed    = -1
GoodExperienceDeficit = 0

RotationScore =
3 - 0.50
= 2.50
```

The recent `Good` reduces recovery need but does not erase the accumulated deficit.

---

# No Equivalence Tolerance in v1

RotationScore v1 does not use a configurable numerical tolerance.

The previous idea:

```text
abs(scoreA - scoreB) <= tolerance
→ fairness equivalent
```

is intentionally deferred.

For the current baseline:

```text
higher score
→ greater recovery need

same score
→ equal recovery need for RotationScore purposes
```

If an actual assignment decision requires a tie-break, the Assignment Strategy should use a deterministic rule.

Stage 5 may later evaluate whether small score differences need an equivalence mechanism.

---

# Group RotationScore

For Stage 4, group recovery need is approximated using the arithmetic mean of individual member scores:

```text
GroupRotationScore =
    sum(MemberRotationScores)
    / MemberCount
```

Example:

```text
Member A = +2.00
Member B =  0.00
Member C = -0.50
```

Then:

```text
GroupRotationScore =
(2.00 + 0.00 - 0.50) / 3
= 0.50
```

A new attendee contributes:

```text
RotationScore = 0
```

to the group average.

Confirmed arithmetic example (illustrative input scores):

```text
MemberRotationScores = [2, 4, 1, 3, 0]
GroupRotationScore = (2 + 4 + 1 + 3 + 0) / 5 = 2
```

The resulting mean guides the group's Target Quality and subsequent Zone/block
selection. Each member retains their individual history. The mean does not
determine how a shared block is classified for members with different eligible
opportunities; that decision belongs to the Experience Quality rules.

---

# Group Trade-Off

Averages can hide internal dispersion.

Example:

```text
Member A = +2
Member B = -2

GroupRotationScore = 0
```

The group appears neutral even though its members have very different histories.

This limitation is intentionally accepted in Stage 4.

Stage 5 should determine whether it materially damages individual or global fairness before a more complex group model is introduced.

---

# RotationScore and Current Inventory

Inventory describes capacity available now; it must not change RotationScore for
the same historical path. The Assignment Strategy combines these separate inputs.

# RotationScore and Global Fairness

RotationScore is an individual or group recovery signal. Global-state influence
belongs to the [strategy](assignment-strategy-v1.md#10-current-global-assignment-state),
and global outcome measurement belongs to Stage 5. Neither is included in the
score calculation.

---

# What RotationScore Does Not Do

RotationScore does not:

- assign a Zone;
- assign a Spot;
- define physical feasibility;
- verify contiguity;
- determine accessibility;
- reserve capacity;
- predict future demand;
- compare against unknown future attendees;
- calculate current inventory pressure;
- calculate global fairness;
- guarantee a `Good` experience;
- punish attendees with historically favorable paths;
- classify the final five-day path.

These responsibilities belong elsewhere.

---

# Lean Evolution Rule

RotationScore v1 should remain unchanged unless a concrete scenario demonstrates a meaningful failure.

Potential future signals such as:

- trend;
- unfavorable streak length;
- variance;
- recent-window scoring;
- days since last `Good`;
- number of previous `Good` assignments;

should not be introduced preemptively.

Preferred evolution:

```text
Observed Stage 4 / Stage 5 behavior
        ↓
Concrete fairness failure
        ↓
Identify missing information
        ↓
Smallest score change
        ↓
Regression test
        ↓
Simulation comparison
```

---

# Validation Requirements

Before RotationScore v1 is considered complete:

- all three signals must be implemented deterministically;
- new attendees must produce a neutral score;
- absences must not modify the history;
- historical quality must be stable once recorded;
- reference scenarios must become automated tests;
- group score must be deterministic;
- calculation must remain independent from persistence;
- inventory must not influence the score itself;
- global fairness must remain outside the score calculation;
- existing Stage 3 invariants must remain unaffected.

---

# Stage 5 Questions

Stage 5 should evaluate:

1. Are the initial weights appropriate?
2. Is HistoricalDeficit too strong or too weak?
3. Is the latest assignment sufficient to represent recency?
4. Does GoodExperienceDeficit improve the five-day outcome?
5. Does group averaging create unacceptable individual imbalance?
6. Do small score differences create undesirable allocation behavior?
7. Is an equivalence tolerance actually necessary?
8. How sensitive are five-day paths to weight changes?
9. Does RotationScore improve outcomes compared with simpler baselines?
10. How should RotationScore map to target quality under different inventory states?

Until those questions are measured, RotationScore v1 remains a **baseline recovery-need hypothesis**, not a final fairness formula.