# S8 concurrency and operational analysis

Execution/analysis date: **2026-11-08 (America/Lima)**. Runtime timestamps and the C1 set's 20261109 directory label use UTC. Simulated dates are 2026-08-15–19, fixture inputs rather than confirmed festival dates.

**All three sequential C1 five-day runs completed. The complete C1 → C4 → C16 comparison contains nine official runs, each with zero final persisted invariant findings and zero unexpected technical failures.** C1 delivered every request successfully, finished faster, and achieved higher terminal and useful throughput than either concurrent configuration for every Seed. C16 increased terminal processing relative to C4 while delivering fewer successful requests and Assignments.

**Final S8 decision: evidence sufficient** for this local operational comparison. No concurrent repetition is required for the current question. The wider Stage 5 evaluation and Decision Gate remain pending; this decision introduces no mitigation.

## Configuration, sequential baseline execution and workload integrity

The unchanged [S8 contract](s8-concurrency-sensitivity.md), [S2 workload](representative-scenarios.md#s2--expected-baseline), [metrics framework](metrics-and-evaluation-criteria.md) and [architecture](simulation-architecture.md) govern interpretation. Initial matrix: C4/C16 × Seeds 1001 / 2001 / 3001. Authorized follow-up: exactly C1 × those three Seeds, once each. Every official entry uses Repetition = 1 and RetryCount = 0. C4/C16 were read from retained evidence and were not rerun.

S2 retains 5,500 registered Attendees, exactly 4,400 daily planned requesters, five days, 90% continuity, 50% FrontStanding request eligibility, group/arrival profiles and the shared 4,998-Spot topology. Generator v2, Engine v0.3.0 and rubric v1 remain unchanged. Each Seed is materialized once in the C1 harness; its definition and exact catalog, empty setup and daily plans match both retained concurrent inputs. This includes requester identities, membership, group-size profile, eligibility, RequestedAt/AssignedAt and BaselineSequence. Natural persisted history accumulates within each independent five-day run.

C1 uses the existing direct sequential loop: one request completes before the next starts. All 7,320 ordered request intervals per C1 run pass the no-overlap check across all five days. WindowNumber/WindowPosition are absent; no concurrent start gate is used. Level 1 retains ScenarioId = s2-expected-baseline with sequential metadata, rather than generating a new workload under an S8 identity. The standard runner uses real runner-owned PostgreSQL/Testcontainers persistence and a fresh database per run.

The preflight checked retained-package checksums, unchanged production/Simulation source fingerprints, source manifest hashes, exact consumed business inputs and matching execution environment before C1/1001. All three C1/C4/C16 triplets also match retained official S2 inputs after normalizing only additive baseline metadata/ExecutionSequence defaults. Those three historical S2 packages are input controls; their exploratory timings are not used as this baseline. No S6 grouping or S7 pre-occupation is imported.

## Non-official smoke validation

These are retained initial-phase smoke observations. No smoke was rerun for the C1 follow-up.

Two small smoke executions exercised C4 and C16 over 48 registered / 40 requesting Attendees, twenty groups of two, one day and four Spots. C16 has a partial final window of four requests. The last request deliberately uses AssignedAt one tick before RequestedAt, exercising the real use case's unexpected-exception capture. It is an explicitly invalid **non-official negative control**, not an official input or synthetic persistence-conflict injection.

| Configuration | Successful | DomainRejected | Known conflict | Expected technical failure | Invariant findings |
| --- | --- | --- | --- | --- | --- |
| C4 | 2 | 11 | 6 | 1 | 0 |
| C16 | 2 | 8 | 9 | 1 | 0 |

Smoke C4 observed five real 40P01 deadlocks and one 23505 conflict; C16 observed nine 23505 conflicts. Both retained exactly one System.ArgumentException from the invalid timestamp, exported all eight files, drained their windows and passed final-state audits without hanging. The unchanged pre-use-case readiness gate is covered by existing scheduler/scope tests; raw intervals verify actual overlap and ordered window progression. Smoke passed over the exact execution-source hashes later used officially.

Smoke packages are identified in the adjacent JSON; the authoritative smoke ledger is `artifacts/simulations/s8-smoke-20261108-539dff5bc31b41e48d6a1b3f7ca25c17/index.json`. **Smoke results are excluded from every official table and aggregate below.**

## Official runs and provenance

| Seed | C | RunId | Repetition | Evidence directory |
| --- | --- | --- | --- | --- |
| 1001 | 1 | `official-s8-c1-1001-c86637ca425a462db8eb9c192e373b4c` | 1 | `artifacts/simulations/s2-expected-baseline/official-s8-c1-1001-c86637ca425a462db8eb9c192e373b4c` |
| 1001 | 4 | `official-s8-c4-1001-74493949e6ac4c6fb3223f415842ea67` | 1 | `artifacts/simulations/s8-concurrency-4/official-s8-c4-1001-74493949e6ac4c6fb3223f415842ea67` |
| 1001 | 16 | `official-s8-c16-1001-2a67b8be05b445da912caa77d8e1ecd2` | 1 | `artifacts/simulations/s8-concurrency-16/official-s8-c16-1001-2a67b8be05b445da912caa77d8e1ecd2` |
| 2001 | 1 | `official-s8-c1-2001-1acf79ee5a7c4f459f16ce10f4a7686e` | 1 | `artifacts/simulations/s2-expected-baseline/official-s8-c1-2001-1acf79ee5a7c4f459f16ce10f4a7686e` |
| 2001 | 4 | `official-s8-c4-2001-0d27719f69eb41e48c15e7326c66ab1a` | 1 | `artifacts/simulations/s8-concurrency-4/official-s8-c4-2001-0d27719f69eb41e48c15e7326c66ab1a` |
| 2001 | 16 | `official-s8-c16-2001-a9541531d9ad47f3873dee98a8dd17e2` | 1 | `artifacts/simulations/s8-concurrency-16/official-s8-c16-2001-a9541531d9ad47f3873dee98a8dd17e2` |
| 3001 | 1 | `official-s8-c1-3001-ae125d37d9264998a9af24c4a8bfb913` | 1 | `artifacts/simulations/s2-expected-baseline/official-s8-c1-3001-ae125d37d9264998a9af24c4a8bfb913` |
| 3001 | 4 | `official-s8-c4-3001-e6208bd5f6344211ab7d87b77364ee41` | 1 | `artifacts/simulations/s8-concurrency-4/official-s8-c4-3001-e6208bd5f6344211ab7d87b77364ee41` |
| 3001 | 16 | `official-s8-c16-3001-8937f45b4f7b4cd99fcb34e856d61036` | 1 | `artifacts/simulations/s8-concurrency-16/official-s8-c16-3001-8937f45b4f7b4cd99fcb34e856d61036` |

Initial index: `artifacts/simulations/s8-initial-concurrency-20261108-16ef900e87c348a58384b11abdc7da42/index.json`. SHA-256: `0c097c370c99976026209db6908c3e89f1d91849f0fe5faf9e8ad1bfd6a491ae`.

C1 index: `artifacts/simulations/s8-sequential-baseline-20261109-caade8775ecc480a90c089ebf128c705/index.json`. SHA-256: `07be60bdefbabe4cebac89afe6f5979f54f337a4b0ccce3f5f26f2483e5b57d9`. Status Completed; exactly three C1 entries. It links the original index by checksum. Every run completed five FestivalDays.

Each package contains run-metadata.json, definition.json, manifest.json, attempts.json, checkpoints.json, attendee-paths.json, recovery-observations.json and metrics.json. All **72 official file checksums** were verified before and after analysis, alongside retained smoke/S2 controls. The [selected structured analysis](s8-concurrency-analysis.json) retains original run facts, hashes, environments, source/input proofs, per-Seed comparisons, aggregates and derived useful delivery rates. Every pre-existing C4/C16 run field and aggregate was checked unchanged; only the two useful throughput metrics were added.

Initial execution revision: `bf3f37a143992ea373cea701a34a4a641f9330af`. C1 execution revision: `3f45b4efe32c8b4b03d6aadb5434e22a3c35ca09`. All protected src source/project fingerprints match. Task-specific harness additions were uncommitted at their respective execution times; each ledger records exact harness/source SHA-256 rather than claiming the commit alone identifies them. The structured analysis also fingerprints its Python sources. Raw packages remain local under ignored artifacts/simulations/; preserve them outside the checkout before deleting it. Only the selected analysis/report belongs in repository history.

## Correctness and outcomes

| Seed | C | Attempted | Normal | Successful | DomainRejected | Known conflict | Unexpected failure | Excluded | Invariant findings |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | 1 | 7320 | 7320 | 7320 | 0 | 0 | 0 | 0 | 0 |
| 1001 | 4 | 7320 | 1830 | 1830 | 0 | 5490 | 0 | 0 | 0 |
| 1001 | 16 | 7320 | 461 | 461 | 0 | 6859 | 0 | 0 | 0 |
| 2001 | 1 | 7320 | 7320 | 7320 | 0 | 0 | 0 | 0 | 0 |
| 2001 | 4 | 7320 | 1830 | 1830 | 0 | 5490 | 0 | 0 | 0 |
| 2001 | 16 | 7320 | 460 | 460 | 0 | 6860 | 0 | 0 | 0 |
| 3001 | 1 | 7320 | 7320 | 7320 | 0 | 0 | 0 | 0 | 0 |
| 3001 | 4 | 7320 | 1830 | 1830 | 0 | 5490 | 0 | 0 | 0 |
| 3001 | 16 | 7320 | 460 | 460 | 0 | 6860 | 0 | 0 | 0 |

Each C1 run has PersistedInvariantViolationCount = 0, 7,320 completed persisted requests and 22,000 durable Assignments. All nine audits consume final durable PostgreSQL reads and check Spot/day and Attendee/day uniqueness; complete contiguous same-Zone/same-Row groups; catalog/request/Assignment identity; consistent status; no orphan/fake persisted requests; no losing partial Assignment graph; checkpoint and path reconciliation. Database constraints remain final integrity protection. No partial invalid state or fake Rejected request remains. No C1 conflict occurred, so there is no sequential loser to reclassify or normalize.

These are final-state audits, not a complete temporal proof of INV-06. Existing behavior offers no replacement/cancellation in this flow. An invariant finding would stop execution after preserving evidence; none occurred. Known rolled-back contention is separate from corruption, domain rejection and unexpected failure.

## Outcome rates and denominators

AttemptedRequests = Successful + DomainRejected + KnownConcurrencyConflict + UnexpectedTechnicalFailure. NormallyProcessedRequests = Successful + DomainRejected. SuccessRate/RejectionRate use NormallyProcessedRequests; conflict/TechnicalErrorRate use AttemptedRequests. Exclusions stay separate; zero denominators remain N/A.

| Seed | C | SuccessRate | RejectionRate | KnownConcurrencyConflictRate | TechnicalErrorRate |
| --- | --- | --- | --- | --- | --- |
| 1001 | 1 | 100.000% | 0.000% | 0.000% | 0.000% |
| 1001 | 4 | 100.000% | 0.000% | 75.000% | 0.000% |
| 1001 | 16 | 100.000% | 0.000% | 93.702% | 0.000% |
| 2001 | 1 | 100.000% | 0.000% | 0.000% | 0.000% |
| 2001 | 4 | 100.000% | 0.000% | 75.000% | 0.000% |
| 2001 | 16 | 100.000% | 0.000% | 93.716% | 0.000% |
| 3001 | 1 | 100.000% | 0.000% | 0.000% | 0.000% |
| 3001 | 4 | 100.000% | 0.000% | 75.000% | 0.000% |
| 3001 | 16 | 100.000% | 0.000% | 93.716% | 0.000% |

SuccessRate = 100% describes the normally processed subset. C1 serves all 7,320 Attempts; C4 serves 1,830 and C16 serves 460–461. The concurrent 100% SuccessRate therefore does not mean full workload delivery. No conflict is converted into DomainRejected, a synthetic Bad outcome or a retry.

## Performance and useful throughput: C1 vs C4 vs C16 per Seed

Latency is in milliseconds, throughput is per second, and duration is elapsed seconds. Nearest-rank percentiles include every AttemptedRequest, including conflicts, with exclusions omitted. All runs have 7,320 measured samples. Each Seed's three adjacent rows are a direct input-matched comparison.

| Seed | C | p50 ms | p95 ms | p99 ms | Terminal Attempts/s | Successful requests/s | Persisted Assignments/s | Execution seconds |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | 1 | 27.095 | 46.382 | 67.731 | 30.871 | 30.871 | 92.783 | 237.112 |
| 1001 | 4 | 74.052 | 2,068.156 | 3,065.974 | 7.825 | 1.956 | 4.024 | 935.496 |
| 1001 | 16 | 265.399 | 3,262.293 | 8,254.079 | 16.420 | 1.034 | 2.201 | 445.789 |
| 2001 | 1 | 28.726 | 51.821 | 80.816 | 28.509 | 28.509 | 85.684 | 256.758 |
| 2001 | 4 | 81.674 | 2,072.600 | 3,071.258 | 7.145 | 1.786 | 3.629 | 1,024.470 |
| 2001 | 16 | 286.052 | 4,212.878 | 8,298.078 | 15.355 | 0.965 | 2.020 | 476.718 |
| 3001 | 1 | 28.351 | 48.683 | 75.011 | 29.397 | 29.397 | 88.352 | 249.003 |
| 3001 | 4 | 87.248 | 2,096.992 | 3,073.869 | 6.786 | 1.696 | 3.462 | 1,078.724 |
| 3001 | 16 | 254.464 | 4,176.740 | 7,269.912 | 15.846 | 0.996 | 2.178 | 461.948 |

The same monotonic timing boundary applies to all nine runs:

```text
execution seconds = [max(ProcessingStartedOffset + Duration)
                     - min(ProcessingStartedOffset)] / ticks per second
terminal Attempts/s = AttemptedRequests / execution seconds
SuccessfulRequestThroughput = Successful / execution seconds
SuccessfulAssignmentThroughput = persisted Assignments / execution seconds
```

Assignment delivery uses audited durable row counts reconciled against exported persisted paths, not attempted group sizes. C1 delivers 22,000 Assignments per run; C4 delivers 3,764 / 3,718 / 3,735 and C16 delivers 981 / 963 / 1,006 for Seeds 1001 / 2001 / 3001.

The interval spans first measured use-case entry through last terminal outcome across Days 1–5, includes overlap and intervening scheduling/checkpoint gaps, and never sums overlapping durations. Setup/generation/container start precede the interval; final reconciliation/export beyond the last terminal outcome are excluded. Per-request duration excludes scope preparation, gate waiting and later reconciliation. Broader runner lifecycle time is not the denominator; C1 lifecycle seconds were 245.202 / 260.932 / 252.991, while retained C4 values remain 943.534 / 1,028.359 / 1,082.575 and C16 values 450.090 / 480.582 / 465.808.

All C1 p95 values (46.382 / 51.821 / 48.683 ms) satisfy the existing provisional **p95 < 1 second** hypothesis. Every C4/C16 p95 fails it. This remains an analytical hypothesis, not an SLA.

## Across-Seed aggregate comparison

Each cell is **minimum / mean / maximum** over three Seed-level results. Means are unweighted; averaged per-run percentiles are not pooled percentiles. All configurations have zero persisted invariant findings in every run.

| Metric | C1 min / mean / max | C4 min / mean / max | C16 min / mean / max |
| --- | --- | --- | --- |
| ConflictRate | 0.000% / 0.000% / 0.000% | 75.000% / 75.000% / 75.000% | 93.702% / 93.711% / 93.716% |
| TechnicalErrorRate | 0.000% / 0.000% / 0.000% | 0.000% / 0.000% / 0.000% | 0.000% / 0.000% / 0.000% |
| SuccessRate | 100.000% / 100.000% / 100.000% | 100.000% / 100.000% / 100.000% | 100.000% / 100.000% / 100.000% |
| RejectionRate | 0.000% / 0.000% / 0.000% | 0.000% / 0.000% / 0.000% | 0.000% / 0.000% / 0.000% |
| p50 ms | 27.095 / 28.057 / 28.726 | 74.052 / 80.991 / 87.248 | 254.464 / 268.638 / 286.052 |
| p95 ms | 46.382 / 48.962 / 51.821 | 2,068.156 / 2,079.249 / 2,096.992 | 3,262.293 / 3,883.970 / 4,212.878 |
| p99 ms | 67.731 / 74.519 / 80.816 | 3,065.974 / 3,070.367 / 3,073.869 | 7,269.912 / 7,940.689 / 8,298.078 |
| Terminal Attempts/s | 28.509 / 29.593 / 30.871 | 6.786 / 7.252 / 7.825 | 15.355 / 15.874 / 16.420 |
| Successful requests/s | 28.509 / 29.593 / 30.871 | 1.696 / 1.813 / 1.956 | 0.965 / 0.998 / 1.034 |
| Persisted Assignments/s | 85.684 / 88.940 / 92.783 | 3.462 / 3.705 / 4.024 | 2.020 / 2.133 / 2.201 |
| Execution seconds | 237.112 / 247.624 / 256.758 | 935.496 / 1,012.897 / 1,078.724 | 445.789 / 461.485 / 476.718 |

This is cross-Seed consistency, not repetition-level scheduler variability. The exact per-Seed configuration/count/population comparisons are also retained in the selected JSON.

## Useful delivery and operational interpretation

**Increasing concurrency did not improve useful delivery under this tested workload.** C1 is faster for every Seed and has higher terminal Attempts/s, successful requests/s and persisted Assignments/s than both C4 and C16. Mean successful request delivery is 29.593 / 1.813 / 0.998 per second for C1/C4/C16; mean persisted Assignment delivery is 88.940 / 3.705 / 2.133 per second. C1 also has lower p50/p95/p99, a shorter measured interval and full participation among requesters.

C4 → C16 does increase terminal throughput (mean 7.252 → 15.874 Attempts/s) and shorten elapsed execution (1,012.897 → 461.485 seconds). At the same time, successful requests per run fall from 1,830 to 460–461 and persisted Assignments fall from 3,718–3,764 to 963–1,006. Higher terminal processing here does not represent higher useful delivery. C1's mean terminal throughput is 29.593 Attempts/s, with no failed-work contribution.

Observed conflict rates rise from 0% in C1 to 75% in C4 and approximately 93.7% in C16. Correctness is preserved while successful delivery and coverage deteriorate. These results answer the local operational question; they do not prescribe retry, locking, serialization, queueing, batching, isolation or Engine changes.

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

C1 has zero KnownConcurrencyConflict in every FestivalDay, baseline third and GroupSize bucket for all three Seeds; its segment counts/rates remain in the selected JSON. Its sequential intervals pass no-overlap checks and have no concurrent windows. Historical C4/C16 diagnostic, segment and window observations above are preserved unchanged.

## Participation and fairness population

| Seed | C | Registered | Requesting | Participating | Only rejected/failed | NoPersistedPath | InsufficientEvidence | Classifiable |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | 1 | 5500 | 5354 | 5354 | 0 | 0 | 232 | 5122 |
| 1001 | 4 | 5500 | 5354 | 2764 | 2590 | 2590 | 1930 | 834 |
| 1001 | 16 | 5500 | 5354 | 909 | 4445 | 4445 | 842 | 67 |
| 2001 | 1 | 5500 | 5359 | 5359 | 0 | 0 | 244 | 5115 |
| 2001 | 4 | 5500 | 5359 | 2793 | 2566 | 2566 | 2025 | 768 |
| 2001 | 16 | 5500 | 5359 | 899 | 4460 | 4460 | 837 | 62 |
| 3001 | 1 | 5500 | 5344 | 5344 | 0 | 0 | 218 | 5126 |
| 3001 | 4 | 5500 | 5344 | 2810 | 2534 | 2534 | 2021 | 789 |
| 3001 | 16 | 5500 | 5344 | 933 | 4411 | 4411 | 862 | 71 |

Requesting counts distinct identities over Days 1–5, not 4,400 daily requesters added together. Registered Attendees who never request are outside this evaluated fairness population. Only-rejected/failed includes requesters whose Attempts all conflict; it does not imply a persisted Failed status. It equals NoPersistedPath here because inputs are valid and all planned requests are attempted.

C1 serves every requester: NoPersistedPath and only-rejected/failed are zero for all three Seeds. It has 5,115–5,126 classifiable paths, compared with C4's 768–834 and C16's 62–71. C16 leaves 4,411–4,460 requesters with no persisted path. C1's 218–244 InsufficientEvidence paths have one durable participation, rather than no participation. These populations must accompany fairness rates.

## Secondary fairness outcome, path and recovery metrics

All values are consumed from stored metrics and reconciled against exported paths/episodes; the analyzer does not rerun the rubric or recovery analysis. Quality rates use persisted Assignment outcomes; path rates use Classifiable paths. C1 is the operational baseline, not a newly defined fairness scenario.

| Seed | C | Persisted outcomes | GoodOutcomeRate | MediumOutcomeRate | BadOutcomeRate |
| --- | --- | --- | --- | --- | --- |
| 1001 | 1 | 22000 | 39.709% | 51.050% | 9.241% |
| 1001 | 4 | 3764 | 16.870% | 83.130% | 0.000% |
| 1001 | 16 | 981 | 0.000% | 100.000% | 0.000% |
| 2001 | 1 | 22000 | 39.718% | 51.036% | 9.245% |
| 2001 | 4 | 3718 | 16.487% | 83.513% | 0.000% |
| 2001 | 16 | 963 | 0.000% | 100.000% | 0.000% |
| 3001 | 1 | 22000 | 39.691% | 51.009% | 9.300% |
| 3001 | 4 | 3735 | 16.573% | 83.427% | 0.000% |
| 3001 | 16 | 1006 | 0.000% | 100.000% | 0.000% |

| Seed | C | Classifiable | BalancedPathRate | AcceptablePathRate | NotBalancedPathRate | Eligible recovery episodes | RecoveryRate |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | 1 | 5122 | 56.677% | 32.194% | 11.128% | 1447 | 97.236% |
| 1001 | 4 | 834 | 17.986% | 82.014% | 0.000% | 0 | N/A |
| 1001 | 16 | 67 | 0.000% | 100.000% | 0.000% | 0 | N/A |
| 2001 | 1 | 5115 | 56.305% | 32.884% | 10.811% | 1449 | 96.825% |
| 2001 | 4 | 768 | 17.839% | 82.161% | 0.000% | 0 | N/A |
| 2001 | 16 | 62 | 0.000% | 100.000% | 0.000% | 0 | N/A |
| 3001 | 1 | 5126 | 56.379% | 32.111% | 11.510% | 1450 | 96.621% |
| 3001 | 4 | 789 | 16.223% | 83.777% | 0.000% | 0 | N/A |
| 3001 | 16 | 71 | 0.000% | 100.000% | 0.000% | 0 | N/A |

C1 has approximately 39.7% Good, 51.0% Medium and 9.2–9.3% Bad durable outcomes, with 56.3–56.7% Balanced and 10.8–11.5% NotBalanced among its much larger classifiable population. Its 1,900–1,908 recovery episodes include 1,447–1,450 eligible episodes; RecoveryRate is 96.621–97.236%. Existing S2 fairness limitations remain relevant; operationally complete delivery is not proof of uniformly balanced paths.

C4/C16 have zero Bad outcomes and no recovery episodes or eligible episodes, so their RecoveryRate remains **N/A**, not 0% or proof of superior recovery. C16's 100% AcceptablePathRate describes only 62–71 surviving classifiable paths and cannot establish better fairness for all requesters. C1's observed Bad/NotBalanced outcomes must not be compared with conflict-truncated histories as if the same population participated equally. Persisted histories evolve under each run's own successes; fairness policy itself is unchanged. Rejections, failures, conflicts and absence create no synthetic quality or recovery opportunity.

## Environment, reproducibility and interpretation limits

Captured execution environments match exactly across C1/C4/C16: Darwin 24.6.0 (Intel/x86_64), .NET runtime 8.0.3, host processor count 16, pinned postgres:16.3-alpine and runner-owned PostgreSQL via Docker/Testcontainers. Docker context remains desktop-linux / Docker Desktop 24.0.2, API 1.43, LinuxKit 5.15.49, eight Docker CPUs and 8,345,194,496 bytes (approximately 7.77 GiB) Docker memory. Build configuration is Debug. No material environment difference was found.

C4 then C16 ran serially in the initial process; C1 Seeds 1001 → 2001 → 3001 ran serially in a later process on the same local host/context, each with a fresh owned PostgreSQL container. There was no warm-up in either phase; first calls remain in samples. JIT/process/host warmness and the fixed historical configuration order are uncontrolled differences. The host was not reserved. Lightweight offline inspection/documentation work occurred during C1; full build/test suites completed before it. The initial phase likewise included lightweight offline validation and a few read-only progress queries. These are scenario observations, not a controlled steady-state benchmark.

Exact input reproducibility is verified by manifests, source hashes, retained-S2 equality, Seeds and topology. **One run per Seed/configuration does not quantify run-to-run scheduler/interleaving variability** for C4/C16, conflict identity stability or production probabilities. The three Seeds consistently show the same operational direction; their min/mean/max cannot be presented as concurrent repetition results. No historical concurrent timings were replaced or normalized.

Conclusions apply to this provisional S2 workload, database, runner and local environment. They do not establish production traffic, HTTP/network experience, distributed multi-node behavior, cloud scaling or all contention patterns. EC2 was neither required nor used. Stakeholder workload/topology assumptions remain provisional.

## Final S8 evidence decision and deferred work

**Chosen: A — S8 evidence sufficient.** The C1 comparison resolves the initial follow-up question: neither C4 nor C16 improves useful delivery, latency, measured execution duration or requester participation over the existing sequential path in any Seed. The large separation in useful delivery, zero technical failures and zero persisted findings provides a clear operational conclusion for the current Stage 5 evidence review. No important repetition-variability question remains that must be answered before making that scoped observation.

This does not claim concurrent outcomes/timings are reproducible or that all concurrency questions are settled. Scheduler variability remains unquantified and could matter in a future explicitly scoped experiment or mitigation evaluation. It does not require C4 or C16 repetition now. No repetitions were automatically scheduled or executed, and no mandatory blanket run total is introduced.

Next task: **Consolidate Stage 5 isolated-scenario evidence for Decision Gate review**, carrying forward the operational contention findings, existing S2 fairness limitations and input/environment caveats. S3/S4, stakeholder validation and the broader Stage 5 Decision Gate remain pending. No mitigation is selected or implemented here.

## Validation and scope protection

Final solution build and dedicated harness build passed with zero warnings/errors. The initial simultaneous builds collided on shared apphost output; sequential reruns passed before official execution. `DOCKER_API_VERSION=1.43 dotnet test Festival.sln` passed **967 .NET tests**, zero failures/skips, with Docker/Testcontainers available. The offline suite passed **81 Python tests**, including **17 focused S8 tests** (nine added in this follow-up) for useful delivery formulas, C1/C4/C16 pairing, aggregate min/mean/max, population comparison, serialization/replay, sequential no-overlap, existing rates, percentiles and checksum boundaries.

The complete selected JSON was independently regenerated and compared byte-for-byte. All 72 official package checksums passed before/after analysis; retained smoke and historical S2 package checksums passed too. Original C4/C16 selected observations and pre-existing aggregates were verified unchanged. `git diff --check` passed. The ledgers fingerprint unchanged execution sources throughout all official runs.

Changes are limited to task-specific execution/auditing, offline analyzer/tests, selected summaries and documentation status. No production Domain/Application/Infrastructure/Simulation behavior, AssignmentEngine, fairness/rubric/recovery/metric policy, S2 workload, scheduling/windows, retries, locking, isolation, serialization/queueing/batching mitigation, S8 matrix, S3/S4 or Decision Gate logic changed. Exactly three official C1 runs were added; no C4/C16 run or additional repetition/level was executed. No branch, commit or push was created.
