# Stage 5 — Metrics and Evaluation Criteria

## Purpose

Stage 5 evaluates whether the `v0.3.0 — Fair Assignment Engine` baseline is sufficiently fair, reliable, consistent, and performant to justify moving toward a real implementation proposal.

The objective is not to prove that the current algorithm is optimal.

The objective is to gather enough evidence to answer:

> Is the current assignment approach viable under realistic festival conditions, and what risks or adjustments should be considered before moving forward?

The evaluation follows this structure:

```text
Decision Question
        ↓
Evaluation Dimension
        ↓
Scenario
        ↓
Metric
        ↓
Observed Result
        ↓
Acceptance Criterion
        ↓
Decision
```

Stage 5 follows a Lean evidence loop:

```text
baseline
    ↓
simulate
    ↓
measure
    ↓
identify material limitation
    ↓
form hypothesis
    ↓
compare candidate improvement
    ↓
keep only if materially better
```

A known limitation is not automatically a reason to increase complexity.

A possible improvement is not automatically a requirement.

---

# 1. Baseline Under Evaluation

Stage 5 evaluates the Stage 4 production baseline:

```text
v0.3.0 — Fair Assignment Engine
```

Current characteristics:

```text
online
deterministic
known-state only
fairness-aware
group-preserving
eligibility-aware
inventory-aware
physically feasible
historically stable
```

The current decision path is:

```text
historical outcomes
        ↓
FairnessHistory
        ↓
RotationScore
        ↓
GroupRotationScore
        ↓
TargetQuality
        ↓
eligibility + inventory
        ↓
AssignmentSelectionPolicy
        ↓
AssignmentSelection
        ↓
AssignmentEngine
        ↓
persisted Assignment.ExperienceQuality
```

Stage 5 must measure this baseline before modifying it.

This document is the authoritative Stage 5 analytical framework. The
[Stage 4 completion checkpoint](../stage-4/completion.md) remains authoritative
for the implemented baseline and historical closure evidence. The
[fairness definition](../stage-4/fairness-definition-v1.md) retains its 19 rules;
the [critical invariants](../critical-invariants.md) remain mandatory.
No analytical category below changes production policies or persisted statuses.

---

# 2. Decision Gate Dimensions

Stage 5 evaluates five primary dimensions:

```text
Fairness
Performance
Assignment Feasibility
Reliability
Consistency
```

A sixth cross-cutting concern is the behavioral envelope produced under different operating scenarios.

---

# 3. Fairness

## 3.1 Objective

Evaluate whether attendee experience across the FestivalDays is reasonably
balanced, using actual persisted `Assignment.ExperienceQuality` outcomes:
`Good`, `Medium`, and `Bad`. Simulation measures assignment outcomes; perceived
fairness requires stakeholder or participant validation.

## 3.2 Reporting Populations

All populations refer to the same explicitly recorded evaluated period and
scenario. Attendee populations count distinct registered identities, including
every identifiable member of a group request, not just its submitter.

| Population | Definition |
| --- | --- |
| RegisteredAttendees | All attendees registered in the scenario, including those who never request. |
| RequestingAttendees | Attendees included in at least one AttemptedRequest, as defined in section 8. ExcludedInvalidInput attempts and their identifiable attendees are reported separately. |
| ParticipatingAttendees | Attendees with at least one successfully persisted Assignment during the evaluated period. |
| AttendeesWithOnlyRejectedOrFailedAttempts | RequestingAttendees with no successfully persisted Assignment in the period. This reporting population includes known conflicts; its name does not imply a persisted Failed status. |

Report these population counts separately. Do not silently omit attendees who
request but receive no Assignment, or input-only requesters. Record excluded
input counts and reasons even though they do not enter request-rate denominators.

`ParticipatingAttendee` means one member of ParticipatingAttendees. An absence,
rejected request, known conflict or technical failure creates no synthetic
ExperienceQuality outcome and no additional participation.

## 3.3 Evaluated Paths

`EvaluatedPath` is the chronological sequence of persisted
`Assignment.ExperienceQuality` values for one attendee during the evaluated
period. Retain FestivalDay indices with the outcomes for recovery timing.

```text
Day 1: Good
Day 2: absent
Day 3: Medium
Day 4: request rejected
Day 5: Bad

EvaluatedPath = Good → Medium → Bad
PathLength = 3
```

```text
PathLength = number of successfully persisted Assignments in the period
EvaluatedPaths = one non-empty EvaluatedPath per ParticipatingAttendee
ClassifiablePaths = EvaluatedPaths where PathLength >= 2
InsufficientEvidencePaths = EvaluatedPaths where PathLength = 1
PersistedAssignments = all successfully persisted Assignments in the period
```

For the five-FestivalDay horizon, PathLength is 1..5. Short paths are real paths;
do not synthesize missing days. An attendee without an Assignment has no
EvaluatedPath and remains visible in the population report. Initial histories
outside the period can influence production decisions but are not appended to
the evaluated path or its outcome metrics.

## 3.4 Calculation Conventions

Population and category names in calculations denote their counts. Every metric
uses the named denominator for the same scenario, period and reported segment.
Report numerator and denominator counts alongside rates. When any denominator
is zero, report `N/A`, never 0% or 100%. Counts can be zero.

ClassifiablePaths defines eligibility for the three-category fairness gate;
one-outcome paths are reported separately as Insufficient Evidence. An unresolved
rubric boundary case remains in ClassifiablePaths and blocks calculation of the
three path rates and fairness gate until explicitly resolved; never shrink the
denominator to obtain a favorable result.

---

# 4. Fairness Path Classification

## 4.1 PathClassificationRubric v1

PathClassificationRubric v1 is a provisional analytical rubric for Stage 5
evaluation only. It is not a production domain policy, part of RotationScore,
TargetQuality or AssignmentSelectionPolicy, or an additional fairness rule.

Apply the precedence below to each persisted outcome sequence. Record the rubric
version and any boundary-case decisions with scenario results before interpreting
the fairness gate. Later calibration must use a new identified rubric revision;
retain earlier classifications and results with their original version.

Temporal order matters. These paths both contain 2 Good, 1 Medium and 2 Bad:

