# Stage 5 — Isolated Scenario Evidence for Decision Gate

**Isolated evidence consolidated. Stage 5 = In Progress.**
**MVP Technical Feasibility Gate = Pending; formal stakeholder Decision Gate =
Pending / future phase.** This assessment consolidates S1, S2,
S5, S6, S7 and S8. It records architectural findings and review actions, without
choosing Continue / Tune / Pivot / Stop or a mitigation architecture. S3/S4
remain unresolved.

The [MVP Technical Feasibility Gate](mvp-technical-feasibility-gate.md) owns the
immediate pre-proposal roadmap: implement/execute the defined Plausible Festival Arrival
Concurrency, then Late Large Groups with Reconfiguration, then the internal Gate and proposal if supported.
The [formal Decision Gate criteria](decision-gate-criteria.md) retain stakeholder
decisions and S3/S4 objectives for the later phase if the organization is
interested. Stakeholder validation remains necessary then, but does not precede
the initial proposal. Stage 5 is not Evidence Complete; measured findings and
risk classifications below remain unchanged. Earlier reports' next-task notes
are historical; the linked MVP roadmap defines the current sequence.
The original [Production-like Arrival Concurrency contract](production-like-arrival-concurrency.md)
is retained as **Open-loop High-rate Arrival Stress — Completed**; its [separate analysis](production-like-arrival-concurrency-analysis.md)
supplements the historical isolated-scenario findings below. It records four valid
runs and five actual executions including the stopped observer-audit collection.
Neither pattern required Seed 3001; C1/C4/C16 were not rerun.

## Authority, evidence and comparison conventions

The [metrics framework](metrics-and-evaluation-criteria.md) owns definitions,
populations, rubric and criteria; the [scenario contracts](representative-scenarios.md)
own provisional inputs; the [simulation architecture](simulation-architecture.md)
owns execution/evidence boundaries. The reports below own measured results.
Their selected JSON summaries retain exact values, RunIds, source revisions,
checksums and denominators. This consolidation reads those reports/summaries;
it does not rerun scenarios, analyze raw packages or regenerate results.

| Evidence | Official coverage | Authoritative report / selected summary |
| --- | --- | --- |
| S1 and S2 | Three five-day runs each; Seeds 1001 / 2001 / 3001 | [S1/S2 results](s1-s2-representative-results.md), [JSON](s1-s2-representative-results.json); [S2 path diagnostics](s2-fairness-path-analysis.md) |
| S5 | Three reversed-order runs paired to retained S2 | [Order sensitivity](s2-s5-order-sensitivity-results.md), [JSON](s2-s5-order-sensitivity-results.json) |
| S6 | Three scenario-level comparisons to retained S2 | [Large-group sensitivity](s2-s6-large-group-sensitivity-results.md), [JSON](s2-s6-large-group-sensitivity-results.json) |
| S7 | Three exact-workload Control/Fragmented pairs | [Fragmentation impact](s7-fragmentation-impact-results.md), [JSON](s7-fragmentation-impact-results.json) |
| S8 | C1/C4/C16 × three Seeds; Repetition 1, RetryCount 0 | [Concurrency and operational analysis](s8-concurrency-analysis.md), [JSON](s8-concurrency-analysis.json) |

All use Generator v2, Engine v0.3.0 and Rubric v1 with real PostgreSQL persistence.
Reproduction/smoke runs are separate from official aggregates. Retained S2
controls are reused rather than counted as fresh experiments in each report.
S8 C1 supplies fresh operational timing over the same S2 inputs, not additional
independent fairness populations. Earlier reports' historical next-step proposals
are superseded by the completed evidence listed here; their observations remain
unchanged. Raw eight-file packages remain local and ignored; their historical
checksum/replay validations are recorded in the source reports, not repeated here.

Rates below retain their original denominators:

