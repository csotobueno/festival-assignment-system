# Open-loop High-rate Arrival Stress — Results and Analysis

Execution/analysis: **2026-11-09 (America/Lima)**. Runtime timestamps/directory labels use UTC; simulated 2026-08-15–19 dates remain fixtures.

**Open-loop High-rate Arrival Stress = Completed: 4 valid initial-matrix runs; 5 actual official executions including one retained stopped collection. Plausible Festival Arrival Concurrency = Defined / Pending Execution. No Seed 3001 or C1/C4/C16 reference rerun. Stage 5 = In Progress; MVP Technical Feasibility Gate = Pending.**

**Roadmap update — 2026-11-11:** the [pre-proposal scope decision](pre-proposal-validation-scope.md)
records that current evidence is sufficient to proceed to **Perform MVP Technical
Feasibility Gate — Pending / Next** without mandatory execution of Plausible
Festival Arrival Concurrency or Late Large Groups with Reconfiguration. Both
remain unexecuted conditional post-proposal validations, dependent on actual
operational information. Plausible concurrency remains Defined / Pending Execution;
late-group reconfiguration remains Pending Definition / Execution. The severe
stress degradation, all measurements/run counts and original observer-audit
findings below are unchanged. Earlier roadmap/next-task notes and the selected
JSON interpretation metadata record the preceding decisions, not today's sequence.
No plausible festival concurrency validation or Gate verdict is claimed.

## Result and interpretation

Severe degradation remains observable without a synchronized start barrier under the tested high-rate open-loop arrival patterns. The current implementation degrades severely under sustained offered load above its reference useful processing rate. Both Burst-like runs show much lower useful delivery and participation alongside many unexpected technical outcomes. Conflict percentage alone cannot describe that degradation: it must be read with technical-failure counts, durable delivery, latency and coverage.

**The experiment does not yet establish whether concurrency is material under plausible festival arrival rates.** That question remains Pending, and no overall MVP Gate decision is made.

**The observed conflict rates, technical failure rates, latencies and active-concurrency levels are not production predictions.** Approximately 79% Moderate conflicts, 69–74% Burst-like conflicts and hundreds/thousands of active requests must not be presented as expected festival operating conditions. Moderate/Burst-like remain historical pattern identifiers, not validated intensity labels for festival traffic.

## Post-run interpretation correction — 2026-11-10

The experiment was initially defined as **Production-like Arrival Concurrency** to examine natural overlap without S8's synchronized release. Its deterministic schedules were implemented and official runs executed under that contract. Post-run analysis of the very large active populations identified sustained overload in those parameters. The evidence is therefore retained primarily as **Open-loop High-rate Arrival Stress**. The original filename, contract intent, ledgers and measurements remain traceable; this correction does not pretend the experiment was designed as stress from the outset.