```text
Bad → Bad → Medium → Good → Good
Good → Good → Medium → Bad → Bad
```

The first shows recovery; the second has an unresolved negative ending, covered
by Step 2 below. Aggregate counts alone cannot classify the experience.

## 4.2 Step 1 — Insufficient Evidence

```text
PathLength < 2 → Insufficient Evidence
```

Among EvaluatedPaths this means PathLength = 1. It is not Balanced, Acceptable or
Not Balanced. An empty sequence does not create an EvaluatedPath; report its
attendee in the population accounting instead.

## 4.3 Step 2 — Not Balanced

For PathLength >= 2, classify as Not Balanced when either condition holds:

1. The final outcome is Bad: an unresolved negative ending.
2. A persistent or recurring negative pattern has no meaningful favorable recovery:
   two consecutive Bad outcomes without a later Good, or multiple Bad episodes
   without any later Good outcome.

For this v1 test, consecutive means adjacent persisted outcomes, not adjacent
calendar days; a Bad episode is a maximal consecutive run of Bad outcomes.
The consecutive-Bad indicator is met if a pair has no Good after that pair.
The recurring-episode indicator is unambiguous when there are at least two
episodes and no Good after the first Bad. Patterns with intervening Good that
remain ambiguous are handled by section 4.6, unless another decisive condition
or reference example applies. Do not infer a new compensation threshold.

Reference examples (G = Good, M = Medium, B = Bad):

| Path | Classification |
| --- | --- |
| G M M B B | Not Balanced |
| M M M M B | Not Balanced |
| M B M B M | Not Balanced |

## 4.4 Step 3 — Acceptable

After Steps 1–2, Acceptable describes meaningful negative exposure with some
recovery or stabilization, insufficient for Balanced. An all-Medium path is also
an explicitly accepted reference despite having no Bad exposure.

The following exact v1 reference sequences resolve that interpretation:

| Path | Classification |
| --- | --- |
| B M M M M | Acceptable |
| G B B G M | Acceptable |
| B B B G G | Acceptable |
| B M B M G | Acceptable |
| M M M M M | Acceptable |
| G G B B G | Acceptable — provisional, calibratable boundary case |

The G G B B G boundary is resolved as Acceptable in v1; a later rubric revision
may change it without rewriting earlier results.

## 4.5 Step 4 — Balanced

After earlier steps, Balanced describes a reasonably balanced complete trajectory
with no persistent unresolved negative pattern; unfavorable outcomes are isolated
or sufficiently recovered. The exact v1 reference sequences are:

| Path | Classification |
| --- | --- |
| G G M G M | Balanced |
| M G M M G | Balanced |
| B G M M G | Balanced |
| G G M B M | Balanced |
| B B M G G | Balanced |
| B M M M G | Balanced |
| G M B M G | Balanced |

## 4.6 Unresolved Boundary Cases

The qualitative descriptions are not permission to invent a numerical balance
threshold, generalize the reference examples by aggregate counts, or default
every remaining path to Acceptable or Balanced. If Steps 1–2 and the exact
references do not decide a path, retain it as an unresolved versioned boundary
case rather than silently inventing a business principle.

For each such sequence, record the exact path, occurrence count, rubric version,
reason for ambiguity, and any explicit later decision and its revision. This is
an evaluation record, not a fourth fairness category or a production abstraction.
This procedure also applies to unlisted paths of length 2..4.

Report known classification counts and unresolved sequences. Do not exclude an
unresolved ClassifiablePath, count it as Insufficient Evidence, or publish a
partial three-category rate as the completed result. Until every ClassifiablePath
has a recorded classification, report those rates and the fairness gate as
`N/A — unresolved rubric boundary cases`. The rubric intentionally preserves
unresolved judgments instead of claiming exhaustive business calibration.

---

# 5. Fairness Principles Relevant to Stage 5

Stage 5 does not redefine the Stage 4 fairness baseline.

The evaluation should preserve these relevant principles:

```text
Bad increases recovery need.

Recovery should happen as early as reasonably possible.

Bad may be an actual constrained outcome
but is never an intentional TargetQuality.

Better-than-target assignments are allowed.

Fairness history contains real recorded outcomes.

Historical ExperienceQuality remains stable.

RotationScore represents recovery need,
not the complete placement decision.

Groups remain one daily decision unit.

Physical feasibility takes precedence over fairness preference.

Request arrival order may affect outcomes.

Unknown future requests are ignored.
```

Stage 5 determines whether the resulting behavior is good enough under realistic conditions.

---

# 6. Core Fairness Metrics v1

Only the minimum metrics needed for the Decision Gate are core. Secondary
metrics remain diagnostic unless evidence justifies their promotion.
The population and N/A conventions in section 3 apply to every calculation.

## 6.1 BalancedPathRate

What percentage of ClassifiablePaths are Balanced under the recorded rubric?

```text
BalancedPathRate = Balanced paths / ClassifiablePaths × 100
```

Higher values indicate more favorable or sufficiently recovered trajectories.
Report separately from AcceptablePathRate.

## 6.2 AcceptablePathRate

What percentage of ClassifiablePaths are not fully balanced but remain tolerable?

```text
AcceptablePathRate = Acceptable paths / ClassifiablePaths × 100
```

## 6.3 NotBalancedPathRate

What percentage of ClassifiablePaths have a materially unfavorable trajectory?

```text
NotBalancedPathRate = Not Balanced paths / ClassifiablePaths × 100

BalancedPathRate + AcceptablePathRate + NotBalancedPathRate = 100%
```

The identity applies when ClassifiablePaths is non-empty and fully classified.
Zero population or unresolved rubric boundaries produce N/A, as defined above.
Short paths and unsuccessfully requesting attendees remain separately visible.

## 6.4 Initial Fairness Gate

```text
BalancedPathRate + AcceptablePathRate >= 85%
```

Equivalently, NotBalancedPathRate <= 15%, when the path rates are evaluable.

Status: **Initial evaluation hypothesis — Requires stakeholder calibration**.
This is not a validated organizational requirement. Report all three categories
separately. N/A does not satisfy or fail the quantitative hypothesis.

## 6.5 RecoveryRate

