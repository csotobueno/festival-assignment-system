# Stage 4 Trade-offs and Open Questions
## Purpose
This document records the main simplifications, accepted limitations, and unresolved questions of Stage 4.
The objective is to make these decisions explicit instead of hiding them inside the implementation.
Stage 4 intentionally builds a **minimum credible online fairness baseline**.
Some decisions remain provisional until Stage 5 simulation or festival-organization feedback provides measurable evidence.
---
# Guiding Principle
A known limitation is not automatically a reason to increase complexity.
Preferred approach:
```text
identify limitation
      ↓
document it
      ↓
measure its impact
      ↓
increase complexity only if justified
```
---
# Accepted Trade-offs
## 1. Online Assignment Instead of Festival-Wide Optimization
Each daily request is processed using the current known state.
### Accepted Consequence
A later request may reveal that an earlier decision was not globally optimal.
### Why Accepted
The online model preserves daily flexibility and allows attendees to change group configuration between festival days.
A festival-wide assignment would be significantly more rigid.
---
## 2. No Daily Batch in the MVP
The MVP does not require attendees to submit requests before a daily cutoff.
### Accepted Consequence
The system cannot compare all daily requests before allocating scarce capacity.
Request arrival order may affect outcomes.
### Why Accepted
Batch processing introduces:
- cutoff windows;
- delayed confirmation;
- operational exceptions;
- adoption concerns;
- group reconfiguration problems.
These should not be implemented before organization feedback confirms their value.
---
## 3. No Hybrid Batch + Online Flow in the MVP
A future model could use:
```text
main daily batch
+
late online requests
```
but this is not part of Stage 4.
### Accepted Consequence
The baseline sacrifices some global optimization potential.
### Why Accepted
The hybrid option adds operational and technical complexity before the organization has validated that workflow.
---
## 4. Unknown Future Requests Are Ignored
The engine does not reserve capacity for hypothetical future attendees.
### Accepted Consequence
An early request may consume a `Good` location that could have benefited a later high-recovery attendee.
### Why Accepted
Future demand is unknown.
Speculative reservation would require prediction or additional policy.
Stage 5 should measure the impact of this limitation.
---
## 5. Global Fairness Is Incremental
Stage 4 does not calculate a global optimum.
Instead, every successful assignment updates the system state used by subsequent decisions.
### Accepted Consequence
Locally reasonable decisions may still produce a suboptimal final distribution.
### Why Accepted
Incremental fairness is compatible with online assignment and preserves MVP simplicity.
---
## 6. RotationScore Represents Recovery Need Only
RotationScore does not contain:
- inventory pressure;
- global distribution;
- future demand;
- physical feasibility.
### Accepted Consequence
RotationScore alone cannot determine the final assignment.
### Why Accepted
Separating recovery need from system state makes the model easier to understand, test, and evolve.
---
## 7. Target Quality Is a Reference, Not an Entitlement
`RotationScore` or `GroupRotationScore` derives `Good` or `Medium` as a
reasonable target under the [Target Quality v1 policy](assignment-strategy-v1.md#3-determine-target-quality).
`Bad` may be an actual constrained outcome, but is never an intentional v1 target.
### Accepted Consequence
The final assigned quality may be better or worse than the target.
### Why Accepted
Final selection uses Target Quality as a reference while respecting:
- availability;
- eligibility;
- feasibility;
- inventory state;
- global fairness state.
### Target Quality v1 Threshold Decision
Stage 4 selects `score >= 1.00 → Good`, otherwise `Medium`, for both individual
and group scores. The boundary is inclusive.
This is a baseline hypothesis derived from representative fairness histories.
It is not the result of mathematically dividing the theoretical RotationScore
range, and it is not treated as a final business constant.
The analysis first compared concrete histories and their resulting signals:
| Fairness history | H | R | G | RotationScore | Target Quality v1 |
| --- | ---: | ---: | ---: | ---: | --- |
| `[]` | 0 | 0 | 0 | 0.00 | Medium |
| `Good` | -1 | -1 | 0 | -1.50 | Medium |
| `Medium` | 0 | 0 | 1 | 0.25 | Medium |
| `Bad` | 1 | 1 | 1 | 1.75 | Good |
| `Good → Bad` | 0 | 1 | 0 | 0.50 | Medium |
| `Bad → Medium` | 1 | 0 | 1 | 1.25 | Good |
| `Medium → Bad` | 1 | 1 | 1 | 1.75 | Good |
| `Bad → Good` | 0 | -1 | 0 | -0.50 | Medium |
| `Medium → Medium` | 0 | 0 | 1 | 0.25 | Medium |
| `Bad → Bad` | 2 | 1 | 1 | 2.75 | Good |
| `Good → Bad → Bad` | 1 | 1 | 0 | 1.50 | Good |
| `Bad → Bad → Medium` | 2 | 0 | 1 | 2.25 | Good |
| `Medium → Medium → Medium` | 0 | 0 | 1 | 0.25 | Medium |
| `Bad → Bad → Good` | 1 | -1 | 0 | 0.50 | Medium |
| `Good → Good → Medium` | -2 | 0 | 0 | -2.00 | Medium |
| `Good → Bad → Bad → Medium` | 1 | 0 | 0 | 1.00 | Good |
These scenarios are design references rather than exhaustive business rules.
They help explain where the recovery-oriented boundary becomes meaningful.
The analysis then compared three candidate thresholds:
#### Candidate: 0.50
A `0.50` threshold would make the policy more aggressive:
```text
score >= 0.50 → Good
score < 0.50  → Medium
```
This would classify both of the following as `Good` targets:
```text
Good → Bad       = 0.50
Bad → Bad → Good = 0.50
```
The first history has a neutral accumulated balance after one favorable and one
unfavorable experience. The second has just received a favorable recovery.
Treating both as immediately requiring another `Good` target was considered too
aggressive for the v1 baseline.
#### Candidate: 1.25
A `1.25` threshold would make the policy more conservative:
```text
score >= 1.25 → Good
score < 1.25  → Medium
```
Most representative scenarios would behave the same as with `1.00`, but an
important boundary case changes:
```text
Good → Bad → Bad → Medium
RotationScore = 1.00
```
This history still has a positive HistoricalDeficit after two unfavorable
experiences, even though the latest experience is `Medium`.
Classifying it as `Medium` was considered too conservative because meaningful
recovery need remains.
#### Candidate: 1.00
The selected baseline is therefore:
```text
score >= 1.00 → Good
score < 1.00  → Medium
```
It separates several useful reference cases:
```text
Good → Bad                 0.50 → Medium
Bad → Medium               1.25 → Good
Good → Bad → Bad           1.50 → Good
Good → Bad → Bad → Medium  1.00 → Good
```
This preserves two intended behaviors:
- comparatively weak or recently recovered states do not immediately demand
  another `Good` target;
- meaningful accumulated recovery need can still produce a `Good` target even
  when the latest experience is not `Bad`.
The `1.00` threshold is therefore the minimum credible Stage 4 baseline for the
current reference scenarios, not a claim of mathematical optimality.
Its code implementation remains a subsequent task. Stage 5 must measure the
resulting five-day paths, compare observed fairness outcomes, and determine
whether evidence justifies recalibration.
---
## 8. Better-Than-Target Assignments Are Allowed
An attendee with low recovery need may still receive `Good`.
### Accepted Consequence
The final distribution may not strictly reflect theoretical Target Quality.
### Why Accepted
The engine should not manufacture poor experiences or waste favorable capacity solely to enforce a score-derived target.
---
## 9. Simplified Three-Level Experience Quality
Experience quality is represented as:
```text
Good
Medium
Bad
```
### Accepted Consequence
Differences inside each quality category are ignored.
### Why Accepted
The goal is to validate fairness behavior before building a detailed location-ranking system.
---
## 10. ZoneType Is Stable Taxonomy; Policy Derives Global Quality
The agreed model is `Zone.Type → ZoneType → ZoneExperienceQualityPolicy → ExperienceQuality`.
The seven ZoneTypes and their `Good` / `Medium` / `Bad` mapping are defined in
[Assignment Strategy](assignment-strategy-v1.md#agreed-zone-taxonomy-and-global-mapping).
This decision is settled; taxonomy and policy implementation remain pending.
### Accepted Consequence
Every request sees the same quality for a given ZoneType. `RequestEligibility`
changes participation only: excluding `FrontStanding` leaves `MiddleLeft` as
`Medium` and `MiddleCenter` as `Good`. It never promotes or demotes remaining Zones.
### Why Accepted
Stable venue meaning and business classification are separate concerns. Deriving
quality through a policy avoids mutable independent quality state on Zone and
contradictory type/quality combinations. Eligibility cannot redefine that policy.

The pending evolution replaces `Zone.IsFrontStanding` with
`Zone.Type == ZoneType.FrontStanding` and updates `ZoneEligibilityPolicy`;
the dedicated Boolean becomes unnecessary once the taxonomy is implemented.

`ZoneCode` is not required yet because ZoneType supplies sufficient stable
semantics for current rules. Defer it until a concrete external integration,
import/export, organization-provided configuration, UI/API stable business
identifier or external-data mapping requirement appears.
---
## 11. Historical Quality Remains Stable
The actual Experience Quality recorded at assignment time remains the fact used
by FairnessHistory and RotationScore. An assignment recorded when `UpperCenter`
was `Medium` stays `Medium` in history even if a later business policy classifies
that ZoneType as `Good`. Current Zone quality is derived; historical quality is
recorded and must not be reinterpreted automatically.
### Accepted Consequence
Later policy or preference changes do not automatically reinterpret old history.
### Why Accepted
Stable history makes RotationScore deterministic and reproducible.
---
## 12. Simplified Recent Recovery Signal
RotationScore v1 only uses the most recent real assignment for recency.
### Accepted Consequence
More complex path trends may be hidden.
### Why Accepted
A larger recency model should only be introduced after evidence shows that one-step recency is insufficient.
---
## 13. Provisional RotationScore Weights
Current baseline:
```text
RotationScore =
    HistoricalDeficit
  + 0.50 × RecentRecoveryNeed
  + 0.25 × GoodExperienceDeficit
```
### Accepted Consequence
The weights may not maximize final fairness.
### Why Accepted
They provide a concrete and testable starting hypothesis.
Stage 5 will evaluate sensitivity.
---
## 14. No Score-Equivalence Tolerance
Stage 4 does not use:
```text
abs(scoreA - scoreB) <= tolerance
```
### Accepted Consequence
Small score differences remain numerically distinct.
### Why Accepted
Current reference examples do not support a consistent global tolerance.
Stage 5 can determine whether one is actually needed.
---
## 15. Group Fairness Uses the Average Member Score
For the MVP:
```text
GroupRotationScore =
average(MemberRotationScores)
```
### Accepted Consequence
Internal group imbalance may be hidden.
### Why Accepted
It is the simplest model consistent with treating a group as one daily decision unit.
---
## 16. Group Integrity Takes Priority Over Individual Optimization
The assignment engine does not split a group automatically.
### Accepted Consequence
Some members may receive a lower-quality outcome than if they requested individually.
### Why Accepted
The group request represents the intention to remain together.
---
## 17. Infeasible Groups Require User Choice
If no valid complete block exists, the engine does not automatically modify the group.
### Accepted Consequence
The AssignmentRequest is `Rejected` with no Assignments. When contiguous
capacity is missing, the existing reason is `CONTIGUOUS_SPOTS_NOT_AVAILABLE`.
The user's need may remain unmet, but the original request is terminal; a changed
group is submitted as a new request.
### Why Accepted
Changing social-group composition is a user decision, not an algorithmic fairness decision.
The exact interaction flow can be defined later with the organization. The
[existing domain outcome](../domain-blueprint-v1.md#11-contiguous-availability-decision)
is already defined and is preserved.
---
## 18. No Internal Group Dispersion Optimization
The MVP does not calculate variance, minimum member fairness, or maximum member need.
### Accepted Consequence
A group with heterogeneous histories may look neutral on average.
### Why Accepted
Stage 5 should measure whether this creates a meaningful problem before complexity is added.
---
## 19. Request Arrival Order Can Affect Outcomes
Online assignments are final.
### Accepted Consequence
Earlier requests may consume scarce capacity before later high-recovery attendees arrive.
### Why Accepted
Removing this dependency would require batching, reservation, or prediction.
Its impact should first be measured.
---
## 20. Deterministic Strategy Instead of Randomness
The Stage 4 baseline is deterministic.
### Accepted Consequence
Identical states may produce repeated allocation patterns.
### Why Accepted
Determinism improves:
- testing;
- debugging;
- simulation comparison;
- explainability.
Weighted randomness remains a future option.
---
## 21. Sequential Determinism Does Not Guarantee Concurrent Fairness
The assignment engine is deterministic for a given state snapshot.
Concurrent transactions may still race.
### Accepted Consequence
The request with the highest recovery need is not guaranteed to win a simultaneous database race.
### Why Accepted
Stage 3 guarantees integrity and rollback, not fairness ordering between concurrent commits.
Sophisticated serialization should not be added without evidence that it is necessary.
---
## 22. Database Constraints Remain the Final Integrity Protection
Concurrent decisions may evaluate overlapping resources.
### Accepted Consequence
A candidate considered available during evaluation can fail at commit time.
### Why Accepted
Database constraints already protect the global invariants established in Stage 3.
---
## 23. Inventory Model Remains Minimal
The initial inventory state may only track simple quality counts.
Example:
```text
Good remaining
Medium remaining
Bad remaining
```
### Accepted Consequence
The model may not capture every inventory pattern.
### Why Accepted
The first goal is to determine whether inventory awareness materially improves online decisions.
---
## 24. Global Assignment State Remains Minimal
The initial global state may use simple aggregate counts.
### Accepted Consequence
It will not represent every distribution detail.
### Why Accepted
Stage 4 needs enough global context to avoid obvious fairness degradation, not a full optimization model.
---
## 25. Accessibility, Reservations, and Preferences Remain Policy Concepts Until Concrete
The architecture recognizes these concerns.
### Accepted Consequence
Not every conceptual rule will necessarily have a Stage 4 implementation.
### Why Accepted
The organization may refine or replace these policies.
The MVP should not invent operational requirements.
---
# Explicitly Deferred Capabilities
Unless implementation evidence clearly requires them, Stage 4 excludes:
- festival-wide assignment optimization;
- daily batch processing;
- hybrid batch + online processing;
- demand forecasting;
- future-capacity reservation;
- weighted randomness;
- global optimization algorithms;
- dynamic score-weight learning;
- advanced preference ranking;
- group fairness variance optimization;
- automatic group restructuring;
- retrospective reassignment;
- advanced operational dashboards;
- complete user-interaction workflows for infeasible groups.
---
# Open Questions for Stage 5
These questions evaluate the implemented baseline and may motivate later changes.
They do not defer the initial target, quality or selection rules beyond Stage 4;
those rules are resolved at the
[implementation point of use](implementation-plan.md#decisions-at-the-point-of-use).
## Fairness Outcome
1. What percentage of attendees finish with Balanced paths?
2. What percentage finish Acceptable?
3. What percentage finish Not Balanced?
4. How many extreme negative paths occur?
5. How quickly does recovery normally happen?
6. How many attendees never receive a `Good` experience?
---
## RotationScore
7. Are the current weights appropriate?
8. Is HistoricalDeficit too dominant?
9. Is RecentRecoveryNeed strong enough?
10. Is one previous assignment sufficient for recency?
11. Does GoodExperienceDeficit improve outcomes?
12. Would a score-equivalence mechanism improve results?
13. Should RotationScore eventually be normalized?
---
## Target Quality
14. Does the agreed inclusive `1.00` Target Quality threshold produce useful outcomes, or does Stage 5 evidence justify recalibration?
15. Does Target Quality influence assignments too strongly?
16. Does it influence them too weakly?
17. How often is the final quality better than target?
18. How often is it worse?
19. Do better-than-target assignments improve or damage global fairness?
---
## Inventory
20. Does inventory-aware selection improve capacity usage?
21. Does the engine consume too much `Good` capacity early?
22. Does it unnecessarily leave `Good` capacity unused?
23. What inventory metrics are actually useful?
24. Does remaining capacity need normalization against expected demand?
---
## Incremental Global Fairness
25. Are simple aggregate counts sufficient?
26. Does the system produce globally reasonable distributions despite online processing?
27. Do locally fair decisions create undesirable global patterns?
28. What additional global metric, if any, is justified?
---
## Arrival Order
29. How strongly does request order affect final fairness?
30. What happens when high-recovery attendees arrive late?
31. What happens when favored attendees arrive early?
32. How variable are outcomes under different realistic arrival sequences?
33. Does this effect justify batch or hybrid processing?
---
## Groups
34. How does group prevalence affect fairness?
35. How does group size affect achievable quality?
36. How much does averaging hide internal imbalance?
37. Are heterogeneous groups systematically disadvantaged?
38. Does the online model create excessive group infeasibility?
---
## Physical Capacity
39. What fairness level is physically achievable?
40. How much unfairness is algorithmic?
41. How much is caused by the venue configuration?
42. How sensitive are results to the proportion of Good, Medium, and Bad capacity?
---
## Concurrency
43. How frequently do real concurrent conflicts occur?
44. Is rejection/retry behavior operationally acceptable?
45. Is stronger serialization justified by measured usage?
---
# Questions for Festival Organization
Some questions should not be answered by the MVP alone.
Examples include:
- Is immediate assignment confirmation important?
- Would attendees accept a daily request cutoff?
- Would a hybrid batch + online model be operationally acceptable?
- How often do attendees change groups between days?
- What should happen when a requested group cannot fit?
- Should users be allowed to change their group and retry?
- Which areas are reserved?
- Which accessibility rules are required?
- Which attendee preferences should be supported?
- Do some attendees need assisted request registration?
- How should unused reserved capacity be released?
These questions should guide later refinement rather than expand Stage 4 prematurely.
---
# Stage 5 Decision Principle
Stage 5 should not automatically make the algorithm more sophisticated.
For each proposed improvement, ask:
> What measurable failure of the Stage 4 baseline does this change solve?
Preferred process:
```text
Stage 4 baseline
      ↓
Five-day simulation
      ↓
Measured limitation
      ↓
Candidate improvement
      ↓
Comparison against baseline
      ↓
Keep only if materially better
```
---
# Current Baseline Summary
The [Stage 4 overview](README.md#baseline-and-boundaries) summarizes the baseline;
the [implementation plan](implementation-plan.md#stage-4-exit-criteria) owns its
exit criteria. This document preserves the reasons and consequences of the
accepted simplifications, without maintaining another implementation checklist.
---
The purpose of Stage 5 is not to prove that the baseline is perfect.
It is to determine where it is good enough, where its limitations come from, and which improvements are actually justified.