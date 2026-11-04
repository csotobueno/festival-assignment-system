# First official Stage 5 S1/S2 representative results

## Protocol and provenance

Execution date: **2026-11-03 (America/Lima)**. Six official runs, in order: S1/1001, S1/2001, S1/3001, S2/1001, S2/2001, S2/3001. The same seeds are used across scenarios. A seventh S1/1001 run verifies reproduction and is excluded from official aggregates.

All use the unchanged definitions, Generator `v2`, Engine `v0.3.0`, Rubric `v1`, Sequential execution and ConcurrencyLevel 1. Each request uses the existing real Application/Infrastructure path and its own DI scope/DbContext. Each run owns a fresh `postgres:16.3-alpine` Testcontainers database; history is retained across its five FestivalDays.

Repository SHA explicitly supplied: `ccc49c21230a315d9fd7544a10049bebdd6dec16`. The executed engine, definitions, rubric and aggregate code match this revision; the new task-specific orchestration/analysis scripts and this report are uncommitted working-tree additions. ScenarioDefinitionVersion and MetricsFrameworkVersion remain **null** because no explicit code versions exist.

Simulated FirstDate: **2026-08-15**, matching the existing test fixture; not a confirmed festival date. All five days have exactly 3,300 requesting attendees for S1 or 4,400 for S2. The shared FrontStandingAllowedRequestShare remains 50% of requests. No parameters were tuned.

Commands and audit/comparison details: [task harness](../../scripts/stage-5-representative-runs/README.md). Complete numeric summary and file checksums: [structured results](s1-s2-representative-results.json).

## Execution matrix and retained evidence

| Run | RunId | Status | Evidence path |
| --- | --- | --- | --- |
| S1/1001 | `official-s1-1001-c57af02f2d8449889958e2857dfecdaf` | Completed | `artifacts/simulations/s1-best-plausible/official-s1-1001-c57af02f2d8449889958e2857dfecdaf` |
| S1/2001 | `official-s1-2001-7242d2fa08af4ea2aab8fb65970b379d` | Completed | `artifacts/simulations/s1-best-plausible/official-s1-2001-7242d2fa08af4ea2aab8fb65970b379d` |
| S1/3001 | `official-s1-3001-4fba71856d294ac5abd4cbf15672b2a6` | Completed | `artifacts/simulations/s1-best-plausible/official-s1-3001-4fba71856d294ac5abd4cbf15672b2a6` |
| S2/1001 | `official-s2-1001-b6c13b403b654deabc2a23302c2a7b5a` | Completed | `artifacts/simulations/s2-expected-baseline/official-s2-1001-b6c13b403b654deabc2a23302c2a7b5a` |
| S2/2001 | `official-s2-2001-38de76b7ac10424295d07ff3272f8333` | Completed | `artifacts/simulations/s2-expected-baseline/official-s2-2001-38de76b7ac10424295d07ff3272f8333` |
| S2/3001 | `official-s2-3001-50a58cfdae484c5d99139d63b0176545` | Completed | `artifacts/simulations/s2-expected-baseline/official-s2-3001-50a58cfdae484c5d99139d63b0176545` |

All eight JSON files per package parsed successfully and matched the SHA-256 recorded in the set index. All six official packages remain intact locally under ignored `artifacts/simulations/`; no raw evidence was promoted or committed. There is no required promotion mechanism. The structured report retains exact paths and checksums for later Decision Gate use. Local retention does not imply off-machine archival.

Set index: `artifacts/simulations/representative-20261103-8bb35534638b466d884ab35ed9aea61b/index.json`.

## Run integrity

| Run | Days | Technical failures | Conflicts | Final-state invariant findings | Missing files |
| --- | --- | --- | --- | --- | --- |
| S1/1001 | 5 | 0 | 0 | 0 | 0 |
| S1/2001 | 5 | 0 | 0 | 0 | 0 |
| S1/3001 | 5 | 0 | 0 | 0 | 0 |
| S2/1001 | 5 | 0 | 0 | 0 | 0 |
| S2/2001 | 5 | 0 | 0 | 0 | 0 |
| S2/3001 | 5 | 0 | 0 | 0 | 0 |

The audit checked final persisted Spot/day and Attendee/day uniqueness, complete groups, single Zone, same-row consecutive Spots, catalog identity and request outcome consistency. The runner also reconciled each returned outcome with durable state. This is **not a complete temporal audit of INV-06**; no replacement/cancellation operation was introduced or executed. Raw audited counts and checksums are in the structured report.

