# S2/S5 processing-order sensitivity results

Execution and analysis date: **2026-11-05 (America/Lima)**. Run identifiers and observed runtime timestamps use UTC (the set directory therefore begins `20261106`). The simulated first FestivalDay remains **2026-08-15**, a fixture date rather than a confirmed festival date.

## Summary

All three official S5 runs and the subsequent S5/1001 reproduction completed. Engine v1 has strong sensitivity to processing order at the identity and execution-position levels, while aggregate quality rates change much less. Only 40.52%–41.26% of paired attendee/day qualities remain unchanged. The controlled reversal moves Bad outcomes from the final baseline third to the first baseline third; they remain in the final **actual execution** third. This is evidence about these exact workloads and this reversal, without a Decision Gate verdict.

## Execution matrix

| Scenario | Seed | RunId | Status | Exact retained evidence path |
| --- | --- | --- | --- | --- |
| S2 | 1001 | `official-s2-1001-b6c13b403b654deabc2a23302c2a7b5a` | Completed | `artifacts/simulations/s2-expected-baseline/official-s2-1001-b6c13b403b654deabc2a23302c2a7b5a/` |
| S5 | 1001 | `official-s5-1001-c8d991b9e3e140f899cc06c70187aab7` | Completed | `artifacts/simulations/s5-adverse-arrival-order/official-s5-1001-c8d991b9e3e140f899cc06c70187aab7/` |
| S2 | 2001 | `official-s2-2001-38de76b7ac10424295d07ff3272f8333` | Completed | `artifacts/simulations/s2-expected-baseline/official-s2-2001-38de76b7ac10424295d07ff3272f8333/` |
| S5 | 2001 | `official-s5-2001-59f9c801e6284185a2832a2cb47fce60` | Completed | `artifacts/simulations/s5-adverse-arrival-order/official-s5-2001-59f9c801e6284185a2832a2cb47fce60/` |
| S2 | 3001 | `official-s2-3001-50a58cfdae484c5d99139d63b0176545` | Completed | `artifacts/simulations/s2-expected-baseline/official-s2-3001-50a58cfdae484c5d99139d63b0176545/` |
| S5 | 3001 | `official-s5-3001-a65fa7f58d1042728b9e0d219b7dd8b3` | Completed | `artifacts/simulations/s5-adverse-arrival-order/official-s5-3001-a65fa7f58d1042728b9e0d219b7dd8b3/` |
| S5 reproduction | 1001 | `reproduction-s5-1001-fccb5df0a34949f68145d640f49b1736` | Completed | `artifacts/simulations/s5-adverse-arrival-order/reproduction-s5-1001-fccb5df0a34949f68145d640f49b1736/` |

S5 set index: `artifacts/simulations/s5-order-sensitivity-20261106-8ae89818c5d74a23b9756f324893d128/index.json`. S2 rows are existing controls; no S2 run was executed for this task.

## Pair integrity and sequence semantics

| Seed | Exact workload / requests | Days / daily cohorts / groups | RequestedAt / AssignedAt | RequestEligibility | Topology / Spot identities | BaselineSequence preserved | Execution exact daily reverse |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | Pass | Pass | Pass | Pass | Pass | Pass | Pass |
| 2001 | Pass | Pass | Pass | Pass | Pass | Pass | Pass |
| 3001 | Pass | Pass | Pass | Pass | Pass | Pass | Pass |

Before execution, the harness verified all eight hashes per retained S2 package and compared the S2 definition/materialized manifest structurally to the retained control. Only absent baseline-default additive provenance and ExecutionSequence fields were omitted for that comparison. S5 consumes those verified in-memory S2 manifests through the unchanged S5ReversedProcessingOrder transformation; it does not generate a separate S5 workload. Analysis independently compares every input field and exact daily reversed dispatch, before joining outcomes.

**BaselineSequence** is original S2 position; manifest `Sequence` retains it. **ExecutionSequence / AttemptSequence** is actual dispatch order. Both are global slots; thirds use independent daily ranks. Original RequestedAt and AssignedAt are unchanged and do not represent S5 processing chronology. Actual persisted timestamps are checked at PostgreSQL microsecond precision. FestivalDay progression and prior-day history remain natural within each run.

## Run integrity

| S5 seed / role | Completed days | Requesters/day | Generator | Engine | Mode / level | Technical failures | Conflicts | Final-state findings | Missing files |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | 5 | 4400 | v2 | v0.3.0 | Sequential / 1 | 0 | 0 | 0 | 0 |
| 2001 | 5 | 4400 | v2 | v0.3.0 | Sequential / 1 | 0 | 0 | 0 | 0 |
| 3001 | 5 | 4400 | v2 | v0.3.0 | Sequential / 1 | 0 | 0 | 0 | 0 |
| 1001 reproduction | 5 | 4400 | v2 | v0.3.0 | Sequential / 1 | 0 | 0 | 0 | 0 |

