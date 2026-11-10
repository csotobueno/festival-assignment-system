# Stage 5 — Decision Gate Acceptance Criteria and Remaining Evidence Plan

**Stage 5 = In Progress. MVP Technical Feasibility Gate = Pending.**
**Formal stakeholder Decision Gate = Pending / future phase. Criteria defined.**
Stage 5 is not Evidence Complete and the formal Decision Gate is not ready.

## Phase boundary

This document is preparatory evidence and criteria for the future formal
stakeholder/production Decision Gate. All stakeholder, S3/S4, service-validation
and mitigation prerequisites below apply to that later review, not to making
the initial proposal. They remain necessary if the organization shows interest.
The immediate pre-proposal roadmap and internal criteria are owned by the
[MVP Technical Feasibility Gate](mvp-technical-feasibility-gate.md): two remaining
experiments, internal review, then proposal if technically supported. Technical
feasibility for proposal is distinct from production readiness and organization
approval; this repositioning changes no measured evidence or material risk status.

## Purpose and authority

> What must be true, accepted or demonstrated before the current design can
> advance through the future formal stakeholder Decision Gate?

Persisted domain invariants must remain protected; stakeholders must decide
whether the measured fairness and operating envelope are acceptable; combined
stress and worst-plausible evidence must be analyzed; and material risks must
receive an explicit disposition. These requirements prepare the eventual
Continue / Tune / Pivot / Stop review without choosing its outcome.

The [consolidated evidence](decision-gate-evidence.md) owns the findings from
S1/S2/S5/S6/S7/S8. The [metrics framework](metrics-and-evaluation-criteria.md)
remains authoritative for metrics, denominators, rubric, hard technical criteria
and provisional hypotheses. The [representative contracts](representative-scenarios.md)
own S1/S2 inputs; [simulation architecture](simulation-architecture.md) owns
execution and evidence boundaries. Official [S1/S2](s1-s2-representative-results.md),
[S5](s2-s5-order-sensitivity-results.md), [S6](s2-s6-large-group-sensitivity-results.md),
[S7](s7-fragmentation-impact-results.md) and [S8](s8-concurrency-analysis.md)
reports retain measured values and provenance. This document maps their findings
to decisions; it does not replace, recompute or pool measured evidence.

All acceptance statements are scoped to an explicitly documented workload,
venue, eligibility, ordering, concurrency and measurement environment. Technical
MVP progression does not establish organization approval or production readiness.

## Evidence status and decision ownership

Each criterion has one primary current status:

| Status | Meaning |
| --- | --- |
| Satisfied | Completed evidence supports the criterion within its stated scope. Future changes or broader claims still need appropriate validation. |
| Decision Required | Evidence or provisional assumptions need an explicit stakeholder or joint decision. |
| Additional Evidence Required | A defined question cannot yet be answered from completed evidence. |
| Mitigation Evaluation Required | An observed material risk requires evaluation against an accepted outcome if its tested envelope applies. No solution is selected. |

These are criterion statuses, separate from the evidence document's finding
classifications and the framework's hard technical / engineering / business
criterion types. Monitoring is a next action, not another status.

Ownership uses **Technical**, **Stakeholder** or **Joint** where responsibility
is clear. Generic stakeholder groups below identify appropriate review audiences;
the repository does not assign named accountable owners. Confirm accountable
participants when capturing decisions. For every decision record the answer,
rationale, evidence reference, accepted envelope, threshold where needed, owner
and consequences for next actions. No stakeholder answer is assumed here.

Where no validated threshold exists, **Stakeholder threshold required** applies.
It applies to Bad exposure, NotBalanced paths, recovery behavior, order sensitivity,
rejection including fragmentation, conflicts, latency and useful delivery.
Acceptance may include an explicit qualitative boundary rather than an invented
percentage. The framework's initial fairness hypothesis (Balanced + Acceptable
at least 85%) remains provisional and cannot substitute for stakeholder approval.

## Technical Acceptance Criteria