## Observed request outcomes

Success/rejection denominators: NormallyProcessedRequests = Successful + DomainRejected. Conflict/technical-error denominators: AttemptedRequests. ExcludedInvalidInput is reported separately. Rates below use those existing framework definitions.

| Run | Attempted | Successful | Rejected | Conflicts | Technical failures | Excluded | Success rate | Rejection rate |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| S1/1001 | 6665 | 6665 | 0 | 0 | 0 | 0 | 100.000% | 0.000% |
| S1/2001 | 6665 | 6665 | 0 | 0 | 0 | 0 | 100.000% | 0.000% |
| S1/3001 | 6665 | 6665 | 0 | 0 | 0 | 0 | 100.000% | 0.000% |
| S2/1001 | 7320 | 7320 | 0 | 0 | 0 | 0 | 100.000% | 0.000% |
| S2/2001 | 7320 | 7320 | 0 | 0 | 0 | 0 | 100.000% | 0.000% |
| S2/3001 | 7320 | 7320 | 0 | 0 | 0 | 0 | 100.000% | 0.000% |

ConcurrencyConflictRate and TechnicalErrorRate are 0% for every run.

## Observed fairness and quality

Values are copied from the existing aggregate metrics, with no reclassification. Coverage rates use EvaluatedAttendeeCount; Balanced/Acceptable/NotBalanced rates use ClassifiablePathCount. Each cell with parentheses shows count (rate).

| Run | Evaluated | No persisted path | Insufficient evidence | Classifiable | Balanced | Acceptable | Not balanced |
| --- | --- | --- | --- | --- | --- | --- | --- |
| S1/1001 | 4353 | 0 (0.000%) | 476 (10.935%) | 3877 (89.065%) | 2547 (65.695%) | 1330 (34.305%) | 0 (0.000%) |
| S1/2001 | 4346 | 0 (0.000%) | 493 (11.344%) | 3853 (88.656%) | 2529 (65.637%) | 1324 (34.363%) | 0 (0.000%) |
| S1/3001 | 4328 | 0 (0.000%) | 457 (10.559%) | 3871 (89.441%) | 2561 (66.159%) | 1310 (33.841%) | 0 (0.000%) |
| S2/1001 | 5354 | 0 (0.000%) | 232 (4.333%) | 5122 (95.667%) | 2903 (56.677%) | 1649 (32.194%) | 570 (11.128%) |
| S2/2001 | 5359 | 0 (0.000%) | 244 (4.553%) | 5115 (95.447%) | 2880 (56.305%) | 1682 (32.884%) | 553 (10.811%) |
| S2/3001 | 5344 | 0 (0.000%) | 218 (4.079%) | 5126 (95.921%) | 2890 (56.379%) | 1646 (32.111%) | 590 (11.510%) |

| Run | Persisted outcomes | Good | Medium | Bad |
| --- | --- | --- | --- | --- |
| S1/1001 | 16500 | 7004 (42.448%) | 9496 (57.552%) | 0 (0.000%) |
| S1/2001 | 16500 | 7003 (42.442%) | 9497 (57.558%) | 0 (0.000%) |
| S1/3001 | 16500 | 7004 (42.448%) | 9496 (57.552%) | 0 (0.000%) |
| S2/1001 | 22000 | 8736 (39.709%) | 11231 (51.050%) | 2033 (9.241%) |
| S2/2001 | 22000 | 8738 (39.718%) | 11228 (51.036%) | 2034 (9.245%) |
| S2/3001 | 22000 | 8732 (39.691%) | 11222 (51.009%) | 2046 (9.300%) |

Outcome quality rates use PersistedOutcomeCount, not requests or distinct attendees.

## Observed recovery

RecoveryRate = RecoveredEpisodeCount / RecoveryEligibleEpisodeCount. Speed summarizes recovered episodes only and measures later persisted opportunities, not calendar days. N/A means no applicable denominator.

| Run | Episodes | Recovered | Unresolved | Eligible | Ineligible | Eligible unresolved | To Medium | To Good |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| S1/1001 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| S1/2001 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| S1/3001 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| S2/1001 | 1908 | 1407 | 501 | 1447 | 461 | 40 | 687 | 720 |
| S2/2001 | 1900 | 1403 | 497 | 1449 | 451 | 46 | 714 | 689 |
| S2/3001 | 1900 | 1401 | 499 | 1450 | 450 | 49 | 685 | 716 |