The persisted-state audit checks unique Spot/day and Attendee/day, complete contiguous single-Zone group results, catalog identity, terminal request consistency, dispatch and checkpoint reconciliation. It is a final-state audit, not a complete temporal proof of INV-06. The unchanged Application flow does not replace or cancel Assignments. Technical findings are counted separately from business outcomes. An initial sandbox launcher could not access Docker and stopped before assignment attempts; the authorized official set above completed normally.

## Aggregate metrics

Request success/rejection use NormallyProcessedRequests = Successful + DomainRejected. Conflict/technical rates use AttemptedRequests. Quality rates use persisted evaluated Assignments. Path rates use ClassifiablePathCount; ClassifiablePathRate uses EvaluatedAttendeeCount. RecoveryRate uses eligible episodes; average/median recovery opportunities use recovered episodes. Null remains N/A. All deltas are **S5 minus S2**; rate deltas are percentage points (pp). Existing formulas and stored fairness/recovery outputs are unchanged.

| Seed | Scenario | Attempted | Successful | DomainRejected | Conflict | Technical | SuccessRate | RejectionRate | ConflictRate | TechnicalRate |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | S2 | 7320 | 7320 | 0 | 0 | 0 | 100.000% | 0.000% | 0.000% | 0.000% |
| 1001 | S5 | 7320 | 7320 | 0 | 0 | 0 | 100.000% | 0.000% | 0.000% | 0.000% |
| 2001 | S2 | 7320 | 7320 | 0 | 0 | 0 | 100.000% | 0.000% | 0.000% | 0.000% |
| 2001 | S5 | 7320 | 7320 | 0 | 0 | 0 | 100.000% | 0.000% | 0.000% | 0.000% |
| 3001 | S2 | 7320 | 7320 | 0 | 0 | 0 | 100.000% | 0.000% | 0.000% | 0.000% |
| 3001 | S5 | 7320 | 7320 | 0 | 0 | 0 | 100.000% | 0.000% | 0.000% | 0.000% |

All request-count and request-rate paired deltas are zero. ExcludedInvalidInput = 0 in all six paired packages.

| Seed | Scenario | Evaluated | Classifiable | Insufficient | Persisted outcomes | Good | Medium | Bad | Balanced | Acceptable | NotBalanced |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | S2 | 5354 | 5122 | 232 | 22000 | 8736 | 11231 | 2033 | 2903 | 1649 | 570 |
| 1001 | S5 | 5354 | 5122 | 232 | 22000 | 8731 | 11223 | 2046 | 2896 | 1653 | 573 |
| 2001 | S2 | 5359 | 5115 | 244 | 22000 | 8738 | 11228 | 2034 | 2880 | 1682 | 553 |
| 2001 | S5 | 5359 | 5115 | 244 | 22000 | 8751 | 11245 | 2004 | 2920 | 1666 | 529 |
| 3001 | S2 | 5344 | 5126 | 218 | 22000 | 8732 | 11222 | 2046 | 2890 | 1646 | 590 |
| 3001 | S5 | 5344 | 5126 | 218 | 22000 | 8739 | 11232 | 2029 | 2880 | 1672 | 574 |

| Seed | Quality / path / coverage metric | S2 | S5 | Delta |
| --- | --- | --- | --- | --- |
| 1001 | goodOutcomeRate | 39.709% | 39.686% | -0.023 pp |
| 1001 | mediumOutcomeRate | 51.050% | 51.014% | -0.036 pp |
| 1001 | badOutcomeRate | 9.241% | 9.300% | +0.059 pp |
| 1001 | balancedPathRate | 56.677% | 56.540% | -0.137 pp |
| 1001 | acceptablePathRate | 32.194% | 32.273% | +0.078 pp |
| 1001 | notBalancedPathRate | 11.128% | 11.187% | +0.059 pp |
| 1001 | classifiablePathRate | 95.667% | 95.667% | +0.000 pp |
| 2001 | goodOutcomeRate | 39.718% | 39.777% | +0.059 pp |
| 2001 | mediumOutcomeRate | 51.036% | 51.114% | +0.077 pp |
| 2001 | badOutcomeRate | 9.245% | 9.109% | -0.136 pp |
| 2001 | balancedPathRate | 56.305% | 57.087% | +0.782 pp |
| 2001 | acceptablePathRate | 32.884% | 32.571% | -0.313 pp |
| 2001 | notBalancedPathRate | 10.811% | 10.342% | -0.469 pp |
| 2001 | classifiablePathRate | 95.447% | 95.447% | +0.000 pp |
| 3001 | goodOutcomeRate | 39.691% | 39.723% | +0.032 pp |
| 3001 | mediumOutcomeRate | 51.009% | 51.055% | +0.045 pp |
| 3001 | badOutcomeRate | 9.300% | 9.223% | -0.077 pp |
| 3001 | balancedPathRate | 56.379% | 56.184% | -0.195 pp |
| 3001 | acceptablePathRate | 32.111% | 32.618% | +0.507 pp |
| 3001 | notBalancedPathRate | 11.510% | 11.198% | -0.312 pp |
| 3001 | classifiablePathRate | 95.921% | 95.921% | +0.000 pp |