| Measure | Denominator / boundary |
| --- | --- |
| SuccessRate / RejectionRate | NormallyProcessedRequests = Successful + DomainRejected |
| ConflictRate / TechnicalErrorRate | AttemptedRequests = Successful + DomainRejected + KnownConcurrencyConflict + UnexpectedTechnicalFailure; exclusions separate |
| Good / Medium / Bad | Persisted evaluated Assignment outcomes; one group can contribute several outcomes |
| Balanced / Acceptable / NotBalanced | Classifiable paths with at least two persisted outcomes |
| RecoveryRate | Recovery-eligible episodes; recovered-episode speed counts later persisted opportunities, not calendar days |
| S8 useful throughput | Successful requests or durable Assignments divided by the same measured interval as terminal Attempts/s |

NoPersistedPath and one-outcome InsufficientEvidence remain visible. Absence,
rejection, conflict and failure create no synthetic quality or recovery
opportunity. Zero denominators remain N/A. Ranges span the three Seeds; pp means
percentage points. S8 means are the published unweighted per-run means, not pooled
percentiles. No new fairness score, threshold or business acceptance is introduced.

## Scenario assessments

### S1 — Best Plausible

**Question / hypothesis.** Does the baseline behave favorably with ample capacity
relative to demand?

**Controlled variable.** Favorable combined workload: 3,300 daily requesters over
4,998 Spots, smaller groups and smoother arrivals; shared venue, eligibility,
policy and five-day horizon. S1/S2 change several workload dimensions together.

**Measured evidence.** Each run completes all 6,665 requests and persists 16,500
Assignments. BadOutcomeRate and NotBalancedPathRate are both 0%; BalancedPathRate
is 65.637–66.159%. There are no recovery episodes, so RecoveryRate is N/A.
Daily residual capacity is 1,698 Spots, including all 996 Bad Spots.

**Conclusion.** Validated favorable behavior; the policy can produce complete
placements without Bad exposure or NotBalanced paths under these conditions.

**Interpretation boundary.** The provisional high-capacity/favorable envelope
does not establish expected production fairness, recovery ability or the effect
of any one workload variable.

**Architectural implication.** Favorable operation is demonstrated, while its
headroom limits its use as evidence about pressured operation.

**Decision Gate relevance.** Primary classification: **Validated Behavior**.
Accept this favorable reference within its tested envelope; assess the baseline
and adverse sensitivities separately. [Source](s1-s2-representative-results.md).

### S2 — Expected Baseline

**Question / hypothesis.** What fairness and feasibility does the primary working
baseline deliver under expected, still provisional usage?

**Controlled variable.** Combined S2 workload: 4,400 daily requesters, unchanged
4,998-Spot venue, normal sequential processing and natural five-day history.
It is the reference for subsequent isolated sensitivities.

**Measured evidence.** Every run completes all 7,320 requests successfully and
persists 22,000 Assignments. BadOutcomeRate is 9.241–9.300%; NotBalancedPathRate
10.811–11.510%; eligible RecoveryRate 96.621–97.236%. Classifiable populations
are 5,122 / 5,115 / 5,126, with 232 / 244 / 218 InsufficientEvidence and no
NoPersistedPath. Every Bad outcome occurs in the final daily processing third.
Larger-group unfavorable-outcome associations persist within that broad timing
stratum; they are secondary to the recurring order/position signal.

**Conclusion.** Business processing is fully feasible here, while substantial
Bad exposure and NotBalanced paths coexist with high eligible recovery.

**Interpretation boundary.** Recovery does not erase unfavorable exposure;
40 / 46 / 49 eligible episodes remain unresolved. Episodes without later
persisted opportunity are separately ineligible. S2 diagnostics alone are
observational and do not isolate timing/group causality. Expected usage and
fairness acceptance remain unvalidated by stakeholders.

**Architectural implication.** Successful placement is insufficient evidence of
acceptable fairness; individual trajectories and processing order matter.

**Decision Gate relevance.** Baseline fairness sufficiency has primary
classification **Stakeholder Validation Required**. Establish acceptable Bad
exposure, path distribution and recovery before making a fairness verdict.
Feasible business processing is separately a validated observation.
[Results](s1-s2-representative-results.md), [diagnostics](s2-fairness-path-analysis.md).

### S5 — Processing-Order Sensitivity

**Question / hypothesis.** How much does allocation change when exact S2 daily
request order is reversed?

