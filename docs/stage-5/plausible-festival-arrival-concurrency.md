# Stage 5 — Plausible Festival Arrival Concurrency Experiment

**Defined / Pending Execution. Conditional Post-Proposal Validation. Stage 5 = In Progress.**
**MVP Technical Feasibility Gate = Completed.** This contract defines future work;
no implementation or experimental execution is included in this documentation task.
Open-loop High-rate Arrival Stress remains Completed with all evidence retained.

The [completed internal Gate](mvp-technical-feasibility-gate-result.md) concludes
**Technically Feasible for Proposal**; next task: **Prepare Organization Proposal**.
This does not validate plausible festival concurrency. This experiment's status,
conditional role, technical schedule and planned matrix remain unchanged.

**Roadmap update — 2026-11-11:** the [pre-proposal scope decision](pre-proposal-validation-scope.md)
moves this experiment to conditional post-proposal validation if real request-arrival
behavior indicates meaningful concurrency risk. Current evidence is sufficient
to proceed to the internal Gate without this experiment as a mandatory prerequisite.
It remains unimplemented/unexecuted, technically useful and not obsolete. Its
schedules, evidence requirements and planned matrix below are preserved; confirm
their relevance against actual operational inputs before separately authorizing
execution. Plausible festival concurrency has not been validated.

## Purpose and authority

> Do small, plausible, bounded overlaps between festival requests produce material
> concurrency conflicts when average offered load remains comfortably below the
> reference service rate?