| Seed | Recovery metric | S2 | S5 | Delta |
| --- | --- | --- | --- | --- |
| 1001 | recoveryEpisodeCount | 1,908 | 1,888 | -20 |
| 1001 | recoveredEpisodeCount | 1,407 | 1,386 | -21 |
| 1001 | unresolvedEpisodeCount | 501 | 502 | +1 |
| 1001 | recoveryEligibleEpisodeCount | 1,447 | 1,440 | -7 |
| 1001 | recoveryRate | 97.236% | 96.250% | -0.986 pp |
| 1001 | averageOpportunitiesToRecovery | 1.056148 | 1.070707 | +0.014559 |
| 1001 | medianOpportunitiesToRecovery | 1 | 1 | +0 |
| 2001 | recoveryEpisodeCount | 1,900 | 1,852 | -48 |
| 2001 | recoveredEpisodeCount | 1,403 | 1,374 | -29 |
| 2001 | unresolvedEpisodeCount | 497 | 478 | -19 |
| 2001 | recoveryEligibleEpisodeCount | 1,449 | 1,415 | -34 |
| 2001 | recoveryRate | 96.825% | 97.102% | +0.277 pp |
| 2001 | averageOpportunitiesToRecovery | 1.061297 | 1.078603 | +0.017305 |
| 2001 | medianOpportunitiesToRecovery | 1 | 1 | +0 |
| 3001 | recoveryEpisodeCount | 1,900 | 1,867 | -33 |
| 3001 | recoveredEpisodeCount | 1,401 | 1,385 | -16 |
| 3001 | unresolvedEpisodeCount | 499 | 482 | -17 |
| 3001 | recoveryEligibleEpisodeCount | 1,450 | 1,433 | -17 |
| 3001 | recoveryRate | 96.621% | 96.650% | +0.030 pp |
| 3001 | averageOpportunitiesToRecovery | 1.066381 | 1.080144 | +0.013763 |
| 3001 | medianOpportunitiesToRecovery | 1 | 1 | +0 |

## Attendee/day quality transition matrices

Rows are S2 quality; columns are S5 quality. G = Good, M = Medium, B = Bad. Each seed has 22,000 stable AttendeeId + FestivalDay pairs with an Assignment in both runs. Pooled counts sum three separate seed experiments (66,000 paired participations); they do not identify a single pooled festival population.

| Seed | S2 → S5 | G | M | B |
| --- | --- | --- | --- | --- |
| 1001 | Good | 4151 | 4513 | 72 |
| 1001 | Medium | 4494 | 4763 | 1974 |
| 1001 | Bad | 86 | 1947 | 0 |
| 2001 | Good | 4234 | 4438 | 66 |
| 2001 | Medium | 4446 | 4844 | 1938 |
| 2001 | Bad | 71 | 1963 | 0 |
| 3001 | Good | 4234 | 4421 | 77 |
| 3001 | Medium | 4430 | 4840 | 1952 |
| 3001 | Bad | 75 | 1971 | 0 |
| Pooled | Good | 12619 | 13372 | 215 |
| Pooled | Medium | 13370 | 14447 | 5864 |
| Pooled | Bad | 232 | 5881 | 0 |

## Improved / worsened / unchanged and missing Assignments

Ordering: Good > Medium > Bad. Rates use assigned-in-both attendee/day pairs; no severity weights are introduced.

| Seed | Unchanged | Improved | Worsened | Assigned both | Only S2 | Only S5 | Neither |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | 8,914/22,000 (40.52%) | 6,527/22,000 (29.67%) | 6,559/22,000 (29.81%) | 22000 | 0 | 0 | 0 |
| 2001 | 9,078/22,000 (41.26%) | 6,480/22,000 (29.45%) | 6,442/22,000 (29.28%) | 22000 | 0 | 0 | 0 |
| 3001 | 9,074/22,000 (41.25%) | 6,476/22,000 (29.44%) | 6,450/22,000 (29.32%) | 22000 | 0 | 0 | 0 |

Both runs retain 100% request success. No missing outcome is forced into G/M/B or excluded silently.

## Bad recipient sensitivity

Unique AttendeeIds are compared across the whole five-day run. Overlap = intersection/union; churn = symmetric difference/union. Neither rate is a fairness threshold. S2 retention = intersection/S2 recipients; S5 new share = S5-only/S5 recipients.

| Seed | S2 | S5 | Both | S2 only | S5 only | Union | Overlap | Churn | S2 retention | S5 new share |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | 1730 | 1722 | 451 | 1279 | 1271 | 3001 | 15.028% | 84.972% | 26.069% | 73.810% |
| 2001 | 1747 | 1710 | 503 | 1244 | 1207 | 2954 | 17.028% | 82.972% | 28.792% | 70.585% |
| 3001 | 1745 | 1738 | 512 | 1233 | 1226 | 2971 | 17.233% | 82.767% | 29.341% | 70.541% |

These sets count attendees with **any** Bad across five days; an attendee in both sets may receive Bad on different days. The stricter attendee/day intersection is zero in every seed: no original S2 Bad participation remains Bad in S5. This explains why person-level overlap can be nonzero while B → B matrix cells are zero.