**Controlled variable.** Only daily dispatch order changes. Identities, membership,
eligibility, original RequestedAt/AssignedAt, topology and BaselineSequence are
preserved; actual ExecutionSequence is the reverse. History evolves naturally.

**Measured evidence.** All requests still succeed. Aggregate S5 Bad is
9.109–9.300%, NotBalanced 10.342–11.198% and Recovery 96.250–97.102%: relatively
close to S2, with mixed directions of change. Yet only 40.52–41.26% of paired
attendee/day qualities are unchanged: approximately 59% change. Exact attendee/day
Bad overlap is **zero in every Seed**, even though some people receive Bad on
different days in both runs. Only 46.90–47.48% of classifiable paths retain their
classification. Bad remains concentrated in the last actual execution third,
which corresponds to the first original baseline third after reversal.

**Conclusion.** Aggregate fairness is relatively stable, while allocation is
materially sensitive to processing order. Aggregate stability does not establish
stable treatment of individual Attendees.

**Interpretation boundary.** One complete reversal across three Seeds does not
represent every order or real arrival behavior. It includes both within-day and
history-mediated effects; preserved simulated timestamps are not S5 execution
chronology. Reverse order is an experiment, not a proposed policy.

**Architectural implication.** Ordering is an allocation-relevant architectural
choice whose distributive consequences require explicit review.

**Decision Gate relevance.** Primary classification: **Material Risk** — material
distributive sensitivity. Discuss acceptable order sensitivity with stakeholders
and determine whether further action is needed; do not label S5 simply
"fairness stable." [Source](s2-s5-order-sensitivity-results.md).

### S6 — Large-Group Sensitivity

**Question / hypothesis.** Does greater large-group prevalence harm fairness or
prevent complete contiguous placement?

**Controlled variable.** Target request share for GroupSize 6–10 increases from
7% to 25%; daily demand, venue, eligibility parameter and normal processing order
are held constant. Generator streams include ScenarioId, so same-Seed cohorts
and requests are not exact S2 identity pairs.

**Measured evidence.** Each run successfully processes all 5,400 requests and
persists 22,000 Assignments, with zero rejections or rejection-based fragmentation
signals. Relative to S2, BadOutcomeRate increases +0.345 / +0.323 / +0.309 pp;
NotBalancedPathRate increases +0.521 / +1.237 / +0.717 pp. Within S6, size 6–10
Bad rates are 13.132–13.933%, versus 5.926–6.566% for size 1–5. All Bad outcomes
remain in the final execution third. Recovery-rate changes are mixed.

**Conclusion.** Large-group prevalence modestly worsens quality/path fairness
without observed feasibility or rejection loss. It alone was not a critical
feasibility risk under this topology.

**Interpretation boundary.** These are controlled scenario-level comparisons,
not attendee/day pairs. Size associations retain timing/history/eligibility
interactions. Zero rejections does not prove every group mix or spatial pattern
is feasible; residual inventory alone cannot demonstrate fragmentation.

**Architectural implication.** Monitor the fairness sensitivity without treating
large groups as demonstrated placement failures or introducing group restrictions.

**Decision Gate relevance.** Primary classification: **Acceptable Sensitivity**
for continued technical review within this tested feasibility envelope; business
acceptance of group-size effects remains open. Action: Monitor.
[Source](s2-s6-large-group-sensitivity-results.md).

### S7 — Spatial Fragmentation Sensitivity

**Question / hypothesis.** Does the spatial arrangement of equivalent occupied
capacity reduce access to valid contiguous Assignments?

**Controlled variable.** Exact paired S2 workload/requests and topology; each day
has 1,000 occupied and 3,998 available Spots in both variants, with equal
per-Row/Zone/quality quotas. Only occupied positions differ: concentrated Control
versus dispersed Fragmented. This is not a clean-inventory S2 comparison.

**Measured evidence.** Control RejectionRate is 7.732 / 7.910 / 7.992%; Fragmented
is 14.959% for every Seed. Published impacts are +7.227 / +7.049 / +6.967 pp
(mean +7.081 pp). All 1,095 Fragmented rejections per run have sufficient eligible
capacity but no feasible contiguous block. Exact request pairs show 959 / 976 /
983 Control successes becoming Fragmented rejections with that diagnosis.
Fragmented leaves 922 of 3,998 available Spots unused daily (23.062%); Control
closes with zero. The tested dispersed pattern has largest opening blocks of
four, so sizes 5+ all reject; that is a pattern-specific result.

