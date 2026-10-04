# Fairness Definition v1

## Purpose

This document defines the first explicit fairness model for the Festival Assignment System.

Its purpose is to describe **what fair behavior means for the MVP**, independently from the specific algorithm used to implement it.

This definition guides:

- `RotationScore`;
- target experience quality;
- group fairness;
- online assignment decisions;
- Stage 5 simulation and validation.

Fairness Definition v1 is a **working business hypothesis**.

It should evolve only when implementation, simulation, or organization feedback shows that the current model is insufficient.

The [Stage 4 guide](README.md) distinguishes implementation from later evaluation.
These principles guide provisional rules; they do not claim that fairness is
already measured or accepted by the organization.

---

## Core Principle

Fairness is evaluated across the attendee's assignment path, not as an isolated decision for a single festival day.

For each daily request, the system should use the attendee or group history together with the current system state to produce a reasonable assignment that:

- protects individual or group fairness;
- contributes to global fairness accumulated so far;
- respects eligibility and physical feasibility;
- uses only information currently known;
- does not deliberately create poor experiences when better options can reasonably be used.

Fairness Definition v1 does not attempt to produce a globally optimal five-day allocation in advance.

---

# Fairness Rules

## 1. Assignment history forms a path

Each real assignment contributes to the attendee's accumulated festival experience.

Future decisions should therefore consider previous assignments rather than treating every festival day independently.

For example:

```text
Bad → Medium → Good
```

represents a different fairness context from an attendee with no previous assignments.

The path may span up to the five festival days.

---

## 2. Absence is neutral

Not participating on a festival day does not create either a fairness advantage or disadvantage.

A missing day leaves the existing history and score unchanged. It is not an
inserted Medium experience and does not reset the most recent real assignment.

No artificial assignment needs to be persisted for an absent attendee.

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

---

## 3. A new attendee starts neutral

An attendee without assignment history begins from a neutral fairness state.

They should receive neither priority nor penalty solely because they have not participated before.

A new attendee therefore starts with no accumulated recovery need.

---

## 4. The system seeks favorable assignments, not artificial balance

The system must not deliberately assign an unfavorable location merely to create an opportunity for compensation later.

Fairness should improve the distribution of available opportunities.

It should never manufacture negative experiences.

For example, an attendee with a historically favorable path may still receive a `Good` assignment if:

- favorable capacity is sufficiently available;
- using it does not meaningfully deteriorate the current global fairness state.

Fairness distributes scarcity.

It does not deliberately create harm.

---

## 5. Unfavorable history increases the need for recovery

An attendee with an accumulated unfavorable path should generally have a stronger need for a favorable future experience.