## Execution position and original arrival bands

Daily thirds reuse the S2 definition `floor((dailyRank - 1) * 3 / dailyRequestCount)`: first/middle/last, using half-open rank intervals. Each third contains 2,440 requests per run; group sizes make the attendee participation denominators differ. Cells below are **Bad persisted participations / persisted participations in that segment (rate)**.

| Seed | Daily third | S2 baseline = execution | S5 baseline position | S5 actual execution position |
| --- | --- | --- | --- | --- |
| 1001 | first | 0/7,384 (0.00%) | 2,046/7,384 (27.71%) | 0/7,327 (0.00%) |
| 1001 | middle | 0/7,289 (0.00%) | 0/7,289 (0.00%) | 0/7,289 (0.00%) |
| 1001 | last | 2,033/7,327 (27.75%) | 0/7,327 (0.00%) | 2,046/7,384 (27.71%) |
| 2001 | first | 0/7,257 (0.00%) | 2,004/7,257 (27.61%) | 0/7,372 (0.00%) |
| 2001 | middle | 0/7,371 (0.00%) | 0/7,371 (0.00%) | 0/7,371 (0.00%) |
| 2001 | last | 2,034/7,372 (27.59%) | 0/7,372 (0.00%) | 2,004/7,257 (27.61%) |
| 3001 | first | 0/7,243 (0.00%) | 2,029/7,243 (28.01%) | 0/7,321 (0.00%) |
| 3001 | middle | 0/7,436 (0.00%) | 0/7,436 (0.00%) | 0/7,436 (0.00%) |
| 3001 | last | 2,046/7,321 (27.95%) | 0/7,321 (0.00%) | 2,029/7,243 (28.01%) |

Original arrival bands are unchanged half-open RequestedAt bands in the source definition. They describe original simulated S2 arrival, **not S5 execution time**.

| Seed | Original S2 arrival band | S2 Bad/participations | S5 Bad/participations |
| --- | --- | --- | --- |
| 1001 | 07:00–09:00 | 0/2,240 (0.00%) | 1,832/2,240 (81.79%) |
| 1001 | 09:00–12:00 | 0/8,814 (0.00%) | 214/8,814 (2.43%) |
| 1001 | 12:00–15:00 | 0/3,231 (0.00%) | 0/3,231 (0.00%) |
| 1001 | 15:00–16:00 | 33/4,365 (0.76%) | 0/4,365 (0.00%) |
| 1001 | 16:00–18:00 | 447/1,580 (28.29%) | 0/1,580 (0.00%) |
| 1001 | 18:00–21:00 | 930/1,108 (83.94%) | 0/1,108 (0.00%) |
| 1001 | 21:00–23:00 | 623/662 (94.11%) | 0/662 (0.00%) |
| 2001 | 07:00–09:00 | 0/2,132 (0.00%) | 1,767/2,132 (82.88%) |
| 2001 | 09:00–12:00 | 0/8,829 (0.00%) | 237/8,829 (2.68%) |
| 2001 | 12:00–15:00 | 0/3,328 (0.00%) | 0/3,328 (0.00%) |
| 2001 | 15:00–16:00 | 8/4,414 (0.18%) | 0/4,414 (0.00%) |
| 2001 | 16:00–18:00 | 395/1,511 (26.14%) | 0/1,511 (0.00%) |
| 2001 | 18:00–21:00 | 985/1,107 (88.98%) | 0/1,107 (0.00%) |
| 2001 | 21:00–23:00 | 646/679 (95.14%) | 0/679 (0.00%) |
| 3001 | 07:00–09:00 | 0/2,166 (0.00%) | 1,826/2,166 (84.30%) |
| 3001 | 09:00–12:00 | 0/8,823 (0.00%) | 203/8,823 (2.30%) |
| 3001 | 12:00–15:00 | 0/3,342 (0.00%) | 0/3,342 (0.00%) |
| 3001 | 15:00–16:00 | 17/4,399 (0.39%) | 0/4,399 (0.00%) |
| 3001 | 16:00–18:00 | 430/1,499 (28.69%) | 0/1,499 (0.00%) |
| 3001 | 18:00–21:00 | 973/1,114 (87.34%) | 0/1,114 (0.00%) |
| 3001 | 21:00–23:00 | 626/657 (95.28%) | 0/657 (0.00%) |

In S5, Bad shifts toward originally early arrival bands after reversal. Those requests were dispatched near the end of S5 execution; they did not “arrive late,” and they were not processed at their preserved RequestedAt.

## Path classification transitions

Stored classifications are compared without rerunning the rubric. Ordering for grouped changes: Balanced > Acceptable > NotBalanced. InsufficientEvidence remains separate; NoPath is not ranked.

