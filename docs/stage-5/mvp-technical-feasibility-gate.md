# Stage 5 — MVP Technical Feasibility Gate

**Stage 5 = In Progress. MVP Technical Feasibility Gate = Pending.**
**Formal stakeholder Decision Gate = Pending / future phase.**

## Purpose and decision boundary

> Is it technically plausible to build a system that can assign festival
> locations fairly and reliably at the target attendee/day scale, with enough
> evidence to justify a proposal to the organization?

This internal pre-proposal Gate evaluates technical credibility at the modeled
festival scale. It does not require proving every production operating condition.
**Technical feasibility for proposal ≠ production readiness ≠ organizational
approval.** Stakeholder calibration and real operational-data validation remain
necessary in the later phase if the organization shows interest; they are not
prerequisites for making the initial proposal.

The [metrics framework](metrics-and-evaluation-criteria.md) owns metric definitions,
populations and analytical hypotheses. The [consolidated evidence](decision-gate-evidence.md)
preserves official findings. The [formal Decision Gate criteria](decision-gate-criteria.md)
remain preparatory requirements for the future stakeholder/production review;
their S3/S4 and stakeholder prerequisites do not apply to this internal Gate.

## Internal feasibility criteria and Gate questions

| Dimension | Internal criterion / decision question | Interpretation boundary |
| --- | --- | --- |
| Fairness | Does the MVP demonstrate **BalancedPathRate + AcceptablePathRate > 85%** in representative comparable scenarios? | Internal MVP feasibility criterion only, using ClassifiablePaths and the recorded rubric. Report each scenario, coverage and all path categories separately; no pooling or survivor-only shortcut. No organizational SLA or production policy follows. |
| Reliability | Are persisted invariants protected, unexpected technical failures absent or minimal, and non-success outcomes understandable and technically manageable? | Require zero confirmed persisted invariant violations. Categorize/investigate any unexpected failure; no new numerical allowance for failures is defined. Safe rollback protects correctness but does not itself establish acceptable delivery. |
| Performance | Does the representative operating flow satisfy **p95 < 1 second**? | **Internal MVP analytical hypothesis**, not a production SLA. Existing evidence measures Application/Domain/persistence use-case processing, not complete browser/HTTP/network experience. Open-loop high-rate stress exceeds this hypothesis in every valid run; plausible festival arrival performance still requires evidence. |
| Capacity utilization | When sufficient capacity exists, can the system ultimately allocate nearly all requesting Attendees without leaving a material population unassigned solely because of avoidable algorithmic inefficiency? | Qualitative technical sanity check, not an organization-approved requirement. No numeric FinalAttendeeAllocationRate, conflict, residual-capacity or spatial-rejection threshold is defined. Distinguish physical/eligible capacity, valid group feasibility and durable final allocation. |

The framework originally recorded the inclusive fairness hypothesis `>= 85%`.
This Gate uses the explicitly requested strict `> 85%` boundary; exactly 85%
does not meet it. This clarifies the internal decision criterion without changing
historical results, rubric, calculator or future stakeholder acceptance. N/A is
not evidence of meeting a quantitative criterion.

### Capacity boundary

**Daily demand > physical capacity → not solvable by software.** The MVP assumes
the organization will not allocate beyond available physical capacity; known
reservations/exclusions reduce the capacity available to the evaluated workload.
Overbooking itself is not an AssignmentEngine technical-feasibility failure.
Registered population, daily requesting Attendees, request count and available
Spot capacity remain distinct. Aggregate sufficiency also cannot guarantee a
valid eligible contiguous block for every current AssignmentGroup.

## Evidence already available

The S2 reference serves 4,400 requesting Attendees per day over five FestivalDays
against a modeled 4,998-Spot venue, persisting 22,000 Assignments per official
run. This demonstrates the modeled attendee/day scale; its inputs remain
provisional rather than an organization-validated forecast.