| Run | Recovery rate | Average opportunities | Median opportunities |
| --- | --- | --- | --- |
| S1/1001 | N/A | N/A | N/A |
| S1/2001 | N/A | N/A | N/A |
| S1/3001 | N/A | N/A | N/A |
| S2/1001 | 97.236% | 1.056 | 1.000 |
| S2/2001 | 96.825% | 1.061 | 1.000 |
| S2/3001 | 96.621% | 1.066 | 1.000 |

Ineligible unresolved episodes have no later persisted opportunity and are excluded from RecoveryRate. They are retained as unresolved observations, not failed recoveries.

## Daily capacity and feasibility

End-of-day global capacity from the existing checkpoints. These are residual counts, not guarantees of a contiguous block or evidence that capacity was exhausted early.

| Run | Day | Requests | Successful | Rejected | Good left | Medium left | Bad left | Total left |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| S1/1001 | 1 | 1333 | 1333 | 0 | 351 | 351 | 996 | 1698 |
| S1/1001 | 2 | 1333 | 1333 | 0 | 351 | 351 | 996 | 1698 |
| S1/1001 | 3 | 1333 | 1333 | 0 | 351 | 351 | 996 | 1698 |
| S1/1001 | 4 | 1333 | 1333 | 0 | 352 | 350 | 996 | 1698 |
| S1/1001 | 5 | 1333 | 1333 | 0 | 351 | 351 | 996 | 1698 |
| S1/2001 | 1 | 1333 | 1333 | 0 | 351 | 351 | 996 | 1698 |
| S1/2001 | 2 | 1333 | 1333 | 0 | 352 | 350 | 996 | 1698 |
| S1/2001 | 3 | 1333 | 1333 | 0 | 350 | 352 | 996 | 1698 |
| S1/2001 | 4 | 1333 | 1333 | 0 | 352 | 350 | 996 | 1698 |
| S1/2001 | 5 | 1333 | 1333 | 0 | 352 | 350 | 996 | 1698 |
| S1/3001 | 1 | 1333 | 1333 | 0 | 353 | 349 | 996 | 1698 |
| S1/3001 | 2 | 1333 | 1333 | 0 | 351 | 351 | 996 | 1698 |
| S1/3001 | 3 | 1333 | 1333 | 0 | 350 | 352 | 996 | 1698 |
| S1/3001 | 4 | 1333 | 1333 | 0 | 352 | 350 | 996 | 1698 |
| S1/3001 | 5 | 1333 | 1333 | 0 | 350 | 352 | 996 | 1698 |
| S2/1001 | 1 | 1464 | 1464 | 0 | 7 | 7 | 584 | 598 |
| S2/1001 | 2 | 1464 | 1464 | 0 | 4 | 0 | 594 | 598 |
| S2/1001 | 3 | 1464 | 1464 | 0 | 1 | 2 | 595 | 598 |
| S2/1001 | 4 | 1464 | 1464 | 0 | 8 | 7 | 583 | 598 |
| S2/1001 | 5 | 1464 | 1464 | 0 | 4 | 3 | 591 | 598 |
| S2/2001 | 1 | 1464 | 1464 | 0 | 5 | 4 | 589 | 598 |
| S2/2001 | 2 | 1464 | 1464 | 0 | 2 | 0 | 596 | 598 |
| S2/2001 | 3 | 1464 | 1464 | 0 | 4 | 4 | 590 | 598 |
| S2/2001 | 4 | 1464 | 1464 | 0 | 6 | 6 | 586 | 598 |
| S2/2001 | 5 | 1464 | 1464 | 0 | 5 | 8 | 585 | 598 |
| S2/3001 | 1 | 1464 | 1464 | 0 | 1 | 1 | 596 | 598 |
| S2/3001 | 2 | 1464 | 1464 | 0 | 8 | 8 | 582 | 598 |
| S2/3001 | 3 | 1464 | 1464 | 0 | 8 | 8 | 582 | 598 |
| S2/3001 | 4 | 1464 | 1464 | 0 | 7 | 7 | 584 | 598 |
| S2/3001 | 5 | 1464 | 1464 | 0 | 4 | 4 | 590 | 598 |

## Rejections and fragmentation signals