| ID / topic / ownership | Decision Question | Acceptance Criterion | Current Evidence | Current Status | Required Next Action |
| --- | --- | --- | --- | --- | --- |
| T1 — Persisted correctness / Technical | Do current and future acceptable architectures protect persisted domain invariants, including under conflicts? | Persisted domain invariants must remain protected. Require zero confirmed persisted invariant violations in every accepted evaluation; any confirmed violation blocks progression with that implementation. Preserve Spot/FestivalDay and Attendee/FestivalDay uniqueness, complete single-Zone contiguous groups, Assignment identity/finality and request/outcome consistency. | S1/S2/S5/S6/S7/S8 report PersistedInvariantViolationCount = 0. S8 exercises full rollback of losing graphs without partial Assignments or fake Rejected requests. | Satisfied | Retain these audits and zero-violation requirement for S3/S4 and any future architecture or mitigation validation. Final-state audits are not a complete temporal proof of INV-06 or every possible interleaving. |
| T2 — Favorable reference / Technical | Is favorable valid placement demonstrated? | Accept S1 as a favorable reference only within its tested envelope; use S2 for baseline fairness. | S1 completes all requests with zero Bad/NotBalanced; no recovery episodes means RecoveryRate is N/A. | Satisfied | Retain the reference; do not extrapolate favorable headroom into stress or production acceptance. |
| T3 — Feasibility and failure diagnosis / Joint | Can acceptable access be demonstrated without confusing capacity, block geometry and contention? | Evaluate access against the agreed demand/eligibility envelope. Distinguish eligible capacity below GroupSize, sufficient eligible capacity without a contiguous block, known rolled-back conflict and unexpected failure. Stakeholder threshold required for acceptable rejection/access loss. | S1/S2/S5/S6 have no domain rejections. S7-Control includes capacity shortage and spatial infeasibility; S7-Fragmented rejections all have sufficient eligible capacity/no block. S8 C4/C16 lose access through conflicts, not DomainRejected. | Decision Required | Agree acceptable access/rejection with B9 and realistic assumptions with B4–B7/B10; apply cause-specific analysis in E1/E2 and M1/M2. Independent pre-save reads are not a transactionally consistent concurrent snapshot. |
| T4 — Large-group technical sensitivity / Technical | Does increased large-group prevalence demonstrate a critical feasibility failure? | Large-group behavior must remain within an accepted fairness/feasibility envelope. Completed S6 supports continued technical review without a critical mitigation requirement; business acceptance remains B3. | S6 raises size 6–10 request share from 7% to 25%, keeps full placement/zero rejection, and modestly worsens Bad (+0.309–0.345 pp) and NotBalanced (+0.521–1.237 pp). | Satisfied | Monitor fairness and feasibility in future accepted envelopes; obtain B3. Scenario-level cohorts are not exact attendee pairs; zero S6 rejection does not prove arbitrary mixes or fragmentation feasible. |
| T5 — Spatial fragmentation / Joint | Can the system accept observed rejection caused by spatial fragmentation despite available capacity? | For the tested applicable envelope, address material feasibility/access loss through validated mitigation evaluation. Stakeholder threshold required for acceptable spatial rejection. Applicability must be resolved separately through B4. | S7 exact-workload spatial pairs show +6.967–7.227 pp rejection; Fragmented has 14.959% rejection, all sufficient-capacity/no-block, and 922 unused Spots daily. | Mitigation Evaluation Required | Complete B4, then M1 if comparable fragmentation is realistic. If stakeholders determine this synthetic envelope operationally unrealistic, document the basis and relevant operating boundary; do not erase the measured risk. |
| T6 — Concurrency / Joint | Is the concurrent operating behavior acceptable despite preserved integrity? | The current concurrent behavior is not production-acceptable within the tested synchronized-concurrency envelope unless (1) a mitigation is validated, or (2) the real production operating model demonstrably avoids that envelope. Correct rollback alone cannot satisfy operational acceptance. | S8 C1 has 0% conflicts/full delivery; C4 has 75%; C16 has 93.702–93.716%, with zero persisted findings. Useful delivery and participation collapse. | Mitigation Evaluation Required | Resolve B5/B8–B10; evaluate M2 or demonstrate avoidance under E4. Stakeholder threshold required for conflict/service acceptance. Preserve S8's scoped evidence-sufficient decision; no automatic C4/C16 repeats. |
| T7 — Useful throughput and evidence interpretation / Technical | Does operational evaluation measure durable useful delivery rather than fast terminal failures or selected survivors? | Prioritize SuccessfulRequestThroughput and SuccessfulAssignmentThroughput alongside latency, access and outcome mix. Keep Terminal Attempts/s distinct. Retain population coverage and all existing denominators/N/A rules. | S8 C16 has more terminal Attempts/s than C4 but fewer durable successes; survivor populations shrink in S7/S8. S8 publishes reconciled useful throughput over a common interval. | Satisfied | Apply the same interpretation to E1/E2 and M1/M2. Service acceptance remains unresolved in B8/B9 and T6; this status accepts the evidence method, not concurrent performance. |
| T8 — Reliability and safe failure / Technical | Are operational failures visible and investigated without hiding persisted corruption? | Preserve safe rollback and distinguish known conflicts from unexpected failures. Categorize/investigate any non-zero UnexpectedTechnicalFailure before a Gate conclusion; no final production error-rate threshold exists. | Completed official evidence reports no unexpected technical failures; S8 records recognized 23505 conflicts and 40P01 deadlocks as known conflicts after rollback. | Satisfied | Retain diagnostic/reconciliation coverage in E1/E2 and mitigation evaluation. Any unresolved critical correctness issue blocks progression under T1. |