| Area | Current evidence and technical interpretation | Source |
| --- | --- | --- |
| Representative fairness | Largely demonstrated within tested S1/S2/S5/S6 envelopes: published classifiable-path distributions exceed the internal reference. S2 remains the baseline; its Bad exposure and unresolved recovery episodes remain visible. S5's approximately 59% individual quality changes/zero exact attendee/day Bad overlap remain a material distributive risk despite stable aggregates. | [S1/S2](s1-s2-representative-results.md), [S5](s2-s5-order-sensitivity-results.md), [S6](s2-s6-large-group-sensitivity-results.md) |
| Persisted correctness | Strongly demonstrated within audited cases: zero persisted invariant findings across completed scenarios, including S8 losing-transaction rollback. Final-state audits are not proof of all temporal behavior/interleavings. | [Correctness consolidation](decision-gate-evidence.md#persisted-correctness-across-scenarios) |
| Sequential/reference performance | Demonstrated locally: S8 C1 has full delivery, zero conflicts and p95 46.382–51.821 ms; it outperforms C4/C16 in useful delivery and latency. This supports the reference path, not production-like concurrency or an end-to-end SLA. | [S8 analysis](s8-concurrency-analysis.md) |
| S7 fragmentation | Known material access risk; operational realism unresolved. Sufficient eligible raw capacity can coexist with no valid contiguous block. | [S7 results](s7-fragmentation-impact-results.md) |
| Synchronized concurrency stress | Severe degradation demonstrated: S8 C4/C16 use synchronized bounded windows. Their conflict, latency and useful-delivery findings remain unchanged and are not production predictions. | [S8](s8-concurrency-analysis.md) |
| Open-loop high-rate arrival stress | Severe degradation demonstrated without a synchronized gate at offered rates of 40 / ≈ 61.5 req/s, above the local C1 mean of ≈ 29.6 successful req/s. PersistedInvariantViolationCount = 0 in all four valid runs; the stopped collection's three original findings and remediation remain retained. | [Arrival analysis](production-like-arrival-concurrency-analysis.md) |
| Plausible festival arrival concurrency | **Additional Evidence Required; contract Defined / Pending Execution.** The completed high-rate schedules do not establish whether concurrency is material at plausible festival arrival rates. | [Bounded contract](plausible-festival-arrival-concurrency.md), [interpretation correction](production-like-arrival-concurrency-analysis.md#post-run-interpretation-correction--2026-11-10) |
| Late large-group final allocation | Additional evidence required: S6 demonstrates full placement for its group mix, not late concentrated large groups followed by reconfiguration. | [S6 results](s2-s6-large-group-sensitivity-results.md) |

This supports individual feasibility dimensions, not a final feasibility verdict.
S7/S8 fairness rates among selected survivors do not establish equivalent overall
fairness. Retain NoPersistedPath, InsufficientEvidence, rejected/conflicted
requesters and recovery eligibility alongside measured rates.

## Known risks and their pre-proposal boundary

### S7 — Fragmentation realism

S7 used synthetic approximately **20% preoccupation** plus deliberately fragmented
spatial occupancy. Its capacity-matched pair established material extra rejection
despite sufficient eligible capacity, with 922 unused Spots daily in Fragmented.
This remains a known fragmentation risk whose operational relevance is not yet
validated. Ordinary capacity shortage and additional spatial loss are separate;
the capacity boundary above does not explain away S7's paired result.

If the Engine controls most/all assignment inventory from the beginning and
external reservations/preoccupations are limited, that tested condition may be
less representative. This is an assumption to disclose, not a demonstrated fact
or dismissal of S7. Post-proposal questions include: Are Spots reserved outside
the system? Are volunteer/staff areas preallocated? Can Spots become unavailable
dynamically? Can manual reassignment fragment inventory? No such operational
capability or exception to Assignment finality is introduced here.

### S8 — Synchronized contention

S8's start gate deliberately released requests in strongly synchronized windows.
It demonstrated real concurrent-access sensitivity: C4 conflicts were **75%** and
C16 **93.702–93.716%**, while data integrity remained protected and C1 was
operationally superior. These are measured experimental rates, not estimates
that production conflicts will be 75% or approximately 93.7%.

The [completed pre-proposal experiment](production-like-arrival-concurrency-analysis.md)
was originally defined as Production-like Arrival Concurrency. Post-run analysis
identified sustained overload: Moderate offers 40 req/s and Burst-like ≈ 61.5 req/s,
both above the retained C1 three-seed mean of 29.593 successful req/s. Its corrected
classification is Open-loop High-rate Arrival Stress. Requests continued arriving
while earlier work remained active, producing hundreds/thousands of active requests.
This supports a severe stress finding without a synchronized gate; it does not yet
establish whether concurrency is material under plausible festival arrival rates.
S8 remains sufficient for its original scoped comparison and was not rerun.

**The observed conflict rates, technical failure rates, latencies and active
concurrency are not production predictions.** Approximately 79% Moderate conflicts,
69–74% Burst-like conflicts and peaks of 672–685 / 1,295–1,377 cannot be treated as
expected festival conditions. S2's busiest hourly bucket is 20% of 1,464 daily
requests, approximately 293/hour or 0.081 req/s on average. Short bursts remain
possible; that average is not a bound on real-user concurrency. Neither stress
experiment independently represents production.

These risks are not automatic blockers to presenting a technically credible
concept. They remain proposal disclosures and later validation inputs. The two
pending experiments below remain inputs to review: plausible festival concurrency
and late-group final allocation still require evidence. The stress finding remains
material; its interpretation boundary is corrected without selecting mitigation.

## Unsuccessful requests and final Attendee allocation

| Mechanism | Current semantics | Future flow boundary |
| --- | --- | --- |
| Spatial/group infeasibility — `DomainRejected` | No contiguous block exists for the current group; this can be a valid business rejection with no Assignments. | Group reconfiguration, an alternative grouping or a new request may later enable allocation. Reconfiguration is not implemented or guaranteed. |
| Persistence contention — `KnownConcurrencyConflict` | A technical/transient persistence conflict, with complete rollback of the losing graph. It is not equivalent to a legitimate business rejection. | Future production handling may evaluate internal coordination/retry/serialization. No option or policy is selected; the measured baseline retains no retries. |

**InitialRequestSuccessRate ≠ FinalAttendeeAllocationRate.** An initial large-group
rejection need not mean those Attendees remain unassigned after allowed
reconfiguration/new requests. Conversely, recovery of their allocation is not
demonstrated yet. Keep initial request outcomes separate from distinct Attendees
ultimately assigned for the FestivalDay. Existing SuccessRate denominators and
analytical outcomes remain unchanged; the new analytical labels below are future
experiment requirements, not implemented metrics.

## Remaining pre-proposal experiments

Open-loop High-rate Arrival Stress is Completed. Plausible Festival Arrival
Concurrency is **Defined / Pending Execution**; late-group reconfiguration still
requires definition/execution afterward, before the overall Gate.

| Experiment | Decision-relevant uncertainty | Required future evidence |
| --- | --- | --- |
| **[Plausible Festival Arrival Concurrency](plausible-festival-arrival-concurrency.md) — Defined / Pending Execution** | Is concurrency material with plausible temporal dispersion derived from the business workload? The completed high-rate stress experiment does not answer this. | Implement/execute the defined S2-derived 120× schedules: evenly spaced Plausible and three-request PlausibleBurst at 0/10/20 ms, with identical bucket average rates and a peak of 9.767 req/s. Four initial runs, up to six with pattern-specific conditional Seed 3001. Verify boundedness before interpreting conflicts; retain integrity, full outcome mix, latency, useful delivery, participation and timing evidence. No result or mitigation is implied by definition. |
| **Late Large Groups with Reconfiguration** | Does concentration of large groups late in daily processing cause fundamental avoidable capacity waste, or recoverable initial-request rejection? When capacity remains available, can affected Attendees ultimately obtain Assignments after allowed group reconfiguration? | InitialRequestSuccessRate, RejectedAttendees, ReconfiguredRequests, FinalAttendeeAllocationRate, UnassignedAttendees and RemainingCapacity, with independent persisted correctness checks and traceable initial-to-final outcomes. |

The original [Production-like Arrival Concurrency contract](production-like-arrival-concurrency.md)
is retained by name for traceability; its executed evidence is **Open-loop High-rate
Arrival Stress — Completed**, with [analysis and evidence](production-like-arrival-concurrency-analysis.md).
Moderate uses 25 ms intervals; Burst-like
uses four-request clusters with 5 ms internal spacing and 50 ms between the last
arrival and the next cluster. Initial Seeds 1001/2001 for both patterns give four
runs; neither pattern required conditional Seed 3001. Four valid runs plus one
retained stopped observer-audit collection give five actual executions, within
the six-execution cap. The stopped findings remain explicit; outcomes were not
rewritten during the documented audit correction.
Arrival rate, realized active concurrency and conflict rate remain distinct.

That contract's implementation retains actual timing/overlap evidence and metric
boundaries; it does not claim actual festival traffic. The late-group
contract, reconfiguration policy and number of follow-up requests remain undefined.
No new numeric acceptance threshold or experimental result is introduced here.
Plausible temporal dispersion remains a hypothesis until real data exists.
Reconfiguration must involve new requests for unassigned Attendees, preserving
indivisibility/contiguity of each new AssignmentGroup and finality of existing
Assignments. No production or Simulation behavior changes in this task.

## Immediate roadmap and Gate outcomes

1. Reframe arrival-concurrency evidence — completed as Open-loop High-rate Arrival Stress.
2. Define Plausible Festival Arrival Concurrency — completed; execution pending.
3. **Implement and Execute Plausible Festival Arrival Concurrency Experiment.**
4. Define and execute Late Large Groups with Reconfiguration separately.
5. Perform the MVP Technical Feasibility Gate using the additional experiments and
   completed evidence, explicitly reviewing fairness, reliability, performance,
   capacity utilization and remaining risks.
6. If technically supported, prepare the proposal to the festival organization.

The internal review may conclude **Technically feasible for proposal** or
**Further technical validation required before proposal**. It does not conclude
Production Ready, Approved by Organization or a final go-live decision. Neither
outcome is selected now; the Gate remains pending after roadmap realignment.

## Post-proposal validation phase

If organization review shows interest: obtain real operating data; validate and
calibrate demand, arrivals, venue topology, reservations/preoccupation, groups,
eligibility, fairness expectations and latency/service expectations; finalize
S3/S4 contracts and execute/analyze them with data-informed inputs; evaluate
applicable mitigations; re-test material risks; then perform the formal
stakeholder Decision Gate before any pilot/production decision.

S3/S4 are deferred to this later phase, not removed. Existing
[formal criteria and readiness requirements](decision-gate-criteria.md)
remain in force for that review, including unacceptable tested operational
envelopes and mitigation-validation requirements. Stakeholder acceptance is still
necessary for that later decision. No pre-proposal completion automatically
accepts known risks for production or claims full Stage 5 evidence completeness.

## Current status

Open-loop High-rate Arrival Stress is Completed with four valid analyzed runs and
a documented stopped collection. Plausible Festival Arrival Concurrency is Defined / Pending Execution;
late-group reconfiguration still needs its contract/execution afterward.
Stage 5 remains In Progress; both Gates remain Pending. No final technical
feasibility, stakeholder approval, production readiness or go/no-go decision is
made here. Next task: **Implement and Execute Plausible Festival Arrival Concurrency Experiment**.