After receiving Bad, how often does an attendee later recover to Medium or Good?

```text
RecoverableBadEvents = persisted Bad events with at least one later
                       successfully persisted Assignment for that attendee
                       within the evaluated period

RecoveredBadEvents = RecoverableBadEvents followed by a later Medium or Good

RecoveryRate = RecoveredBadEvents / RecoverableBadEvents × 100
```

A later participation opportunity means later participation as defined in
section 3, including a later Bad Assignment. Later absence, rejection, conflict
or technical failure alone creates no recovery opportunity. Each Bad event is
counted independently: in B B M, both Bad events recover at M. This event-based
rate measures transition to Medium/Good, not complete path compensation.

A Bad on the attendee's final participation day is not failed recovery if there
is no later participation opportunity, even if the festival continues. Final-path
metrics retain that outcome. If RecoverableBadEvents = 0, RecoveryRate = N/A.

## 6.6 DaysToRecovery

For each RecoveredBadEvent:

```text
DaysToRecovery = FestivalDay index of first later Medium/Good
                 - FestivalDay index of Bad
```

Use chronological FestivalDay indices across the scenario, not compressed path
positions. An absent day adds no history but remains part of elapsed time.
For each RecoverableBadEvent, report exactly one exclusive bucket:

```text
Recovery in 1 day
Recovery in 2 days
Recovery in 3 days
Recovery in 4 days
No recovery
```

These buckets cover the five-day horizon and their counts sum to
RecoverableBadEvents. If rates are reported, each bucket uses RecoverableBadEvents
as denominator; with none, rates are N/A. No recovery has no numeric duration.
Later median/p90 diagnostics would use RecoveredBadEvents only, with unrecovered
events still reported separately. Longer-horizon scenarios must extend the
exclusive day buckets explicitly.

## 6.7 NeverGoodRate

```text
NeverGoodRate = ParticipatingAttendees with no Good in EvaluatedPath
                / ParticipatingAttendees × 100
```

Includes one-outcome paths. NeverGoodRate is not NotBalancedPathRate: M M M M M
and B M M M M are Acceptable reference paths without any Good.

## 6.8 EndsInBadRate

```text
EndsInBadRate = EvaluatedPaths whose final persisted outcome is Bad
                / EvaluatedPaths × 100
```

Includes one-outcome paths as an output diagnostic without classifying them as
Not Balanced. The ending is the attendee's last persisted outcome in the period,
not necessarily the last FestivalDay. Examples include M M M M B and G M M B B.

## 6.9 Good / Medium / Bad Outcome Distribution

Track actual persisted assignment quality, counting individual Assignments,
not group requests:

```text
GoodOutcomeRate = Good Assignments / PersistedAssignments × 100
MediumOutcomeRate = Medium Assignments / PersistedAssignments × 100
BadOutcomeRate = Bad Assignments / PersistedAssignments × 100
```

Report per FestivalDay, cumulative across the festival, and per scenario, using
PersistedAssignments for that scope. These metrics describe output; alone they
do not determine attendee fairness.

## 6.10 Insufficient Evidence Reporting

```text
InsufficientEvidencePathCount = InsufficientEvidencePaths
InsufficientEvidencePathRate = InsufficientEvidencePaths
                               / EvaluatedPaths × 100
```

EvaluatedPaths has one entry per ParticipatingAttendee. Report the count and rate
alongside ClassifiablePaths and any unresolved rubric cases, never inside the
three-category fairness-rate denominator. No EvaluatedPaths means the rate is N/A.

---

# 7. Secondary Fairness Metrics

## Evaluation Coverage for Existing Stage 4 Questions

Supporting observations may explain core results without becoming mandatory
Decision Gate metrics:

| Stage 4 handoff question | Supporting evaluation coverage |
| --- | --- |
| Is Good capacity consumed too early or unnecessarily left unused? | Simulation checkpoints of Good / Medium / Bad capacity remaining per FestivalDay, Good capacity consumed over request-order segments, and unused Good capacity at end of day. |
| Does GroupRotationScore averaging hide meaningful individual imbalance? | Inspect existing attendee-level paths in group scenarios when needed: NotBalancedPathRate for attendees assigned in groups, NeverGoodRate by group-size range, and individual paths inside heterogeneous groups. |

Use the same named denominators restricted to each explicitly recorded cohort;
retain short-path and unresolved-boundary reporting within those cohorts.
Record group membership per day rather than assuming permanent groups.
GroupSizeSuccessRate alone does not answer the fairness question. Inventory
checkpoints and these breakdowns remain diagnostics unless measured evidence
justifies promotion. They add no production global state, group variance
optimization or assignment policy.

## Candidate Diagnostic Metrics

Do not require these for the first simulation unless core results reveal a need for deeper analysis.

Candidate diagnostic metrics:

```text
BadExposureRate
MultipleBadExposureRate
ConsecutiveBadRate
ImmediateRecoveryRate
StrongRecoveryRate
PartialRecoveryRate
PersistentNegativePathRate
```

Working principle:

```text
core metric
→ detect problem

secondary metric
→ explain problem
```

---

# 8. Assignment Feasibility

## 8.1 Objective

Measure normal assignment success and domain rejection separately from known
concurrency conflicts and unexpected technical failures.

## 8.2 Analytical Request Outcomes

Every evaluated execution attempt belongs to exactly one analytical category,
independently of persisted AssignmentRequest.Status:

| Analytical outcome | Evidence / meaning |
| --- | --- |
| Successful | AssignmentRequest persisted as Completed and the complete Assignments persisted. |
| DomainRejected | AssignmentRequest persisted as Rejected with zero Assignments, e.g. no feasible contiguous block or incomplete attendee resolution rejected by the use case. |
| KnownConcurrencyConflict | A valid decision was reached, commit lost a known concurrent persistence race, and the transaction rolled back. No Rejected request or Assignments from the losing attempt are persisted; a structured operational log and simulation attempt record trace it. |
| UnexpectedTechnicalFailure | An unexpected exception, timeout or persistence/infrastructure failure; recognized concurrency conflicts are excluded. |
| ExcludedInvalidInput | Basic input/precondition validation prevents the attempt from reaching the assignment evaluation being measured. |

