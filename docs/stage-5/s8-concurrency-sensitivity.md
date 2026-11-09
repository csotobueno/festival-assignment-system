# S8 — Concurrency / Operational Sensitivity

## Status, authority and experimental question

**Contract defined; concurrent runner implemented; six initial concurrent runs
and three sequential baseline runs completed. C1/C4/C16 analysis completed;
current S8 operational evidence sufficient. Stage 5 Decision Gate remains pending.**
S8 asks: when equivalent assignment requests compete concurrently for shared
inventory, does the current system preserve correctness, and how much operational
degradation or conflict appears? Evaluate correctness, known concurrency
conflicts, unexpected technical failures, operational stability, latency and
throughput separately.

The [S2 contract](representative-scenarios.md#s2--expected-baseline) owns workload
and venue inputs. The [Stage 5 framework](metrics-and-evaluation-criteria.md)
remains authoritative for outcomes, populations, formulas, rubric and recovery;
the [simulation architecture](simulation-architecture.md) owns execution and
evidence flow. S8 changes only execution concurrency. It changes neither Engine
v1 nor fairness or business rules.

The runner supports Sequential / 1 and Concurrent / 4 or 16. Small PostgreSQL
tests exercise execution and rollback; they are not official S8 observations.
The [concurrency and operational analysis](s8-concurrency-analysis.md) records all
nine official runs, the separate initial smoke validation, zero persisted
findings and the final S8 evidence decision. The authorized C1 follow-up used
the direct sequential runner path and retained the original C4/C16 evidence.

## Implemented execution and evidence

`S8ConcurrentExecution.Create(s2Manifest, concurrencyLevel, repetition)` accepts
an untransformed S2 v2 manifest with empty setup and unchanged baseline dispatch
positions. It reuses the exact catalog, setup and daily plans. Level 1 retains
S2 identity and sequential execution; levels 4/16 identify `s8-concurrency-4` /
`s8-concurrency-16` and retain `WorkloadSourceScenarioId = s2-expected-baseline`.
The source S2 definition remains in definition.json. No generator change or
regeneration occurs. `SimulationExecutionConfiguration` validates the supported
mode/level pairs and optional Repetition (1–3).

`ConcurrentSimulationExecutor` sorts each daily list by BaselineSequence and
builds consecutive windows, including a partial final window. Each request
creates its scope, resolves services and prepares its command before marking
itself ready at an async TaskCompletionSource gate. The gate releases when the
actual window size is ready, before any Application ExecuteAsync call. There is
no synchronization after inventory reads or candidate selection. Task.WhenAll
drains every outcome, scope disposal and reconciliation before the next window.
Preparation failure faults the gate and drains waiters; cancellation propagates
without starting another window. Business exceptions retain per-attempt handling.
Sequential execution uses its existing loop without concurrency primitives.

Attempt evidence adds optional WindowNumber (one-based per FestivalDay),
WindowPosition (one-based within window) and ProcessingStartedOffset relative to
a common monotonic run clock. The terminal offset is ProcessingStartedOffset +
Duration. The minimum entry offset through maximum terminal offset supports
window/scenario elapsed-time and overlap reconstruction; UTC StartedAt and
CompletedAt remain correlation timestamps. Optional DatabaseSqlState retains a
PostgreSQL error code from nested provider exceptions. The processing exception
boundary explicitly recognizes a detected deadlock (`40P01`) as
KnownConcurrencyConflict after disposing the failed scope: PostgreSQL aborts the
victim transaction and no retry or synthetic rejection is created. Existing typed
Application conflicts retain their mapping; unknown/unrelated SQLSTATEs remain
UnexpectedTechnicalFailure. Reconciliation failures remain technical failures
because the request may already have committed. These intervals exclude gate waiting
from request latency, include intervening scheduling/checkpoint work in scenario
duration, and exclude final reconciliation/export beyond the last terminal
outcome. No extra window timing records or throughput analysis are generated.

Manifest/run metadata carry optional Repetition; run metadata captures OS,
runtime/.NET version, processor count and the pinned PostgreSQL image /
Docker-Testcontainers context for S8 execution. Metadata is captured by Create in
the execution process; it is not remote-machine discovery. ExecutionMode,
ConcurrencyLevel, Seed and RunId remain existing fields rather than being
duplicated in each attempt. Optional additions are omitted for earlier scenario
evidence. The exporter validates repetition and S8 source/configuration agreement
while retaining the same eight files. No new filesystem logs or Engine tracing
are introduced.

Focused tests cover exact S2 reuse for all three Seeds, windows/partial windows,
readiness, draining, failure and cancellation without sleeps. Small real
PostgreSQL tests cover levels 1/4/16, day history, final-state invariants and
export. A separate test-only pre-save coordinator guarantees contention to prove
real constraint translation, independent contexts and complete rollback. This
fixture elects one completed winner before allowing the staged losers to save,
avoiding reliance on random multi-row insert/deadlock order. It is not part of
S8 execution and does not manufacture official conflicts. Natural-execution tests
validate observed deadlocks as KnownConcurrencyConflict with SQLSTATE retained
and no persisted losing graph; they require consistent persisted state without
prescribing a conflict percentage. Focused exception-boundary tests cover direct
and wrapped 40P01, conservative fallback for other SQLSTATEs and no retries.
The official harness adds final persisted-state audits and eight-file
checksums; its [analysis](s8-concurrency-analysis.md) retains performance,
contention and fairness-population evidence separately from this input contract.

## Controlled workload and experimental matrix

| Preserved S2 parameter | Value |
| --- | --- |
| RegisteredPopulation | 5,500 |
| DailyRequesterCount | 4,400 distinct planned Attendees, not requests |
| FestivalDays | 5 |
| DailyParticipationContinuity | 90% of planned daily requesters |
| FrontStandingAllowedRequestShare | 50% of daily requests |
| GeneratorVersion | v2 |
| EngineVersion | v0.3.0 |

Retain S2's exact group-size distribution and arrival profile, shared 4,998-Spot
topology, Zone identities and quality mapping, eligibility and fairness policies.
Their detailed distributions remain in S2 rather than being duplicated here.
The provisional S2 stakeholder hypotheses remain provisional. Concurrency levels
are analytical inputs, not production traffic assumptions.

The initial official phase compares C4 directly with C16 over the same S2
workload, concentrating evidence on correctness, contention and operational
sensitivity as concurrency increases. S2/sequential behavior is already
extensively validated; a new sequential official batch is conditional follow-up.

| Initial configuration | ExecutionMode | ConcurrencyLevel | Seeds | Repetitions per Seed | Runs |
| --- | --- | --- | --- | --- | --- |
| S8-Low | Concurrent | 4 | 1001 / 2001 / 3001 | 1 | 3 |
| S8-Moderate | Concurrent | 16 | 1001 / 2001 / 3001 | 1 | 3 |
| Initial total | | | 3 Seeds × 2 levels | 1 | 6 |

The minimum initial evidence is **six official five-day runs**. Additional runs
are conditional; there is no mandatory final total.

`RetryCount = 0` in every configuration. Repetition is an independent full
five-day run with a distinct RunId; it is not a request retry or a new Seed.
Initial runs record Repetition = 1. Keep repetition metadata and the runner's
existing supported indices (1–3); these are metadata values, not an upfront
requirement to run three repetitions of each configuration. Keep individual
results for every initial or follow-up run.

### Adaptive follow-up strategy

After the six initial runs, analyze KnownConcurrencyConflictRate,
UnexpectedTechnicalFailureRate, PersistedInvariantViolationCount, SuccessRate,
DomainRejectionRate (the existing RejectionRate formula), p50/p95/p99, throughput,
wall-clock duration and fairness population effects before choosing more runs.
Every added run must answer an explicit unresolved analytical question and
record its reason and selected Seed/configuration.

| Initial finding / unresolved question | Conditional follow-up |
| --- | --- |
| C4 and C16 show stable, consistent differences across Seeds | Initial evidence may be sufficient for the primary concurrency signal; no automatic extra batch. |
| Conflict/performance results vary materially, are inconsistent or remain ambiguous | Repeat the affected C4 and/or C16 configuration with the same Seed/workload to distinguish Seed/workload sensitivity from scheduler/PostgreSQL interleaving variability. Repetitions become required follow-up when such uncertainty remains. |
| Latency, throughput or wall-clock duration needs comparison against sequential execution in the same environment | Add Sequential / ConcurrencyLevel = 1 for Seeds 1001 / 2001 / 3001, one run each (+3 runs), preserving existing S2 inputs and order. |

C1 remains a supported valid control configuration; it is optional follow-up,
not part of the initial official matrix. Historical S2 evidence provides context
but cannot replace a same-environment operational baseline when that comparison
is needed. Do not pre-commit to three repetitions for every Seed/configuration
or introduce numerical thresholds for material variability.

For each Seed, materialize S2 once with the same explicit FirstDate and reuse
that exact workload for all levels and repetitions. Preserve SimulationRequestId,
FestivalDay identities/dates/windows, daily cohorts, ordered group membership,
group sizes, eligibility, RequestedAt, AssignedAt, baseline positions, topology
and empty initial state. Generator v2 streams include ScenarioId: generating a
new scenario with the same Seed is insufficient. S8 execution/evidence
identity must reference the S2 source without resampling its business input.
Production AssignmentRequestId and AssignmentId remain generated outputs.

Each run starts from a clean independent S2 state: no S7 pre-occupation or
supporting occupancy and no prior evaluated history on Day 1. Days 2–5 retain
that run's actual persisted Assignments; never copy S2 control outcomes into a
concurrent run or reset between days. All days open with normal S2 day-scoped
inventory, with fragmentation developing naturally through actual assignments.
Do not import S6 group-size stress or S7 spatial stress.

## Deterministic windows and ordering boundary

Keep the existing S2 sequential workload and ordering unchanged as the supported
control; its official execution is conditional follow-up. For each concurrent FestivalDay:

1. Read the exact S2 daily list in increasing BaselineSequence order.
2. Partition it into consecutive, non-overlapping windows of at most K requests,
   where K is 4 or 16. A final partial window keeps its actual size. Windows never
   cross FestivalDay boundaries.
3. Prepare every request scope/service context and wait at the shared pre-use-case
   readiness gate. Release after all actual window members are ready. Dispatch
   each window's members with at most K requests in flight. Requests
   within it may execute concurrently; do not await each member before starting
   the next. Do not promise simultaneous use-case entry or commit.
4. Drain every member, capture its outcome and finish request scope disposal /
   reconciliation before advancing to the next window in baseline order. Drain
   the whole day before its closing checkpoint and the next FestivalDay.

```text
K = 4:   [R1 R2 R3 R4] → drain → [R5 R6 R7 R8] → drain → …
K = 16:  [R1 … R16]    → drain → [R17 … R32]    → drain → …
```

R denotes ordinal position within the daily baseline list. A window can be
identified by FestivalDayId plus its one-based window ordinal, derived from
that list and K; no new business identity is needed. Do not launch an entire
FestivalDay concurrently or refill a window with requests from the next one.

`BaselineSequence` always means the original deterministic S2 request order
(`DailyRequestPlan.Sequence`). Retain `ExecutionSequence` and `AttemptSequence`
as planned dispatch positions; in S8 they follow baseline order, without S5's
reversal. None represents scheduler entry, completion or commit order. Existing
observed timestamps and monotonic offsets describe actual timing;
no additional completion-order identifier is required by this definition.

S2 RequestedAt and AssignedAt remain deterministic simulated business timestamps.
The arrival profile controls the baseline ordering and window membership; it
does not require real-time sleeps or replay at venue clock speed. Windows may
span arrival-band boundaries. The readiness gate coordinates entry only; do not
add post-read barriers, race-inducing delays or deterministic locking to force
conflicts or reproduce sequential winners. A configured level alone is not evidence of observed overlap; future
reports must retain timing sufficient to establish the execution actually used.

## Request isolation, persistence and error handling

**Hard execution invariant: one DI scope and one DbContext per request.**
Concurrent requests must never share a DbContext, tracked request state or
request-local observation collector. Use separate short-lived read scopes for
drained checkpoints and final reconciliation.

Use the real Stage 5 PostgreSQL path through the existing Application use case,
repositories and EfCoreUnitOfWork. The complete request outcome is staged and
confirmed by the existing save/transaction semantics. Preserve database
constraints as final integrity protection and discard failed contexts. Do not
add a scenario/window transaction, stronger isolation, locking, serialization or
other transaction orchestration. Pre-save reads still do not guarantee one
transactionally consistent snapshot; do not interpret concurrent observations
as if they did. Official execution cannot substitute in-memory repositories,
mock locking or injected synthetic conflicts.

An exception in one request must produce its analytical outcome and diagnostics
without crashing unrelated requests when safe continuation is possible. Do not
silently swallow exceptions or claim a completed run if the runner cannot safely
continue or reconcile durable evidence. Setup/container failures are run failures,
not fabricated outcomes for requests that never entered the use case.

Preserve existing cancellation: the runner propagates OperationCanceledException
when its supplied CancellationToken is cancelled; it does not turn caller
cancellation into a completed request-failure result. Other processing exceptions
are captured per attempt, including a timeout if one occurs. No S8-specific
request timeout currently exists; retain existing provider/container behavior.
New timeout/cancellation policy and aggressive timeouts are outside scope.
An interrupted run must be identified as incomplete, not official completed
evidence.

## Outcomes, retries and rate denominators

Use the existing analytical outcomes independently of persisted request status:

| Outcome | Required evidence / meaning |
| --- | --- |
| Successful | Persisted Completed AssignmentRequest and the complete reconciled Assignments. |
| DomainRejected | Persisted Rejected AssignmentRequest with zero Assignments; a real business rejection, including no feasible contiguous block. |
| KnownConcurrencyConflict | Existing typed Application persistence conflict or explicit PostgreSQL processing deadlock (40P01); the losing transaction rolls back completely, with no persisted Rejected request or losing Assignments. The simulation Attempt records the conflict and retains SQLSTATE when available. |
| UnexpectedTechnicalFailure | Other unexpected processing/persistence exception or timeout; retain diagnostics and reconcile uncertain durable state. Arbitrary database errors are not automatically known conflicts. |
| ExcludedInvalidInput | Basic input/precondition boundary prevents measured assignment evaluation; record reason separately without hiding rejections or failures. |

`RetryCount = 0`: no automatic retry, regrouping or synthetic business rejection.
A conflict remains visible and produces no synthetic ExperienceQuality. Retry
policy can be evaluated later only through Decision Gate evidence.

Preserve the existing framework formulas:

```text
AttemptedRequests = Successful + DomainRejected
                    + KnownConcurrencyConflict + UnexpectedTechnicalFailure
NormallyProcessedRequests = Successful + DomainRejected
All evaluated records = AttemptedRequests + ExcludedInvalidInput

KnownConcurrencyConflictRate = KnownConcurrencyConflict / AttemptedRequests × 100
UnexpectedTechnicalFailureRate = UnexpectedTechnicalFailure / AttemptedRequests × 100
SuccessRate = Successful / NormallyProcessedRequests × 100
RejectionRate = DomainRejected / NormallyProcessedRequests × 100
```

These S8 report labels map respectively to the existing ConcurrencyConflictRate,
TechnicalErrorRate, AssignmentSuccessRate and AssignmentRejectionRate; they are
not new framework metrics. Repetition summaries may abbreviate the first two as
ConflictRate and TechnicalFailureRate. Always report counts and denominators;
zero denominators produce N/A. Normally processed rates alone cannot show
operational usability when many requests conflict.

## Persisted correctness and operational interpretation

Every official run, at every level and repetition, must independently satisfy
`PersistedInvariantViolationCount = 0`. This is the S8 report label for the
framework's confirmed persisted CriticalInvariantViolationCount, not a changed
correctness formula. A single confirmed corruption event must be reported
explicitly and cannot be averaged away across repetitions.

Before disposing PostgreSQL, reconcile attempts and final persisted state against
the [critical invariants](../critical-invariants.md), including:

- At most one Assignment per SpotCode / FestivalDay and per Attendee / FestivalDay.
- A Completed request has exactly one Assignment for every requested member;
  no partial groups, extra members or duplicate members.
- A group's Spots share one Zone and Row and consecutive SpotNumbers.
- Assignment identities, members, FestivalDay and originating request agree;
  request outcome and complete persisted result agree, with final Assignments
  preserved.
- Rejected requests have no Assignments; known conflict losers leave no
  persisted request/Assignment graph. Reconcile any uncertain technical failure
  rather than assuming rollback solely from its outcome label.

Retain validation coverage, zero-finding evidence and row/attempt references for
findings. An outcome classification never hides a persisted invariant violation.
Database constraints must not be weakened to improve conflict rate.

A correctly detected and rolled-back conflict is not automatically a defect.
Increasing conflicts with zero invariant violations means safety was preserved
but operational contention exists. Distinguish that from corrupted persisted
state and unexpected technical failures. No acceptable conflict threshold is
defined yet. Nonzero unexpected technical failure requires categorization and
investigation under the existing framework; no new acceptance threshold is set.
A conflict-free run does not demonstrate exercised rollback handling.

## Performance and window analysis

Future execution must measure, where tooling supports reliable evidence:

| Observation | Population and boundary |
| --- | --- |
| Request p50 / p95 / p99 latency | Monotonic use-case entry through observed terminal return/exception over AttemptedRequests, in milliseconds. Include persistence and waits within the use case. Use nearest-rank ceil(p × sample count), including p = 0.99; empty samples are N/A. |
| Scenario throughput | Completed AttemptedRequests / measured elapsed execution seconds, in requests/second; all four terminal measured outcomes count once, irrespective of GroupSize. |
| Scenario processing wall-clock duration | First measured use-case entry through last measured terminal outcome over the declared five-day workload, using monotonic elapsed time. Include overlap, scheduling gaps and intervening checkpoint overhead; never sum request durations. |
| Window duration / throughput | Apply the same entry-to-terminal boundaries and completed-attempt numerator to that window only; retain actual window size. |

Retain the provisional `p95 < 1 second` Stage 5 baseline hypothesis where
applicable. It is an initial analytical hypothesis, not an SLA or a requirement
that every request meet one second. Report sample counts and outcome mix so
fast conflicts/rejections do not imply good successful-request performance.

Exclude generation, container startup, migrations, seeding, warm-up, scope
preparation, later reconciliation and export from individual request latency.
Work between the first and last measured entries/outcomes still contributes to
scenario elapsed time. Record checkpoint cadence and observation overhead.
Run StartedAt/CompletedAt describe the broader runner lifecycle and must not be
used as the throughput denominator. Keep cold-start observations separate;
restore exact initial business state after any warm-up that writes data.

The implemented monotonic offset boundaries above apply across S8 1 / 4 / 16;
official analysis must retain those same boundaries. Zero elapsed time or no
measured attempts yields N/A throughput. Do not compare inconsistent intervals
or present missing timing as zero. S8 should provide stronger evidence than
earlier exploratory sequential timings.

Where practical, report each window's size, Successful, DomainRejected,
KnownConcurrencyConflict and UnexpectedTechnicalFailure counts and duration.
Derive window membership and aggregates from existing attempts plus minimal
missing timing rather than building extensive telemetry. Preserve baseline daily
positions to inspect early/middle/late clustering; report the segmentation rule
and normalized conflict rates over AttemptedRequests in each segment. Baseline
position alone does not establish arrival causality.

Report conflicts by GroupSize **1, 2, 3, 4, 5, 6–7, 8–10**, with numerator and
AttemptedRequests in each bucket and normalized rates. Do not assume larger
groups are more sensitive. TargetQuality or selected quality-path comparisons
are secondary and use only existing supporting evidence; do not infer targets
from missing outcomes or add telemetry solely for this comparison.

## Reproducibility and repeated-run comparison

The explicit S8 boundary is **deterministic input, not necessarily deterministic
outcome ordering**. Reproduction verifies identical workload, topology, initial
state, Seed, versions, concurrency configuration and window construction. The
scheduler may change entry order, snapshots, transaction winners, exact conflict
identities and final quality/placement identities even with identical inputs.
Report variation rather than suppressing it or forcing sequential equivalence.

Initially compare 4 / 16 for each Seed at aggregate level and by SimulationRequestId
where identity comparison is valid. Include 1 only if the optional sequential
baseline is added. Request-level pairing identifies changed
observed outcomes; it cannot promise exact concurrent final-quality reproduction.
Preserve every individual run and its correctness result.

One run per Seed does not fully characterize scheduler nondeterminism. The six-run
initial phase detects the primary concurrency signal; apparent cross-Seed
consistency does not establish exact outcome reproducibility. Material variation,
inconsistency or ambiguity requires targeted repetitions as defined above.

When follow-up repetitions exist for a Seed × level configuration, report
**min / mean / max** of ConflictRate, TechnicalFailureRate, p50, p95, p99 and
throughput. Compute each run's rates/percentiles first, then summarize them;
never present only pooled averages or treat the mean of percentiles as a pooled
percentile. Identify missing/N/A observations and available sample counts.
For configurations with one observed run, report that result and sample count
without claiming scheduling stability from a repetition range. Cross-Seed
summaries may supplement results without hiding individual runs, repetitions
or any nonzero persisted violation.

## Fairness populations and participation coverage

Continue calculating GoodOutcomeRate, MediumOutcomeRate, BadOutcomeRate,
BalancedPathRate, AcceptablePathRate, NotBalancedPathRate and RecoveryRate where
meaningful, using the unchanged framework and SimulationMetricsCalculator.
Quality rates use persisted outcomes; path rates use Classifiable paths;
RecoveryRate uses eligible episodes. Conflicts, rejections, failures and absence
create no synthetic ExperienceQuality, participation or recovery opportunity.

Every report must expose RequestingAttendees, ParticipatingAttendees,
AttendeesWithOnlyRejectedOrFailedAttempts, NoPersistedPath and Classifiable,
with the existing insufficient-evidence population and exclusions visible.
Only-rejected/failed includes attendees whose attempts all conflict, without
implying a persisted Failed status. A requester with no persisted Assignment has
no path; someone with other successful days may retain a shorter path.

Concurrency can reduce the population and opportunities evaluated for fairness
and recovery. Interpret rates alongside coverage and persisted outcome counts,
as in S7. Do not claim improved fairness because conflicting requests disappear
from classifiable paths. S8's primary question remains operational correctness
and concurrency sensitivity.

## Evidence and execution environment

Reuse the existing eight-file package: run-metadata.json, definition.json,
manifest.json, attempts.json, checkpoints.json, attendee-paths.json,
recovery-observations.json and metrics.json. Do not introduce a separate S8
evidence format. Reuse fields already present; add only minimal missing metadata
and measurements during implementation, keeping consumed manifest and metadata
consistent and retaining existing provenance:

- RunId, Seed, ExecutionMode (Sequential / Concurrent), ConcurrencyLevel and
  Repetition; explicit S2 workload-source identity and exact input pairing.
- FirstDate, GeneratorVersion, EngineVersion, rubric/framework/definition versions
  when available, source revision and normal run lifecycle metadata. Retain
  unavailable values as null/N/A and distinguish incomplete runs.
- Deterministic window construction, planned positions and attempt identities;
  observed start/end and monotonic duration/offset evidence, outcome counts,
  diagnostic types/codes and confirmed business identities.
- Persisted-invariant validation coverage/findings and declared timing boundaries,
  warm-up policy, checkpoint cadence and observation overhead.
- OS, runtime / .NET version and build configuration, CPU count / relevant machine
  resources, actual PostgreSQL image/version and database/container configuration
  and placement, Docker/Testcontainers context and concurrency configuration.

ExecutionMode and ConcurrencyLevel reuse existing fields. Repetition, minimal
environment context and monotonic attempt offsets are now available as described
above; any additional environment/audit detail needed for official evidence must
be documented by that execution task. Preserve analytical N/A, normal
traceability and individual evidence packages without a generic benchmark platform.

Local real PostgreSQL through Docker/Testcontainers is sufficient; S8 must remain
locally executable and does not require EC2. These performance observations
describe the local machine/container environment, not production infrastructure
benchmarks. Later EC2/cloud runs may supplement the same controlled contract as
an explicitly different environment, not replace it.

## Separation, interpretation and deferred work

| Experiment | Isolated driver |
| --- | --- |
| S5 | Deterministic processing-order sensitivity through daily reversal of exact S2 requests. |
| S7 | Spatial fragmentation of equivalent pre-occupied capacity. |
| S8 | Actual concurrent execution and persistence contention over normal S2 input/opening inventory. |

S8 supports conclusions about concurrency sensitivity under the tested workload,
database, runner and execution environment. It cannot alone establish production
traffic behavior, distributed multi-node behavior, cloud scaling, network latency
effects or all possible contention patterns.

S3/S4 combined stress stays deferred until isolated drivers are understood; do
not combine high demand, fragmentation, large groups and concurrency in S8.
Retry policy, serialization, locking strategy, queueing and batching may later
be evaluated at the Decision Gate. This task recommends and implements none of
them, introduces no Decision Gate logic and sets no conflict threshold.

The runner implementation added the window executor and minimal evidence support
with existing conflict handling and no retries. Separate execution tasks added
the official harness and offline analysis. Domain, Application and Infrastructure
production behavior, AssignmentEngine, AssignmentSelectionPolicy,
BlockSelectionPolicy, RotationScore, GroupRotationScore, TargetQuality,
PathClassificationRubric, recovery semantics, SimulationMetricsCalculator and
S2/S5/S6/S7 policies remain unchanged. Initial execution completed six concurrent
runs; its adaptive decision selected three C1 baseline runs, now completed once
per Seed. No C4/C16 repetition was added. The final comparison selects **S8
evidence sufficient** for the current operational question; scheduler variability
remains unquantified. Next: consolidate isolated-scenario evidence for Stage 5
Decision Gate review, without selecting or implementing mitigation here.