**Conclusion.** Spatial fragmentation is a material feasibility/access risk
independent of a simple aggregate capacity-shortage explanation.

**Interpretation boundary.** The synthetic approximately 20% pre-occupation
pattern requires stakeholder validation. Five-day effects include diverging
histories/inventory; paired forward losses and reverse gains differ from net
rejection impact. Apparent fairness improvement among successful Fragmented
participants is a survivor/access-selection effect: classifiable populations
fall from 5,026–5,039 to 4,550–4,553. It does not establish improved fairness.

**Architectural implication.** Available Spot counts cannot guarantee complete
group access; physical block geometry must inform the later operating assessment.

**Decision Gate relevance.** Primary classification: **Material Risk**. Review
how this access risk must be addressed if the validated operating envelope admits
comparable fragmentation; validate the synthetic assumptions separately. No
fragmentation mitigation is selected. [Source](s7-fragmentation-impact-results.md).

### S8 — Concurrency / Operational Sensitivity

**Question / hypothesis.** Does concurrency preserve correctness and improve
useful delivery over the existing sequential path?

**Controlled variable.** Exact S2 business inputs at C1/C4/C16, three Seeds, one
run per configuration/Seed. C1 uses the direct sequential path; C4/C16 use
deterministic coordinated windows. Real PostgreSQL, RetryCount = 0 throughout.

**Measured evidence.** Every run has PersistedInvariantViolationCount = 0 and
UnexpectedTechnicalFailure = 0. Known conflicts are 0% / 75% / 93.702–93.716%
for C1/C4/C16. C1 completes all 7,320 requests; C4 completes 1,830 and C16
460–461. The published across-Seed means are:

| Measure | C1 | C4 | C16 |
| --- | --- | --- | --- |
| Terminal Attempts/s | 29.593 | 7.252 | 15.874 |
| Successful requests/s | 29.593 | 1.813 | 0.998 |
| Persisted Assignments/s | 88.940 | 3.705 | 2.133 |
| Per-run p95 latency, ms | 48.962 | 2,079.249 | 3,883.970 |
| Measured execution seconds | 247.624 | 1,012.897 | 461.485 |

**Conclusion.** Concurrency preserves data integrity while producing severe
contention and reducing useful delivery. C16's higher terminal Attempts/s than
C4 does not mean higher successful throughput; C1 outperforms both for every
Seed in latency, useful delivery and duration.

**Interpretation boundary.** One run per Seed/configuration does not quantify
scheduler/interleaving variability. Local environments match, but C1 ran in a
later process; fixed ordering, no warm-up and an unreserved host limit performance
generalization. C1 has 5,115–5,126 classifiable paths; C4 has 768–834 and C16
62–71. C16's 100% Acceptable rate among survivors does not establish better
fairness for all requesters; 4,411–4,460 have NoPersistedPath.

**Architectural implication.** Safe rollback and constraints protect consistency
but do not make the tested concurrent service behavior operationally suitable.
Coordination risk needs review before proposing concurrent operation.

**Decision Gate relevance.** Primary classification: **Material Risk** —
operational/coordination risk. Action: Mitigation Required for the tested
concurrent envelope, with no technical option chosen. Preserve S8's scoped
**evidence sufficient** decision: no concurrent repetition is required for this
comparison; broader variability remains unquantified. This is not a final Stage 5
verdict. [Source](s8-concurrency-analysis.md).

## Persisted correctness across scenarios

The completed reports record zero final persisted invariant findings across
official S1/S2/S5/S6/S7/S8 configurations. Audits check unique Spot/FestivalDay
and Attendee/FestivalDay, complete single-Zone contiguous groups, catalog and
request identity, terminal request/Assignment consistency and reconciliation.
S7 also validates supporting occupancy separately from evaluated Assignments.