Persisted Rejected outcomes remain DomainRejected even when their reason concerns
attendee resolution. ExcludedInvalidInput must not hide valid domain rejections
or technical errors inside the measured path. Record its exclusion reason and
the validation boundary. A known conflict is recognized by the existing
Application conflict contract, not inferred from an arbitrary exception.

Use persistence evidence, returned outcomes and captured exceptions to reconcile
attempt records. Invariant violations are a separate correctness check regardless
of analytical outcome; they must never be hidden by request-rate classification.
No new request status, persistence model, retry or logging implementation is
introduced by these definitions.

## 8.3 Shared Request Denominators

Category names denote execution counts, not attendee or Assignment counts.

```text
AttemptedRequests = Successful + DomainRejected
                    + KnownConcurrencyConflict + UnexpectedTechnicalFailure

NormallyProcessedRequests = Successful + DomainRejected

All evaluated input/execution records = AttemptedRequests + ExcludedInvalidInput
```

Use each denominator consistently in the same scenario, period or segment.
Report all five category counts, including exclusions, so normally processed
rates cannot conceal conflict or technical-failure volume. A zero denominator
always produces N/A. There are currently no automatic retries; each actual
execution is recorded once.

---

# 9. Core Assignment Feasibility Metrics v1

The analytical categories and denominators in section 8 apply throughout.

## 9.1 AssignmentSuccessRate

```text
AssignmentSuccessRate = Successful / NormallyProcessedRequests × 100
```

Measures success among normally processed requests. A low value alone does not
prove an algorithmic defect; examine rejection causes and other outcome counts.

## 9.2 AssignmentRejectionRate

```text
AssignmentRejectionRate = DomainRejected / NormallyProcessedRequests × 100

AssignmentSuccessRate + AssignmentRejectionRate = 100%
```

The identity applies only to non-empty NormallyProcessedRequests. Conflicts and
technical failures are excluded from this denominator and reported separately;
do not impose the identity over AttemptedRequests.

## 9.3 RejectionReasonDistribution

```text
RejectionReasonRate(reason) = DomainRejected requests with that production reason
                             / DomainRejected × 100
```

Use one existing terminal rejection reason per DomainRejected request. Known
conflicts, technical failures and excluded input do not belong to this
distribution. Existing outcomes may not distinguish every physical cause;
the evaluation may additionally derive a diagnosis from observed state, without
introducing production rejection codes.

## 9.4 ContiguityRejectionRate

```text
ContiguityRejectionRate = DomainRejected requests with no complete eligible
                          contiguous block at evaluation
                          / NormallyProcessedRequests × 100
```

Use the existing NO_CONTIGUOUS_SPOTS_AVAILABLE outcome and the observed evaluation
state. This includes insufficient eligible capacity as well as fragmentation;
it is particularly relevant for groups.

## 9.5 RawCapacityButNoFeasibleBlockRate

Raw capacity for this diagnosis is the **eligible available raw Spot count at
the assignment evaluation snapshot**, excluding unavailable, reserved/out-of-pool,
and request-ineligible Spots. It is not venue-wide physical capacity.

```text
EligibleAvailableRawSpotCount < GroupSize
→ eligible capacity limitation

EligibleAvailableRawSpotCount >= GroupSize, no complete valid contiguous block
→ fragmentation / physical feasibility limitation

RawCapacityButNoFeasibleBlockRate = DomainRejected requests in the second case
                                   / NormallyProcessedRequests × 100
```

For example, GroupSize = 4, six eligible available Spots and no valid block
larger than three illustrates the second case. A valid block must satisfy the
existing same-Zone, same-Row, consecutive-position and complete-group rules.
Spots spread across incompatible Rows/Zones are not a complete block.

For no-block rejections with a captured evaluation snapshot, these two diagnostic
causes are mutually exclusive. Non-physical rejections retain their existing
reason; missing observations are reported as unavailable, not guessed.
If any no-block rejection lacks the diagnostic state needed to decide its case,
report RawCapacityButNoFeasibleBlockRate as N/A with the missing-observation count;
do not silently count an unknown case as zero or reduce NormallyProcessedRequests.
Fragmentation alone is not proof of an algorithm defect.

Capture the state supplied to that evaluation, not a fresh post-rejection read.
The current baseline does not guarantee transactionally consistent pre-save
reads; record that limitation and any observed contention. This diagnostic
changes neither production RemainingInventoryState nor its counting scope.

## 9.6 GroupSizeSuccessRate

```text
GroupSizeSuccessRate(size) = Successful requests of that GroupSize
                             / NormallyProcessedRequests of that GroupSize × 100
```

Report sizes 1..10 or explicitly recorded ranges. Use the same segment for both
counts and N/A for empty segments. This measures feasibility, not the individual
fairness hidden by group averaging.

---

# 10. Secondary Feasibility Metrics

Add only when core metrics justify deeper investigation.

Candidate metrics:

```text
rejection rate by Zone

rejection rate by eligibility profile

rejection rate by arrival-order segment

fragmentation evolution after each request

largest contiguous run distribution

unused capacity by Row

request success by GroupRotationScore
```

---

# 11. Reliability

## 11.1 Objective

Evaluate whether valid requests can be processed under normal and heavy workloads without unexpected technical failures.

Expected domain rejections and KnownConcurrencyConflict are not technical errors.
Use the analytical categories in section 8; a failure need not persist a request
to enter AttemptedRequests. A zero denominator produces N/A.

---

## 11.2 TechnicalErrorRate

### Question

What percentage of AttemptedRequests end in UnexpectedTechnicalFailure?

Possible technical failures include:

```text
unhandled exceptions

unexpected persistence failures

timeouts

deadlocks

unexpected transaction failures

infrastructure failures
```

### Calculation

```text
TechnicalErrorRate = UnexpectedTechnicalFailure / AttemptedRequests × 100
```

### Classification

```text
Engineering baseline
```

Initial expectation:

```text
as close to 0% as reasonably achievable
```

The final acceptable threshold is not yet defined.

Any non-zero result must be categorized and investigated before reaching a Decision Gate conclusion.

---

# 12. Consistency

## 12.1 Objective

