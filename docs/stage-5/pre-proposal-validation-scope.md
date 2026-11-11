# Stage 5 — Pre-Proposal Validation Scope

Decision recorded: **2026-11-11 (America/Lima)**.
**Stage 5 = In Progress. MVP Technical Feasibility Gate = Pending / Next.**
**Formal stakeholder / production Decision Gate = Pending / future phase.**

## Purpose

Determine whether the concept has enough technical evidence and credibility to
justify presenting it to the festival organization. Technical feasibility for
proposal ≠ production readiness ≠ organization approval. This record updates
validation priority and decision sequencing; it does not perform either Gate.

## Decision

**Current evidence is sufficient to proceed to the internal MVP Technical
Feasibility Gate without requiring the remaining conditional experiments as
mandatory prerequisites.** The immediate next activity is **Perform MVP Technical
Feasibility Gate**, using the [existing criteria](mvp-technical-feasibility-gate.md)
and [consolidated evidence](decision-gate-evidence.md).

Plausible Festival Arrival Concurrency and Late Large Groups with Reconfiguration
remain relevant, unexecuted experiments. They move to **conditional post-proposal
validation**, dependent on actual operational information from the organization.
Their contracts/planning references are preserved, neither obsolete nor completed.
This supersedes the earlier sequence requiring both experiments before the Gate.

## Evidence supporting the decision