S8 actually exercises conflict handling: losing transactions roll back without
partial Assignment graphs or fake persisted Rejected requests. Recognized
23505 conflicts and 40P01 deadlocks retain diagnostics; deadlocks are known
concurrency conflicts, not unexpected technical failures. Database constraints
remain final integrity protection. Conflict-free sequential runs alone do not
demonstrate exercised rollback handling.

These are final-state observations, not a complete temporal proof of INV-06 or
all possible interleavings. The unchanged flow does not replace/cancel completed
Assignments. **Correctness preserved and operational behavior acceptable are
separate findings**: S8 demonstrates the difference. Retain zero invariant
violations as a hard technical criterion in future evaluations.

## Fairness across scenarios

| Dimension | Consolidated interpretation |
| --- | --- |
| Favorable and expected baselines | S1's zero Bad/NotBalanced is conditional on favorable conditions; S2 supplies the primary baseline and reveals unfavorable trajectories despite full access/high eligible recovery. |
| Order | S5 shows relatively stable totals with substantial individual recipient/classification churn. Aggregate-only review misses distributive sensitivity. |
| Group size | S6 shows a modest scenario-level fairness worsening and higher unfavorable rates for larger groups, without observed placement loss. |
| Fragmentation/access | S7 changes who receives Assignments and how often; improved survivor metrics cannot offset omitted access loss or establish overall fairness improvement. |
| Concurrency/population | S8's smaller participating/classifiable populations truncate histories; zero Bad/NotBalanced among survivors cannot be compared as equivalent coverage to C1/S2. |

Do not pool these fairness percentages. S1/S2 differ in multiple inputs; S6
changes generated cohorts; S7 excludes setup Attendees and loses access; S8 loses
most attempts to contention. Always read Registered, Requesting, Participating,
only-rejected/failed, NoPersistedPath, InsufficientEvidence and Classifiable
alongside rates. High RecoveryRate cannot answer whether a path or access policy
is acceptable, and N/A recovery in S1 or concurrent S8 is not evidence of 0% or
100% recovery.

## Access and feasibility: different failure mechanisms

| Mechanism | Supported observation | Review implication |
| --- | --- | --- |
| Eligible capacity shortage | Some S7-Control rejections have eligible capacity below GroupSize: 268 / 282 / 245 cases. | Demand, eligibility and capacity are distinct from block geometry; positive capacity below GroupSize is still insufficient. |
| Spatial infeasibility | Every S7-Fragmented rejection has eligible capacity at least GroupSize but no feasible block. | Raw capacity can coexist with loss of access; the rejection code or closing inventory alone cannot diagnose the cause. |
| Persistence contention | S8 C4/C16 lose known races and roll back, without DomainRejected outcomes. | Report conflicts separately from business rejection, technical failure and corruption; assess useful delivery. |

S1/S2/S5/S6 have no observed domain rejection in their completed official runs.
That does not demonstrate feasibility under S7 geometry, combined stress or a
different demand envelope. The S7 capacity/block diagnostics come from the
reported sequential evaluation evidence; independent pre-save reads are not a
general transactionally consistent snapshot guarantee under concurrency.

## Performance and evidence strength

S2/S6/S7 timings are exploratory local Debug/PostgreSQL/Testcontainers observations:
published p95 ranges are S2 42.759–44.737 ms, S6 49.193–52.254 ms, S7-Control
35.452–37.702 ms and S7-Fragmented 34.117–35.170 ms. Different outcome mixes and
group/request counts prevent treating those figures as interchangeable useful
throughput or controlled performance effects.

S8 supplies stronger operational evidence through exact workload comparisons,
captured matching environment and a common monotonic five-day interval:
max(ProcessingStartedOffset + Duration) − min(ProcessingStartedOffset). Terminal
Attempts/s, successful requests/s and durable Assignments/s use that same
interval, including intervening checkpoint/scheduling gaps; overlapping durations
are never summed. All terminal attempted outcomes enter latency percentiles.
Scope setup, container startup and later export/reconciliation are excluded from
request latency. S8's C1 p95 is 46.382–51.821 ms; all C4/C16 p95 values exceed
one second. The **p95 < 1 second** hypothesis remains provisional, not an SLA.