This need is represented by the fairness model. Sufficient recovery need justifies
a `Good` target under the [Target Quality v1 policy](assignment-strategy-v1.md#3-determine-target-quality).

This does not imply an absolute entitlement to a specific Zone or Spot.

---

## 6. Recovery should happen as early as reasonably possible

When an attendee has accumulated unfavorable experiences, the system should prefer opportunities that help restore balance sooner rather than unnecessarily delaying recovery.

This is a priority, not a guarantee.

Recovery remains subject to:

- eligibility;
- physical feasibility;
- current inventory;
- current global assignment state.

---

## 7. Never having received a favorable experience is relevant

When otherwise comparable paths are evaluated, an attendee who has never received a `Good` experience may have additional recovery need.

This factor is complementary.

It should not automatically override a substantially larger historical deficit.

---

## 8. History changes priority but does not create absolute rights

An unfavorable experience does not guarantee that the next assignment will be favorable.

Similarly, favorable previous assignments do not require the system to later assign an unfavorable experience.

History influences the current fairness need. Low or negative recovery need
does not justify intentional degradation, and prior favorable experience never
creates an obligation to assign `Bad`.

It does not dictate the next assignment.

---

## 9. Equivalent fairness states may use deterministic tie-breaking

The fairness model does not need to produce a meaningful distinction between every possible pair of states.

For RotationScore v1, the baseline does not introduce a numerical equivalence tolerance.

Therefore:

```text
higher score
→ greater recovery need

equal score
→ deterministic tie-break when required
```

More advanced equivalence rules may be evaluated later if Stage 5 shows that exact numerical ordering creates undesirable behavior.

---

## 10. Decisions use only the information available at that moment

Stage 4 uses an online assignment model.

At the moment a request is processed, the system may know:

- the request attendees;
- their previous assignments;
- currently available capacity;
- current assignment distribution;
- currently applicable eligibility and organization policies.

The system does not know with certainty:

- who will request later;
- future group compositions;
- final daily demand;
- who will stop participating;
- who will return on later days.

The decision must therefore be sufficiently fair using the information currently available.

Unknown future requests do not participate in the current fairness decision.

---

## 11. Fairness is both individual and global

Fairness has two complementary perspectives.

### Individual or Group Fairness

The current request should receive an assignment consistent with its accumulated history and recovery need.

### Global Fairness

The current decision should avoid unnecessarily deteriorating the distribution of experiences already accumulated across the festival population.

For Stage 4, global fairness is **incremental**.

The system does not globally optimize all future assignments.

Instead:

```text
current individual/group state
        +
current global assignment state
        +
current inventory
        ↓
best reasonable decision now
```

Each completed assignment becomes part of the state used by later requests.

---

## 12. Physical capacity limits achievable fairness

The algorithm cannot create favorable capacity that does not exist.

A poor fairness outcome may therefore result from:

- the assignment strategy;
- venue capacity distribution;
- group constraints;
- eligibility restrictions;
- request arrival order;
- actual attendee participation.

Stage 5 must help distinguish algorithmic limitations from physical or operational limitations.

The desired percentage of balanced paths is therefore a validation target, not a guaranteed outcome.

---

# Group Fairness

## 13. A group is treated as one decision unit

A daily request may contain one or multiple attendees.

For a group request, remaining together is part of the assignment intent.

The group should therefore be evaluated and assigned as a unit.

Group composition may change between festival days.

There is no requirement for a permanent group identity across the festival.

---

## 14. Group fairness is approximated using the average fairness state of its members

For the MVP, the fairness state of a group is derived from the average fairness state of its members.

A member without assignment history contributes a neutral state.

Conceptually:

```text
Member RotationScores
        ↓
average
        ↓
GroupRotationScore
```

This is an intentional simplification.

It allows groups to participate in the same fairness model without introducing a separate complex group algorithm.

---

## 15. Physical feasibility precedes fairness

Fairness only operates among physically valid assignment options.

For a group, an option is not a candidate if it cannot satisfy the complete request.

Relevant constraints include:

- same FestivalDay;
- same Zone;
- same Row;
- sufficient number of Spots;
- contiguous Spots;
- currently available Spots;
- complete group assignment.

A better-quality option that cannot accommodate the complete group is not a valid fairness alternative.

---

## 16. Internal fairness dispersion inside groups is not optimized in the MVP

The MVP does not attempt to optimize the differences between individual fairness states inside the same group.

For example:

```text
Member A = high recovery need
Member B = strongly favored history

Group fairness ≈ average
```

This may hide internal imbalance.

The simplification is intentionally accepted in Stage 4.

Stage 5 should measure whether it creates meaningful fairness problems before a more sophisticated group model is introduced.

---

# Eligibility and Organization Policies

## 17. Fairness operates only over eligible and feasible options

The conceptual sequence is:

```text
Request
   ↓
Organization Policies
   ↓
Eligibility
   ↓
Physical Feasibility
   ↓
Experience Quality
   ↓
Fairness-Aware Selection
```

Fairness must never transform an invalid option into a valid candidate.

Relevant eligibility concepts may include:

- accessibility;
- allowed attendee preferences;
- location-category exclusions;
- organization-defined restrictions.

Only rules concretely required by the MVP should be implemented.

---

## 18. Organization-reserved capacity does not participate in general fairness distribution

Capacity reserved by the organization for operational reasons is removed from the general assignment pool before fairness evaluation.

Examples may include:

- volunteers;
- accessibility-specific capacity;
- operational reservations.

The policy determining what capacity is reserved is separate from the fairness model.

Fairness operates only over capacity currently available to the request.

---

## 19. Attendees are not penalized for excluding an allowed location category

An attendee may legitimately exclude a category of location.

For example:

```text
Front Standing excluded
```

must not imply:

```text
No Front Standing
→ fairness deficit
```

Eligibility limits participating options without redefining their globally
business-defined Experience Quality. Excluding `FrontStanding` leaves
`MiddleLeft` as `Medium` and `MiddleCenter` as `Good`; remaining options are never
promoted or demoted because of the exclusion.

A voluntarily excluded option is not considered a missed favorable opportunity.

---

# Supporting Fairness Concepts

The following concepts help interpret the 19 rules but are **not additional fairness rules**.

---

## Path Direction Matters

The order of experiences can influence current recovery need.

For example:

```text
Good → Medium → Bad
```

and:

```text
Good → Bad → Medium
```

may have the same accumulated balance but different immediate fairness needs.

This observation contributes to the design of `RotationScore`.

---

## Recovery Has Limits

A recent favorable experience can reduce current recovery need without automatically erasing a large accumulated historical deficit.

For example:

```text
Bad → Bad → Bad → Bad → Good
```

should not necessarily be treated as fully recovered.

---

## Avoid Persistent Negative Concentration

A fair system should reduce the occurrence of attendees whose paths contain sustained unfavorable experiences without sufficient recovery.

This is one of the outcomes that Stage 5 should measure.

---

## Target Quality Is Not an Entitlement

The score helps estimate a quality objective; it does not guarantee or cap the
outcome. A better option may be selected when current conditions justify it.
The [strategy](assignment-strategy-v1.md#3-determine-target-quality) owns the
provisional target policy and concrete selection behavior.

## Current Inventory Matters

Inventory is decision context, not part of RotationScore. Its role is to inform
which feasible quality can reasonably be assigned now. The
[inventory model](assignment-strategy-v1.md#9-current-inventory-state) must be
made concrete for the implemented scenarios.

## Global Fairness Is Incremental

Each committed outcome contributes to the state used by later requests.
The [global-state rule](assignment-strategy-v1.md#10-current-global-assignment-state)
will be defined through a concrete Stage 4 scenario; Stage 5 measures the
resulting distribution across five days.

---

# Experience Quality

The MVP uses three conceptual experience-quality levels:

```text
Good
Medium
Bad
```

Zone Experience Quality is globally derived from stable `ZoneType` semantics
through `ZoneExperienceQualityPolicy`, independently from request eligibility.
The agreed [Zone taxonomy and mapping](assignment-strategy-v1.md#agreed-zone-taxonomy-and-global-mapping)
belong to Assignment Strategy; their implementation remains pending.

Fairness uses the actual recorded Experience Quality of previous assignments.
Historical quality remains stable across later business-policy changes: an
assignment recorded when `UpperCenter` was `Medium` remains `Medium` in
`FairnessHistory` and RotationScore, even if a future policy classifies that
ZoneType as `Good`. History must not be reinterpreted automatically.

---

# Path Classification

For Stage 5 analysis, attendee paths may be classified into three conceptual categories.

## Balanced

A path with:

- reasonable overall distribution;
- meaningful access to favorable experiences;
- sufficient recovery from unfavorable periods;
- no persistent negative concentration.

## Acceptable

A path that does not show substantial harm but may lack strong rotation or complete recovery.

For example:

```text
Medium → Medium → Medium → Medium → Medium
```

may be acceptable even though no `Good` experience occurred.

## Not Balanced

A path with:

- significant unfavorable concentration;
- insufficient recovery;
- persistent deterioration;
- or a clear deviation from the intended fairness objective.

Exact thresholds are intentionally not defined in Fairness Definition v1.

Stage 5 should establish provisional measurable criteria before evaluating simulation results.
Stage 4 instead verifies explicit reference scenarios and invariant checks in
its small presimulation. Neither those checks nor a working RotationScore
constitute proof that the final distribution is sufficiently fair.

---

# Stage 5 Measurement Dimensions

Although final classification thresholds are deferred, Stage 5 should at minimum be able to observe:

- number of `Good` experiences;
- number of `Medium` experiences;
- number of `Bad` experiences;
- accumulated historical balance;
- unfavorable streaks;
- whether at least one `Good` experience occurred;
- recovery after unfavorable periods;
- final or recent experience;
- extreme negative paths.

These metrics provide the evidence required to define and calibrate:

```text
Balanced
Acceptable
Not Balanced
```

without introducing arbitrary thresholds during Stage 4.

---

# Separation of Responsibilities

Fairness Definition, RotationScore, Experience Quality, Assignment Strategy, and Path Classification represent different concerns.

## Fairness Definition

Defines **what fair behavior means**.

## RotationScore

Estimates the attendee's **current recovery need**.

## Experience Quality

Describes the globally business-defined quality of a Zone or actual assignment
outcome. Eligibility filters participation without changing that quality.

## Assignment Strategy

Combines:

- RotationScore;
- target quality;
- eligibility;
- feasibility;
- current inventory;
- current global assignment state;

to select a reasonable available assignment.

## Path Classification

Evaluates resulting assignment paths for Stage 5 analysis.

Conceptually:

```text
Fairness Definition
        ↓
RotationScore
        +
Experience Quality
        ↓
Assignment Strategy
        ↓
Assignments
        ↓
Assignment Path
        ↓
Path Classification
```

---

# Online Fairness Boundary

Fairness Definition v1 explicitly accepts the following boundary:

> The system does not reserve favorable capacity for attendees who have not yet requested an assignment.

Unknown future demand cannot participate in the current decision.

Therefore:

```text
known current state
→ decision
```

not:

```text
predicted future demand
→ speculative reservation
```

If Stage 5 or organization feedback shows that this limitation produces unacceptable outcomes, hybrid or batch strategies may be evaluated later.

---

# Lean Evolution Rule

Fairness Definition v1 should not grow by anticipating every possible future operational scenario.

A new fairness rule, signal, or allocation mechanism should be introduced only when a concrete case demonstrates that the existing model is insufficient.

Preferred evolution cycle:

```text
Concrete scenario
      ↓
Observed limitation
      ↓
Smallest justified change
      ↓
Automated validation
      ↓
Simulation
      ↓
Organization feedback when applicable
```

---

# Explicitly Deferred Decisions

Fairness Definition v1 does not define:

- final RotationScore weights;
- final path-classification thresholds;
- advanced recency models;
- batch assignment;
- hybrid batch + online assignment;
- weighted randomness;
- global optimization;
- prediction of future requests;
- reservation strategies for unknown future attendees;
- complex preference ranking;
- optimized fairness dispersion inside groups;
- automatic group restructuring.

These decisions require evidence from implementation, Stage 5 simulation, or organization feedback.