This is a conditional bounded concurrency validation after proposal and
organization review, alongside conditional Late Large Groups with Reconfiguration.
It can inform concurrency, reliability and performance review; it is not a
prerequisite for the [internal MVP Gate](mvp-technical-feasibility-gate.md) or a
Gate verdict. Real arrival timing, gradual/simultaneous opening and FestivalDay
request windows determine its operational relevance; no answers are assumed.
The [S2 contract](representative-scenarios.md#s2--expected-baseline) owns business
inputs; the [metrics framework](metrics-and-evaluation-criteria.md) owns populations
and denominators; [simulation architecture](simulation-architecture.md) owns
persistence, timing and evidence boundaries.

**Plausible** and **Plausible Burst** (`PlausibleBurst` in pattern identifiers) are
business-derived exploratory timing hypotheses, not measured festival traffic,
production forecasts or organization-approved concurrency assumptions. Actual
calibration remains post-proposal work. Bounded overlap is the intended observed
behavior, not a configured worker cap or a guarantee of bounded backlog.

## Unchanged workload and business timing

Materialize the exact S2 workload once per Seed and reuse it across both patterns:
five FestivalDays, **1,464 requests/day**, 7,320 requests/run, original request
identities/order/membership, Attendee continuity, group-size distribution, venue
topology, eligibility, empty initial history and subsequent persisted history.
Preserve original RequestedAt/AssignedAt and baseline sequence. Do not regenerate
under a new ScenarioId; retain S2 identity and record experiment provenance separately.
Fairness, AssignmentEngine, Application use case and PostgreSQL Infrastructure
remain unchanged. Each run starts independently; history accumulates across its days.

Map each request to its original S2 `[Start, End)` business-time bucket and retain
its order within that bucket. Only elapsed execution offsets change. This preserves
bucket membership/density and order, not the original sampled intra-bucket gaps.
S2 clock times remain UTC business fixtures, not confirmed local festival hours.

**CompressionFactor = 120×.** Divide each business duration and its offset from
07:00 by 120. Two hours become 60 elapsed seconds, three hours 90 seconds, one
hour 30 seconds; the 16-hour day becomes **480 seconds (8 minutes)**. This is a
simulation parameter, not measured behavior or faster execution of the use case.

| S2 business bucket | Share | Exact requests N | Compressed start O (s) | Duration D (s) | Offered rate N/D (req/s) |
| --- | --- | --- | --- | --- | --- |
| 07:00–09:00 | 10% | 146 | 0 | 60 | 2.433 |
| 09:00–12:00 | 40% | 586 | 60 | 90 | 6.511 |
| 12:00–15:00 | 15% | 220 | 150 | 90 | 2.444 |
| 15:00–16:00 | 20% | 293 | 240 | 30 | 9.767 |
| 16:00–18:00 | 7% | 102 | 270 | 60 | 1.700 |
| 18:00–21:00 | 5% | 73 | 330 | 90 | 0.811 |
| 21:00–23:00 | 3% | 44 | 420 | 60 | 0.733 |
| Full day | 100% | 1,464 | 0 | 480 | 3.050 |

Counts come from S2's existing largest-remainder allocation and are confirmed in
the retained C1/S2 manifests for Seeds 1001/2001/3001, on every FestivalDay. Reuse
manifest membership/counts rather than independently rounding the displayed shares.
The peak hourly bucket has `1,464 × 20% = 292.8` nominal requests and exactly 293
allocated requests: `293 / 3,600 ≈ 0.0814 req/s` before compression.

After compression, `293 / 30 = 9.7667 ≈ 9.8 offered req/s`, intentionally below
the retained [C1 three-seed mean](s8-concurrency-analysis.md#across-seed-aggregate-comparison)
of **29.593 ≈ 29.6 successful req/s**. Every bucket has a lower average offered
rate than that reference. C1 is a local sequential reference used to avoid the
previous sustained-overload design, not a universal production capacity ceiling.
Short bursts can still create contention and reduce useful service; observations
must establish whether this design actually avoids sustained accumulation.

## Deterministic schedules

Use the table's bucket origin `O`, duration `D`, actual request count `N`, and
zero-based request ordinal `j = 0..N-1`. Let `Δ = D/N`, in seconds. All formulas
give elapsed offsets relative to that FestivalDay's observed monotonic origin.

### Pattern A — Plausible

```text
ScheduledOffset(j) = O + j × D/N
```

Requests are evenly distributed over the half-open bucket. The first is due at
its start; the last is due one spacing before its end. Peak spacing is exactly
`30/293 seconds ≈ 102.389 ms`. Low active concurrency, occasional overlap and
little or no sustained backlog are expected, not required outcomes. Little overlap
is a useful business-derived control; material conflicts even here are significant.
No observed overlap cannot establish safety under concurrent execution.

### Pattern B — Plausible Burst

```text
burstSize = 3
intraBurstSpacing = 0.010 seconds
ScheduledOffset(j) = O + (3 × floor(j/3)) × D/N + (j mod 3) × 0.010
```

Each full burst has offsets **0 / 10 / 20 ms** relative to its own start. Burst
starts are spaced by `3D/N`, so the last-to-next-start gap for a full burst is
`3D/N − 0.020 seconds`; do not repeat bursts every 20 ms or wait for completion.
At the peak, starts are `90/293 seconds ≈ 307.167 ms` apart, and the gap after the
third request is approximately **287.167 ms**. These derive from the bucket, not
a hard-coded 306 ms cycle. Under 120× compression, 10/20 ms correspond to
1.2/2.4 seconds of original business time: a small number of groups submitting
close together, an exploratory hypothesis rather than measured user behavior.

A final partial burst contains only the remaining one or two requests at 0 or
0/10 ms. Do not pad, move requests across buckets or shorten the bucket. Its
remaining arrival budget stays idle until the fixed bucket end. Every current
bucket has `D/N > 10 ms`, so offsets stay strictly increasing and inside the
bucket, including partial bursts. Thus both patterns retain exactly `N` arrivals
over exactly `D`, the same average offered rate and relative bucket density.

For deterministic representation, calculate offsets from absolute integer bucket
durations at TimeSpan tick precision (100 ns): round down `j × Dticks/N` for A,
or `(3 × floor(j/3)) × Dticks/N` for B before adding exact 10 ms increments.
Do not accumulate rounded inter-arrival delays. The sub-tick rounding does not
change count, order, bucket membership or the defined `N/D` rate.

## Dispatch and observation boundary

Release each request when its absolute scheduled offset is due, independently of
earlier completions, scope readiness or reconciliation. **No synchronized start
gate**, wait-for-N release, readiness barrier, synchronized larger groups, or
draining between bursts/buckets is permitted. No target concurrency cap or
completion-based admission throttling is introduced. Execution intervals alone
determine actual overlap; planned ordering does not guarantee CPU/commit ordering.
Each request retains an independent DI scope/DbContext and the real use-case path.
RetryCount remains 0; existing disposal, rollback and cancellation semantics apply.

Retain the complete 480-second daily schedule, including trailing idle time; do
not advance the next bucket/day merely because the last request has completed.
At day end, wait for both that horizon and outstanding work to finish, then take
the existing checkpoint and start the next day with a recorded origin. This is
daily isolation, not a burst synchronization barrier. The five daily windows
alone budget 40 minutes/run, plus any drain/checkpoint/setup/export overhead.

**OfferedRequestRate ≠ ActiveConcurrency ≠ PersistenceConflictRate.** `N/D` is
scheduled offered load per bucket, including idle budget. ActiveConcurrency counts
actual unfinished use-case intervals; PersistenceConflictRate uses known losing
conflicts divided by all attempted requests. None can substitute for the others.

| Required timing evidence | Reuse / derivation |
| --- | --- |
| ScheduledArrivalOffset | Recorded day origin + deterministic daily offset; retain pattern, compression, bucket boundaries/counts and schedule version/mapping. |
| ProcessingStartedOffset, Duration | Existing run-relative monotonic use-case entry and measured duration. |
| CompletionOffset | Derive `ProcessingStartedOffset + Duration`; no duplicate exported field needed. |
| SchedulingLag | Derive `ProcessingStartedOffset − ScheduledArrivalOffset`; report per-run/per-day distribution and time progression from existing records. |
| AttemptSequence, BaselineSequence, Outcome | Existing planned identity/order and unchanged analytical outcome; retain request/day/Seed correlation and diagnostics. |

Missing timing cannot be replaced by assumed punctual starts. Retain actual lag
and any reordering/coalescing caused by scheduling; do not silently revise offsets.
Invalid-input exclusions have no invented processing interval. Reconstruct
**MaxActiveRequests** from `[start, completion)`, handling completions before
starts at equal offsets and ignoring empty intervals. Reuse cheaply derived mean
active concurrency over the common measured interval, plus per-day/bucket interval
progression to expose rise/fall or accumulation. These require offline derivation
from existing intervals, not new production telemetry or secondary diagnostic scope.

## Boundedness warning and interpretation

Before interpreting conflicts, check whether activity repeatedly falls as work
completes between later arrivals. **Sustained monotonic or near-monotonic active
request accumulation**, growing scheduling lag, or wall-clock behavior dominated
by backlog are experimental warnings that a run may have crossed into overload.
Use the reconstructed time progression across bursts/buckets and daily drains;
a maximum or mean alone cannot establish boundedness. No hard production threshold
for MaxActiveRequests or rigid percentage warning threshold is defined.

Preserve such a run, its outcomes and diagnostics; report the affected interval
and limit the bounded-concurrency conclusion. Do not automatically call it
representative festival behavior, conceal it by throttling, or modify the schedule
mid-run. The below-reference average is a design property, not a proof against
overload. A systematic design/realization failure is not fixed by adding Seed 3001;
report the unresolved question before proposing separately scoped changes.

## Core metrics and integrity

Report each run separately with exact numerators/denominators and N/A for missing
or zero denominators, using the existing framework and observation remediation.

| Family | Required metrics / boundary |
| --- | --- |
| Accounting | AttemptedRequests, SuccessfulRequests, KnownConcurrencyConflicts, UnexpectedTechnicalFailures, DomainRejected, ExcludedInvalidInput. Attempted = Successful + DomainRejected + KnownConcurrencyConflict + UnexpectedTechnicalFailure; exclusions separate. |
| Rates | KnownConcurrencyConflictRate = known conflicts / AttemptedRequests (framework ConcurrencyConflictRate); UnexpectedTechnicalFailureRate = unexpected failures / AttemptedRequests (TechnicalErrorRate). NormallyProcessedRequests remains Successful + DomainRejected for normal-subset success/rejection rates. |
| Integrity | PersistedInvariantViolationCount from independent durable-state audit: unique Spot/day and Attendee/day, complete contiguous single-Zone/same-Row groups, valid identity, request/outcome consistency and no partial losing graphs; reconcile paths/checkpoints. Final-state checks are not a proof of all INV-06 temporal behavior. |
| Latency | p50, p95, p99, nearest rank over all AttemptedRequests' Duration including conflicts/failures. Use-case time excludes arrival waiting, scope preparation and post-use-case reconciliation. |
| Delivery / time | SuccessfulRequestThroughput, SuccessfulAssignmentThroughput, WallClockDuration. Reuse the common first-actual-entry to last-terminal monotonic interval across five days, including intervening idle/checkpoint/drain gaps; exclude setup/final export. Never sum overlapping durations. Successful requests count observed Successful outcomes; Assignments count independently audited durable delivery, retaining validated post-commit observer-failure commits without reclassifying outcomes. |
| Participation | RequestingAttendees, ParticipatingAttendees, NoPersistedPath: distinct identities across five days, not sums of daily counts. Retain existing InsufficientEvidence/classifiable/recovery coverage to avoid survivor-only conclusions. |
| Overlap / fidelity | MaxActiveRequests, SchedulingLag and cheaply derived mean/time progression as above, with day/bucket context. |

Keep the full scheduled 480-second budget distinct from WallClockDuration, whose
last-day trailing idle time after the final terminal outcome is outside the
existing metric. Bucket offered rates always use `N/D`, not first-to-last-arrival
span. Pacing deliberately limits useful throughput; lower successful requests/s
than unpaced C1 alone does not indicate service loss. Read delivery counts,
participation, complete outcome mix, lag and latency together.

Preserve recognized **23505 → SpotAlreadyAssigned** and **40P01 → DeadlockDetected**
as KnownConcurrencyConflict after safe rollback. Unknown database errors remain
UnexpectedTechnicalFailure. Retain the existing post-commit observation resolver
and independent final-state audit; do not erase observer failures or reclassify
them as successes. The [previous stop/remediation](arrival-observer-audit-remediation.md)
remains historical evidence, including its three original findings and missing
retrospective request-state proof.

Any **PersistedInvariantViolationCount > 0** is a material correctness finding:
preserve it, stop further official progression and investigate before continuation.
Never average it away or use another Seed to vote away corruption.

## Official execution matrix and adaptive expansion

| Pattern | Initial Seeds | Conditional additional Seed |
| --- | --- | --- |
| Plausible | 1001, 2001 | 3001 for this pattern only, if justified |
| PlausibleBurst | 1001, 2001 | 3001 for this pattern only, if justified |

**Initial official runs = 4; maximum planned official runs = 6.** Each is one
independent five-day run; no repetitions are planned. None is executed now.
Analyze both initial Seeds for a pattern before considering expansion. Record
the precise question and rationale first: genuinely different qualitative conflict
or technical-failure behavior, large unexplained latency divergence, different
integrity results, or different active-concurrency shape. No rigid percentage
threshold applies. Integrity differences require investigation and safe continuation
before any additional run. No ambiguity means no Seed 3001; expansion of one
pattern does not imply expansion of the other. Count stopped/invalid executions
toward the cap; no automatic replacement or extra run is authorized by this contract.
If ambiguity remains at the cap, report it unresolved.

Retain exact source/workload/schedule provenance, environment/build/PostgreSQL and
warm-up context, run identities, ledgers, source fingerprints, standard evidence
packages/checksums and audit results. Same Seed/schedule reproduces input, not
thread interleavings or winners. Compare same-Seed patterns with matched recorded
conditions; one run per cell does not quantify scheduler variability.

## Reference progression and questions

| Evidence | Role |
| --- | --- |
| [C1](s8-concurrency-analysis.md) | Retained sequential/reference full delivery and low latency. |
| [S8 C4/C16](s8-concurrency-analysis.md) | Completed bounded synchronized stress with severe contention. |
| [Open-loop High-rate Arrival Stress](production-like-arrival-concurrency-analysis.md) | Completed unsynchronized sustained-overload stress; original intent, measurements and stopped-run history preserved. |
| Plausible | Defined business-derived low-overlap control; results pending. |
| PlausibleBurst | Defined business-derived micro-bursts at the same bucket average rate; results pending. |

Use existing reference evidence **without rerunning C1, C4/C16 or Moderate/Burst-like**.
The future analysis must answer:

- Does Pattern A produce material conflicts under business-derived distributed
  arrivals with low expected overlap? Report actual overlap, including its absence.
- Do Pattern B's three-request bursts increase conflicts, latency or technical
  failures while average offered load remains unchanged?
- Are small overlapping requests sufficient to create meaningful competition for
  shared assignment inventory? Separate observed association from proven causality.
- Does persisted integrity remain protected, including losing and observer-failure paths?
- Is the bounded flow compatible with the **internal provisional p95 < 1 second**
  hypothesis, without promoting that hypothesis to a production SLA?

Interpret all answers only after assessing boundedness/fidelity. Lower ConflictRate
is not improvement if failures increase or useful delivery falls. These experiments
do not isolate synchronization as the sole cause or establish production behavior.

## Status, next task and scope

The earlier roadmap placed this experiment and Late Large Groups with
Reconfiguration before the internal Gate. The
[2026-11-11 decision](pre-proposal-validation-scope.md#updated-roadmap) supersedes
that sequence: internal Gate → proposal if supported → organization review/data
→ relevant conditional validations → applicable mitigation evaluation/validation
and architecture calibration → data-informed S3/S4 → formal stakeholder/production
Gate → pilot/production decision. This contract remains **Defined / Pending Execution**;
implementation and evidence collection are conditional future work. Stage 5 is
**In Progress**; the MVP Gate is **Completed — Technically Feasible for Proposal**,
without production readiness or organizational approval.

Next task: **Prepare Organization Proposal** using the completed Gate and its limits.
This definition changes no code, S2 business inputs, fairness, AssignmentEngine,
Application or PostgreSQL behavior. No retry, Semaphore, locks, queue, serialization,
advisory/pessimistic locking, inventory partitioning or selection-dispersion logic
is introduced. No official/exploratory run, Late Large Groups or S3/S4 execution
occurs here. Mitigation evaluation remains separate if evidence later requires it.

## Contract-definition validation

Read-only arithmetic checks against all 15 retained C1/S2 daily manifests verified
bucket counts/order, both offset formulas, tick rounding, partial bursts, equal
bucket average rates and the 480-second horizon. Documentation links/statuses and
`git diff --check` passed. Existing code, selected JSON, raw evidence and remediation
remain unchanged. Repository-required `dotnet build Festival.sln` passed with zero
warnings/errors; `DOCKER_API_VERSION=1.43 dotnet test Festival.sln` passed all 990
tests with zero failures/skips. No tests were added; these checks are not execution
or empirical validation of the newly defined experiment.