Existing NO_CONTIGUOUS_SPOTS_AVAILABLE diagnostics are separated using the captured evaluation input: eligible raw capacity >= GroupSize plus block-found=false supports a fragmentation/physical-feasibility limitation; capacity < GroupSize supports eligible-capacity limitation. Neither establishes a production defect or single-variable cause. End-of-day remaining venue capacity alone is insufficient.

| Run | Diagnostic counts | No block, sufficient eligible capacity | No block, insufficient eligible capacity | Unavailable diagnosis |
| --- | --- | --- | --- | --- |
| S1/1001 | {} | 0 | 0 | 0 |
| S1/2001 | {} | 0 | 0 | 0 |
| S1/3001 | {} | 0 | 0 | 0 |
| S2/1001 | {} | 0 | 0 | 0 |
| S2/2001 | {} | 0 | 0 | 0 |
| S2/3001 | {} | 0 | 0 | 0 |

Daily rejection counts and reasons are preserved in the structured report. The baseline has no transactionally consistent pre-save snapshot guarantee; this sequential set introduces no contention. The aggregate no-block code alone cannot identify which workload dimension contributed.

## Cross-seed stability

Cells show minimum / unweighted mean / maximum across the three official runs. Percentages are displayed to three decimals. N/A runs are excluded only for applicable nullable metrics; their counts are shown explicitly.

| Metric | S1 min / mean / max | S2 min / mean / max |
| --- | --- | --- |
| successRate | 100.000% / 100.000% / 100.000%; N/A=0 | 100.000% / 100.000% / 100.000%; N/A=0 |
| rejectionRate | 0.000% / 0.000% / 0.000%; N/A=0 | 0.000% / 0.000% / 0.000%; N/A=0 |
| goodOutcomeRate | 42.442% / 42.446% / 42.448%; N/A=0 | 39.691% / 39.706% / 39.718%; N/A=0 |
| mediumOutcomeRate | 57.552% / 57.554% / 57.558%; N/A=0 | 51.009% / 51.032% / 51.050%; N/A=0 |
| badOutcomeRate | 0.000% / 0.000% / 0.000%; N/A=0 | 9.241% / 9.262% / 9.300%; N/A=0 |
| balancedPathRate | 65.637% / 65.830% / 66.159%; N/A=0 | 56.305% / 56.454% / 56.677%; N/A=0 |
| acceptablePathRate | 33.841% / 34.170% / 34.363%; N/A=0 | 32.111% / 32.396% / 32.884%; N/A=0 |
| notBalancedPathRate | 0.000% / 0.000% / 0.000%; N/A=0 | 10.811% / 11.150% / 11.510%; N/A=0 |
| recoveryRate | N/A / N/A / N/A; N/A=3 | 96.621% / 96.894% / 97.236%; N/A=0 |
| averageOpportunitiesToRecovery | N/A / N/A / N/A; N/A=3 | 1.056 / 1.061 / 1.066; N/A=0 |
| medianOpportunitiesToRecovery | N/A / N/A / N/A; N/A=3 | 1.000 / 1.000 / 1.000; N/A=0 |

## Reproducibility

Reference: `official-s1-1001-c57af02f2d8449889958e2857dfecdaf`. Separate verification run: `reproduction-s1-1001-a85b41b384294ef79ff25eb3ab8014f3` at `artifacts/simulations/s1-best-plausible/reproduction-s1-1001-a85b41b384294ef79ff25eb3ab8014f3`.

| Logical output | Equivalent |
| --- | --- |
| manifest | True |
| attempts | True |
| attendee-paths | True |
| recovery-observations | True |
| metrics | True |

The definition, manifest and metrics match byte for byte. Observed times/durations are excluded from attempt comparison. Generated persisted request/Assignment IDs are matched by stable simulation-request and attendee/day identities. All remaining attempt outcome/diagnostic/capacity fields, path classifications, recovery observations and metrics are compared. Canonical logical SHA-256 fingerprints are retained in the structured report.

## Exploratory performance observation

Local Debug build, .NET SDK 8.0.203, Docker Desktop 24.0.2 / API 1.43, local PostgreSQL container. No dedicated benchmark environment, warm-up phase or performance optimization. Nearest-rank percentiles use all AttemptedRequests and the existing monotonic use-case durations; reconciliation/setup/export are excluded.