| Area | Existing support and limits | Authority |
| --- | --- | --- |
| Representative fairness | Completed representative comparisons support the current approach under the internal MVP criterion. Coverage, Bad exposure, recovery limits and individual order effects remain visible; measurements and thresholds are unchanged. | [S1/S2](s1-s2-representative-results.md), [S5](s2-s5-order-sensitivity-results.md), [S6](s2-s6-large-group-sensitivity-results.md) |
| Persisted integrity | Audited completed scenarios, including severe contention/stress, provide strong evidence of protected persisted invariants within the tested cases. The stopped high-rate collection retains its original findings and cannot be retrospectively certified clean. Final-state audits do not prove all temporal behavior or operational readiness. | [Correctness consolidation](decision-gate-evidence.md#persisted-correctness-across-scenarios), [stress integrity and observer limits](production-like-arrival-concurrency-analysis.md#integrity-observer-stop-and-technical-failures) |
| Sequential/reference performance | C1 demonstrates strong local reference performance and full delivery. The existing provisional p95 < 1 second remains an internal MVP analytical hypothesis, not a production SLA or proof of concurrent performance. | [C1/C4/C16 analysis](s8-concurrency-analysis.md) |
| Known stress limits | S8 C4/C16 synchronized stress and open-loop high-rate Moderate/Burst-like arrivals both demonstrate severe degradation. These are material findings, not production traffic measurements or predictions. | [S8](s8-concurrency-analysis.md), [high-rate stress](production-like-arrival-concurrency-analysis.md) |
| Large groups and fragmentation | S6 shows modest fairness impact from large-group prevalence under tested topology, without observed placement loss. S7 shows rejection despite remaining eligible capacity; operational realism and final allocation after reconfiguration remain unvalidated. | [S6](s2-s6-large-group-sensitivity-results.md), [S7](s7-fragmentation-impact-results.md) |

## What remains unresolved

Concurrency over shared mutable inventory, fragmentation/contiguous-block
feasibility, large-group behavior and arrival/order sensitivity remain known
technical risks. Plausible festival concurrency has not been validated. The
capacity-utilization question is not fully resolved across group/reconfiguration
conditions; initial request rejection is not the same as final unassigned Attendees.

Organization-specific information is still needed about:

- Actual request timing, gradual versus simultaneous opening, and when
  FestivalDay requests are allowed.
- Actual group-size distribution, maximum practical group size, whether groups
  may split/reconfigure, whether split groups are acceptable, and what happens
  after group rejection, including resubmission.
- Reserved or externally occupied inventory, staff/volunteer allocations and
  manual reassignment behavior. Existing Assignment finality remains unchanged.
- Operational response-time expectations and acceptable failure handling.

No answers are assumed. Venue, demand, eligibility and fairness expectations
also retain their [stakeholder-validation requirements](representative-scenarios.md#assumptions-requiring-stakeholder-validation).

## Why unresolved items are not current proposal blockers

Simulation has reduced technical uncertainty. The dominant remaining uncertainty
is increasingly business/operational; technical uncertainty is still present.
Additional pre-proposal simulation now has diminishing value compared with
validating business and operational assumptions directly with the organization.
Further refinement without those inputs risks optimizing against invented assumptions.

A **known technical risk** is a demonstrated sensitivity or open operating-envelope
question requiring disclosure and disposition. A **fundamental technical blocker**
would prevent a credible proposal without further technical work. Potential,
non-exhaustive examples include persistent invariant violations, representative
fairness consistently failing the internal criterion, sequential/reference
throughput clearly insufficient for expected scale, unavoidable large-scale
capacity waste, or no technically plausible mitigation path for a material failure.

Current Stage 5 evidence has not established such a fundamental blocker. This is
the project's evidence-based position for proceeding to review, **not the Gate
verdict**. It does not erase severe stress findings or the stopped audit limitation.

The capacity sanity principle remains: when sufficient physical capacity exists,
the system should not leave a material Attendee population unassigned solely
because of avoidable algorithmic inefficiency. Current evidence does not reveal
a fundamental capacity-utilization blocker, but organization-specific validation
may still be required. S7's demonstrated spatial loss remains material; no claim
of universal final allocation or new percentage target follows.

Technically plausible mitigation categories exist: serialization/coordination,
database-level coordination, retry strategies, queueing, group reconfiguration
and inventory/selection strategy changes. These are high-level possibilities,
not recommendations, selected architecture or demonstrated effective mitigations.

## Conditional post-proposal validations

| Experiment | Status | Role and possible trigger |
| --- | --- | --- |
| [Plausible Festival Arrival Concurrency](plausible-festival-arrival-concurrency.md) | **Defined / Pending Execution**; not implemented or executed | **Conditional post-proposal validation** if real request-arrival behavior indicates meaningful concurrency risk. The existing exploratory 120× contract remains available; assess its relevance against actual inputs before separately authorizing execution. |
| Late Large Groups with Reconfiguration | **Pending Definition / Execution** | **Conditional post-proposal validation** if real group behavior and reconfiguration rules make fragmentation/capacity utilization material. Determine whether initial rejection becomes final unassigned Attendees, using confirmed split/resubmission rules and preserving existing Assignment finality. |

Neither experiment is a mandatory initial-proposal or internal-Gate prerequisite.
No numeric trigger is defined. S3/S4 remain deferred until real organization data
can support credible stress/worst-plausible contracts; they remain in the roadmap.

## Updated roadmap

1. Update pre-proposal validation scope — **completed by this record**.
2. **Perform MVP Technical Feasibility Gate — Pending / Next**.
3. Prepare the proposal to the organization, if supported by the Gate.
4. Present/review with the organization.
5. Obtain real operational constraints and business rules.
6. Execute only the relevant conditional validations.
7. Evaluate and implement applicable mitigations, then validate their effects.
8. Calibrate production architecture / cloud deployment.
9. Finalize S3/S4 with real inputs, then execute/analyze them for the formal review.
10. Perform the formal stakeholder / production Decision Gate.
11. Make the pilot / production decision.

Post-proposal steps depend on organization interest and validated inputs. No
architecture, deployment choice, mitigation or production acceptance is selected.

## Decision boundaries

The internal Gate still evaluates **Fairness, Reliability, Performance and Capacity
Utilization**. It may consider known unresolved risks, technically plausible
mitigation paths and missing operational data when deciding which risks block a
proposal and which become post-proposal validation items. It may conclude
**Technically Feasible for Proposal** or **Further Technical Validation Is Required
Before Proposal**; neither conclusion is made here. No numerical criterion changes.

A future proposal should present demonstrated capabilities, existing evidence,
known risks, interpretation limits, open organization-specific questions and next
validation steps. It must not claim production readiness, a guaranteed SLA, final
architecture, a validated production traffic model, or fully resolved concurrency
or fragmentation. The proposal itself remains future work.

All measurements, official run counts, original findings, evidence packages and
remediation history remain unchanged. No simulation, mitigation implementation,
business-rule change, S3/S4 execution or Gate decision occurs in this scope update.

## Validation of this documentation update

`git diff --check` passed. Relative links and current-roadmap references were
checked. Task-start SHA-256 comparisons verified all 322 retained raw simulation
JSON files unchanged, with no new raw packages; selected analysis JSON, source,
tests, S6/S7/S8 reports and the observer-audit remediation record also remained
unchanged. All measured consolidation/stress table rows and the plausible
contract's technical body were preserved.

Repository-required `dotnet build Festival.sln` passed with zero warnings/errors;
`DOCKER_API_VERSION=1.43 dotnet test Festival.sln` passed. No tests were added.
These were automated repository checks, not experimental scenario executions.
No new operational assumptions, thresholds or scope deviations were introduced.