| Seed | S2 → S5 | Balanced | Acceptable | NotBalanced | InsufficientEvidence |
| --- | --- | --- | --- | --- | --- |
| 1001 | Balanced | 1844 | 841 | 218 | 0 |
| 1001 | Acceptable | 836 | 523 | 290 | 0 |
| 1001 | NotBalanced | 216 | 289 | 65 | 0 |
| 1001 | InsufficientEvidence | 0 | 0 | 0 | 232 |
| 2001 | Balanced | 1818 | 885 | 177 | 0 |
| 2001 | Acceptable | 896 | 509 | 277 | 0 |
| 2001 | NotBalanced | 206 | 272 | 75 | 0 |
| 2001 | InsufficientEvidence | 0 | 0 | 0 | 244 |
| 3001 | Balanced | 1821 | 845 | 224 | 0 |
| 3001 | Acceptable | 849 | 515 | 282 | 0 |
| 3001 | NotBalanced | 210 | 312 | 68 | 0 |
| 3001 | InsufficientEvidence | 0 | 0 | 0 | 218 |

| Seed | Same, classifiable | Improved | Worsened | IE → IE | Same, all evaluated | Ranked denominator |
| --- | --- | --- | --- | --- | --- | --- |
| 1001 | 2,432/5,122 (47.48%) | 1,341/5,122 (26.18%) | 1,349/5,122 (26.34%) | 232 | 2664 | 5122 |
| 2001 | 2,402/5,115 (46.96%) | 1,374/5,115 (26.86%) | 1,339/5,115 (26.18%) | 244 | 2646 | 5115 |
| 3001 | 2,404/5,126 (46.90%) | 1,371/5,126 (26.75%) | 1,351/5,126 (26.36%) | 218 | 2622 | 5126 |

All equivalent attendees retain the same path length. There are no NoPath cases or transitions between InsufficientEvidence and classifiable paths; all off-diagonal IE cells are zero. The derivative retains the path-length transition counts.

## NotBalanced recipient sensitivity

Unique AttendeeIds are compared across the whole five-day run. Overlap = intersection/union; churn = symmetric difference/union. Neither rate is a fairness threshold. S2 retention = intersection/S2 recipients; S5 new share = S5-only/S5 recipients.

| Seed | S2 | S5 | Both | S2 only | S5 only | Union | Overlap | Churn | S2 retention | S5 new share |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | 570 | 573 | 65 | 505 | 508 | 1078 | 6.030% | 93.970% | 11.404% | 88.656% |
| 2001 | 553 | 529 | 75 | 478 | 454 | 1007 | 7.448% | 92.552% | 13.562% | 85.822% |
| 3001 | 590 | 574 | 68 | 522 | 506 | 1096 | 6.204% | 93.796% | 11.525% | 88.153% |

## Recovery sensitivity

Episodes are matched through stable attendee + opening FestivalDay, never generated episode or Assignment IDs. Episode-opening recipients equal attendees with any Bad, whose overlap is shown above. Eligible unresolved means stored Unresolved with RecoveryEligible=true; a final Bad with no later persisted opportunity stays ineligible and is not a failed recovery.

| Seed | Episode openings S2 / S5 | Same attendee + opening day | Eligible unresolved S2 / S5 | Same eligible unresolved attendee | S2-only / S5-only eligible unresolved |
| --- | --- | --- | --- | --- | --- |
| 1001 | 1908 / 1888 | 0 | 40 / 54 | 0 | 40 / 54 |
| 2001 | 1900 / 1852 | 0 | 46 / 41 | 0 | 46 / 41 |
| 3001 | 1900 / 1867 | 0 | 49 / 48 | 0 | 49 / 48 |

Reversal changes who opens episodes on which days and who remains eligible unresolved. The aggregate table separately reports episode totals, recovery rate and speed; a changed eligible population is not hidden inside RecoveryRate.

## Daily closing capacity

Vectors list remaining **Good / Medium / Bad / total** Spots. Delta is S5 − S2. All checkpoints follow a drained daily request plan. Equal total residual capacity does not establish equal block geometry or absence of fragmentation; no fragmentation cause is inferred.

| Seed | Day | S2 G/M/B/total | S5 G/M/B/total | Delta G/M/B/total |
| --- | --- | --- | --- | --- |
| 1001 | 1 | 7 / 7 / 584 / 598 | 8 / 8 / 582 / 598 | +1 / +1 / -2 / +0 |
| 1001 | 2 | 4 / 0 / 594 / 598 | 4 / 4 / 590 / 598 | +0 / +4 / -4 / +0 |
| 1001 | 3 | 1 / 2 / 595 / 598 | 5 / 4 / 589 / 598 | +4 / +2 / -6 / +0 |
| 1001 | 4 | 8 / 7 / 583 / 598 | 7 / 7 / 584 / 598 | -1 / +0 / +1 / +0 |
| 1001 | 5 | 4 / 3 / 591 / 598 | 5 / 4 / 589 / 598 | +1 / +1 / -2 / +0 |
| 2001 | 1 | 5 / 4 / 589 / 598 | 2 / 0 / 596 / 598 | -3 / -4 / +7 / +0 |
| 2001 | 2 | 2 / 0 / 596 / 598 | 4 / 4 / 590 / 598 | +2 / +4 / -6 / +0 |
| 2001 | 3 | 4 / 4 / 590 / 598 | 2 / 1 / 595 / 598 | -2 / -3 / +5 / +0 |
| 2001 | 4 | 6 / 6 / 586 / 598 | 1 / 0 / 597 / 598 | -5 / -6 / +11 / +0 |
| 2001 | 5 | 5 / 8 / 585 / 598 | 0 / 0 / 598 / 598 | -5 / -8 / +13 / +0 |
| 3001 | 1 | 1 / 1 / 596 / 598 | 5 / 5 / 588 / 598 | +4 / +4 / -8 / +0 |
| 3001 | 2 | 8 / 8 / 582 / 598 | 3 / 3 / 592 / 598 | -5 / -5 / +10 / +0 |
| 3001 | 3 | 8 / 8 / 582 / 598 | 6 / 6 / 586 / 598 | -2 / -2 / +4 / +0 |
| 3001 | 4 | 7 / 7 / 584 / 598 | 4 / 4 / 590 / 598 | -3 / -3 / +6 / +0 |
| 3001 | 5 | 4 / 4 / 590 / 598 | 3 / 0 / 595 / 598 | -1 / -4 / +5 / +0 |