| Run | Samples | p50 ms | p95 ms | Mean ms | Max ms |
| --- | --- | --- | --- | --- | --- |
| S1/1001 | 6665 | 29.817 | 46.058 | 31.433 | 526.847 |
| S1/2001 | 6665 | 30.380 | 47.534 | 32.113 | 209.178 |
| S1/3001 | 6665 | 29.537 | 44.276 | 30.896 | 150.211 |
| S2/1001 | 7320 | 26.723 | 44.737 | 28.294 | 99.382 |
| S2/2001 | 7320 | 27.194 | 44.616 | 28.524 | 212.131 |
| S2/3001 | 7320 | 26.312 | 42.759 | 27.492 | 148.467 |

The p95 < 1 second value remains an initial engineering hypothesis, not a stakeholder-approved production threshold or a Decision Gate verdict.

## Interpretation and later hypotheses

**Observed evidence.** All 41,955 official attempts were Successful, with no
rejections, conflicts, unexpected technical failures or final-state invariant
findings. The six runs persisted 115,500 individual outcomes. Every evaluated
attendee has a persisted path; some have only one participation and remain
InsufficientEvidence under the unchanged rubric.

S1's three seeds produced no Bad outcomes, NotBalanced paths or recovery
episodes. Its recovery rate and recovery distances are N/A in all three runs;
this set cannot evaluate recovery under S1. S2 produced 9.241–9.300% Bad outcomes
and 10.811–11.510% NotBalanced paths. S2 recovery was 96.621–97.236% among
eligible episodes, with 1.056–1.066 average opportunities and median 1.
Eligible unresolved episode counts were 40, 46 and 49; the separate 461, 451
and 450 ineligible episodes had no later observed opportunity.

Daily total residual capacity was exactly 1,698 for every S1 day and 598 for
every S2 day. S1 retained all 996 Bad Spots, with 350–353 Good and 349–352 Medium
Spots left. S2 closed with 1–8 Good, 0–8 Medium and 582–596 Bad Spots. These
checkpoints show pressure on Good/Medium capacity at closing; they do not
establish early exhaustion or a fragmentation cause. There were no rejected
requests, diagnostic rejection reasons or rejection-based fragmentation signals.

**Cross-seed interpretation.** Request outcome rates are identical across all
seeds. Quality rates vary in narrow observed ranges: S1 Good spans 0.006
percentage points; S2 Good spans 0.027 and Bad spans 0.059. Path classification
and recovery vary more: Balanced spans 0.521 points for S1 and 0.372 for S2;
S2 NotBalanced spans 0.699 and RecoveryRate spans 0.615. These describe the three
observed seeds only; no formal stability threshold or statistical inference is
introduced.

**S1 vs S2 interpretation.** Under the combined S2 baseline workload, request
feasibility remained at 100% while outcome quality and path classifications
were less favorable than S1: mean Good 39.706% vs 42.446%, mean Balanced 56.454%
vs 65.830%, and mean NotBalanced 11.150% vs 0%. High eligible recovery does not
erase the observed Bad exposure or NotBalanced paths. Recovery cannot be
compared numerically against S1's N/A. No Decision Gate verdict follows here.

**Hypotheses and deferred work.** Daily demand, group-size profile and arrival
concentration change together; their interactions may contribute to these
differences. This set does not identify one variable as the cause. Later S5/S6
work can isolate arrival order and group size, and targeted S7 evidence can
exercise fragmentation because these runs had no no-block rejections.
Inspect the retained S2 NotBalanced paths and eligible unresolved episodes in
later analysis before proposing any policy change. A later workload with Bad
exposure is needed to assess recovery where S1 supplies no eligible episodes.
No such scenarios, tuning or production changes are implemented in this task.

## Validation and scope

`dotnet build Festival.sln`: passed, 0 warnings/errors. `DOCKER_API_VERSION=1.43 dotnet test Festival.sln`: 883 passed, 0 failed/skipped. The standalone harness build passed with 0 warnings/errors. Three Python regression tests pass; they protect the task-specific comparison against the index/exporter enum/timestamp serialization difference and reject mismatched seed/mode. `git diff --check`: passed.

No Engine, Domain, Application assignment behavior, PostgreSQL repository semantics, scenario parameters, fairness thresholds, PathClassificationRubric, recovery semantics or aggregate formulas changed. No S3–S8 implementation, concurrent execution, tuning, optimization, dashboard or Decision Gate verdict. Raw evidence, summary and interpretation remain separate. No Git commit, push or branch change was performed.