Verify that critical domain invariants remain protected under sequential execution, high load, and concurrency.

The critical invariants remain:

```text
1. A Spot cannot be assigned more than once
   for the same FestivalDay.

2. An Attendee cannot receive more than one Spot
   for the same FestivalDay.

3. An AssignmentGroup cannot be partially persisted.
```

These are correctness requirements, not optimization goals.

---

## 12.2 CriticalInvariantViolationCount

### Calculation

```text
CriticalInvariantViolationCount =
    total confirmed persisted invariant violations
```

### Acceptance Criterion

```text
CriticalInvariantViolationCount = 0
```

### Classification

```text
Hard Technical Criterion
```

Any confirmed value greater than zero blocks progression with the current implementation.

---

## 12.3 DuplicateSpotViolationCount

Count persisted combinations:

```text
FestivalDayId + SpotId
```

with more than one successful Assignment.

Criterion:

```text
DuplicateSpotViolationCount = 0
```

---

## 12.4 DuplicateAttendeeViolationCount

Count persisted combinations:

```text
FestivalDayId + AttendeeId
```

with more than one successful Assignment.

Criterion:

```text
DuplicateAttendeeViolationCount = 0
```

---

## 12.5 PartialGroupViolationCount

For a completed group request:

```text
requested group size
=
persisted assignment count
```

For a rejected request:

```text
persisted assignments = 0
```

Criterion:

```text
PartialGroupViolationCount = 0
```

---

# 13. Concurrency

## 13.1 ConcurrencyConflictRate

How often do attempts lose a known concurrent persistence race?

```text
ConcurrencyConflictRate = KnownConcurrencyConflict / AttemptedRequests × 100
```

The denominator is all AttemptedRequests in the reported scenario/segment,
including sequential attempts if present, not an undefined concurrent-only
subset. Record numeric workload concurrency so comparisons remain interpretable.
Use N/A when there are no attempts.

A safely rolled-back conflict is an operational limitation, not an invariant
violation or UnexpectedTechnicalFailure. Inconsistent persisted state is a
correctness failure regardless of how requests were classified.

## 13.2 Safe Conflict Handling