## Group size interaction

Secondary descriptive interaction: Bad Assignments / persisted participations of that request group size. Group sizes and membership are identical in each pair. These comparisons do not isolate group-size causality or substitute for S6.

| Seed | Group size | S2 Bad/participations | S5 Bad/participations | Rate delta |
| --- | --- | --- | --- | --- |
| 1001 | 1 | 2/1,100 (0.18%) | 0/1,100 (0.00%) | -0.182 pp |
| 1001 | 2 | 222/4,390 (5.06%) | 274/4,390 (6.24%) | +1.185 pp |
| 1001 | 3 | 444/5,490 (8.09%) | 522/5,490 (9.51%) | +1.421 pp |
| 1001 | 4 | 420/4,400 (9.55%) | 392/4,400 (8.91%) | -0.636 pp |
| 1001 | 5 | 365/2,925 (12.48%) | 280/2,925 (9.57%) | -2.906 pp |
| 1001 | 6–7 | 372/2,379 (15.64%) | 306/2,379 (12.86%) | -2.774 pp |
| 1001 | 8–10 | 208/1,316 (15.81%) | 272/1,316 (20.67%) | +4.863 pp |
| 2001 | 1 | 0/1,100 (0.00%) | 5/1,100 (0.45%) | +0.455 pp |
| 2001 | 2 | 288/4,390 (6.56%) | 296/4,390 (6.74%) | +0.182 pp |
| 2001 | 3 | 483/5,490 (8.80%) | 405/5,490 (7.38%) | -1.421 pp |
| 2001 | 4 | 436/4,400 (9.91%) | 464/4,400 (10.55%) | +0.636 pp |
| 2001 | 5 | 320/2,925 (10.94%) | 390/2,925 (13.33%) | +2.393 pp |
| 2001 | 6–7 | 237/2,382 (9.95%) | 298/2,382 (12.51%) | +2.561 pp |
| 2001 | 8–10 | 270/1,313 (20.56%) | 146/1,313 (11.12%) | -9.444 pp |
| 3001 | 1 | 0/1,100 (0.00%) | 4/1,100 (0.36%) | +0.364 pp |
| 3001 | 2 | 248/4,390 (5.65%) | 304/4,390 (6.92%) | +1.276 pp |
| 3001 | 3 | 579/5,490 (10.55%) | 480/5,490 (8.74%) | -1.803 pp |
| 3001 | 4 | 440/4,400 (10.00%) | 448/4,400 (10.18%) | +0.182 pp |
| 3001 | 5 | 275/2,925 (9.40%) | 305/2,925 (10.43%) | +1.026 pp |
| 3001 | 6–7 | 317/2,381 (13.31%) | 345/2,381 (14.49%) | +1.176 pp |
| 3001 | 8–10 | 187/1,314 (14.23%) | 143/1,314 (10.88%) | -3.349 pp |

Across S5 seeds, size 1 has 0.00–0.45% Bad versus 10.88–20.67% for size 8–10. However, size 8–10 changes by +4.863, −9.444 and −3.349 pp respectively; the intermediate-size ordering is not universally monotonic. This retains a larger-group association while showing that its magnitude interacts with order. It does not identify group-size causality.

## FrontStanding eligibility interaction

Eligibility is request-level and unchanged. Bad rates use persisted category participations. NB participation rate means a participation of an attendee whose final stored path is NotBalanced; it does not classify that individual request. Changing groups can expose the same attendee to both eligibility categories, so distinct category populations are not additive.