Strong evidence below means a well-supported effect *inside the tested design*:
S5 has exact order pairs/reproduction; S7 has capacity-matched spatial pairs,
diagnostics and reproduction; S8 has exact inputs, direct conflict metadata and
durable reconciliation. Moderate evidence applies to S1/S2 combined baselines,
S6's non-identical generated cohorts and local timing generalization. Limited
evidence concerns extrapolation to production; Pending means no empirical
result. Three Seeds establish observed consistency, not production probabilities
or independent samples of all attendees. Deterministic logical reproduction
does not establish timing or concurrent interleaving reproducibility.

All local Docker/Testcontainers observations are environment-specific, without
HTTP/network/distributed-user latency or production-load validation. No-warm-up
and host activity limitations remain as recorded in each report. No cloud or
scaling claim follows.

## Stakeholder-dependent assumptions and unresolved acceptance

All rows below remain **Stakeholder Validation Required** for the post-proposal
organizational phase; they are not prerequisites for the initial proposal.
This consolidation does not supply their answers.

| Topic | Decision or validation needed |
| --- | --- |
| Acceptable fairness | Confirm perceived fairness, Good/Medium/Bad mapping, rubric thresholds, acceptable Bad/NotBalanced exposure, treatment of short/no paths and recovery distance/horizon. Technical metrics alone do not establish acceptance. |
| Processing order | Decide acceptable distributive sensitivity and intended arrival/priority semantics; validate actual arrival peaks, late-request behavior and the provisional 07:00–23:00 window. |
| Group behavior | Validate group-size prevalence/composition and acceptable differences in access/quality for temporary groups; do not infer authorization to relax indivisibility or contiguity. |
| Venue and spatial occupancy | Validate real capacities, Row/Zone geometry and physical contiguity, synthetic 20% pre-occupation and its spatial pattern. Confirm standing-area discretization (10 × 75 logical Spots), grandstand 12-row/+1 growth approximation and quality-capacity shares 35%/45%/20%. |
| Conflict and retry behavior | Establish acceptable conflict volume and user-visible failure/resubmission/retry expectations, including fairness implications. RetryCount = 0 is measured behavior, not stakeholder acceptance or a selected future policy. |
| Production concurrency | Validate realistic request bursts, overlapping requests and operating load; coordinated C4/C16 windows are sensitivity experiments, not measured production traffic. |
| Demand and participation | Validate 5,500 registrations versus approximately 5,000 physical capacity, 3,300/4,400 daily demand, five-day horizon, 90% daily requester continuity and group/arrival distributions. Simulated dates are fixtures. |
| Eligibility | Validate the conservative 50% request-level FrontStanding baseline against the separate 75–80% business hypothesis; neither is observed festival evidence. |
| Service expectations | Agree acceptable rejection, latency and successful delivery for the intended workload/environment; no fairness, conflict or production SLA threshold is invented here. |

## Pending combined/stress evidence: S3 and S4

S3/S4 are retained for the later data-informed phase after proposal and
organization interest. They do not block the internal pre-proposal Gate.

**S3 — Stress** remains unimplemented/unexecuted. Its documented purpose is to
assess controlled degradation under credible combined pressure: higher demand,
larger groups, eligibility restrictions, high utilization/fragmentation,
constrained Good capacity, less favorable order and high concurrency. It should
reduce uncertainty about interacting effects on rejection, paths/recovery,
latency, technical failures and invariants.

**S4 — Worst Plausible** remains unimplemented/unexecuted. Its documented purpose
is to locate the practical operating boundary under a credible unfavorable
combination and determine which limitation dominates, whether degradation is
graceful and whether invariants remain protected.

Detailed parameters and validated acceptance criteria remain undefined; the
high-level envelope comes from framework sections 23–24, not a newly designed
matrix. Isolated effects cannot be added or extrapolated into S3/S4 results.
Action for the future formal Gate: **Additional Evidence Required**, with real
operating-data validation and credible inputs/acceptance questions agreed before
separate design/execution tasks. S8's evidence-sufficient decision does not close
either scenario or either Gate.

## Decision Gate evidence matrix

This matrix prepares the future formal stakeholder/production Gate. Its risk
findings also inform the internal MVP review, without making later stakeholder
or S3/S4 prerequisites mandatory before proposal.