For T7, use the [S8 measurement boundary](s8-concurrency-analysis.md#performance-and-useful-throughput-c1-vs-c4-vs-c16-per-seed):
terminal Attempts, successful requests and durable Assignments divided by the
same interval from first measured use-case entry to last terminal outcome.
Never sum overlapping durations. Retain p50/p95/p99, sample counts, outcome mix
and environment/warm-up limitations. Existing local use-case latency excludes
scope preparation, HTTP/network/frontend and other external user-flow costs.

Fairness review must retain Requesting/Participating, only-rejected/failed,
NoPersistedPath, InsufficientEvidence and Classifiable populations. Bad rates
use persisted evaluated Assignments; path rates use ClassifiablePaths; recovery
uses eligible episodes and persisted opportunities. Rejection/conflict/absence
does not create synthetic quality or recovery opportunity. Apparent survivor
improvement in S7/S8 cannot establish overall fairness improvement. Recovery
does not erase Bad exposure; N/A is neither zero nor success.

## Stakeholder Acceptance Criteria

All rows currently have **Decision Required** as their primary status. Every
question needs a recorded required decision and next action; technical findings
alone cannot supply business acceptance. Group labels are generic review
audiences, not newly assigned positions or named decision makers.

| ID / topic | Question | Why it matters | Current evidence | Required decision / next action | Ownership / stakeholder group | Decision status |
| --- | --- | --- | --- | --- | --- | --- |
| B1 — Baseline fairness | Is this baseline fairness profile acceptable for the festival? | Placement success/high recovery do not establish acceptable experience distribution. | S2 BadOutcomeRate 9.241–9.300%, NotBalancedPathRate 10.811–11.510%, RecoveryRate 96.621–97.236%; some eligible episodes remain unresolved. | Record acceptable Bad experience share, NotBalanced path share and recovery behavior including distance/horizon. Validate perceived quality mapping/rubric and treatment of short/no paths. Stakeholder threshold required; existing provisional fairness hypothesis supplies no approval. | Stakeholder / Festival organizers, Product/business stakeholders | Decision Required |
| B2 — Processing-order sensitivity | Is this level of distributive sensitivity to request processing order acceptable? | Similar aggregate fairness can allocate unfavorable outcomes to different people. | S5 approximately 59% attendee/day quality changes, zero exact attendee/day Bad overlap and only 46.90–47.48% classifiable paths retaining classification; aggregate rates relatively stable. | Accept the measured sensitivity and intended arrival/priority semantics explicitly, or require mitigation evaluation with an agreed outcome. Stakeholder threshold required; no arbitrary sensitivity percentage. Validate actual peaks/late-request behavior through B7. | Stakeholder / Festival organizers, Product/business stakeholders | Decision Required |
| B3 — Large-group behavior | Are group-size prevalence, composition and fairness/feasibility effects acceptable? | S6 technical feasibility does not approve different experience distributions for groups. | T4: full placement with modest fairness worsening; larger-group Bad associations retain timing/history/eligibility interactions. | Validate request-level group distributions/composition and acceptable effects; define the accepted envelope and monitor T4. Stakeholder threshold required. Do not authorize splitting groups or changing contiguity. | Stakeholder / Festival organizers, Product/business stakeholders | Decision Required |
| B4 — Fragmentation realism and venue | Is the tested fragmentation/preoccupation pattern representative enough of real festival operations? | Determines applicability of T5; aggregate capacity cannot validate geometry. | S7 synthetic approximately 20% preoccupation (1,000 Spots) and dispersed pattern are provisional. Venue topology, capacity and quality shares are also hypotheses. | Validate occupancy amount/positions and plausible block patterns, usable Zone/Row capacities, physical contiguity, 35%/45%/20% quality-capacity shares, 12-row/+1 grandstand approximation and FrontStanding 10 × 75 logical discretization. Record whether comparable fragmentation belongs in the operating envelope and the basis for any exclusion. | Joint / Technical, Festival organizers, Operations | Decision Required |
| B5 — Expected production concurrency | What bursts and overlapping requests must the intended operating model support? | C4/C16 coordinated windows are experiments, not measured production arrival traffic; C1 superiority does not itself validate a production model. | S8 severe contention under synchronized windows; sequential C1 full useful delivery. | Agree numeric in-flight/load and arrival/burst assumptions with their operational basis. If relying on avoiding the tested envelope, require E4 demonstration; otherwise carry T6/M2. No scheduling solution is selected. | Joint / Technical, Operations, Product/business stakeholders | Decision Required |
| B6 — Realistic demand and continuity | Which population, daily participation and historical horizon are credible? | Input realism governs generalization of every completed result and S3/S4 design. | 5,500 registrations versus approximately 5,000 physical capacity (4,998 modeled); S1/S2 daily requesters 3,300/4,400; five days; 90% daily requester continuity. | Validate each assumption separately, including returning requester replacement, initial history and five-day applicability. Keep registered population distinct from physical capacity and requests from Attendees. Simulated dates are fixtures. | Stakeholder / Festival organizers, Operations | Decision Required |
| B7 — Arrivals and assignment window | Are the modeled peaks, late requests and daily window credible? | Simulated time controls ordering; it does not replay actual traffic overlap. | S1 smoother arrivals, S2 peaks/tail and provisional 07:00–23:00 window; S5 reverses processing while retaining original timestamps. | Validate arrival profiles, event/assignment window and late-request/priority expectations; feed B2/B5 and S3/S4 input definitions. | Joint / Technical, Operations, Festival organizers | Decision Required |
| B8 — Response-time expectations | What response-time expectation is acceptable for the user-facing assignment flow? | Local engine/use-case timing is only part of the user experience. | S8 C1 p95 46.382–51.821 ms; every C4/C16 p95 exceeds one second. p95 < 1 second remains a provisional analytical hypothesis, not an SLA. | Agree the service target, measurement boundary, workload/environment and applicable percentile. Stakeholder threshold required. Identify any additional end-to-end evidence needed; do not convert the provisional hypothesis into a hard Gate criterion. | Joint / Technical, Product/business stakeholders, Operations | Decision Required |
| B9 — Conflict, rejection and useful delivery expectations | What failure/resubmission/retry experience and durable service delivery are acceptable? | Correct rollback or a high terminal Attempt rate can conceal severe access loss. | RetryCount = 0 is measured behavior; S8 conflicts dominate C4/C16; S7 rejects despite capacity; T7 distinguishes useful throughput. | Agree acceptable conflicts, business rejection, successful delivery and user-visible failure/resubmission/retry expectations, including fairness consequences. Stakeholder threshold required. This defines desired behavior without selecting a retry or coordination policy. | Joint / Technical, Product/business stakeholders, Operations | Decision Required |
| B10 — Eligibility assumptions | What request-level eligibility distribution is credible? | Restricted capacity affects feasible blocks, fairness and stress inputs. | Shared 50% FrontStandingAllowedRequestShare is a conservative input; separate 75–80% business hypothesis is unvalidated. Neither is observed festival data. | Validate request-level eligibility and daily distribution; record applicability to S3/S4. Do not convert it to permanent attendee eligibility or change the existing policy. | Stakeholder / Festival organizers, Product/business stakeholders | Decision Required |

## Remaining Evidence Requirements

| ID / topic / ownership | Decision Question | Acceptance Criterion / Required Decision | Current Evidence | Current Status | Required Next Action |
| --- | --- | --- | --- | --- | --- |
| E1 — S3 combined stress / Joint | Does the design degrade acceptably under credible combined pressure? | Complete/analyze S3 against agreed inputs and acceptance questions below, preserving T1 and distinguishing access, fairness, reliability and operational degradation. | Only framework section 23/high-level purpose exists; no detailed validated parameters or results. Isolated effects cannot be added into combined predictions. | Additional Evidence Required | Resolve prerequisite stakeholder assumptions, define the scenario contract, then separately implement/execute/analyze S3. |
| E2 — S4 worst plausible boundary / Joint | Where does the practical boundary lie, and how does the system fail or degrade? | Complete/analyze S4 under a justified worst-plausible envelope; identify dominant limitation, degradation mode, operational survivability and correctness, without requiring ideal fairness. | Framework section 24 supplies framing, not inputs/results. | Additional Evidence Required | Agree credible boundary inputs and interpretation, then separately implement/execute/analyze S4 after reviewing S3. |
| E3 — Variability and generalization limits / Joint | Which claim still needs evidence beyond the scoped local comparison? | Limit claims to measured conditions; explicitly review scheduler variability, environment and production extrapolation. Obtain additional evidence only for a specific decision-relevant unanswered question. | S8 evidence is sufficient for its local C1/C4/C16 comparison; one run per Seed/configuration does not quantify interleaving variability. Local Debug/no warm-up/fixed order/unreserved host supplies no production or multi-node guarantee. | Decision Required | At B5/B8/B9 review, accept the stated limits for the intended claim or define the precise new question, evidence and scope. No blanket repetition, cloud benchmark or scenario rerun is required here. |
| E4 — Operating-model avoidance claim / Joint | If mitigation is not validated, can the real operating model demonstrably avoid S8's unacceptable envelope? | Support any avoidance-based acceptance with documented workload/burst assumptions and evidence that the intended operating model stays outside the tested problematic envelope while delivering the agreed service. A preference for C1 or an unsupported low-load assertion is insufficient. | Completed S8 shows the failure envelope and sequential reference, not validated real production traffic/avoidance. | Additional Evidence Required | Resolve B5/B7/B8/B9, then provide the claim-specific demonstration if this route is used. A validated M2 route can close this conditional requirement as inapplicable with rationale. No implementation route is selected now. |

### S3 evidence objectives

Use the existing [Stress framing](metrics-and-evaluation-criteria.md#23-stress-scenario):
high participation/utilization, increased medium/large groups and eligibility
restrictions, elevated fragmentation, constrained Good capacity, less favorable
order and high concurrency. These are qualitative dimensions, not newly fixed
input values. Stakeholder/architectural definition of exact inputs and numeric
concurrency/arrival configuration is required before execution. Do not silently
combine S6/S7/S8 parameters into an approved S3 contract.

S3 must answer:

- Does correctness remain protected with zero persisted violations and safe
  failure/reconciliation under the combined envelope?
- Does feasibility remain acceptable under agreed access criteria, and how do
  demand, eligible capacity and fragmentation interact? Separate spatial
  rejection, ordinary shortage, conflict and unexpected failure.
- Do Bad exposure, NotBalanced paths, recovery and short/no-path populations
  degrade materially relative to the S2 reference under the agreed interpretation?
- Are latency, useful throughput, conflicts and technical failures within the
  agreed stress operating envelope? Explain controlled degradation and dominant
  interactions without claiming isolated causal effects from a combined scenario.

### S4 evidence objectives

Use the existing [Worst Plausible framing](metrics-and-evaluation-criteria.md#24-worst-plausible-scenario):
very high participation, unfavorable concentration of large groups, significant
eligibility restrictions, near-capacity utilization, high fragmentation, scarce
Good/Medium capacity, adverse order and very high but realistic concurrency.
Exact values and operational survivability expectations remain a joint definition
task; an artificially impossible configuration cannot establish a plausible
festival boundary.

S4 must demonstrate where practical failure boundaries occur, which limitation
dominates first, whether degradation remains graceful/understandable and whether
the system remains operationally survivable under the agreed interpretation.
Report rejection/access loss, fairness/recovery deterioration with coverage,
latency/useful delivery, conflicts and unexpected failures. Require protected
invariants and reconciled terminal outcomes throughout the evaluated flow; do
not require ideal fairness or infer any result from isolated scenarios.

Both scenarios need exact manifests/input provenance, baseline/engine/rubric
versions, explicit initial histories, five-day accumulated persisted history,
numeric load/order configuration, environment/timing boundaries, per-run audits,
traceable measured results and stated limits under the existing
[reproducibility](metrics-and-evaluation-criteria.md#27-reproducibility-requirements)
and [evidence contracts](simulation-architecture.md#artifacts-and-evidence-versioning).
No seeds, run counts, distributions, new telemetry or thresholds are selected here.

## Mitigation Evaluation Requirements

These rows define only the problem, required outcome and validation evidence.
Evaluation is not implementation approval or a selected architecture.

| ID / ownership | Problem / Decision Question | Acceptance outcome required | Current Evidence | Current Status | Required Next Action / validation evidence |
| --- | --- | --- | --- | --- | --- |
| M1 — Fragmentation / Joint | How can avoidable rejection from unusable spatial capacity be reduced when comparable fragmentation is operationally realistic? | Reduce fragmentation-driven access loss within an agreed fairness/feasibility envelope while protecting invariants, group completeness, single Zone and physical contiguity. Stakeholder threshold required. | T5/S7 establish material sufficient-capacity/no-block rejection; B4 applicability remains open. | Mitigation Evaluation Required | After B4, evaluate candidates using documented, controlled comparisons with spatial capacity/block evidence, group access, residual capacity, coverage, fairness/recovery, performance and zero persisted findings. If unrealistic, explicitly disposition the applicability boundary instead. |
| M2 — Concurrency / Joint | How can conflict/coordination loss be reduced while preserving correctness and useful throughput? | Demonstrate durable useful delivery and response/failure behavior within agreed workload/service expectations, with invariants protected. Stakeholder threshold required. | T6/S8 establish safe rollback with severe conflict, latency and delivery loss. | Mitigation Evaluation Required | Unless E4 demonstrably excludes the envelope, evaluate candidates against documented applicable workload controls, reporting known conflict vs unexpected failure, p50/p95/p99, common-interval terminal/useful throughput, coverage, fairness/recovery and independent persisted audits. Assess variability when relevant to the candidate's claim, without automatic baseline repeats. |

If B2 rejects measured order sensitivity, it must open a separately scoped
mitigation evaluation specifying the distributive problem, accepted outcome and
paired individual/aggregate evidence needed. B2 remains Decision Required now;
S5 does not select a mitigation. S6 alone establishes no critical mitigation need.

Explicit deferral of required evaluation must record rationale, owner, affected
claims/envelope and the condition for reopening. Deferral may complete the
evidence disposition for a conditional or non-advancing Gate outcome; it cannot
make the unacceptable T5/T6 envelope accepted or permit progression without
satisfying the relevant technical requirement. No mitigation is prescribed here.

## Finding-to-criterion coverage

This cross-reference covers every topic in the
[consolidated evidence matrix](decision-gate-evidence.md#decision-gate-evidence-matrix).
The referenced criterion rows contain the decision question, required acceptance
or decision, current evidence, one primary status and next action.

| Consolidated finding | Decision Gate treatment |
| --- | --- |
| Favorable behavior | T2; S1 remains a conditional favorable reference. |
| Baseline fairness | B1; S2 is the acceptance reference. |
| Processing-order sensitivity | B2; individual distribution requires a decision despite stable totals. |
| Large-group sensitivity | T4/B3; monitor technical sensitivity, obtain business acceptance. |
| Spatial fragmentation | T3/T5, B4/B9, M1; separate spatial risk, realism and rejection expectations. |
| Concurrent persistence contention | T6/T8, B5/B9, E4/M2; integrity and operating acceptability are distinct. |
| Persisted correctness | T1; zero violations remains mandatory for future acceptable architectures. |
| Latency / useful throughput | T7, B8/B9, T6/M2; durable service and agreed response expectations. |
| Stakeholder fairness assumptions | B1–B3; perceived quality/rubric, order and group acceptance. |
| Synthetic fragmentation assumptions | B4; provisional preoccupation, geometry and usable capacity. |
| Production load and service assumptions | B4–B10, E4; venue, demand, continuity, arrivals, concurrency, eligibility and service. |
| Concurrent variability / extrapolation | E3; scoped S8 sufficiency retained, future evidence justified by a question. |
| Pending S3/S4 | E1/E2; combined degradation and worst-plausible boundary evidence. |

## Ordered remaining evidence plan

This is the **post-proposal formal Gate plan**, conditional on organization
interest. It does not precede the internal MVP Gate or initial proposal. The
[immediate roadmap](mvp-technical-feasibility-gate.md#immediate-roadmap-and-gate-outcomes)
first requires Production-like Arrival Concurrency and Late Large Groups with
Reconfiguration, followed by the internal feasibility review.

1. **Capture stakeholder acceptance decisions.** Resolve B1–B3/B8/B9, including
   thresholds or explicit qualitative boundaries, perceived fairness and
   user-visible failure expectations. Record owners, rationale and consequences;
   do not supply stakeholder answers from technical metrics.
2. **Confirm operational assumptions.** Resolve B4–B7/B10: realistic fragmentation,
   venue/quality geometry, demand/continuity, eligibility, arrivals and concurrency.
   Review E3 limits and determine whether E4 is needed. Document which material
   risks apply; exclusion requires a supported operating boundary.
3. **Finalize S3/S4 contracts.** Use E1/E2's existing framework dimensions to agree
   exact credible inputs, reproducibility, observation/measurement boundaries,
   acceptance questions and interpretation of controlled degradation/survivability.
   Stakeholder/architectural definition precedes implementation/execution.
4. **Execute and analyze S3 in a separate task.** Review combined interactions
   against agreed criteria, retain evidence and update risk dispositions.
5. **Execute and analyze S4 in a separate task.** Review S3 first to confirm the
   still-credible worst-plausible envelope; identify boundary and degradation mode.
6. **Evaluate mitigation candidates for applicable material risks.** Apply M1/M2
   and any consequent B2 requirement. Record validation or explicit justified
   deferral; do not treat deferral as technical acceptance of the affected envelope.
7. **Re-test affected risks only where required.** Identify the exact changed claim,
   architecture or unanswered question and validate it with targeted evidence,
   including correctness. Completed scenarios are not blanket rerun requirements.
8. **Perform the formal Decision Gate.** Review the completed input pack, accepted
   scope, unresolved/deferred risks and all readiness rows, then separately record
   Continue / Tune / Pivot / Stop and the justified next action.

## Evidence-Complete definition

For the future formal Gate, the evidence pack can be considered **Evidence
Complete** only when:

- Stakeholder decisions needed to interpret results and define the evaluated
  operating envelope are recorded, including acceptance criteria and owners.
- S3 is executed and analyzed; S4 is executed and analyzed against credible,
  agreed contracts, with traceable results and explicit measurement limits.
- All material risk evidence is reviewed and mapped to an accepted disposition;
  applicable mitigation evaluation is completed or explicitly deferred with
  rationale, owner, scope and consequences for progression.
- No unresolved critical correctness issue remains; any finding prevents
  advancement with that implementation until resolved and validated under T1.
- Decision Gate inputs are complete: evidence links/provenance, coverage and
  denominators, stakeholder decisions, accepted envelope, remaining limitations,
  mitigation/deferment records and readiness assessment.

This formal Evidence Complete definition does not set pre-proposal MVP completion
requirements. It means enough evidence and decisions exist for the later formal
review, not a positive Gate verdict or production readiness. An explicit negative
stakeholder answer is still a decision;
it may support a non-advancing outcome. Required mitigation deferral cannot waive
T1/T5/T6 for advancement. None of these completion claims is made now.

## Decision Gate readiness matrix

This matrix concerns the future formal stakeholder/production Gate only.
Gate Ready? uses Yes / No / Conditional for each area. Yes accepts a scoped
input; Conditional identifies an explicit remaining dependency. Neither makes
the overall Gate ready while S3/S4 and stakeholder decisions remain unresolved.

| Area | Current Status | Acceptance Requirement | Remaining Action | Gate Ready? |
| --- | --- | --- | --- | --- |
| Persisted correctness | Satisfied | T1: zero persisted violations; invariants protected in future acceptable designs. | Maintain checks in new evidence. | Yes |
| Favorable reference | Satisfied | T2: retain S1's limited favorable claim. | Preserve scope. | Yes |
| Baseline fairness | Decision Required | B1: explicitly accepted Bad/path/recovery profile. | Obtain stakeholder acceptance boundaries. | No |
| Processing-order sensitivity | Decision Required | B2: distributive sensitivity accepted or consequent evaluation required. | Record acceptance or mitigation-evaluation requirement. | No |
| Large-group behavior | Satisfied | T4 supports technical review; B3 business envelope still required. | Monitor; resolve B3. | Conditional |
| Access / rejection | Decision Required | T3/B9: agreed access envelope and cause-specific interpretation. | Resolve access expectations and apply in S3/S4. | No |
| Spatial fragmentation | Mitigation Evaluation Required | T5/M1: risk addressed if B4 confirms applicability. | Validate realism, then evaluate or document supported exclusion. | No |
| Concurrency | Mitigation Evaluation Required | T6: validated mitigation or demonstrated avoidance. | Resolve operating envelope; M2 or E4. | No |
| Latency/useful throughput | Decision Required | B8/B9: agreed service; T7 durable delivery method retained. | Define expectations; address T6 service loss. | No |
| Reliability / safe failure | Satisfied | T8: safe handling and investigated unexpected failures. | Retain coverage in new evidence. | Yes |
| S3 | Additional Evidence Required | E1: combined stress analyzed against agreed criteria. | Define, then separately execute/analyze. | No |
| S4 | Additional Evidence Required | E2: worst-plausible boundary/survivability analyzed. | Define, then separately execute/analyze. | No |
| Stakeholder validation | Decision Required | B1–B10 decisions and assumption validation recorded. | Confirm accountable participants and capture answers. | No |
| Variability / generalization | Decision Required | E3: claim limits accepted or specific evidence gap scoped. | Preserve S8 sufficiency; no automatic repetitions. | Conditional |

**Formal Gate: not ready / future phase. Stage 5 = In Progress.**
**MVP Technical Feasibility Gate = Pending.**
The [arrival-concurrency contract](production-like-arrival-concurrency.md) is
Defined / Pending Execution. The immediate next task is **Implement and Execute
Production-like Arrival Concurrency Experiment**, followed by Late Large Groups
with Reconfiguration and the internal Gate. After proposal and organization interest, capture
stakeholder acceptance decisions and validate operating assumptions, then
finalize S3/S4 contracts for the later formal review.
No S3/S4 execution, completed-scenario rerun, implementation change, mitigation
selection or final go/no-go decision is authorized or performed by this document.