| Seed | AllowsFrontStanding | S2 Bad rate | S5 Bad rate | S2 NB participation rate | S5 NB participation rate |
| --- | --- | --- | --- | --- | --- |
| 1001 | false | 1,084/10,888 (9.96%) | 1,100/10,888 (10.10%) | 1,177/10,888 (10.81%) | 1,243/10,888 (11.42%) |
| 1001 | true | 949/11,112 (8.54%) | 946/11,112 (8.51%) | 1,182/11,112 (10.64%) | 1,171/11,112 (10.54%) |
| 2001 | false | 1,127/10,939 (10.30%) | 1,131/10,939 (10.34%) | 1,181/10,939 (10.80%) | 1,140/10,939 (10.42%) |
| 2001 | true | 907/11,061 (8.20%) | 873/11,061 (7.89%) | 1,120/11,061 (10.13%) | 1,083/11,061 (9.79%) |
| 3001 | false | 1,007/10,868 (9.27%) | 1,053/10,868 (9.69%) | 1,201/10,868 (11.05%) | 1,226/10,868 (11.28%) |
| 3001 | true | 1,039/11,132 (9.33%) | 976/11,132 (8.77%) | 1,250/11,132 (11.23%) | 1,169/11,132 (10.50%) |

These differences describe interaction with the reversal, not changed eligibility prevalence. They remain secondary to the direct execution-position result.

In S5, FrontStanding-excluded requests have higher Bad and NotBalanced participation rates in all three seeds. Eligibility itself is unchanged; these are secondary order-interaction observations, without a new fairness threshold or a primary eligibility conclusion.

## Cross-seed stability

S5 statistics use unweighted run values; null runs are excluded and counted as N/A, never zero. Every paired delta appears above; this summary supplements those comparisons.

| S5 metric | Min | Mean | Max | N/A runs |
| --- | --- | --- | --- | --- |
| attemptedRequests | 7,320 | 7,320 | 7,320 | 0 |
| successfulRequests | 7,320 | 7,320 | 7,320 | 0 |
| domainRejectedRequests | 0 | 0 | 0 | 0 |
| knownConcurrencyConflictRequests | 0 | 0 | 0 | 0 |
| unexpectedTechnicalFailureRequests | 0 | 0 | 0 | 0 |
| successRate | 100.000% | 100.000% | 100.000% | 0 |
| rejectionRate | 0.000% | 0.000% | 0.000% | 0 |
| concurrencyConflictRate | 0.000% | 0.000% | 0.000% | 0 |
| technicalErrorRate | 0.000% | 0.000% | 0.000% | 0 |
| goodOutcomeRate | 39.686% | 39.729% | 39.777% | 0 |
| mediumOutcomeRate | 51.014% | 51.061% | 51.114% | 0 |
| badOutcomeRate | 9.109% | 9.211% | 9.300% | 0 |
| balancedPathRate | 56.184% | 56.604% | 57.087% | 0 |
| acceptablePathRate | 32.273% | 32.487% | 32.618% | 0 |
| notBalancedPathRate | 10.342% | 10.909% | 11.198% | 0 |
| classifiablePathRate | 95.447% | 95.678% | 95.921% | 0 |
| recoveryEpisodeCount | 1,852 | 1,869 | 1,888 | 0 |
| recoveredEpisodeCount | 1,374 | 1381.666667 | 1,386 | 0 |
| unresolvedEpisodeCount | 478 | 487.333333 | 502 | 0 |
| recoveryEligibleEpisodeCount | 1,415 | 1429.333333 | 1,440 | 0 |
| recoveryRate | 96.250% | 96.668% | 97.102% | 0 |
| averageOpportunitiesToRecovery | 1.070707 | 1.076485 | 1.080144 | 0 |
| medianOpportunitiesToRecovery | 1 | 1 | 1 | 0 |

The direction of aggregate change is not uniform: BadOutcomeRate rises for 1001 and falls for 2001/3001; RecoveryRate falls for 1001 and rises for 2001/3001. Identity replacement and concentration in the last actual execution third are consistent across seeds. Reversal is therefore not evidence of a uniform aggregate improvement.

## Performance observation

Exploratory nearest-rank p95 of existing attempted-request durations; no benchmark, warm-up control or optimization. Timing covers the Application call, excluding scope preparation, reconciliation and checkpoints. The required solution tests ran on the same host during part of the first official S5 run, further limiting any timing comparison.

| Seed | S2 p95 (ms) | S5 p95 (ms) |
| --- | --- | --- |
| 1001 | 44.736900 | 53.637400 |
| 2001 | 44.616400 | 47.633700 |
| 3001 | 42.759500 | 45.959100 |

## Observed evidence

- Exact pair input equality and daily reversal pass for each seed.
- All six paired packages have 100% request success; S5 has no technical failures, conflicts, missing files or final-state invariant findings.
- Bad outcomes are entirely in the final actual execution third in S2 and S5; reversal changes their baseline third from last to first.
- Every seed has zero B → B attendee/day transitions, despite some overlap among attendees receiving any Bad on different days.
- Aggregate quality/path/recovery deltas, individual quality/classification transitions and recipient replacement are recorded separately above.
- The repeated S5/1001 logical outputs match.

## Interpretation

**How sensitive is Engine v1 to request processing order?** Strongly sensitive in who receives each quality and which attendee/day outcomes are unfavorable, even when aggregate rates look similar. The unchanged workload and controlled reversal support an order-sensitivity interpretation more strongly than the earlier observational S2 analysis: unfavorable outcomes follow experimental execution position rather than preserved original simulated arrival time. The reversal also changes prior-day histories and subsequent decisions as part of its total effect; this task does not separate direct within-day and history-mediated effects.