Each finding has one primary classification. **Validated Behavior** means
observed within the tested envelope. **Acceptable Sensitivity** here means bounded
technical sensitivity that permits continued review, without stakeholder/business
approval. **Material Risk** identifies observed consequences requiring explicit
Gate discussion. **Unresolved Uncertainty** identifies missing evidence.
**Stakeholder Validation Required** identifies decisions technical measurements
cannot make. Action labels are review dispositions, not a final Gate verdict.

| Area | Evidence | Strength | Impact | Status / primary classification | Decision Gate Action |
| --- | --- | --- | --- | --- | --- |
| Favorable behavior | S1 zero Bad/NotBalanced with full request success | Moderate: combined favorable inputs | Favorable reference demonstrated | Validated Behavior | Accept within tested favorable envelope |
| Baseline fairness | S2 Bad 9.241–9.300%, NotBalanced 10.811–11.510%, high eligible recovery | Moderate: three provisional baselines | Fairness sufficiency remains unapproved | Stakeholder Validation Required | Stakeholder Validation Required: calibrate acceptable exposure/path/recovery |
| Processing-order sensitivity | S5 approximately 59% quality changes, zero paired Bad overlap, substantial path churn | Strong for exact reversal | Material distributive sensitivity hidden by stable aggregates | Material Risk | Stakeholder Validation Required: decide acceptable order sensitivity and consequent action |
| Large-group sensitivity | S6 modest quality/path worsening; 100% request success, zero rejection | Moderate: scenario-level controls, different cohorts | Fairness sensitivity without demonstrated feasibility loss | Acceptable Sensitivity | Monitor within tested topology; group acceptance remains open |
| Spatial fragmentation | S7 +6.967–7.227 pp rejection, sufficient-capacity/no-block pairs, 922 unused daily | Strong for tested synthetic pattern | Material feasibility/access loss | Material Risk | Mitigation Required if intended operating envelope admits comparable fragmentation; no option selected |
| Concurrent persistence contention | S8 C4 75%, C16 approximately 93.7% conflicts with complete rollback | Strong for observed windows; single run/Seed | Severe loss of useful delivery and participation | Material Risk | Mitigation Required for tested concurrent envelope; evaluate coordination without choosing architecture |
| Persisted correctness | Zero final persisted findings; safe S8 losing graphs | Strong within audited cases; not full temporal proof | Integrity preserved despite operational loss | Validated Behavior | Accept observed evidence; retain zero-violation hard criterion |
| Latency / useful throughput | S8 C1 materially exceeds C4/C16 delivery; C4/C16 fail provisional p95 hypothesis | Moderate: local environment, no warm-up, fixed order | Operational limitation; no production performance guarantee | Material Risk | Mitigation Required for concurrent service envelope; assess successful delivery and latency together |
| Stakeholder fairness assumptions | No validated fairness/order/group acceptance criteria | Limited: organization validation absent | Technical metrics cannot authorize acceptance | Stakeholder Validation Required | Stakeholder Validation Required |
| Synthetic fragmentation assumptions | Approximately 20% pre-occupation and spatial pattern are analytical inputs | Limited for real festival prevalence | Determines applicability of measured S7 risk | Stakeholder Validation Required | Stakeholder Validation Required: validate venue/occupancy patterns |
| Production load and service assumptions | Demand, eligibility and concurrency patterns remain provisional | Limited for production generalization | Operating envelope/thresholds unconfirmed | Stakeholder Validation Required | Stakeholder Validation Required |
| Concurrent variability / extrapolation | One run per Seed/configuration; no multi-node/production traffic evidence | Limited | Exact interleaving variability remains unknown; does not reverse S8's scoped evidence decision | Unresolved Uncertainty | Monitor limits; Additional Evidence Required only for a newly justified variability/generalization question, no automatic repeats |
| Pending S3/S4 | No combined/stress results or detailed validated parameters | Pending | Interaction effects and practical boundary unresolved | Unresolved Uncertainty | Additional Evidence Required after separately scoped design |

## Pre-proposal arrival-concurrency supplement

