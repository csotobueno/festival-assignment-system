# Stage 5 — Production-like Arrival Concurrency Experiment

**Original contract name retained for traceability. Open-loop High-rate Arrival Stress = Completed; Plausible Festival Arrival Concurrency = Pending. Stage 5 = In Progress.**
**MVP Technical Feasibility Gate = Pending.** The
[official analysis](production-like-arrival-concurrency-analysis.md) records four
valid initial-matrix runs and five actual executions including a documented
stopped collection. No Seed 3001 was needed. The timing/metric contract below
remains unchanged; severe operational degradation persists without a start gate
under the tested high-rate open-loop schedules, not established festival traffic.
The stopped collection's three original audit findings remain explicit in the
[remediation record](arrival-observer-audit-remediation.md), not overwritten.

## Purpose and authority

**Original design intent (retained):**

> Does concurrency remain materially problematic when requests arrive with
> temporal dispersion and overlap naturally, rather than being released through
> a synchronization barrier?

The experiment was initially defined as Production-like Arrival Concurrency.
**Post-run correction (2026-11-10):** its parameters produced sustained overload,
so its completed evidence is now classified as **Open-loop High-rate Arrival Stress**.
It supplies stress evidence to the [internal MVP Gate](mvp-technical-feasibility-gate.md),
but does not yet establish whether concurrency is material under plausible festival
arrival rates. Moderate and Burst-like remain original pattern names, not validated
festival intensity categories. The [analysis](production-like-arrival-concurrency-analysis.md#post-run-interpretation-correction--2026-11-10)
preserves the design → implementation → official runs → large active populations →
overload recognition → corrected interpretation sequence without changing evidence.

**Observed conflicts, technical failures, latencies and active-concurrency levels
are not production predictions.** Plausible festival concurrency validation is
still required before the internal Gate; organization calibration remains later.

The [S2 contract](representative-scenarios.md#s2--expected-baseline) owns business
inputs; the [metrics framework](metrics-and-evaluation-criteria.md) owns outcome
and denominator semantics; [simulation architecture](simulation-architecture.md)
owns persistence/evidence boundaries. The [official S8 analysis](s8-concurrency-analysis.md)
remains sufficient for its synchronized C1/C4/C16 comparison. This is a distinct
arrival-model experiment, not a repetition or revision of that evidence.

## Controlled workload and execution

Materialize S2 once per Seed and reuse its exact business input for both patterns.
Preserve request/Attendee identities, membership, group-size distribution,
continuity, five FestivalDays, venue topology, request eligibility, original
RequestedAt/AssignedAt, baseline order and empty initial history. Accumulate real
persisted history naturally within each independent run. Do not regenerate S2
under a new ScenarioId: generator streams include scenario identity. Retain the
S2 source identity and record experiment/pattern provenance separately.

Only the execution arrival schedule changes. Compressed elapsed arrivals do not
rewrite S2's simulated 07:00–23:00 business timestamps. No S5 reversal, S6 group
mix, S7 preoccupation or reconfiguration is imported. Fairness rules,
AssignmentEngine, Application use-case path and PostgreSQL Infrastructure remain
the baseline. Different transaction winners and subsequent histories are outputs.

Execution uses real Application/PostgreSQL processing, **one
independent DI scope and DbContext per request**, and **RetryCount = 0**. Each
request becomes independently due at its planned elapsed offset; subsequent
arrivals do not wait for preceding completions, scope readiness or reconciliation.
There is no fixed target active concurrency or completion-based admission
throttling in this experiment. The finite daily workload is drained before its
checkpoint and the next FestivalDay; persistence is not reset between days.

**Do not use S8's synchronized start gate**, readiness counts, wait-until-N
release, barriers after reads or grouped completion windows. A burst is a set of
distinct arrival offsets, not a simultaneous release or a required concurrency
level. Preserve rollback, disposal, cancellation and reconciliation semantics.
Tooling now also supports ArrivalDriven/0 with ArrivalSchedule metadata; zero
means no configured worker cap, not measured inactivity. Existing Sequential/1
and synchronized Concurrent/4 or /16 behavior remains. ArrivalDrivenExecution
preserves exact S2 business objects; ArrivalSimulationExecutor implements the
deadline producer independently of S8's readiness gate.

## Deterministic arrival patterns

For each FestivalDay, let `i = 0..N-1` be the request's ordinal in the unchanged
S2 baseline daily list. The first arrival has relative offset zero. Apply the
following schedule to every day, without waiting for a cluster to drain:

| Pattern | Fixed experimental parameters | Relative scheduled offset, milliseconds |
| --- | --- | --- |
| **Moderate** | Inter-arrival interval **25 ms** | `25 × i` |
| **Burst-like** | **4 requests** per cluster; **5 ms** between arrivals inside it; **50 ms** from the last arrival to the first of the next cluster | `65 × floor(i / 4) + 5 × (i mod 4)` |

Examples: Moderate `0, 25, 50, 75, 100, …`; Burst-like
`0, 5, 10, 15, 65, 70, 75, 80, 130, …`. A final partial cluster contains only
remaining requests; no padding or extra requests are created.

**Original parameter-selection rationale (retained, not a realism claim):**
Moderate's 25 ms lies below C1's observed p50 of 27.095–28.726 ms and p95 of
46.382–51.821 ms, making some overlap plausible without guaranteeing it.
Burst-like's small four-request clusters and 5/50 ms gaps select a reproducible
higher offered-pressure hypothesis within the exploratory range. They are design
parameters, not measured arrival behavior or desired conflict/concurrency values.

**Corrected interpretation:** Moderate offers `1 / 0.025 = 40 req/s`;
Burst-like offers `4 / 0.065 ≈ 61.5 req/s` over its repeating cluster cycle.
Both exceed the retained [S8 C1](s8-concurrency-analysis.md) three-seed mean
successful service rate of **29.593 ≈ 29.6 req/s**. This local reference is not a
proven capacity ceiling. Selecting intervals near API latency did not model S2's
business temporal dispersion: S2 has 1,464 requests/day and approximately 293/hour
(0.081 req/s) in its highest average hourly bucket, 15:00–16:00. Short bursts
remain possible, but the sustained tested schedules are hundreds of times that
average. The analysis documents the resulting accumulation interpretation, not a
mathematically proven queueing model.

No random timing draw is needed: for a given Seed, S2 defines the exact request
order and these formulas define its schedule. The same timing rule applies to
both Seeds; Seeds vary the preserved workload. Record the pattern, parameters,
schedule version and exact request-to-offset mapping. A changed timing rule is
a changed experiment configuration, not an unrecorded adjustment after results.
Seeds/schedules reproduce offered input, not actual starts, durations or winners.

## Timing and overlap evidence

**ArrivalRate ≠ ActiveConcurrency ≠ PersistenceConflictRate.** Scheduled arrivals
describe offered load; actual use-case execution intervals determine overlap;
known losing persistence races determine conflict rate. Neither the 25 ms
interval nor four-request clusters establish a measured active concurrency.

Use one run-relative monotonic clock. Retain each day's observed origin offset
and add the deterministic daily offsets above to obtain ScheduledArrivalOffset.
Later days' origins follow the preceding day's drain/checkpoint; they are observed
execution context, not seed-derived business timestamps. Dispatch against absolute
due offsets, not delays restarted after previous request completion. Host/timer
lateness may occur; record it rather than coalescing arrivals into ready-N batches
or silently changing the schedule.

| Per-attempt evidence | Contract / existing equivalent |
| --- | --- |
| Run/pattern/Seed, FestivalDayId and SimulationRequestId | Correlate the exact S2 request, configuration, timing and persisted result. |
| ScheduledArrivalOffset | Optional arrival-specific exported elapsed due offset. Uses the same origin/units as ProcessingStartedOffset. |
| Actual start | Reuse **ProcessingStartedOffset**, measured immediately before ExecuteAsync. |
| Actual completion and Duration | Derive terminal offset as **ProcessingStartedOffset + Duration**; reuse StartedAt/CompletedAt for UTC correlation, not monotonic overlap calculations. No redundant completion-offset field is needed. |
| AttemptSequence and BaselineSequence | Reuse existing planned sequence fields; preserve baseline dispatch order. They do not assert actual entry, completion or commit order. |
| Outcome and diagnostics | Reuse analytical Outcome, DiagnosticCode/Type, DatabaseSqlState and confirmed persisted identity. |

Report actual-start minus scheduled-arrival offset to expose preparation and
scheduler lag separately from use-case latency. Preserve its per-attempt values
or reconstructibility; do not claim offered timing was realized from the formula
alone. If timing evidence is missing, the overlap conclusion is unsupported.

The smallest required aggregate overlap observation is **MaxActiveRequests**:
count actual use-case intervals `[start, start + Duration)`, processing completions
before starts at equal offsets. Exported intervals allow independent reconstruction;
no average-concurrency metric is required. Report realized overlap and schedule
fidelity per run, with day context. ExcludedInvalidInput is outside measured
processing intervals; retain its exclusion reason rather than inventing a start.
No observed overlap cannot demonstrate safety
under concurrent use; substantial dispatch lag or unintended coalescing limits
interpretation rather than proving the intended pattern.

## Required metrics and correctness

Report per official run, retaining numerator/denominator counts and N/A for zero
denominators. Requested labels reuse existing categories rather than new statuses.

| Family | Required observations / definitions |
| --- | --- |
| Request accounting | **AttemptedRequests, SuccessfulRequests, KnownConcurrencyConflicts, UnexpectedTechnicalFailures, DomainRejected, ExcludedInvalidInput**. SuccessfulRequests counts existing Successful outcomes; the plural conflict/failure labels count their existing categories. AttemptedRequests = Successful + DomainRejected + KnownConcurrencyConflict + UnexpectedTechnicalFailure; exclusions remain separate. |
| Rates | **KnownConcurrencyConflictRate** = KnownConcurrencyConflict / AttemptedRequests; **UnexpectedTechnicalFailureRate** = UnexpectedTechnicalFailure / AttemptedRequests. These reuse framework ConcurrencyConflictRate and TechnicalErrorRate respectively; express consistently as ratios or percentages. Normal success/rejection rates retain NormallyProcessedRequests if reported. |
| Correctness | **PersistedInvariantViolationCount**, independently audited from durable PostgreSQL state: uniqueness by Spot/day and Attendee/day, complete contiguous single-Zone/same-Row groups, valid identity, request/outcome consistency, no partial losing graphs and reconciled paths/checkpoints. Zero confirmed persisted violations remains mandatory; final-state audits are not a full temporal proof of INV-06. |
| Latency | **p50, p95, p99**, nearest-rank over all AttemptedRequests' existing Duration, including conflicts/failures. Timing covers use-case processing and persistence; scope preparation, arrival waiting and later reconciliation are excluded and remain visible through timing context. |
| Useful delivery and elapsed time | **WallClockDuration, SuccessfulRequestThroughput, SuccessfulAssignmentThroughput**. WallClockDuration is the measured workload interval: last terminal offset minus first actual processing-start offset across all five days, equivalent to TotalScenarioProcessingTime. Divide successful requests and audited durable Assignments by that same interval in seconds. Keep Terminal Attempts/s separate; never sum overlapping durations. Include intervening scheduling/checkpoint gaps; exclude setup/container startup/final export outside the interval. Broad run lifecycle timestamps remain separate context. |
| Final participation | **RequestingAttendees, ParticipatingAttendees, NoPersistedPath**, distinct identities over the evaluated five-day workload, not summed daily counts. Retain existing InsufficientEvidence/Classifiable coverage with fairness/recovery outputs; conflicts/rejections create no synthetic quality or recovery opportunity. |
| Realized concurrency | **MaxActiveRequests** and reconstructible scheduled/start/end offsets, including start lag, as defined above. |

Preserve existing conflict classification: recognized `23505 → SpotAlreadyAssigned`
and `40P01 → DeadlockDetected` remain KnownConcurrencyConflict after safe rollback;
other unrecognized failures remain UnexpectedTechnicalFailure. Do not reclassify
conflicts as DomainRejected or add retries. Persist the losing attempt evidence,
not a fake persisted Rejected request or losing Assignments.

Retain the existing eight-file evidence separation, exact S2 input proofs,
schedule provenance, environment/build/PostgreSQL and warm-up context, checksums
and durable reconciliation. Optional schedule/timing additions preserve earlier
package shapes. There is no new production persistence model. The
[observer-audit remediation](arrival-observer-audit-remediation.md) separates
post-commit observation failures from actual graph violations using independently
loaded final state; original outcomes, diagnostics and timing are never rewritten.

## Official execution matrix and adaptive expansion

The original execution plan below is retained as history. Collection is complete;
no further Moderate/Burst-like, Seed 3001 or C1/C4/C16 runs are required for this
correction. Another run cannot fix the interpretation of the offered arrival rates.

| Pattern | Initial Seeds | Conditional additional Seed |
| --- | --- | --- |
| Moderate | **1001, 2001** | **3001**, only if this pattern needs clarification |
| Burst-like | **1001, 2001** | **3001**, only if this pattern needs clarification |

**Initial official runs = 4; maximum planned official runs = 6.** Each entry is
one independent five-day run; no repetitions are scheduled. Review a pattern's
two initial runs before deciding on its Seed 3001. Record the precise unresolved
question and rationale before expansion. Qualifying signals include substantially
different conflict behavior, different qualitative interpretations, material
latency/useful-throughput divergence, an unexpected failure in only one Seed or
different persisted-integrity findings. No arbitrary percentage cutoffs apply.

No ambiguity requiring clarification means four runs. One affected pattern
means five; both means six. Adding Seed 3001 to one pattern does not authorize
it for the other. A confirmed persisted violation blocks technical progression:
preserve evidence and investigate before further execution; another Seed cannot
vote away corruption. Safe continuation must be established before a conditional
run. If ambiguity remains after the allowed expansion, report it unresolved;
additional Seeds, levels, repetitions or replacement runs need separate scope.

## Reference comparisons and interpretation

Use retained official **C1** as the sequential reference, **Moderate/Burst-like**
as open-loop high-rate stress patterns and **C4/C16** as the synchronized high-contention
envelope. **Do not rerun C1/C4/C16** as part of this matrix. Their existing evidence
is sufficient; any later rerun requires a new specific evidential reason.

Compare same-Seed exact S2 inputs and recorded environments. Timing differences
also reflect host/scheduler behavior and offered arrival pacing; lower throughput
alone cannot identify lost delivery without counts, participation, start lag and
overlap. Existing S8 has no warm-up, fixed configuration ordering and local-host
limits. Preserve these caveats rather than normalizing historical measurements.

The original analysis questions were whether meaningful contention persists
under actual natural overlap; how strongly results suggest synchronized release contributed
to S8's observed severity; whether persisted correctness is protected; whether
the distributed use-case flow is compatible with the **internal provisional
p95 < 1 second hypothesis**; and whether useful delivery/participation deteriorates.
These questions remain historical intent; the executed rates do not answer the
plausible festival arrival question. This comparison cannot quantify a causal
production synchronization effect or establish organization-approved service behavior.

The corrected conclusion is: severe degradation remains observable without a
synchronized start barrier under the tested high-rate open-loop arrival patterns.
Neither pattern needs Seed 3001 for that stress conclusion. S8 C4/C16 deliberately
synchronize release and cap active work by window size; these arrivals continue
while earlier work is active and accumulate without a small fixed bound. Neither
experiment independently represents production, and synchronization alone is not
an isolated causal effect. The experiment does not yet establish whether concurrency
is material under plausible festival arrival rates. No new production pass/fail
threshold, mitigation, SLA or MVP feasibility verdict is introduced.

## Roadmap, status and scope

Sequence: reframe arrival-concurrency evidence (completed) → define Plausible
Festival Arrival Concurrency → execute bounded Lean concurrency validation →
define/execute Late Large Groups with Reconfiguration → MVP Technical Feasibility
Gate → proposal if supported. S3/S4, real traffic calibration and the formal
stakeholder Gate remain in the later post-proposal phase.

**Open-loop High-rate Arrival Stress = Completed; Plausible Festival Arrival Concurrency = Pending.** Four valid initial-matrix
runs were analyzed; the fifth actual execution is the retained stopped collection.
Its observer-audit replacement was documented before execution and did not alter
business execution, timing, conflict semantics or original outcomes. No conditional
Seed 3001 or reference rerun occurred. Original stopped findings are not averaged
away; no clean retrospective request-state audit is claimed for that package.
**Stage 5 = In Progress; MVP Technical Feasibility Gate = Pending.** No production,
AssignmentEngine/fairness change, retry, locking, serialization/queue mitigation,
reconfiguration, S3/S4 execution or final feasibility decision occurred.

Next task: **Define Plausible Festival Arrival Concurrency Experiment**.
Model plausible temporal dispersion from the business workload rather than deriving
arrival rates primarily from API latency. Distinguish OfferedRequestRate,
ActiveConcurrency and ConflictRate; avoid sustained offered load that automatically
accumulates backlog without a small bound unless overload is the explicit objective.
Final intervals, compression factors and run counts are not defined here.