The experiment covers one complete daily reversal and three seeds under the S2 workload. It does not establish real-world arrival behavior, effects of every possible order, concurrency behavior, group-size causality or fragmentation causality. Reverse order is not proposed as a production policy. Similar aggregate rates do not establish stable attendee treatment.

## Implication for later Stage 5

**S6 remains the next supported workload experiment**, to isolate large-group prevalence now that S5 confirms order sensitivity. The group-size association remains secondary and must not be treated as an S6 causal result. Keep S7’s fragmentation/feasibility question as a later evidence gap: closing inventory alone cannot diagnose it, and these runs do not justify elevating S7 above S6. No later scenario or Decision Gate verdict is implemented.

## Reproducibility and evidence retention

After the three official runs, `reproduction-s5-1001-fccb5df0a34949f68145d640f49b1736` repeated S5/1001. Logical manifest/execution order, attempt outcomes/diagnostics/capacity, paths/classifications, recovery and metrics all match. Generated durable request/Assignment IDs are normalized by stable SimulationRequestId and AttendeeId/FestivalDay; observed start/end/duration are excluded. Definition, manifest and metrics also match byte-for-byte SHA-256. No S2 was rerun.

| Logical artifact | Equivalent | Normalized reference SHA-256 |
| --- | --- | --- |
| manifest | True | `dc1bb598d64b7fdbdbc3068cabab45dc0ca8704b966bacaf1eca5b045aff545b` |
| attempts | True | `87398cf8720eeeb70ff35fc25d35be7c572efa9826f14e69894186c18e66ef65` |
| attendee-paths | True | `ff02857f31febcfded213d388bfb66e672c2f1e36774d9b26061b3ae5e24681f` |
| recovery-observations | True | `fac81bc3bf2c39fb11563f83b229c00edbf300f19e0ef264dccc6b4121efd394` |
| metrics | True | `cd34a92d25ab1499ea9a7de062118e7f683a6d187b8097288ecdb0e0354f37b5` |

Each raw package contains exactly `run-metadata.json`, `definition.json`, `manifest.json`, `attempts.json`, `checkpoints.json`, `attendee-paths.json`, `recovery-observations.json`, and `metrics.json`. Every file parses, and all S2/S5/reproduction file hashes remain unchanged after analysis. Per-file SHA-256 values are retained in the structured derivative and raw set index; S2 originals retain their original hashes and bytes.

Source S2 structured-results SHA-256: `e54ec777abf20c8ac9b3981d2d47d50b1afb94f8c3797cfff3c33186ec9c3a87`.

Source S5 index SHA-256: `cf9b3d41c461e2208cea3d0eb29102054dfc5afd403a37b783ef2d5ea9c01a4f`.

Selected derived files for developer review are this report and [the structured summary](s2-s5-order-sensitivity-results.json). Raw evidence stays ignored under `artifacts/simulations/`, at the exact paths above. No raw packages or derived results were committed; Git operations remain with the developer. Local raw evidence needs backup before removing this checkout; no retention subsystem is added.

Reproduce analysis from the repository root with a fresh output path:

```bash
python3 -B scripts/stage-5-representative-runs/analyze_s5_order.py \
  artifacts/simulations/s5-order-sensitivity-20261106-8ae89818c5d74a23b9756f324893d128/index.json \
  --output artifacts/analysis/s2-s5-order-sensitivity-repeat.json
```

## Tests and validation

- `dotnet build Festival.sln`: passed, 0 warnings and 0 errors.
- `DOCKER_API_VERSION=1.43 dotnet test Festival.sln`: **897 passed, 0 failed, 0 skipped** (490 Domain, 43 Application, 86 Infrastructure, 77 Infrastructure integration, 201 Simulation).
- Task-specific harness build: passed, 0 warnings and 0 errors.
- Python unittest discovery: **28 passed**, including 15 new S5 regression tests.
- `git diff --check`: passed.
- All raw evidence JSON parses; recorded hashes verified before and after analysis; repeated derived analysis is deterministic.
- The sandbox initially blocked .NET local pipes/Docker; the full authorized suite passed. Those launcher restrictions are not simulated request outcomes.

## Files, assumptions and scope protection

Modified: `scripts/stage-5-representative-runs/Program.cs`, its `README.md`. Added: `analyze_s5_order.py`, `test_s5_order.py`, this report and its structured JSON summary. No NuGet packages, solution membership or production source files changed.

Assumptions: the existing fixture FirstDate and exact S2 controls remain authoritative; no new business decision or threshold was needed. Identity overlap uses the union denominator declared above. Source code revision is `51a61f930ab972f903f619baaf9c64a13d5deec0`; task-specific harness/analysis/report changes are uncommitted and are not represented by that base SHA.

No S2 rerun/tuning, S5 transformation changes, Engine/fairness/rubric/recovery/metric changes, S6–S8, request concurrency or Decision Gate logic were introduced. No branch creation/switch, commit or push occurred. Remaining work is a separately scoped later Stage 5 experiment (S6 next; S7 gap retained). No requested analysis is deferred.