**Open-loop High-rate Arrival Stress = Completed; Plausible Festival Arrival
Concurrency = Defined / Pending Execution.** The high-rate experiment was initially
defined as production-like;
post-run analysis identified sustained overload. Moderate offers 40 req/s and
Burst-like ≈ 61.5 req/s, above the retained [S8](s8-concurrency-analysis.md#across-seed-aggregate-comparison)
C1 mean of 29.593 successful req/s. The most direct interpretation is unfinished
work accumulation, increasing active concurrency/contention and duration, then
further backlog growth; this is not a proven queueing model. The finite workload
drains daily. S2's busiest hourly bucket averages approximately 293 requests/hour
(0.081 req/s); that average does not rule out short bursts. The observed conflicts,
technical failures, latencies and hundreds/thousands of active requests are not
production predictions. Plausible festival concurrency still requires evidence.
The [bounded contract](plausible-festival-arrival-concurrency.md) is now defined:
120× S2 bucket compression, Plausible uniform spacing and PlausibleBurst groups
of three at 0/10/20 ms with equal bucket averages. Peak offered load is 9.767 req/s;
collection must verify absence of sustained accumulation. Four initial runs and
at most two pattern-specific conditional Seed 3001 runs are planned, not executed.

The [high-rate arrival analysis](production-like-arrival-concurrency-analysis.md)
preserves exact S2 inputs with 25 ms Moderate and deterministic 5/50 ms Burst-like
arrivals. Severe degradation remains observable without a synchronized start
barrier under these high-rate open-loop arrival patterns:
Moderate p95 is 21.414–22.968 seconds and useful requests/s 4.946–5.100;
Burst-like p95 is 41.404–50.179 seconds and useful requests/s 0.159–0.202.
All valid runs include unexpected technical outcomes. Lower Burst-like conflict
fractions coexist with more technical failures and worse useful delivery, not
improved access. Actual peaks of 672–685 / 1,295–1,377 differ from S8's bounded
4/16 windows; these comparisons do not isolate synchronization as a causal effect
or estimate production behavior.

Four valid-run audits report zero persisted/execution findings. The original
stopped BurstLike/1001 retains three audit findings arising around two post-commit
RECONCILIATION_FAILED observations. It is excluded from the clean comparison;
its disposed database cannot now be independently re-audited for the unmatched
request statuses. The [remediation record](arrival-observer-audit-remediation.md)
documents the audit assumption, tests and replacement before execution. Original
outcomes/findings are not overwritten or averaged away. Subsequent audits require
real Completed requests and complete valid graphs for final-state correlation;
observer errors remain UnexpectedTechnicalFailure. Generic measured-path failures
retain their classification and their unavailable inner causes remain a limit.

This completes the high-rate stress experiment with a clear operational concern;
it does not establish whether concurrency is material under plausible festival
arrival rates or make the MVP Gate decision. Next: **Implement and Execute Plausible
Festival Arrival Concurrency Experiment** under the defined contract, before
late-group reconfiguration. Stage 5 remains In Progress; the MVP Gate and future
formal stakeholder Gate remain Pending.

## Mitigation boundary and next review task

"Mitigation Required" identifies a supported risk to address in a defined
operating envelope; it does not select retries, locking or serialization. Future
Decision Gate evaluation may consider request serialization, retry strategy,
inventory reservation/locking, queueing, different assignment-selection
coordination or fragmentation-aware allocation. These are candidate areas only,
with no preferred option, implementation or authorization to change policy.

The [MVP roadmap](mvp-technical-feasibility-gate.md) now sets the immediate next
task: **Implement and Execute Plausible Festival Arrival Concurrency Experiment**,
followed by definition/execution of Late Large Groups with
Reconfiguration and the internal feasibility Gate. High-rate stress is completed;
plausible festival concurrency is Defined / Pending Execution. If a proposal attracts organization interest, the
[formal criteria and evidence plan](decision-gate-criteria.md) govern stakeholder
validation, data-informed S3/S4 and applicable mitigation evaluation before the
eventual formal Continue / Tune / Pivot / Stop review.
No scenario rerun or final architecture/go-no-go decision is made by this
consolidation.