| Comparison | Derivation / retained source | Rate |
| --- | --- | --- |
| Moderate offered requests | `1 / 0.025 s` | **40 req/s** |
| Burst-like offered requests | Four requests per `3 × 5 + 50 = 65 ms` cluster cycle; `4 / 0.065 s` | **≈ 61.5 req/s** |
| C1 reference successful requests | [S8 cross-seed summary](s8-concurrency-analysis.md#across-seed-aggregate-comparison): unweighted mean over its three retained C1 runs | **29.593 ≈ 29.6 req/s** |

Both offered rates exceed the observed C1 reference service rate (including its highest retained per-seed rate, 30.871 req/s). This is a local sequential/reference comparison, not a measured universal capacity ceiling or a production capacity estimate. Rates describe the offered schedule while daily arrivals are being dispatched; days drain before the next day, and whole-run useful throughput includes intervening gaps. Actual scheduling lag remains measured separately.

The most direct analytical interpretation of the measured behavior is:

```text
arrival rate exceeds reference service rate
        ↓
unfinished requests accumulate
        ↓
active concurrency grows
        ↓
contention increases
        ↓
request duration increases
        ↓
backlog grows further
```

Moderate peaks of 672–685 active requests, Burst-like peaks of 1,295–1,377, severe latency growth and large conflict/failure counts support this interpretation. It is not a mathematically proven queueing model or an isolated causal estimate. The workload is finite and eventually drains; “without a fixed cap” describes admission during dispatch, not an infinite measured queue. Removing synchronization still left a different stress characteristic: sustained open-loop accumulation rather than only limited natural overlap.

### Festival business-arrival boundary

The [S2 business contract](representative-scenarios.md#s2--expected-baseline) materializes **1,464 requests per FestivalDay**. Its highest average hourly arrival bucket is **15:00–16:00, 20%** of daily requests: `1,464 × 0.20 = 292.8`, approximately **293 requests/hour**, or **0.0813 req/s**. The retained manifests allocate exactly 293 requests to that bucket per day; `293 / 3,600 ≈ 0.0814 req/s`. The 09:00–12:00 bucket has a larger total share but spans three hours.

The executed sustained schedules are roughly **490–756 times** that busiest-bucket average (about 2.7–2.9 orders of magnitude). Reusing S2 identities and business timestamps did not preserve its temporal dispersion in elapsed execution time. S2 itself is a provisional business hypothesis, not measured traffic. An hourly average does not exclude short bursts or concurrency spikes from real users; it establishes why sustained 40–61.5 req/s is not currently supported as plausible festival traffic.

This correction retains a material technical risk finding. It may inform later capacity planning, backpressure, concurrency coordination, load shedding or retry/serialization evaluation; no mitigation is selected here.

## Contract, implementation and input preservation

The [contract](production-like-arrival-concurrency.md), [metrics framework](metrics-and-evaluation-criteria.md), [S2 inputs](representative-scenarios.md) and [S8 references](s8-concurrency-analysis.md) remain authoritative. Moderate uses 25 ms spacing; BurstLike uses four arrivals at 5 ms spacing, then 50 ms from the last arrival to the next cluster. ArrivalSchedule/arrival-v1 records parameters in manifest/run metadata. ArrivalDriven/0 means no configured worker cap; measured concurrency is separate.

A common monotonic clock supplies absolute per-day deadlines. Due requests dispatch independently without waiting for earlier requests or clusters; there is no start gate, readiness count, semaphore or new admission/coordination policy. Each request retains its independent DI scope/DbContext and real Application/PostgreSQL path. Each day drains before its checkpoint/next day. RetryCount remains 0.

Exact source definitions/catalog/setup/daily plans were verified against retained S2/C1 before execution and independently against historical S2 during offline analysis. Request identities/order/membership, five-day continuity/history, topology, eligibility and business timestamps are unchanged. Seeds 1001/2001 are shared across patterns. Different transaction winners/history are observed outputs.

## Official executions and immutable evidence

Final ledger: `artifacts/simulations/arrival-concurrency-20261110-2ba8335287484fd69d6cf26eb98f456a/index.json`. SHA-256: `17e153d60712568b6efc099b6f69d7445dd3416906ddaa78b13ba9ae049826be`.

Stopped ledger: `artifacts/simulations/arrival-concurrency-20261110-2216981442fb4823afbea2d2de6b10d2/index.json`. SHA-256: `be6872a78a471ebb442624a100272044895a1aa119d17144497ea54aa32c721f`.

Base repository revision: `364e886dcb001e2f09f4967135791f6fda719fed`. Executed changes were uncommitted; ledger source/harness hashes identify actual files, not the revision alone. The continuation retains original source hashes for Moderate and proves every pre-existing src file is byte-identical; only the read-only post-commit resolver is added. Harness audit changes are recorded separately.

| Pattern | Seed | RunId | Role |
| --- | --- | --- | --- |
| Moderate | 1001 | `official-arrival-moderate-1001-fe89f3a16f734bfe82e43716c239a454` | Initial |
| Moderate | 2001 | `official-arrival-moderate-2001-bdfb389507d9498f904365da8bfcb6ef` | Initial |
| BurstLike | 1001 | `official-arrival-burstlike-1001-3a861121889c4f9a8a0634dcf49f5aec` | Documented replacement of stopped initial collection |
| BurstLike | 2001 | `official-arrival-burstlike-2001-b0345fba601b4651a941c1373a43727d` | Initial |
| BurstLike | 1001 | `official-arrival-burstlike-1001-7669f0983bf74b06adb043b70d495743` | Stopped/excluded; original audit findings = 3 |

Every directory is `artifacts/simulations/s2-expected-baseline/<RunId>/` and contains the standard eight files. Raw packages remain local/ignored and immutable. The [selected JSON](production-like-arrival-concurrency-analysis.json) retains exact paths, checksums, source provenance, metrics, per-day diagnostics and retained-reference identifiers; no raw packages are added to repository history.

## Main comparison

Latency is milliseconds; useful throughput is per second. Each valid run has 7,320 AttemptedRequests. Inv counts are independently audited persisted/execution findings in the valid runs.

| Pattern | Seed | Successful | Known conflicts | Conflict % | p50 ms | p95 ms | p99 ms | Max active | Successful requests/s | Durable Assignments/s | Participating | NoPersistedPath | Inv |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Moderate | 1001 | 1375 | 5854 | 79.973 | 3669.398 | 21414.323 | 34107.899 | 672 | 4.946 | 14.790 | 3043 | 2311 | 0 |
| Moderate | 2001 | 1503 | 5740 | 78.415 | 4602.310 | 22967.696 | 40089.750 | 685 | 5.100 | 15.480 | 3491 | 1868 | 0 |
| BurstLike | 1001 | 62 | 5442 | 74.344 | 19044.989 | 41403.971 | 62369.201 | 1295 | 0.202 | 0.502 | 152 | 5202 | 0 |
| BurstLike | 2001 | 54 | 5032 | 68.743 | 24463.441 | 50178.519 | 66459.254 | 1377 | 0.159 | 0.403 | 135 | 5224 | 0 |

| Pattern | Seed | Unexpected technical failures | Failure % | DomainRejected | ExcludedInvalidInput | Durable Assignments | Post-commit observer failures | Wall-clock seconds |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Moderate | 1001 | 91 | 1.243 | 0 | 0 | 4112 | 0 | 278.029 |
| Moderate | 2001 | 77 | 1.052 | 0 | 0 | 4562 | 0 | 294.699 |
| BurstLike | 1001 | 1816 | 24.809 | 0 | 0 | 154 | 4 | 306.626 |
| BurstLike | 2001 | 2234 | 30.519 | 0 | 0 | 137 | 3 | 340.289 |

NormallyProcessedRequests remains Successful + DomainRejected. Conflicts/technical failures use all AttemptedRequests. The 100% normal-subset SuccessRate here must not be mistaken for full delivery. UnexpectedTechnicalFailureRate aliases TechnicalErrorRate. Useful throughput uses one common first-actual-entry to last-terminal monotonic interval over five days, including intervening scheduling/checkpoint gaps; overlapping durations are never summed. Setup/container startup/final export are outside it.

SuccessfulRequestThroughput counts observed Successful outcomes. SuccessfulAssignmentThroughput counts independently audited durable Assignments, including validated commits whose later observer read failed. Those attempts remain unexpected outcomes; their recorded fields are not rewritten. Requesting/participating populations count distinct identities across five days, not sums of daily counts.

RequestingAttendees is 5,354 for Seed 1001 and 5,359 for Seed 2001 in both
patterns. BurstLike has only two classifiable paths per Seed; survivor-only
quality/path rates cannot establish improved fairness for the full requester
population. The selected JSON retains InsufficientEvidence and recovery coverage.

## Actual overlap and scheduling fidelity

| Pattern | Seed | Mean lag ms | p95 lag ms | Maximum lag ms | Mean active concurrency |
| --- | --- | --- | --- | --- | --- |
| Moderate | 1001 | 20.071 | 127.727 | 840.697 | 168.049 |
| Moderate | 2001 | 21.200 | 133.429 | 707.184 | 186.406 |
| BurstLike | 1001 | 56.684 | 314.742 | 1393.987 | 496.832 |
| BurstLike | 2001 | 23.632 | 162.203 | 1147.111 | 546.909 |

MaxActiveRequests uses actual [ProcessingStartedOffset, ProcessingStartedOffset + Duration) intervals, completing before starting at ties. Empty intervals contribute no activity. An independent Python sweep agrees with the C# peak in every valid run. Scheduled offsets reproduce exact contract arithmetic; actual CPU starts can be late or reordered. Lag includes preparation/scheduler delay and is separate from use-case latency. Active requests include waits inside the use case and are not a count of simultaneously running database transactions. Mean activity uses the same full workload interval and includes its gaps.

## Pattern analysis

**Moderate.** Conflict rates are 78.415–79.973%; p95 is 21414.323–22967.696 ms; useful request delivery is 4.946–5.100/s. Both Seeds show material delivery/participation loss, long tails and unexpected technical outcomes while valid final-state audits report zero findings. These ranges describe Seed-level observations, not repeated scheduler samples.

**BurstLike.** Conflict rates are 68.743–74.344%; p95 is 41403.971–50178.519 ms; useful request delivery is 0.159–0.202/s. Both Seeds show material delivery/participation loss, long tails and unexpected technical outcomes while valid final-state audits report zero findings. These ranges describe Seed-level observations, not repeated scheduler samples.

A lower conflict fraction under denser arrivals does not establish improvement when outcomes shift to unexpected technical failures and useful delivery collapses. All distributed p95 values exceed the existing internal provisional p95 < 1 second hypothesis. It remains an analytical hypothesis, not a production SLA.

## Directional reference comparison

| Seed | Retained configuration | Successful | Conflict % | p95 ms | Successful requests/s | Durable Assignments/s | Participating | NoPersistedPath |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | C1 | 7320 | 0.000 | 46.382 | 30.871 | 92.783 | 5354 | 0 |
| 1001 | C4 | 1830 | 75.000 | 2068.156 | 1.956 | 4.024 | 2764 | 2590 |
| 1001 | C16 | 461 | 93.702 | 3262.293 | 1.034 | 2.201 | 909 | 4445 |
| 2001 | C1 | 7320 | 0.000 | 51.821 | 28.509 | 85.684 | 5359 | 0 |
| 2001 | C4 | 1830 | 75.000 | 2072.600 | 1.786 | 3.629 | 2793 | 2566 |
| 2001 | C16 | 460 | 93.716 | 4212.878 | 0.965 | 2.020 | 899 | 4460 |

C1 remains the full-delivery low-latency reference. Compare C4/C16 using useful throughput and participation as well as terminal attempt rate; their synchronized release is not required for severe contention to appear under high-rate stress. S8 deliberately synchronized release within bounded C4/C16 windows, constrained maximum active work by window size and drained each window. This experiment has no synchronized start gate: open-loop scheduled arrivals continue while earlier work remains active, so activity can accumulate without a small fixed bound. Both patterns exceed the C1 reference service rate. The comparisons therefore cannot isolate or quantify synchronization alone as the causal effect. Neither experiment independently represents production; plausible festival arrival concurrency remains unresolved.

## Integrity, observer stop and technical failures

**PersistedInvariantViolationCount = 0 in all four valid runs: persisted integrity remained protected under severe overload within those final-state audits, without implying operational success. The original stopped BurstLike/1001 retains its three original audit findings and is excluded, not overwritten or averaged into zero.** The [remediation record](arrival-observer-audit-remediation.md) documents two post-use-case RECONCILIATION_FAILED observations with persisted singleton paths that exposed an invalid legacy audit assumption. The original database was already disposed, so its unmatched request statuses cannot be independently re-audited from the standard exports; no clean retrospective correctness claim is made for that package.

The corrected audit correlates only explicit post-commit observation failures using independently loaded Completed requests, exact group/day/time and complete valid graphs. Known conflicts still require zero losing rows. Missing/partial/inconsistent graphs still fail audit. No observation query or business request is retried, removed, delayed or reclassified. Correlations and durable identities remain in the new ledger; original attempt outcomes/timing/diagnostics remain unchanged.

Unexpected technical outcomes retain the outer System.InvalidOperationException message "An exception has been raised that is likely due to a transient failure." SQLSTATE is absent for these errors. RECONCILIATION_FAILED identifies the observer subset; the remaining generic failures occurred in the measured Application-call path. Their inner causes are not exported, so this report does not claim a specific pool/timeout root cause or choose mitigation. Counts are retained for later characterization. These are separate from recognized 23505/SpotAlreadyAssigned and 40P01/DeadlockDetected conflicts. Final-state checks are not a proof of all INV-06 temporal behavior or all interleavings.

## Adaptive expansion decision

| Pattern | Seed 3001 needed? | Recorded reason |
| --- | --- | --- |
| Moderate | No | Both Seeds show 78.415–79.973% known conflicts, nonzero unexpected failures, p95 above 21 seconds, severely reduced useful delivery and zero valid-run audit findings. Magnitude differences do not change the interpretation; there is no one-Seed-only technical-failure or integrity result. |
| BurstLike | No | Both Seeds show only 54–62 observed successes, 1,816–2,234 unexpected outcomes, p95 above 41 seconds, very low useful throughput/participation and zero valid-run audit findings. Numerical variation does not create directional ambiguity. A third Seed would not diagnose unavailable inner exceptions or recover the disposed original snapshot. |

**Final count: 4 valid matrix runs; 5 actual official execution attempts including the stopped collection.** No conditional Seed 3001 was executed. The observer-audit replacement is documented separately and stays within the six-execution cap. Another Seed cannot recover the disposed original snapshot or average away its findings; that evidence limitation remains explicit.

## Environment, validation and scope

The build/test and implementation statements in this section record the original execution task. The 2026-11-10 interpretation correction changes documentation and additive JSON interpretation metadata only; all original JSON fields, numerical evidence, raw packages, fingerprints and remediation history are preserved. The JSON interpretation note is a documentary addition, not regenerated output from the unchanged offline analyzer. No scenario is rerun for this correction, including Seed 3001 or C1/C4/C16: additional runs cannot repair the offered-rate interpretation boundary.

Recorded OS/runtime/processor/PostgreSQL descriptions match the retained references. Runs use local Debug .NET 8/PostgreSQL 16.3-alpine via Docker Desktop 24/API 1.43, with no warm-up. Initial Moderate runs shared one process; corrected Burst-like runs used a later process after the documented audit repair. Fixed ordering, cold/JIT/host activity, native resource limits, scheduler delay and an unreserved host limit performance generalization. Light read-only evidence inspection/documentation occurred during runs; full build/test suites completed before each execution phase. No HTTP/network/distributed-client or production-load validation is claimed.

Solution and harness builds passed with zero warnings/errors. The full solution passed **990 .NET tests**, zero failures/skips, before initial execution and again after observer-audit correction. **89 Python tests** passed. Focused coverage includes contract schedules, determinism/exact S2 inputs/no loss, non-draining request/cluster release, cancellation drain, half-open overlap, real PostgreSQL/serialization and valid/invalid post-commit observation correlation. Selected analysis is reproducible read-only from immutable packages; all package checksums and historical reference checksums are verified before/after analysis. git diff --check passed.

Changes are confined to simulation scheduling/evidence, task-specific execution/auditing/offline analysis, tests and documentation. No production Domain/Application/Infrastructure/API, AssignmentEngine, fairness/quality/eligibility policy, S2 input, retry, locking, queue/serialization mitigation, late-group reconfiguration, S3/S4, stakeholder contact or final MVP/production decision changed or occurred.

**Historical next task at interpretation correction: Define Plausible Festival Arrival Concurrency Experiment.** Model plausible temporal dispersion from the business workload rather than deriving arrival rates primarily from API latency. Distinguish OfferedRequestRate, ActiveConcurrency and ConflictRate; avoid sustained offered load that automatically accumulates backlog without a small bound unless overload is the explicit objective. Final intervals, compression factors and run counts remain undefined.

Historical roadmap at the 2026-11-10 correction (superseded by the 2026-11-11 scope decision): reframe arrival-concurrency evidence (completed) → define Plausible Festival Arrival Concurrency (completed) → implement/execute bounded Lean concurrency validation → define/execute Late Large Groups with Reconfiguration → MVP Technical Feasibility Gate. Stage 5 remains In Progress; the MVP Technical Feasibility Gate remains Pending. The observed stress limitations and original observer-audit evidence gap remain Gate inputs, while plausible festival concurrency is Defined / Pending Execution.

### Subsequent contract definition — 2026-11-10

[Plausible Festival Arrival Concurrency](plausible-festival-arrival-concurrency.md)
is now **Defined / Pending Execution**: 120× S2 bucket compression, uniform
Plausible arrivals and three-request PlausibleBurst at 0/10/20 ms with the same
bucket averages (peak 9.767 req/s). Four initial runs, maximum six with conditional
Seed 3001, are defined but not executed. The preceding next-task/undefined-parameter
note and selected JSON interpretation record describe the earlier correction;
the new contract owns current parameters/status. All stress measurements and
remediation history remain unchanged. Historical next task at contract definition: **Implement and Execute
Plausible Festival Arrival Concurrency Experiment**, before late-group reconfiguration.

The [2026-11-11 scope decision](pre-proposal-validation-scope.md) now governs the
next activity: **Perform MVP Technical Feasibility Gate** using existing evidence.
The conditional experiments retain technical value if real arrivals or actual
group/reconfiguration rules justify them; further pre-proposal simulation against
unconfirmed operational assumptions has diminishing value. S3/S4 and the formal
stakeholder/production Gate remain later data-informed work. This sequencing
update neither weakens the severe stress findings nor decides technical feasibility.

## Interpretation-correction validation — 2026-11-10

The correction changed documentation and additive JSON interpretation metadata only.
Task-start comparisons verified all 322 retained raw simulation JSON files byte-for-byte,
all pre-existing analysis JSON fields and their serialized values, all 37 original
analysis table lines, and the unchanged remediation record. Both arrival ledger
checksums and the 80 valid/reference package file checksums were verified read-only.
The retained S8 JSON confirms C1's three-seed mean; all four valid manifests confirm
1,464 requests/day with exactly 293 in the 15:00–16:00 bucket. Links, schedules,
status boundaries and immediate-roadmap consistency were checked; `git diff --check`
passed. Repository-required `dotnet build Festival.sln` passed with zero warnings
or errors; `DOCKER_API_VERSION=1.43 dotnet test Festival.sln` passed all 990 tests
with zero failures/skips. No tests were added and no official scenario was rerun.
No code, mitigation, future experiment parameters or final Gate decision changed.

## Original execution task — files changed

* [README.md](../../README.md)
* [docs/project-operating-model.md](../project-operating-model.md)
* [docs/stage-5/arrival-observer-audit-remediation.md](arrival-observer-audit-remediation.md)
* [docs/stage-5/decision-gate-criteria.md](decision-gate-criteria.md)
* [docs/stage-5/decision-gate-evidence.md](decision-gate-evidence.md)
* [docs/stage-5/metrics-and-evaluation-criteria.md](metrics-and-evaluation-criteria.md)
* [docs/stage-5/mvp-technical-feasibility-gate.md](mvp-technical-feasibility-gate.md)
* [docs/stage-5/production-like-arrival-concurrency-analysis.json](production-like-arrival-concurrency-analysis.json)
* [docs/stage-5/production-like-arrival-concurrency-analysis.md](production-like-arrival-concurrency-analysis.md)
* [docs/stage-5/production-like-arrival-concurrency.md](production-like-arrival-concurrency.md)
* [docs/stage-5/representative-scenarios.md](representative-scenarios.md)
* [docs/stage-5/simulation-architecture.md](simulation-architecture.md)
* [scripts/stage-5-representative-runs/ArrivalRuns.cs](../../scripts/stage-5-representative-runs/ArrivalRuns.cs)
* [scripts/stage-5-representative-runs/Program.cs](../../scripts/stage-5-representative-runs/Program.cs)
* [scripts/stage-5-representative-runs/README.md](../../scripts/stage-5-representative-runs/README.md)
* [scripts/stage-5-representative-runs/S8Runs.cs](../../scripts/stage-5-representative-runs/S8Runs.cs)
* [scripts/stage-5-representative-runs/analyze_arrival_concurrency.py](../../scripts/stage-5-representative-runs/analyze_arrival_concurrency.py)
* [scripts/stage-5-representative-runs/test_arrival_concurrency.py](../../scripts/stage-5-representative-runs/test_arrival_concurrency.py)
* [src/Festival.Simulation/ArrivalDrivenExecution.cs](../../src/Festival.Simulation/ArrivalDrivenExecution.cs)
* [src/Festival.Simulation/ArrivalSchedule.cs](../../src/Festival.Simulation/ArrivalSchedule.cs)
* [src/Festival.Simulation/ArrivalSimulationExecutor.cs](../../src/Festival.Simulation/ArrivalSimulationExecutor.cs)
* [src/Festival.Simulation/MinimalSimulationRunner.cs](../../src/Festival.Simulation/MinimalSimulationRunner.cs)
* [src/Festival.Simulation/PostCommitObservationResolver.cs](../../src/Festival.Simulation/PostCommitObservationResolver.cs)
* [src/Festival.Simulation/ScenarioDefinition.cs](../../src/Festival.Simulation/ScenarioDefinition.cs)
* [src/Festival.Simulation/ScenarioManifest.cs](../../src/Festival.Simulation/ScenarioManifest.cs)
* [src/Festival.Simulation/SimulationEvidenceExporter.cs](../../src/Festival.Simulation/SimulationEvidenceExporter.cs)
* [src/Festival.Simulation/SimulationExecutionConfiguration.cs](../../src/Festival.Simulation/SimulationExecutionConfiguration.cs)
* [src/Festival.Simulation/SimulationOverlapAnalyzer.cs](../../src/Festival.Simulation/SimulationOverlapAnalyzer.cs)
* [src/Festival.Simulation/SimulationResult.cs](../../src/Festival.Simulation/SimulationResult.cs)
* [src/Festival.Simulation/SimulationRunMetadata.cs](../../src/Festival.Simulation/SimulationRunMetadata.cs)
* [tests/Festival.Simulation.Tests/ArrivalConcurrencyTests.cs](../../tests/Festival.Simulation.Tests/ArrivalConcurrencyTests.cs)