Preserve the [existing concurrency contract](../stages/stage-3-closure.md#concurrency-model):

```text
KnownConcurrencyConflict
→ complete rollback of the losing graph
→ no persisted Rejected request or losing Assignments
→ no retry
→ no invariant violation
```

Persisted invariant violations must remain zero when conflicts occur. Report
whether conflicts were observed and correctness checks passed; a conflict-free
run supplies no evidence of exercised conflict handling. Do not add
SafeConflictHandlingRate to the core metrics.

## 13.3 Traceability Without New Persistence

Later Stage 5 implementation should correlate structured operational logs with
simulation attempt records, including attempts whose graph rolled back. Minimum
context: CorrelationId / RequestId, FestivalDayId, GroupSize, analytical outcome,
conflict/error type, duration, and timestamp. Keep these records distinct from
successfully persisted business requests and historical ExperienceQuality.

This describes expected evaluation traceability, not logging already implemented
by this documentation change. Do not add a request status, audit aggregate,
AssignmentRequestAttempt persistence, retries, locks or stronger serialization.
Those remain future options only if evidence justifies them.

---

# 14. Secondary Reliability / Concurrency Metrics

Add only if core results reveal a material issue.

Candidates:

```text
failure rate by exception type

deadlock count

timeout count

persistence conflict distribution

retry count

retry success rate

conflict rate by GroupSize

conflict rate by concurrency level
```

Do not introduce retries or stronger serialization merely to improve simulation results.

---

# 15. Performance

## 15.1 Objective

Evaluate the assignment-processing path for an online assignment experience:
Application processing, Domain decision and persistence through the terminal
analytical outcome. This is an engine/use-case benchmark, not complete API,
browser or network user latency.

Scenario setup and data generation are excluded. Warm-up is explicitly recorded
and excluded from steady-state measurements unless a scenario intentionally
measures cold start. Queueing outside the assignment execution path, browser
rendering, network/CDN latency, frontend work and unrelated external systems are
excluded. Database work and waits within the measured execution remain included.

Record the benchmark runtime/build configuration, machine resources, database
version/configuration, placement of benchmark and database, warm-up policy and
workload/concurrency configuration. Report cold-start runs separately.

---

# 16. Core Performance Metrics v1

## 16.1 AssignmentProcessingTime

```text
start: entry into the assignment-processing use-case execution being evaluated
end: terminal analytical outcome
unit: milliseconds
```

Terminal outcomes are Successful, DomainRejected, KnownConcurrencyConflict and
UnexpectedTechnicalFailure. Timing stops when the completion/rejection or
exception outcome is observed; it does not require a persisted request for
conflicts and failures. Use a monotonic elapsed-time measurement. ExcludedInvalidInput
does not enter benchmark summaries. AttemptedRequests is the common measured
population for the latency metrics; report outcome counts so fast rejection or
failure cannot be mistaken for successful performance.

## 16.2 MedianAssignmentLatency

MedianAssignmentLatency = p50 of AssignmentProcessingTime over AttemptedRequests.
It represents typical execution behavior. Report N/A when AttemptedRequests = 0.

## 16.3 P95AssignmentLatency

P95AssignmentLatency = p95 of AssignmentProcessingTime over AttemptedRequests.
For reproducible percentiles, sort durations ascending and use the nearest-rank
observation at ceil(p × AttemptedRequests), with p = 0.50 or 0.95. Empty samples
produce N/A; record the sample count.

Initial Expected / Baseline hypothesis:

```text
P95AssignmentLatency < 1 second
```

Classification: **Engineering acceptance hypothesis**. Isolated outliers should
be investigated; the hypothesis does not require every request below one second.
It does not override reliability or invariant criteria.

## 16.4 MeanAssignmentLatency

```text
MeanAssignmentLatency = sum(AssignmentProcessingTime for AttemptedRequests)
                        / AttemptedRequests
```

Useful for scenario comparison; never use it alone for performance acceptance.

## 16.5 AssignmentThroughput

```text
AssignmentThroughput = AttemptedRequests / TotalScenarioProcessingTime
unit: requests / second (elapsed time converted to seconds)
```

Use the same measured workload for numerator and time interval. This is terminal
attempt throughput, not successful Assignments per second. Count each execution
once, even when it produces multiple Assignments. Report N/A for zero elapsed
time. Latency remains the primary online-experience measure.

## 16.6 TotalScenarioProcessingTime

```text
TotalScenarioProcessingTime = last measured terminal-outcome timestamp
                              - first measured use-case-entry timestamp
```

Use monotonic timestamps for the measured workload. The interval includes overlap
and any inter-request scheduling gaps within that workload; never sum overlapping
request durations as the throughput denominator. Record arrival scheduling so
intentional idle time is interpretable. Setup, generation and warm-up occur
outside this interval. With no AttemptedRequests, report N/A for the interval
and throughput. Report the FestivalDays included, whether one day or the full
five-day scenario.

---

# 17. Secondary Performance Metrics

Add only when additional diagnostic information is required:

```text
P99AssignmentLatency

MaximumAssignmentLatency

latency by GroupSize

latency by inventory size

latency by fragmentation

database query duration

database query count

latency by concurrency level
```

Do not optimize before the baseline identifies a material bottleneck.

---

# 18. Initial Evaluation Hypotheses

Stage 5 begins with the following working hypotheses.

They are not all final business requirements.

---

## 18.1 Fairness

Use the [initial fairness gate](#64-initial-fairness-gate), including its
ClassifiablePaths denominator, rubric-version requirements and N/A conditions.
Its status remains **Initial evaluation hypothesis — Requires stakeholder calibration**.

---

## 18.2 Performance

Use the [P95 hypothesis](#163-p95assignmentlatency) for the Expected / Baseline
scenario and the measurement boundaries in sections 15–16.
Its status remains **Engineering acceptance hypothesis**.

---

## 18.3 Consistency

```text
CriticalInvariantViolationCount = 0

DuplicateSpotViolationCount = 0

DuplicateAttendeeViolationCount = 0

PartialGroupViolationCount = 0
```

Status:

```text
Hard Technical Criterion
```

---

## 18.4 Assignment Rejection

No final acceptable rejection threshold is currently established.

Status:

```text
Engineering baseline
+
Requires stakeholder validation
```

---

## 18.5 Technical Errors

No final acceptable production error rate is currently established.

Initial expectation:

```text
TechnicalErrorRate should remain as close to 0% as reasonably achievable.
```

Status:

```text
Engineering baseline
```

---

# 19. Scenario Envelope

A single average simulation is insufficient to evaluate production behavior.

Stage 5 should establish a behavioral envelope:

```text
Best Plausible Case
        ↓
Expected / Baseline Case
        ↓
Stress Case
        ↓
Worst Plausible Case
```

The worst scenario must remain plausible.

Artificially impossible conditions should not be used to judge production viability.

---

# 20. Scenario Variables

Each scenario must explicitly define the relevant variables.

---

## 20.1 Population Size and Physical Capacity

Record registered attendee population separately from venue Spot capacity:

```text
Expected physical-capacity reference → approximately 5,000 people
Growth / oversubscription variant → approximately 5,500 registered attendees
```

5,500 is not confirmed current physical venue capacity or a mandatory Expected
registered population. Actual request demand depends on daily participation.
The Expected manifest must record its registered population estimate and exact
available Spot topology separately. Real population and participation estimates
require stakeholder validation. Future venue changes must update capacity and
population explicitly. Smaller deterministic populations may support harness
development without being presented as realistic venue evidence.

---

## 20.2 Festival Duration

Primary evaluation horizon:

```text
5 FestivalDays
```

Historical outcomes from earlier days must influence later assignment decisions.

---

## 20.3 Participation Rate

Define:

```text
distinct registered attendees in AttemptedRequests for the FestivalDay
/ RegisteredAttendees for that FestivalDay × 100
```

This is request participation, distinct from successfully assigned
ParticipatingAttendees. Count all group members once per day, even with multiple
attempts; excluded input remains separately reported. A zero RegisteredAttendees
denominator produces N/A. Expected real values require stakeholder validation.

---

## 20.4 Group Size Distribution

Requests contain:

```text
1 to 10 attendees
```

Scenarios should define the proportion of:

```text
individual / small groups
medium groups
large groups
```

The real festival distribution is currently unknown.

---

## 20.5 Eligibility Distribution

Scenarios should contain both unrestricted and restricted requests.

Evaluate effects on:

```text
assignment success
fairness
capacity consumption
```

---

## 20.6 Venue Utilization

Conceptual levels:

```text
Comfortable

Expected

High

Near Capacity
```

---

## 20.7 Physical Fragmentation

Possible levels:

```text
Low
→ long contiguous runs

Moderate
→ mixed block lengths

High
→ many short or isolated runs
```

Raw capacity and fragmentation should be independently controllable where practical.

---

## 20.8 Quality Capacity Distribution

Define relative availability of:

```text
Good
Medium
Bad
```

This helps separate:

```text
algorithmic fairness limitation
vs.
venue configuration limitation
```

---

## 20.9 Request Arrival Order

The same population should support deterministic variants such as:

```text
baseline / neutral order

favorable order

adverse order
```

An adverse variant may allow lower-recovery attendees to consume favorable inventory before higher-recovery attendees arrive.

Randomized orders must use explicit reproducible seeds.

---

## 20.10 Concurrency Level

Possible levels:

```text
Sequential

Normal concurrent load

High concurrent load
```

Concurrency must be varied independently from total request volume. Each manifest
must replace qualitative labels with numeric worker/in-flight limits and the
arrival schedule; record any deliberately controlled overlap.

---

# 21. Best Plausible Scenario

## Objective

Establish the upper behavioral bound under favorable but realistic conditions.

Initial characteristics:

```text
Population
→ realistic

Participation
→ moderate

Group sizes
→ mostly individual / small groups

Eligibility constraints
→ low

Venue utilization
→ comfortable

Fragmentation
→ low

Good / Medium capacity
→ favorable

Arrival order
→ balanced

Concurrency
→ low
```

Expected behavior:

```text
very high AssignmentSuccessRate

low rejection

high BalancedPathRate

low NotBalancedPathRate

rapid recovery

low latency

very low TechnicalErrorRate

zero invariant violations
```

This scenario must remain operationally plausible rather than artificially perfect.

---

# 22. Expected / Baseline Scenario

## Objective

Represent the most credible expected production behavior.

This is the primary Decision Gate scenario.

Initial characteristics:

```text
Registered population
→ explicitly recorded best-known organization estimate

Physical venue capacity
→ approximately 5,000 people; exact Spot topology recorded separately

Festival duration
→ 5 days

Participation
→ expected organization estimate

Group sizes
→ representative mixed distribution

Eligibility
→ representative distribution

Venue utilization
→ expected utilization

Fragmentation
→ naturally produced by assignments

Quality capacity
→ actual or best-known venue distribution

Arrival order
→ deterministic realistic sequence

Concurrency
→ normal expected load
```

Apply the [initial evaluation hypotheses](#18-initial-evaluation-hypotheses)
with their declared populations and benchmark boundaries. Invariant preservation
is a hard criterion, not a provisional hypothesis. Unknown operational rejection
and error thresholds must be identified rather than invented; acceptable rejection
requires stakeholder validation.

---

# 23. Stress Scenario

## Objective

Measure controlled degradation under difficult but credible operating pressure.

Initial characteristics:

```text
Participation
→ high

Group sizes
→ increased medium / large groups

Eligibility restrictions
→ increased

Venue utilization
→ high

Fragmentation
→ elevated

Good capacity
→ more constrained

Arrival order
→ less favorable

Concurrency
→ high
```

Questions:

```text
How much does rejection increase?

How much does NotBalancedPathRate increase?

Does recovery become materially worse?

Does latency remain operationally acceptable?

Do technical errors increase?

Are invariants still preserved?
```

Worse results than baseline are expected.

The main concern is whether degradation remains controlled and understandable.

---

# 24. Worst Plausible Scenario

## Objective

Identify the practical operating boundary of the current assignment strategy.

Initial characteristics:

```text
Participation
→ very high

Group sizes
→ unfavorable concentration of large groups

Eligibility
→ significant restrictions

Venue utilization
→ near capacity

Fragmentation
→ high

Good / Medium capacity
→ scarce relative to demand

Arrival order
→ adverse

Concurrency
→ very high but realistic
```

Questions:

```text
Does the system degrade gracefully?

Which limitation dominates first?

How high does rejection become?

How much does fairness deteriorate?

Does latency exceed the expected online threshold?

Do concurrency conflicts become operationally problematic?

Does any invariant fail?
```

---

# 25. Focused Scenario Variants

Macro scenarios answer:

```text
How does the complete system behave?
```

Focused variants answer:

```text
Why does behavior change?
```

Do not modify every scenario variable simultaneously when investigating a specific effect.

Initial focused variants:

```text
Baseline
vs.
Baseline + adverse arrival order

Baseline
vs.
Baseline + high large-group prevalence

Baseline
vs.
Baseline + high fragmentation

Baseline
vs.
Baseline + higher concurrency
```

This supports causal interpretation.

---

# 26. Initial Scenario Set v1

The first complete Stage 5 evaluation should include:

```text
S1 — Best Plausible

S2 — Expected Baseline

S3 — Stress

S4 — Worst Plausible

S5 — Baseline with adverse arrival order

S6 — Baseline with high large-group prevalence

S7 — Baseline with high fragmentation

S8 — Baseline with higher concurrency
```

The approximately 5,500-registered-attendee growth / oversubscription variant
is an explicitly identified population sensitivity of the Expected baseline;
keep physical capacity and other inputs fixed where practical. It is not a new
confirmed venue capacity. Do not otherwise expand the set without evidence.

---

# 27. Reproducibility Requirements

A seed alone is insufficient. Require an exact scenario manifest or a versioned
deterministic generation procedure with all inputs needed to reconstruct:

- scenario identifier and simulation seed;
- repository / simulation revision and generation procedure version;
- registered population size and daily participation (exact members or generation rules);
- FestivalDay configuration and evaluated period;
- physical capacity and Zone / Row / Spot topology, including unavailable or reserved capacity;
- quality-capacity distribution and applicable classification configuration;
- initial fairness histories, including explicit empty-history initialization;
- daily group memberships and group-size distribution;
- request eligibility assignment and its distribution;
- deterministic attendee / Zone / Spot identity generation and relevant ordering timestamps;
- arrival-order generation strategy, schedule and recorded random seeds;
- numeric concurrency configuration and any explicitly controlled race coordination.

Preserve or regenerate the actual workload, not only its marginal distributions.
Deterministic Zone identities matter because ZoneId is an exact-tie ordering key.
Record PathClassificationRubric version and boundary-case resolutions with the
evaluation configuration, plus benchmark environment and warm-up/cold-start
treatment for performance runs.

Sequential deterministic scenarios should reproduce outcomes from the same
manifest/procedure, revision and initial state. Concurrent scenarios reproduce
configuration and workload, but seeds do not guarantee identical commit winners.
Record observed outcomes; claim fixed winner order only if the harness explicitly
controls it. This adds no serialization mechanism to production.

---

# 28. Common Scenario Result Structure

Each scenario should produce the same core summary. Report the evaluated period,
scenario/repository/rubric revisions, named denominator counts and exclusions.
Include RegisteredAttendees, RequestingAttendees, ParticipatingAttendees,
AttendeesWithOnlyRejectedOrFailedAttempts, EvaluatedPaths, ClassifiablePaths,
unresolved boundary sequences/counts, and all five analytical attempt counts.
Preserve N/A results and their reason; never replace them with favorable zeros.

## Fairness

```text
InsufficientEvidencePathCount
InsufficientEvidencePathRate

BalancedPathRate
AcceptablePathRate
NotBalancedPathRate

RecoveryRate
DaysToRecovery

NeverGoodRate
EndsInBadRate

GoodOutcomeRate
MediumOutcomeRate
BadOutcomeRate
```

## Feasibility

```text
AssignmentSuccessRate
AssignmentRejectionRate
RejectionReasonDistribution

ContiguityRejectionRate
RawCapacityButNoFeasibleBlockRate

GroupSizeSuccessRate
```

## Reliability / Consistency

```text
TechnicalErrorRate

ConcurrencyConflictRate

CriticalInvariantViolationCount
DuplicateSpotViolationCount
DuplicateAttendeeViolationCount
PartialGroupViolationCount
```

## Performance

```text
MedianAssignmentLatency
P95AssignmentLatency
MeanAssignmentLatency

AssignmentThroughput
TotalScenarioProcessingTime
```

---

# 29. Core Metrics vs Diagnostic Metrics

Stage 5 intentionally distinguishes metrics needed to make a decision from metrics useful only for explaining a detected problem.

Preferred model:

```text
Core Metrics
    ↓
detect material problem
    ↓
Diagnostic Metrics
    ↓
explain cause
```

Do not collect or implement metrics merely because they are easy to calculate.

For every proposed metric ask:

> What decision could this metric change?

If no useful decision depends on it, exclude it from the initial simulation.

---

# 30. Metric vs Threshold vs Decision Gate

These concepts must remain separate.

```text
Metric
= what is observed

Threshold
= what value is considered acceptable

Decision Gate
= what action follows from the evidence
```

A metric may be useful even when no validated business threshold exists yet.

---

# 31. Criterion Classification

Every criterion should be categorized as one of:

## Hard Technical Criterion

Must always hold.

Examples:

```text
DuplicateSpotViolationCount = 0

DuplicateAttendeeViolationCount = 0

PartialGroupViolationCount = 0
```

## Engineering Baseline Criterion

Used to compare scenarios or strategies.

Examples:

```text
P95AssignmentLatency

AssignmentRejectionRate

RecoveryRate

arrival-order sensitivity
```

## Business Validation Required

A threshold that should not be invented by the technical team.

Examples:

```text
acceptable NotBalancedPathRate

acceptable rejection rate

acceptable Bad exposure

acceptable recovery time
```

These should be marked:

```text
Requires stakeholder validation
```

---

# 32. Decision Gate Interpretation

Scenario results must not be interpreted independently.

The Decision Gate should consider both:

```text
expected production behavior
```

and:

```text
behavioral degradation under adverse conditions
```

Possible conclusions:

## Continue

Evidence supports proceeding to the next implementation/proposal phase.
Stage 5 completes the technical MVP evaluation / Decision Gate; Continue does
not automatically start another predefined implementation stage or imply
organization approval or production readiness.

## Tune

The overall architecture remains valid, but specific policies, parameters, or operational thresholds require evidence-backed adjustment.

## Pivot

Material evidence indicates that the current assignment strategy or operating model is not sufficient.

Possible examples may include justified investigation of:

```text
different RotationScore weights

different TargetQuality threshold

additional fairness signals

different inventory policy

request windows

batch / hybrid processing

global optimization
```

These are hypotheses, not automatic next steps.

## Stop

The core assignment hypothesis is not viable under realistic festival conditions.

---

# 33. Decision Gate Questions

At minimum Stage 5 must answer:

| Question | Required Result |
| --- | --- |
| Is the solution technically viable? | Yes / No / Conditional |
| Are critical invariants preserved? | Yes / No |
| Is fairness behavior acceptable? | Yes / No / Requires tuning |
| Is performance acceptable? | Yes / No / Requires optimization |
| Is assignment rejection operationally acceptable? | Yes / No / Requires stakeholder validation |
| Is arrival-order sensitivity acceptable? | Yes / No / Requires mitigation |
| Is group behavior acceptable? | Yes / No / Requires adjustment |
| Is concurrency behavior operationally acceptable? | Yes / No / Requires redesign |
| Are there critical unresolved risks? | Explicit list |
| Recommended next step | Continue / Tune / Pivot / Stop |

Where the business does not yet provide a valid threshold, report:

```text
Requires stakeholder validation
```

rather than inventing one.

---

# 34. Explicitly Deferred Metrics and Capabilities

Do not add complexity at the start of Stage 5 unless core simulation evidence justifies it.

Deferred analytical metrics may include:

```text
ConsecutiveBadRate

StrongRecoveryRate

PartialRecoveryRate

BadExposureRate

P99 latency

database query count

detailed fragmentation distribution

per-Zone fairness statistics
```

Deferred algorithmic capabilities may include:

```text
CurrentGlobalAssignmentState

ZoneAssignmentState

different RotationScore weights

different TargetQuality threshold

weighted randomness

future-demand prediction

request windows

daily batch processing

hybrid processing

global optimization
```

These remain hypotheses until a measurable failure of the Stage 4 baseline demonstrates their value.

---

# 35. Stage 5 Evaluation Principle

For every proposed improvement ask:

> What measurable failure of the `v0.3.0` baseline does this change solve?

Preferred process:

```text
Stage 4 baseline
        ↓
simulation
        ↓
measured limitation
        ↓
candidate improvement
        ↓
comparison against baseline
        ↓
keep only if materially better
```

Stage 5 is not intended to prove that the baseline is perfect.

It is intended to determine:

```text
where it is good enough

where its limitations come from

how it behaves across plausible operating conditions

which improvements are actually justified

whether the project should continue
```

---

# 36. Current Status

The definitions in this document represent:

```text
Stage 5 Metrics and Evaluation Criteria v1
```

Current status:

```text
Fairness classification
→ PathClassificationRubric v1; provisional analytical baseline with explicit boundary cases

Fairness threshold
→ initial hypothesis

Performance threshold
→ engineering hypothesis

Critical invariant thresholds
→ hard technical criteria

Rejection thresholds
→ pending evidence / stakeholder validation

Scenario distributions
→ engineering hypotheses pending real organization data
```

This document defines what Stage 5 needs to learn. The initial framework is
documented; simulation, logging/attempt-record instrumentation and empirical
evaluation remain pending. No production behavior or test changed with this
documentation baseline, and the official Stage 4 fairness-rule count remains 19.

The [simulation architecture and reproducibility strategy](simulation-architecture.md)
defines the planned Application/PostgreSQL execution and evidence flow around
this framework. Harness implementation, instrumentation and simulation runs remain
pending; this document remains the analytical authority.
