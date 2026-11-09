# S8 initial concurrency analysis

Execution/analysis date: **2026-11-08 (America/Lima)**. Runtime timestamps use UTC. Simulated dates are 2026-08-15–19, fixture inputs rather than confirmed festival dates.

**Smoke and all six initial official five-day runs completed. Every final persisted-state audit found zero violations; no official request produced UnexpectedTechnicalFailure.** C16 raises conflict rate from 75% to approximately 93.7% across all three Seeds. Terminal-attempt throughput increases while request latency tails worsen and substantially fewer Attendees receive persisted Assignments.

**Adaptive decision: Add C1 baseline.** Recommend three same-environment sequential runs to resolve the operational comparison described below. No C1 or repetition was executed. The wider Stage 5 evaluation and Decision Gate remain pending.

## Configuration and authority

The unchanged [S8 contract](s8-concurrency-sensitivity.md), [S2 workload](representative-scenarios.md#s2--expected-baseline), [metrics framework](metrics-and-evaluation-criteria.md) and [architecture](simulation-architecture.md) govern interpretation. Initial matrix: C4 and C16 × Seeds 1001 / 2001 / 3001, one run each, Repetition = 1, RetryCount = 0.

S2 retains 5,500 registered Attendees, exactly 4,400 daily planned requesters, five days, 90% continuity, 50% FrontStanding request eligibility, group/arrival profiles and the shared 4,998-Spot topology. Generator v2, Engine v0.3.0 and rubric v1 are unchanged. Each Seed is materialized once and its exact catalog, empty setup and daily plans reused at both levels. Natural persisted history accumulates within each independent run across Days 1–5.

All three C4/C16 pairs pass exact definition, catalog, topology, request identity, ordered membership, eligibility, RequestedAt/AssignedAt and baseline sequence equality. Their consumed business input also matches the retained official S2 packages, after removing only additive baseline metadata/ExecutionSequence defaults. All eight package checksums were verified for each retained S2 Seed. No S6 grouping or S7 pre-occupation is imported.

## Non-official smoke validation

Two small smoke executions exercised C4 and C16 over 48 registered / 40 requesting Attendees, twenty groups of two, one day and four Spots. C16 has a partial final window of four requests. The last request deliberately uses AssignedAt one tick before RequestedAt, exercising the real use case's unexpected-exception capture. It is an explicitly invalid **non-official negative control**, not an official input or synthetic persistence-conflict injection.

| Configuration | Successful | DomainRejected | Known conflict | Expected technical failure | Invariant findings |
| --- | --- | --- | --- | --- | --- |
| C4 | 2 | 11 | 6 | 1 | 0 |
| C16 | 2 | 8 | 9 | 1 | 0 |

Smoke C4 observed five real 40P01 deadlocks and one 23505 conflict; C16 observed nine 23505 conflicts. Both retained exactly one System.ArgumentException from the invalid timestamp, exported all eight files, drained their windows and passed final-state audits without hanging. The unchanged pre-use-case readiness gate is covered by existing scheduler/scope tests; raw intervals verify actual overlap and ordered window progression. Smoke passed over the exact execution-source hashes later used officially.

Smoke packages are identified in the adjacent JSON; the authoritative smoke ledger is `artifacts/simulations/s8-smoke-20261108-539dff5bc31b41e48d6a1b3f7ca25c17/index.json`. **Smoke results are excluded from every official table and aggregate below.**

## Official run identities and provenance

| Seed | Concurrency | RunId | Evidence directory |
| --- | --- | --- | --- |
| 1001 | 4 | `official-s8-c4-1001-74493949e6ac4c6fb3223f415842ea67` | `artifacts/simulations/s8-concurrency-4/official-s8-c4-1001-74493949e6ac4c6fb3223f415842ea67` |
| 1001 | 16 | `official-s8-c16-1001-2a67b8be05b445da912caa77d8e1ecd2` | `artifacts/simulations/s8-concurrency-16/official-s8-c16-1001-2a67b8be05b445da912caa77d8e1ecd2` |
| 2001 | 4 | `official-s8-c4-2001-0d27719f69eb41e48c15e7326c66ab1a` | `artifacts/simulations/s8-concurrency-4/official-s8-c4-2001-0d27719f69eb41e48c15e7326c66ab1a` |
| 2001 | 16 | `official-s8-c16-2001-a9541531d9ad47f3873dee98a8dd17e2` | `artifacts/simulations/s8-concurrency-16/official-s8-c16-2001-a9541531d9ad47f3873dee98a8dd17e2` |
| 3001 | 4 | `official-s8-c4-3001-e6208bd5f6344211ab7d87b77364ee41` | `artifacts/simulations/s8-concurrency-4/official-s8-c4-3001-e6208bd5f6344211ab7d87b77364ee41` |
| 3001 | 16 | `official-s8-c16-3001-8937f45b4f7b4cd99fcb34e856d61036` | `artifacts/simulations/s8-concurrency-16/official-s8-c16-3001-8937f45b4f7b4cd99fcb34e856d61036` |

Set index: `artifacts/simulations/s8-initial-concurrency-20261108-16ef900e87c348a58384b11abdc7da42/index.json`. SHA-256: `0c097c370c99976026209db6908c3e89f1d91849f0fe5faf9e8ad1bfd6a491ae`. Each package contains the normal eight files; all 48 official file hashes were verified before and after analysis. The [selected structured analysis](s8-concurrency-analysis.json) retains their hashes, source/input proofs, environment, counts and denominators.

Repository revision: `bf3f37a143992ea373cea701a34a4a641f9330af`. Production/Simulation source is unchanged in this task. The task-specific harness was built from uncommitted additions; the ledger records SHA-256 for every execution-source/project file and the harness, rather than claiming the commit alone identifies those additions. The structured analysis also fingerprints its Python sources. Raw packages remain local under ignored `artifacts/simulations/`; back them up before deleting the checkout. No raw evidence is committed.

## Correctness and outcome counts

| Seed | C | Attempted | Normally processed | Successful | DomainRejected | Known conflict | Unexpected failure | Excluded | PersistedInvariantViolationCount |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | 4 | 7320 | 1830 | 1830 | 0 | 5490 | 0 | 0 | 0 |
| 1001 | 16 | 7320 | 461 | 461 | 0 | 6859 | 0 | 0 | 0 |
| 2001 | 4 | 7320 | 1830 | 1830 | 0 | 5490 | 0 | 0 | 0 |
| 2001 | 16 | 7320 | 460 | 460 | 0 | 6860 | 0 | 0 | 0 |
| 3001 | 4 | 7320 | 1830 | 1830 | 0 | 5490 | 0 | 0 | 0 |
| 3001 | 16 | 7320 | 460 | 460 | 0 | 6860 | 0 | 0 | 0 |

Each run independently satisfies PersistedInvariantViolationCount = 0. Audits consume the runner's final durable PostgreSQL reads and check Spot/day and Attendee/day uniqueness; complete, same-Zone/same-Row contiguous groups; catalog/request/Assignment identity; persisted status; no orphan/fake requests; no losing partial Assignment graph; checkpoint and path reconciliation. Failed/conflict members have no persisted Assignment for that FestivalDay, and every persisted request maps to a confirmed normal Attempt. Database constraints remain final integrity protection.

These are final-state audits, not a complete temporal proof of INV-06. Existing production behavior offers no replacement/cancellation in this flow. An integrity finding would stop the protocol after retaining evidence. None occurred. Known rolled-back contention is reported separately from corruption, domain rejection and unexpected failure.

## Outcome rates and denominators

AttemptedRequests = Successful + DomainRejected + KnownConcurrencyConflict + UnexpectedTechnicalFailure. NormallyProcessedRequests = Successful + DomainRejected. Success/RejectionRate use NormallyProcessedRequests; conflict/TechnicalErrorRate use AttemptedRequests. Exclusions stay separate; zero denominators remain N/A.

| Seed | C | SuccessRate | RejectionRate | KnownConcurrencyConflictRate | TechnicalErrorRate |
| --- | --- | --- | --- | --- | --- |
| 1001 | 4 | 100.000% | 0.000% | 75.000% | 0.000% |
| 1001 | 16 | 100.000% | 0.000% | 93.702% | 0.000% |
| 2001 | 4 | 100.000% | 0.000% | 75.000% | 0.000% |
| 2001 | 16 | 100.000% | 0.000% | 93.716% | 0.000% |
| 3001 | 4 | 100.000% | 0.000% | 75.000% | 0.000% |
| 3001 | 16 | 100.000% | 0.000% | 93.716% | 0.000% |

SuccessRate = 100% describes the normally processed subset. It does not mean all requests were served: C4 persists 1,830 of 7,320 Attempts; C16 persists only 460–461. No conflict is converted to DomainRejected, a synthetic Bad outcome or a retry.

## Request latency, throughput and execution duration

Units: request latency in milliseconds, throughput in terminal Attempts/second, duration in elapsed seconds. Nearest-rank percentiles use all 7,320 AttemptedRequests per run, including conflicts; they do not select successes only.

| Seed | C | p50 ms | p95 ms | p99 ms | Attempts/s | Execution seconds | Runner lifecycle seconds |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | 4 | 74.052 | 2,068.156 | 3,065.974 | 7.825 | 935.496 | 943.534 |
| 1001 | 16 | 265.399 | 3,262.293 | 8,254.079 | 16.420 | 445.789 | 450.090 |
| 2001 | 4 | 81.674 | 2,072.600 | 3,071.258 | 7.145 | 1,024.470 | 1,028.359 |
| 2001 | 16 | 286.052 | 4,212.878 | 8,298.078 | 15.355 | 476.718 | 480.582 |
| 3001 | 4 | 87.248 | 2,096.992 | 3,073.869 | 6.786 | 1,078.724 | 1,082.575 |
| 3001 | 16 | 254.464 | 4,176.740 | 7,269.912 | 15.846 | 461.948 | 465.808 |

The common monotonic interval is:

```text
execution seconds = [max(ProcessingStartedOffset + Duration)
                     - min(ProcessingStartedOffset)] / ticks per second
throughput = AttemptedRequests / execution seconds
```

It spans first measured use-case entry through last observed terminal outcome across all five days, includes overlap and intervening scheduling/checkpoint gaps, and never sums overlapping request durations. Setup/generation/container start precede the interval; final reconciliation/export beyond the last terminal outcome are excluded. Per-request duration excludes scope preparation, gate waiting and later reconciliation. Runner lifecycle timestamps include broader setup/analysis/teardown and are **not** the throughput denominator.

Every p95 exceeds the provisional p95 < 1 second hypothesis. This is an analytical hypothesis, not an SLA. C16 finishes the declared workload sooner and records higher terminal throughput, while p50/p95/p99 are worse and far fewer requests persist. Faster conflict completions must not be read as more delivered assignments.

## Across-Seed aggregate comparison

Each cell is **minimum / mean / maximum** over the three individual Seed-level results. Means are unweighted; averaged per-run percentiles are not pooled percentiles. This is Seed-level variation, not repetition-level scheduler variability.

| Metric | C4 min / mean / max | C16 min / mean / max |
| --- | --- | --- |
| ConflictRate | 75.000% / 75.000% / 75.000% | 93.702% / 93.711% / 93.716% |
| TechnicalErrorRate | 0.000% / 0.000% / 0.000% | 0.000% / 0.000% / 0.000% |
| p50 ms | 74.052 / 80.991 / 87.248 | 254.464 / 268.638 / 286.052 |
| p95 ms | 2,068.156 / 2,079.249 / 2,096.992 | 3,262.293 / 3,883.970 / 4,212.878 |
| p99 ms | 3,065.974 / 3,070.367 / 3,073.869 | 7,269.912 / 7,940.689 / 8,298.078 |
| Attempts/s | 6.786 / 7.252 / 7.825 | 15.355 / 15.874 / 16.420 |
| Execution seconds | 935.496 / 1,012.897 / 1,078.724 | 445.789 / 461.485 / 476.718 |

## Direct C4 vs C16 per Seed

Deltas below are C16 minus its exact same-Seed C4 pair. SuccessRate remains 100%, RejectionRate/TechnicalErrorRate remain 0%, and persisted findings remain zero in **every** pair.

| Seed | Conflict delta pp | p50 delta ms | p95 delta ms | p99 delta ms | Throughput delta /s | Execution delta s | Participating delta |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | 18.702 | 191.347 | 1,194.137 | 5,188.105 | 8.596 | -489.708 | -1855 |
| 2001 | 18.716 | 204.379 | 2,140.278 | 5,226.819 | 8.210 | -547.752 | -1894 |
| 3001 | 18.716 | 167.216 | 2,079.748 | 4,196.043 | 9.060 | -616.776 | -1877 |

The direction is consistent across Seeds: more conflicts, longer latency tails, fewer participating Attendees, shorter total execution and higher terminal-attempt throughput. These are observations under the tested no-retry windows; exact winner identities and timings are not guaranteed by Seed.

## Conflict by FestivalDay

All denominators are 1,464 AttemptedRequests per day/run.

| Category | Attempts per run | C4/1001 | C16/1001 | C4/2001 | C16/2001 | C4/3001 | C16/3001 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 1464 | 1098 (75.000%) | 1371 (93.648%) | 1098 (75.000%) | 1372 (93.716%) | 1098 (75.000%) | 1372 (93.716%) |
| 2 | 1464 | 1098 (75.000%) | 1372 (93.716%) | 1098 (75.000%) | 1372 (93.716%) | 1098 (75.000%) | 1372 (93.716%) |
| 3 | 1464 | 1098 (75.000%) | 1372 (93.716%) | 1098 (75.000%) | 1372 (93.716%) | 1098 (75.000%) | 1372 (93.716%) |
| 4 | 1464 | 1098 (75.000%) | 1372 (93.716%) | 1098 (75.000%) | 1372 (93.716%) | 1098 (75.000%) | 1372 (93.716%) |
| 5 | 1464 | 1098 (75.000%) | 1372 (93.716%) | 1098 (75.000%) | 1372 (93.716%) | 1098 (75.000%) | 1372 (93.716%) |

## Conflict by baseline daily third

Each third contains 488 daily requests and 2,440 Attempts over five days. Daily ordinal j uses floor((j - 1) × 3 / daily request count); BaselineSequence retains S2 order, not completion order.

| Category | Attempts per run | C4/1001 | C16/1001 | C4/2001 | C16/2001 | C4/3001 | C16/3001 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| first | 2440 | 1830 (75.000%) | 2286 (93.689%) | 1830 (75.000%) | 2289 (93.811%) | 1830 (75.000%) | 2286 (93.689%) |
| middle | 2440 | 1830 (75.000%) | 2288 (93.770%) | 1830 (75.000%) | 2286 (93.689%) | 1830 (75.000%) | 2289 (93.811%) |
| last | 2440 | 1830 (75.000%) | 2285 (93.648%) | 1830 (75.000%) | 2285 (93.648%) | 1830 (75.000%) | 2285 (93.648%) |

## Conflict by GroupSize

Each cell reports conflict count (normalized rate). The shared denominator column is AttemptedRequests in that bucket for each individual run; every request counts once, irrespective of members.

| Category | Attempts per run | C4/1001 | C16/1001 | C4/2001 | C16/2001 | C4/3001 | C16/3001 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 1100 | 387 (35.182%) | 938 (85.273%) | 357 (32.455%) | 925 (84.091%) | 352 (32.000%) | 944 (85.818%) |
| 2 | 2195 | 1569 (71.481%) | 2031 (92.528%) | 1586 (72.255%) | 2034 (92.665%) | 1605 (73.121%) | 2035 (92.711%) |
| 3 | 1830 | 1525 (83.333%) | 1747 (95.464%) | 1535 (83.880%) | 1762 (96.284%) | 1540 (84.153%) | 1744 (95.301%) |
| 4 | 1100 | 995 (90.455%) | 1067 (97.000%) | 995 (90.455%) | 1062 (96.545%) | 976 (88.727%) | 1064 (96.727%) |
| 5 | 585 | 529 (90.427%) | 573 (97.949%) | 536 (91.624%) | 576 (98.462%) | 532 (90.940%) | 573 (97.949%) |
| 6–7 | 365 | 347 (95.068%) | 359 (98.356%) | 342 (93.699%) | 360 (98.630%) | 348 (95.342%) | 357 (97.808%) |
| 8–10 | 145 | 138 (95.172%) | 144 (99.310%) | 139 (95.862%) | 141 (97.241%) | 137 (94.483%) | 143 (98.621%) |

Daily/third rates do not show a late-position conflict spike in this set. Baseline position alone does not establish arrival causality; simulated RequestedAt does not replay real-time venue traffic. GroupSize tables show an association between larger groups and higher conflict rates, with local exceptions to strict ordering. They do not isolate a causal group-size mechanism from window composition and SQL/scheduler interleavings.

## Conflict diagnostics and window evidence

| Seed | C | 23505 / SpotAlreadyAssigned | 40P01 / DeadlockDetected | Deadlock share of conflicts |
| --- | --- | --- | --- | --- |
| 1001 | 4 | 4679 | 811 | 14.772% |
| 1001 | 16 | 6534 | 325 | 4.738% |
| 2001 | 4 | 4597 | 893 | 16.266% |
| 2001 | 16 | 6517 | 343 | 5.000% |
| 3001 | 4 | 4548 | 942 | 17.158% |
| 3001 | 16 | 6508 | 352 | 5.131% |

All 40P01 Attempts retain DatabaseSqlState = 40P01 and DiagnosticCode = DeadlockDetected and are KnownConcurrencyConflict after rollback. 23505 is recognized through the existing typed constraint translation; arbitrary other database errors are not automatically conflicts. No unexpected database failures occurred. C16 has fewer deadlock victims yet worse latency tails in each pair; victim counts alone do not describe operational usability.

| Seed | C | Windows | Partial windows | Max observed overlap | Windows without overlap | Max window ms |
| --- | --- | --- | --- | --- | --- | --- |
| 1001 | 4 | 1830 | 0 | 4 | 0 | 3,178.957 |
| 1001 | 16 | 460 | 5 | 16 | 0 | 14,247.236 |
| 2001 | 4 | 1830 | 0 | 4 | 0 | 3,129.865 |
| 2001 | 16 | 460 | 5 | 16 | 0 | 13,299.232 |
| 3001 | 4 | 1830 | 0 | 4 | 0 | 3,186.836 |
| 3001 | 16 | 460 | 5 | 16 | 0 | 13,185.537 |

Every adjacent-window timing check passes. C16 has one final window of eight requests per day; C4 divides each day exactly. Raw attempts retain every WindowNumber/WindowPosition and reconstruct window outcome counts and duration. The selected JSON keeps aggregate window observations rather than duplicating all raw telemetry. Observed intervals demonstrate actual overlap; window construction and pre-use-case coordinated release remain unchanged.

## Fairness population and coverage

| Seed | C | Registered | Requesting | Participating | Only rejected/failed | NoPersistedPath | InsufficientEvidence | Classifiable |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | 4 | 5500 | 5354 | 2764 | 2590 | 2590 | 1930 | 834 |
| 1001 | 16 | 5500 | 5354 | 909 | 4445 | 4445 | 842 | 67 |
| 2001 | 4 | 5500 | 5359 | 2793 | 2566 | 2566 | 2025 | 768 |
| 2001 | 16 | 5500 | 5359 | 899 | 4460 | 4460 | 837 | 62 |
| 3001 | 4 | 5500 | 5344 | 2810 | 2534 | 2534 | 2021 | 789 |
| 3001 | 16 | 5500 | 5344 | 933 | 4411 | 4411 | 862 | 71 |

Requesting counts distinct identities over Days 1–5, not 4,400 daily requesters added together. Only-rejected/failed includes attendees whose Attempts all conflict; it does not imply a persisted Failed status. It equals NoPersistedPath here because inputs are valid and every planned request is attempted.

C16 leaves 4,411–4,460 requesting Attendees without any persisted path and only 62–71 classifiable paths, compared with C4's 2,534–2,590 without a path and 768–834 classifiable paths. This is a substantial population-selection effect. Conflict, failure, rejection and absence create no synthetic ExperienceQuality or recovery opportunity.

## Secondary quality, path and recovery metrics

All values below are consumed from stored metrics, reconciled against exported paths/episodes without rerunning the rubric or recovery analysis. Quality rates use persisted Assignment outcomes; path rates use Classifiable paths.

| Seed | C | Persisted outcomes | GoodOutcomeRate | MediumOutcomeRate | BadOutcomeRate |
| --- | --- | --- | --- | --- | --- |
| 1001 | 4 | 3764 | 16.870% | 83.130% | 0.000% |
| 1001 | 16 | 981 | 0.000% | 100.000% | 0.000% |
| 2001 | 4 | 3718 | 16.487% | 83.513% | 0.000% |
| 2001 | 16 | 963 | 0.000% | 100.000% | 0.000% |
| 3001 | 4 | 3735 | 16.573% | 83.427% | 0.000% |
| 3001 | 16 | 1006 | 0.000% | 100.000% | 0.000% |

| Seed | C | Classifiable | BalancedPathRate | AcceptablePathRate | NotBalancedPathRate | Eligible recovery episodes | RecoveryRate |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | 4 | 834 | 17.986% | 82.014% | 0.000% | 0 | N/A |
| 1001 | 16 | 67 | 0.000% | 100.000% | 0.000% | 0 | N/A |
| 2001 | 4 | 768 | 17.839% | 82.161% | 0.000% | 0 | N/A |
| 2001 | 16 | 62 | 0.000% | 100.000% | 0.000% | 0 | N/A |
| 3001 | 4 | 789 | 16.223% | 83.777% | 0.000% | 0 | N/A |
| 3001 | 16 | 71 | 0.000% | 100.000% | 0.000% | 0 | N/A |

Every run has zero Bad outcomes, zero recovery episodes and zero eligible episodes; RecoveryRate is **N/A**, not 0%. C16's 100% AcceptablePathRate and zero NotBalancedPathRate describe just 62–71 surviving classifiable paths. They do not establish improved system fairness for the full requesting population. Persisted history evolves naturally under each run's own successes, so concurrency also changes the observed histories available to the unchanged fairness policy.

## Adaptive follow-up decision

**Chosen: Add C1 baseline.** The six runs provide a clear, consistent concurrency signal and preserved final-state correctness. They do not answer the remaining operational question: **does either concurrent configuration improve latency, useful assignment delivery or elapsed processing over the existing sequential flow in this same environment?** Terminal throughput rises while persisted delivery and participation fall, making that comparator relevant.

Next task: **Execute S8 Optional Sequential Baseline and Compare Operational Results** — C1 for Seeds 1001 / 2001 / 3001, once each (+3), same exact S2 inputs and timing boundaries. The retained historical S2 execution is an input/control reference; its exploratory timings are not substituted for a same-environment monotonic baseline. No C1 or repeat was executed here, and no mandatory final run total or blanket repetition requirement is introduced.

This is an evidence-collection recommendation, not a retry/locking/queueing/Engine mitigation or a Decision Gate verdict. Exact scheduler variability remains unquantified by this initial phase.

## Environment, reproducibility and interpretation limits

OS: Darwin 24.6.0 (Intel/x86_64); runtime/.NET 8.0.3; host processor count 16. Docker Desktop 24.0.2, API 1.43, LinuxKit 5.15.49, eight Docker CPUs and approximately 7.77 GiB Docker memory; pinned postgres:16.3-alpine via Testcontainers. Build: Debug. Each run uses a fresh owned PostgreSQL container; the six configurations run serially in one process, C4 first, then C16.

No warm-up is performed; first calls remain in the full-workload samples and process/JIT warmness differs across the fixed execution order. These are full-scenario operational observations, not a steady-state benchmark. The host was not reserved: lightweight offline validation/analysis and a few read-only progress queries occurred during execution. Observation overhead and normal runner checkpoints remain included where they fall inside the declared interval.

Input reproducibility is verified by exact manifests, source fingerprints, source-S2 comparison, Seeds, topology and deterministic windows. One execution per Seed/level cannot establish run-to-run reproducibility, conflict-identity stability or the magnitude of scheduler/runtime-ID/PostgreSQL interleaving variation. Cross-Seed min/mean/max must not be presented as repetition-level variability or production probabilities.

Conclusions apply to this provisional S2 workload, database, runner and local execution environment. They cannot establish production traffic behavior, HTTP/network experience, distributed multi-node behavior, cloud scaling or all contention patterns. EC2 is not required; none was used. Stakeholder workload/topology assumptions remain provisional.

## Validation and scope

Solution build and the dedicated harness build passed with zero warnings/errors. DOCKER_API_VERSION=1.43 dotnet test Festival.sln passed **967 .NET tests**, zero failures/skips. The offline script suite passed **72 Python tests**, including eight new analytical regression tests for denominators, percentiles, overlap/gaps, exact ticks, N/A, buckets, matrix/smoke exclusion and tampered evidence.

The selected JSON was independently regenerated and compared byte-for-byte; raw checksums were verified before/after analysis. git diff --check passed. The execution ledger fingerprints the unchanged execution sources throughout all runs.

Changes are confined to the task-specific harness, offline analysis/tests and documentation. No Domain/Application/Infrastructure/Simulation production behavior, scheduler, conflict classification, fairness/rubric/recovery/metrics policy, S2/S5/S6/S7 workload, retries, locking, isolation, window sizes, S3/S4 implementation or Decision Gate mitigation was changed. Exactly six initial official runs were executed; smoke and automated test fixtures are separate.
