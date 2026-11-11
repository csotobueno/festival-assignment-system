# Stage 5 — MVP Technical Feasibility Gate Result

Decision date: **2026-11-11 (America/Lima)**. Evidence baseline: repository
revision `8179a4a6405c893d19569d1078c31e071d208f35`.
**MVP Technical Feasibility Gate = Completed. Stage 5 = In Progress.**
**Formal stakeholder / production Decision Gate = Pending / future phase.**

## Gate purpose and evidence reviewed

> Does the current body of technical evidence support proceeding with an initial
> proposal to the festival organization, while keeping known unresolved risks
> explicit and deferring organization-dependent validation to the post-proposal phase?

This is an internal technical-feasibility-for-proposal decision under the
[Gate criteria](mvp-technical-feasibility-gate.md#internal-feasibility-criteria-and-gate-questions)
and [pre-proposal scope decision](pre-proposal-validation-scope.md). It uses
existing reports and selected summaries only, with no new experimental evidence.

| Evidence family | Authoritative sources and role |
| --- | --- |
| S1/S2 representative runs | [Results](s1-s2-representative-results.md), [selected JSON](s1-s2-representative-results.json): three Seeds per scenario; fairness, coverage, complete placement and integrity. |
| S5 order sensitivity | [Results](s2-s5-order-sensitivity-results.md), [selected JSON](s2-s5-order-sensitivity-results.json): exact reversed-order S2 comparisons; aggregate versus recipient-level effects. |
| S6 large groups | [Results](s2-s6-large-group-sensitivity-results.md), [selected JSON](s2-s6-large-group-sensitivity-results.json): scenario-level group-mix sensitivity, fairness and feasibility. |
| S7 fragmentation | [Results](s7-fragmentation-impact-results.md), [selected JSON](s7-fragmentation-impact-results.json): capacity-matched spatial pairs, rejection diagnosis and access-selection limits. |
| S8 / C1 | [Analysis](s8-concurrency-analysis.md), [selected JSON](s8-concurrency-analysis.json): nine C1/C4/C16 runs; useful delivery, timing, conflicts and persisted audits. |
| Open-loop High-rate Arrival Stress | [Analysis](production-like-arrival-concurrency-analysis.md), [selected JSON](production-like-arrival-concurrency-analysis.json), [observer-audit remediation](arrival-observer-audit-remediation.md): four valid runs plus one retained stopped/excluded collection; overload and evidence limits. |

The [consolidation](decision-gate-evidence.md) supplies cross-scenario findings;
the [metrics framework](metrics-and-evaluation-criteria.md) owns populations,
denominators and hypotheses; [representative contracts](representative-scenarios.md)
own provisional business inputs; [simulation architecture](simulation-architecture.md)
owns execution boundaries. The [operating model](../project-operating-model.md)
and [future formal criteria](decision-gate-criteria.md) distinguish this decision
from organizational acceptance. The [Plausible contract](plausible-festival-arrival-concurrency.md)
defines future work, not supporting execution evidence. Reused S2/C1 inputs and
reproduction/smoke checks are not additional independent fairness populations.

## Fairness assessment

**Evidence.** The internal criterion is **BalancedPathRate + AcceptablePathRate > 85%**,
over ClassifiablePaths. The recorded rates below retain all three
categories; every individual run meets that criterion, without pooling scenarios
or averaging away a failing Seed.

| Scenario | Seed | Balanced % | Acceptable % | NotBalanced % | Internal criterion |
| --- | --- | --- | --- | --- | --- |
| S1 | 1001 | 65.695 | 34.305 | 0.000 | Met |
| S1 | 2001 | 65.637 | 34.363 | 0.000 | Met |
| S1 | 3001 | 66.159 | 33.841 | 0.000 | Met |
| S2 | 1001 | 56.677 | 32.194 | 11.128 | Met |
| S2 | 2001 | 56.305 | 32.884 | 10.811 | Met |
| S2 | 3001 | 56.379 | 32.111 | 11.510 | Met |
| S6 | 1001 | 56.020 | 32.330 | 11.650 | Met |
| S6 | 2001 | 56.415 | 31.537 | 12.048 | Met |
| S6 | 3001 | 55.859 | 31.914 | 12.227 | Met |

Sources: [S1/S2 fairness](s1-s2-representative-results.md#observed-fairness-and-quality)
and [S6 comparison](s2-s6-large-group-sensitivity-results.md#primary-aggregate-comparison).
These are copied, independently rounded published rates, not new measurements.
S1 has 3,853–3,877 classifiable paths and 457–493 InsufficientEvidence;
S2 has 5,115–5,126 and 218–244 respectively; S6 has 5,116–5,121 and 234–261.
All have zero NoPersistedPath. Short paths remain outside the fairness denominator.

**Known limitations.** Representative aggregate results are reasonably stable
across Seeds and the tested group-mix variation, not invariant across workloads.
S1 is favorable and changes multiple inputs relative to S2. S6 modestly worsens
NotBalanced by +0.521–1.237 pp and Bad by +0.309–0.345 pp; its same-Seed cohorts
are not exact S2 attendee pairs. S2 still has 9.241–9.300% Bad outcomes and
40 / 46 / 49 eligible unresolved recovery episodes; high recovery does not erase
that exposure. All these findings retain the source denominators.

[S5](s2-s5-order-sensitivity-results.md#summary) shows material distribution risk:
only 40.52–41.26% of paired attendee/day qualities are unchanged, exact attendee/day
Bad overlap is zero, and only 46.90–47.48% of classifiable paths retain classification,
despite relatively stable aggregates. [S7](s7-fragmentation-impact-results.md#fairness-population-and-coverage)
and [S8](s8-concurrency-analysis.md#participation-and-fairness-population) lose access
and classifiable coverage; better survivor rates are not comparable baseline
fairness improvements or representative-baseline failures.

**Assessment: Supported with Known Risk. Proposal-blocking decision: No.**
Representative evidence supports the internal policy criterion. Recipient-level
order sensitivity must be disclosed and later reviewed with stakeholders;
this Gate does not decide whether that distribution is organizationally acceptable.

## Reliability assessment

**Evidence — persisted correctness.** Completed S1/S2/S5/S6/S7/S8 reports record
**PersistedInvariantViolationCount = 0**. All nine S8 configurations and all four
valid high-rate runs preserve their zero final-state findings. Audits cover unique
Spot/day and Attendee/day, complete contiguous single-Zone/same-Row groups,
identity/request consistency, paths and checkpoints. S8 exercises real losing
transactions with no partial Assignment graphs or fake Rejected requests.
[Correctness consolidation](decision-gate-evidence.md#persisted-correctness-across-scenarios).

**Evidence — operational reliability.** C1 delivers all 7,320 requests and 22,000
Assignments per run with zero conflicts, unexpected failures and domain rejections.
S8 C4/C16 instead have 75% / 93.702–93.716% conflicts, only 1,830 / 460–461
successes, and zero unexpected failures. Recognized 23505/SpotAlreadyAssigned
and 40P01/DeadlockDetected remain KnownConcurrencyConflict after rollback.
They are technical contention, not DomainRejected. S7's persisted no-block
rejections are legitimate business outcomes for the indivisible groups presented,
while their frequency remains an access risk.

The [high-rate experiment](production-like-arrival-concurrency-analysis.md#main-comparison)
has 91 / 77 unexpected failures for Moderate and 1,816 / 2,234 for BurstLike,
alongside severe contention and useful-delivery loss. Offered rates of 40 / ≈ 61.5
req/s produced sustained backlog with peaks of 672–685 / 1,295–1,377 active requests.
Finite daily workloads drained; this does not establish bounded plausible traffic,
operational recoverability in general, or production failure probabilities.

**Known limitations.** Final-state audits do not prove every temporal INV-06
behavior or interleaving. The stopped/excluded original BurstLike/1001 retains
**three audit findings**; its disposed database prevents independent retrospective
request-status verification. It is not certified clean. The documented correction
correlates only independently verified complete committed graphs; observer failures
remain UnexpectedTechnicalFailure. Generic failures lack exported inner causes:
no pool/timeout root cause is established. The four valid runs and five actual
executions remain distinct. [Remediation and limits](arrival-observer-audit-remediation.md).

**Assessment: Supported with Known Risk. Proposal-blocking decision: No.**
Persisted correctness is sufficiently demonstrated for a technical proposal.
Operational reliability is **not supported in the tested concurrent/stress
envelopes**. Contention, overload and observation failures are identifiable
engineering concerns, but detailed generic causes and real operating bounds remain
open. The existing full-delivery reference, protected valid audited graphs and
technically plausible coordination paths support deferring mitigation selection
until operational requirements are known; possibility alone is not proof of a fix.
No mandatory pre-proposal mitigation is imposed. T1/T5/T6/T8 formal acceptance
requirements remain in force: characterize failures before service acceptance,
and validate applicable mitigation or supported envelope avoidance before production.

## Performance assessment

**Evidence.** [C1](s8-concurrency-analysis.md#performance-and-useful-throughput-c1-vs-c4-vs-c16-per-seed)
is the primary local reference, with full useful delivery:

| Seed | p95 ms | Successful requests/s | Persisted Assignments/s |
| --- | --- | --- | --- |
| 1001 | 46.382 | 30.871 | 92.783 |
| 2001 | 51.821 | 28.509 | 85.684 |
| 3001 | 48.683 | 29.397 | 88.352 |

Published unweighted means are 48.962 ms for per-run p95, 29.593 successful
requests/s and 88.940 Assignments/s. The mean of per-run percentiles is not a
pooled percentile. Every C1 run comfortably meets **p95 < 1 second**.

**Known limitations.** Every S8 C4/C16 p95 fails that hypothesis: 2,068.156–2,096.992
ms / 3,262.293–4,212.878 ms. High-rate Moderate p95 is 21,414.323–22,967.696 ms;
BurstLike is 41,403.971–50,178.519 ms with much poorer useful delivery. These
remain operating-limit findings, not averaged with C1. Local Debug/PostgreSQL,
no warm-up, fixed ordering, uncontrolled host activity and one run per Seed/configuration
limit generalization. Processing excludes scope preparation and later observation;
it does not measure complete HTTP/network/frontend experience. Useful throughput
uses the common monotonic interval, not summed overlapping durations.
[Boundaries](s8-concurrency-analysis.md#environment-reproducibility-and-interpretation-limits).

**Assessment: Supported. Proposal-blocking decision: No.**
The reference supports the existing internal hypothesis at modeled scale; the
stress evidence primarily exposes unresolved contention/overload rather than
demonstrating insufficient sequential baseline capacity. This is not a production
capacity claim or an isolated causal estimate. No additional performance
experiment is mandatory before a proposal; actual arrivals, opening policy and
response expectations determine later service validation. The hypothesis is not an SLA.

## Capacity-utilization assessment

**Evidence.** [S2](s1-s2-representative-results.md#daily-capacity-and-feasibility)
and [S6](s2-s6-large-group-sensitivity-results.md#run-integrity) each place all
4,400 requesting Attendees/day in the 4,998-Spot topology, with 22,000 Assignments
per run and zero rejections. S6's increased large-group prevalence preserves
feasibility under that topology while modestly worsening fairness.

[S7](s7-fragmentation-impact-results.md#pair-and-opening-integrity) uses synthetic
approximately 20% preoccupation: both variants have 1,000 occupied / 3,998 available
Spots daily against the same S2 demand. Ordinary shortage and additional spatial
loss must remain separate. Fragmented has 14.959% rejection versus Control's
7.732–7.992%; all 1,095 Fragmented rejections/run have sufficient eligible raw
capacity but no feasible contiguous block. It leaves 922 Spots unused daily.
That is demonstrated material access loss, not merely physical overbooking.

**Known limitations.** Daily demand > usable physical capacity is mathematically
unsatisfiable and not an AssignmentEngine defect; the planning assumption is that
the organization will not allocate beyond usable capacity. Raw/eligible capacity
does not guarantee group geometry. S7's preoccupation amount/positions are
provisional and operational realism is unvalidated. S6 does not test late large
groups followed by splitting/resubmission. Initial request rejection is not final
unassigned Attendees; final allocation after reconfiguration has not been demonstrated.

**Assessment: Supported with Known Risk. Proposal-blocking decision: No.**
The sanity principle remains that sufficient physical capacity should not leave
a material Attendee population unassigned solely through avoidable algorithmic
inefficiency. Current evidence demonstrates fragmentation loss but does not
establish **unavoidable** material capacity waste or a fundamental utilization
blocker. It also does not prove the principle under every operational condition.
S7 warrants organization-specific inventory validation; the final-allocation
question remains conditional post-proposal work under confirmed reconfiguration
rules. No utilization percentage target or successful reconfiguration is assumed.

## Known risks and proposal dispositions

These classifications concern an initial proposal only. None means solved or
accepted for production; historical material-risk classifications remain unchanged.

| Risk | Classification | Evidence and required disposition |
| --- | --- | --- |
| Arrival/order sensitivity | **Known / Non-blocking for Proposal** | S5 recipient-level churn despite aggregate stability. Disclose the allocation relevance of order; obtain fairness/order acceptability and actual opening rules before deciding further work. |
| Shared-inventory concurrency / overload | **Conditional Post-Proposal Validation Required** | S8 and high-rate stress show severe degradation. Obtain actual arrivals/opening/service requirements; determine relevant concurrency validation and evaluate applicable mitigations. Plausible festival concurrency remains unvalidated. |
| Fragmentation | **Conditional Post-Proposal Validation Required** | S7 proves sufficient-capacity/no-block loss. Confirm reservations, external occupancy, staff areas and permitted manual operations; address the risk if comparable fragmentation applies. |
| Large groups / reconfiguration | **Conditional Post-Proposal Validation Required** | S6 full placement is limited to its mix/topology. Confirm practical group sizes, split acceptability and resubmission; define/execute late-group final-allocation validation only if relevant. |
| Unexpected failures / observer evidence limits | **Known / Non-blocking for Proposal** | Preserve all high-rate failure counts and the stopped audit gap; characterize technical failures before formal service acceptance. No complete generic root cause or retrospective clean audit is claimed. |

Technically plausible categories include serialization/coordination, database-level
coordination, optimistic retry, queueing, group reconfiguration and inventory/selection
changes. **Plausible mitigation exists ≠ mitigation validated.** No option is
selected, recommended as a final design, implemented or presumed effective.

## Organization-dependent uncertainties

Obtain actual request timing, gradual/simultaneous opening and FestivalDay request
windows; actual group-size distribution and maximum practical size; whether groups
may split/reconfigure, whether split groups are acceptable, and resubmission after
rejection. Confirm reserved/external inventory, staff/volunteer allocations,
physical topology/eligibility and manual reassignment expectations without
changing existing Assignment finality. Obtain acceptable response times, failure
handling and fairness expectations, including Bad exposure/recovery and order
sensitivity. These are unanswered business/operational questions, not new assumptions.

## Fundamental blocker assessment

| Potential fundamental blocker | Assessment from retained evidence |
| --- | --- |
| Persistent correctness failure | Not demonstrated in completed valid audits. The stopped collection's three findings remain an explicit unresolved audit limit, not proof of clean state or a demonstrated persistent corruption mode. |
| Representative fairness consistently below the internal criterion | Not demonstrated: every reviewed S1/S2/S6 run meets the strict criterion with coverage limits retained. |
| Reference throughput insufficient for modeled scale | Not demonstrated: C1 delivers the complete modeled workload with low latency. Real production service demand remains unvalidated. |
| Unavoidable material capacity waste | Not demonstrated: S2/S6 fully place daily demand; S7 proves spatial loss, not its inevitability across organization rules or all technically plausible approaches. |
| Material failure mode with no technically plausible mitigation path | Not demonstrated. Coordination/inventory alternatives are technically plausible; their effectiveness and organizational suitability remain unproven. |

The list is illustrative. **No fundamental technical blocker to an initial
proposal is demonstrated by the current evidence.** This finding does not accept
the current stress behavior or require missing operational answers to be invented.

## Gate matrix

| Dimension | Evidence status | Open risk | Proposal impact |
| --- | --- | --- | --- |
| Fairness | Supported with Known Risk | Order/recipient sensitivity; provisional rubric and stakeholder acceptability | Non-blocking; disclose distributions and validate expectations |
| Reliability | Supported with Known Risk | Severe concurrent/stress service loss, generic failure causes and stopped audit limit | Non-blocking for proposal; operational validation/mitigation remains required before accepting applicable service envelopes |
| Performance | Supported | Reference-only local processing evidence; stress hypothesis failures | Non-blocking; production service claims require real operating inputs |
| Capacity Utilization | Supported with Known Risk | Fragmentation loss and untested final allocation after reconfiguration | Non-blocking; conditional organization-specific validation retained |

## Gate outcome

**Technically Feasible for Proposal**

Representative fairness supports the internal criterion; valid persisted audits
strongly support correctness; C1 supports the internal performance hypothesis;
no fundamental capacity-utilization blocker is demonstrated. The unresolved
risks can reasonably be investigated and applicable mitigation evaluated after
obtaining organization-specific data. Their severity and missing diagnostic
coverage remain explicit. Therefore the concept has enough technical support
for an initial proposal and organization-level validation, without new mandatory
pre-proposal experiments or mitigation implementation.

## Decision boundaries and status

This outcome does **not** mean Production Ready, Ready to Go Live, SLA Validated,
Concurrency Solved, Fragmentation Solved, Final Architecture Approved or
Organization Approved. The proposal must disclose capabilities, evidence, risks,
interpretation limits, open questions and next validations. It is not created here.
No production go/no-go or formal Continue / Tune / Pivot / Stop is selected.

The internal Gate is **Completed**. **Stage 5 remains In Progress** because its
broader [formal evidence-completion scope](decision-gate-criteria.md#evidence-complete-definition)
still includes stakeholder decisions, applicable risk disposition and data-informed
S3/S4. This result completes the pre-proposal technical review, not that wider
evaluation. The formal stakeholder/production Gate remains Pending / future phase.

## Post-Gate roadmap

1. **Prepare Organization Proposal** using this decision and its disclosures.
2. Present/review with the organization.
3. Obtain real operating constraints and business rules.
4. Execute only relevant conditional validations: Plausible Festival Arrival
   Concurrency remains **Defined / Pending Execution**; Late Large Groups with
   Reconfiguration remains **Pending Definition / Execution**. Both retain the
   role **Conditional Post-Proposal Validation**.
5. Evaluate/validate applicable mitigations, with implementation separately scoped.
6. Calibrate production/cloud architecture from validated requirements.
7. Finalize data-informed S3/S4 contracts, then separately execute/analyze them.
8. Perform the formal stakeholder / production Decision Gate.
9. Make the pilot / production decision.

S3/S4 remain Deferred pending real organization data; no contract is redefined here.
The [Plausible contract](plausible-festival-arrival-concurrency.md) and late-group
planning references remain available. No new experiment, metric, threshold,
production infrastructure, code, policy or stakeholder contact is introduced.

## Validation and evidence preservation

`git diff --check` passed. Relative links, current status/roadmap references and
the explicit binary decision were checked. Copied fairness and C1 values were
matched to the authoritative reports. All existing documentation table rows,
fairness/performance threshold definitions and the Plausible technical-contract
body remain unchanged.

Task-start SHA-256 comparisons verified all **322 retained raw simulation JSON
files** unchanged, with no new raw evidence. Selected analysis JSON, authoritative
S1/S2/S5/S6/S7/S8 reports, source/tests and the observer-audit remediation record
are unchanged. No analyzer or simulation harness was executed to generate evidence.

Repository-required `dotnet build Festival.sln` passed with **0 warnings/errors**;
`DOCKER_API_VERSION=1.43 dotnet test Festival.sln` passed **990 tests, 0 failures,
0 skips**. No tests were added. Automated checks validate repository health only;
they add no experimental scenario measurements to this Gate. No new operational
assumptions, numeric thresholds or deviations from the requested scope were introduced.
