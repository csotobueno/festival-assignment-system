# S2/S6 large-group sensitivity results

Execution/analysis date: **2026-11-06 (America/Lima)**. RunIds and runtime timestamps use UTC. The simulated first date is **2026-08-15**, a fixture rather than a confirmed event date.

## Summary

Across the three official S6 runs, increased large-group prevalence preserved observed request feasibility. BadOutcomeRate and NotBalancedPathRate increased in every Seed; recovery-rate changes were mixed and average recovery distance increased slightly. All S6 Bad Assignments occurred in the final execution third, where the size gradient also remained visible. No placement rejection demonstrated contiguous-block pressure. These are scenario-level sensitivity findings under provisional inputs, with no Decision Gate verdict.

## Experimental answers

1. **Does increasing 6–10 prevalence materially change feasibility?** Observed S6 DomainRejected = 0; SuccessRate changes: 1001: +0.000 pp; 2001: +0.000 pp; 3001: +0.000 pp. No request-feasibility degradation was observed across these Seeds. This does not establish feasibility for all group mixes or loads.

2. **Does it materially change quality distribution?** BadOutcomeRate changes: 1001: +0.345 pp; 2001: +0.323 pp; 3001: +0.309 pp; Good: 1001: -0.145 pp; 2001: -0.118 pp; 3001: -0.123 pp; Medium: 1001: -0.200 pp; 2001: -0.205 pp; 3001: -0.186 pp. These are measured effect sizes; no new materiality threshold or Decision Gate verdict is imposed.

3. **Does it materially change NotBalanced prevalence?** NotBalancedPathRate changes: 1001: +0.521 pp; 2001: +1.237 pp; 3001: +0.717 pp. Cohorts and classifiable denominators can differ; this is scenario-level sensitivity, not exact attendee pairing.

4. **Does it materially change recovery?** RecoveryRate changes: 1001: -0.365 pp; 2001: +0.408 pp; 3001: -0.250 pp; average opportunities: 1001: +0.006395; 2001: +0.016427; 3001: +0.006143. Eligible-unresolved counts and episode-opening attribution remain separate from Bad counts.

5. **Are larger groups disproportionately affected?** 1001: size 1–5 6.566%, 6–10 13.132%; 6–7 11.973%, 8–10 14.401%; nondecreasing across all seven buckets: True; 2001: size 1–5 6.330%, 6–10 13.370%; 6–7 11.863%, 8–10 15.011%; nondecreasing across all seven buckets: True; 3001: size 1–5 5.926%, 6–10 13.933%; 6–7 10.966%, 8–10 17.158%; nondecreasing across all seven buckets: True. These are normalized BadOutcomeRates. A size gradient is not evidence of a discrete feasibility threshold, and within-S6 association alone cannot establish causality.

6. **Are negative effects explained by group size, execution position, or both?** S6 has 0 Bad Assignments in the first/middle execution thirds. Final-third Bad rates for larger buckets: 1001: 6–7 34.857%, 8–10 45.019%; 2001: 6–7 36.269%, 8–10 43.507%; 3001: 6–7 36.652%, 8–10 49.583%. This controls only a coarse timing stratum; exact rank, eligibility, history and changing composition remain intertwined. A persistent within-third size association supports both timing and size as descriptive dimensions, not a causal decomposition.

7. **Do rejections occur while total/eligible capacity remains?** There are 0 S6 rejections. Each observed rejection is retained with execution identity, eligible-capacity snapshot, block-found observation and reconstructed total inventory; zero rejections means no such event was observed, not that residual capacity is always usable.

8. **Is there evidence of contiguous-block pressure?** Only no-block rejections with eligible raw capacity at least GroupSize support that diagnosis in this report. Positive eligible capacity below GroupSize is insufficient capacity. No rejection evidence means no demonstrated placement failure from contiguous-block pressure; residual quality inventory alone cannot prove fragmentation.

9. **Does the evidence justify S7 as a distinct fragmentation experiment?** S7 remains a distinct unanswered sensitivity question. S6 changes group prevalence without controlling fragmentation geometry; its residual inventory and timing signals can motivate S7 design, but neither establish fragmentation causality nor predefine S7 parameters. Defining the S7 contract is the evidence-supported next analytical task, subject to the existing Stage 5 scope.

## Execution matrix and retained evidence

| Scenario | Seed | RunId | Status | Raw evidence path |
| --- | --- | --- | --- | --- |
| S2 | 1001 | official-s2-1001-b6c13b403b654deabc2a23302c2a7b5a | Completed | `artifacts/simulations/s2-expected-baseline/official-s2-1001-b6c13b403b654deabc2a23302c2a7b5a/` |
| S6 | 1001 | official-s6-1001-a8c7fe7463384083ade28a0708b5ce20 | Completed | `artifacts/simulations/s6-large-groups/official-s6-1001-a8c7fe7463384083ade28a0708b5ce20/` |
| S2 | 2001 | official-s2-2001-38de76b7ac10424295d07ff3272f8333 | Completed | `artifacts/simulations/s2-expected-baseline/official-s2-2001-38de76b7ac10424295d07ff3272f8333/` |
| S6 | 2001 | official-s6-2001-0d494f860e444d98adbc2d193fcf3424 | Completed | `artifacts/simulations/s6-large-groups/official-s6-2001-0d494f860e444d98adbc2d193fcf3424/` |
| S2 | 3001 | official-s2-3001-50a58cfdae484c5d99139d63b0176545 | Completed | `artifacts/simulations/s2-expected-baseline/official-s2-3001-50a58cfdae484c5d99139d63b0176545/` |
| S6 | 3001 | official-s6-3001-cfbc13411f9c490e95e929d08a6f774e | Completed | `artifacts/simulations/s6-large-groups/official-s6-3001-cfbc13411f9c490e95e929d08a6f774e/` |
| S6 reproduction | 1001 | reproduction-s6-1001-379cc399cf6e464ca0402d2f4881b518 | Completed | `artifacts/simulations/s6-large-groups/reproduction-s6-1001-379cc399cf6e464ca0402d2f4881b518/` |

Set index: `artifacts/simulations/s6-large-group-sensitivity-20261106-7e3855c30230448893bfeb561b8be700/index.json`. No S2 controls were rerun or overwritten. All eight raw-file SHA-256 hashes are retained in the structured report and checked before and after analysis. Raw packages remain ignored under `artifacts/simulations/` and require separate backup before deleting the checkout.

## Controls, provenance and comparison limits

For every Seed, definitions match after removing only scenario identity and GroupSizeProfile. Both preserve 5,500 registered Attendees, exactly 4,400 daily planned requesters, five days, 90% continuity (3,960 retained / 440 replacements), 50% request-level FrontStanding eligibility, generator v2, Engine v0.3.0, sequential execution/concurrency 1, calendar, S2 arrival configuration, exact catalog/topology identities and normal order. BaselineSequence equals ExecutionSequence; no S5 transformation is present. Clean Day 1 and natural persisted Day 2–5 history follow the unchanged runner.

S2 target shares are 15/30/25/15/8/5/2%; S6 shares are 10/20/20/15/10/15/10%, over request buckets 1, 2, 3, 4, 5, 6–7, 8–10. Thus 6–10 increases from 7% to 25%. Exact daily attendee demand takes priority over integer group quotas. These runs materialized 1,464 S2 versus 1,080 S6 requests/day. S6 bucket counts/day are 108/216/216/162/108/162/108; target shares are exactly representable here.

Equal Seeds do not imply equal daily cohorts or requests: generator streams include ScenarioId. This report performs scenario-level controlled comparisons and no attendee/day transition matrices. The recorded source revision identifies unchanged scenario/engine code; the execution harness has local task changes. Harness and analysis-tool hashes accompany the index hash in JSON.

## Run integrity

| Scenario | Seed/role | Attempted | Successful | DomainRejected | Conflict | Technical | Final-state findings |
| --- | --- | --- | --- | --- | --- | --- | --- |
| S2 | 1001 | 7320 | 7320 | 0 | 0 | 0 | 0 |
| S6 | 1001 | 5400 | 5400 | 0 | 0 | 0 | 0 |
| S2 | 2001 | 7320 | 7320 | 0 | 0 | 0 | 0 |
| S6 | 2001 | 5400 | 5400 | 0 | 0 | 0 | 0 |
| S2 | 3001 | 7320 | 7320 | 0 | 0 | 0 | 0 |
| S6 | 3001 | 5400 | 5400 | 0 | 0 | 0 | 0 |
| S6 | 1001 reproduction | 5400 | 5400 | 0 | 0 | 0 | 0 |

Each S6 package has five completed days, all eight expected files, 22,000 persisted Assignments and 4,400 unique planned requesters/day with valid sizes 1..10. ExcludedInvalidInput is zero. The durable-state audit checks unique Spot/day and Attendee/day, full contiguous same-Zone groups, catalog identity, request terminal consistency, dispatch and checkpoint reconciliation. This is a final-state audit; it is not a complete temporal proof of INV-06. The unchanged Application flow does not replace/cancel Assignments.

## Primary aggregate comparison

Deltas are **S6 minus S2**. Rates use percentage points (pp); recovery distances use assigned opportunities. Success/rejection use NormallyProcessedRequests; quality uses persisted evaluated Assignments; path classification uses ClassifiablePaths; recovery rate uses eligible episodes and recovery speed uses recovered episodes. Stored metrics/rubric/recovery outputs are consumed unchanged. Null is N/A.

| Seed | Metric | S2 | S6 | Delta |
| --- | --- | --- | --- | --- |
| 1001 | successRate | 100.000% | 100.000% | +0.000 pp |
| 1001 | rejectionRate | 0.000% | 0.000% | +0.000 pp |
| 1001 | goodOutcomeRate | 39.709% | 39.564% | -0.145 pp |
| 1001 | mediumOutcomeRate | 51.050% | 50.850% | -0.200 pp |
| 1001 | badOutcomeRate | 9.241% | 9.586% | +0.345 pp |
| 1001 | balancedPathRate | 56.677% | 56.020% | -0.657 pp |
| 1001 | acceptablePathRate | 32.194% | 32.330% | +0.135 pp |
| 1001 | notBalancedPathRate | 11.128% | 11.650% | +0.521 pp |
| 1001 | recoveryRate | 97.236% | 96.871% | -0.365 pp |
| 1001 | averageOpportunitiesToRecovery | 1.056148 | 1.062543 | +0.006395 |
| 1001 | medianOpportunitiesToRecovery | 1.000000 | 1.000000 | +0.000000 |
| 2001 | successRate | 100.000% | 100.000% | +0.000 pp |
| 2001 | rejectionRate | 0.000% | 0.000% | +0.000 pp |
| 2001 | goodOutcomeRate | 39.718% | 39.600% | -0.118 pp |
| 2001 | mediumOutcomeRate | 51.036% | 50.832% | -0.205 pp |
| 2001 | badOutcomeRate | 9.245% | 9.568% | +0.323 pp |
| 2001 | balancedPathRate | 56.305% | 56.415% | +0.110 pp |
| 2001 | acceptablePathRate | 32.884% | 31.537% | -1.347 pp |
| 2001 | notBalancedPathRate | 10.811% | 12.048% | +1.237 pp |
| 2001 | recoveryRate | 96.825% | 97.233% | +0.408 pp |
| 2001 | averageOpportunitiesToRecovery | 1.061297 | 1.077724 | +0.016427 |
| 2001 | medianOpportunitiesToRecovery | 1.000000 | 1.000000 | +0.000000 |
| 3001 | successRate | 100.000% | 100.000% | +0.000 pp |
| 3001 | rejectionRate | 0.000% | 0.000% | +0.000 pp |
| 3001 | goodOutcomeRate | 39.691% | 39.568% | -0.123 pp |
| 3001 | mediumOutcomeRate | 51.009% | 50.823% | -0.186 pp |
| 3001 | badOutcomeRate | 9.300% | 9.609% | +0.309 pp |
| 3001 | balancedPathRate | 56.379% | 55.859% | -0.520 pp |
| 3001 | acceptablePathRate | 32.111% | 31.914% | -0.197 pp |
| 3001 | notBalancedPathRate | 11.510% | 12.227% | +0.717 pp |
| 3001 | recoveryRate | 96.621% | 96.371% | -0.250 pp |
| 3001 | averageOpportunitiesToRecovery | 1.066381 | 1.072524 | +0.006143 |
| 3001 | medianOpportunitiesToRecovery | 1.000000 | 1.000000 | +0.000000 |

## Group-size outcomes

Attendee counts below are repeated attendee/day participations, not distinct people. Planned and persisted counts are separate in JSON; they coincide here because every request succeeded. Quality rates are attendee-weighted; request rates count each group once. NB association is the final stored NotBalanced path of a participation's attendee, divided by classifiable persisted participations in that bucket. It never labels a request NotBalanced. Attendees can appear in different buckets across days; category attendee populations are not additive.

| Seed | Scenario | Size | Requests | Attendee participations | Successful | Rejected | Success | Rejection | Good | Medium | Bad | NB/classifiable participations |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | S2 | 1 | 1100 | 1100 | 1100 | 0 | 100.000% | 0.000% | 45.727% | 54.091% | 0.182% | 57/1085 (5.253%) |
| 1001 | S2 | 2 | 2195 | 4390 | 2195 | 0 | 100.000% | 0.000% | 40.638% | 54.305% | 5.057% | 448/4337 (10.330%) |
| 1001 | S2 | 3 | 1830 | 5490 | 1830 | 0 | 100.000% | 0.000% | 42.022% | 49.891% | 8.087% | 562/5429 (10.352%) |
| 1001 | S2 | 4 | 1100 | 4400 | 1100 | 0 | 100.000% | 0.000% | 40.455% | 50.000% | 9.545% | 483/4365 (11.065%) |
| 1001 | S2 | 5 | 585 | 2925 | 585 | 0 | 100.000% | 0.000% | 40.000% | 47.521% | 12.479% | 349/2889 (12.080%) |
| 1001 | S2 | 6–7 | 365 | 2379 | 365 | 0 | 100.000% | 0.000% | 32.114% | 52.249% | 15.637% | 294/2359 (12.463%) |
| 1001 | S2 | 8–10 | 145 | 1316 | 145 | 0 | 100.000% | 0.000% | 32.523% | 51.672% | 15.805% | 166/1304 (12.730%) |
| 1001 | S6 | 1 | 540 | 540 | 540 | 0 | 100.000% | 0.000% | 46.852% | 53.148% | 0.000% | 43/534 (8.052%) |
| 1001 | S6 | 2 | 1080 | 2160 | 1080 | 0 | 100.000% | 0.000% | 43.519% | 53.796% | 2.685% | 206/2135 (9.649%) |
| 1001 | S6 | 3 | 1080 | 3240 | 1080 | 0 | 100.000% | 0.000% | 43.889% | 50.000% | 6.111% | 348/3200 (10.875%) |
| 1001 | S6 | 4 | 810 | 3240 | 810 | 0 | 100.000% | 0.000% | 36.667% | 56.420% | 6.914% | 363/3198 (11.351%) |
| 1001 | S6 | 5 | 540 | 2700 | 540 | 0 | 100.000% | 0.000% | 39.444% | 49.444% | 11.111% | 331/2666 (12.416%) |
| 1001 | S6 | 6–7 | 810 | 5287 | 810 | 0 | 100.000% | 0.000% | 36.940% | 51.088% | 11.973% | 622/5227 (11.900%) |
| 1001 | S6 | 8–10 | 540 | 4833 | 540 | 0 | 100.000% | 0.000% | 38.961% | 46.638% | 14.401% | 577/4779 (12.074%) |
| 2001 | S2 | 1 | 1100 | 1100 | 1100 | 0 | 100.000% | 0.000% | 42.636% | 57.364% | 0.000% | 72/1090 (6.606%) |
| 2001 | S2 | 2 | 2195 | 4390 | 2195 | 0 | 100.000% | 0.000% | 40.228% | 53.212% | 6.560% | 406/4347 (9.340%) |
| 2001 | S2 | 3 | 1830 | 5490 | 1830 | 0 | 100.000% | 0.000% | 39.672% | 51.530% | 8.798% | 554/5430 (10.203%) |
| 2001 | S2 | 4 | 1100 | 4400 | 1100 | 0 | 100.000% | 0.000% | 40.273% | 49.818% | 9.909% | 490/4361 (11.236%) |
| 2001 | S2 | 5 | 585 | 2925 | 585 | 0 | 100.000% | 0.000% | 39.487% | 49.573% | 10.940% | 326/2884 (11.304%) |
| 2001 | S2 | 6–7 | 365 | 2382 | 365 | 0 | 100.000% | 0.000% | 39.253% | 50.798% | 9.950% | 245/2348 (10.434%) |
| 2001 | S2 | 8–10 | 145 | 1313 | 145 | 0 | 100.000% | 0.000% | 35.263% | 44.174% | 20.564% | 208/1296 (16.049%) |
| 2001 | S6 | 1 | 540 | 540 | 540 | 0 | 100.000% | 0.000% | 42.037% | 57.963% | 0.000% | 46/533 (8.630%) |
| 2001 | S6 | 2 | 1080 | 2160 | 1080 | 0 | 100.000% | 0.000% | 41.111% | 57.315% | 1.574% | 183/2131 (8.588%) |
| 2001 | S6 | 3 | 1080 | 3240 | 1080 | 0 | 100.000% | 0.000% | 40.833% | 53.889% | 5.278% | 363/3209 (11.312%) |
| 2001 | S6 | 4 | 810 | 3240 | 810 | 0 | 100.000% | 0.000% | 39.259% | 52.346% | 8.395% | 363/3199 (11.347%) |
| 2001 | S6 | 5 | 540 | 2700 | 540 | 0 | 100.000% | 0.000% | 36.852% | 52.963% | 10.185% | 341/2678 (12.733%) |
| 2001 | S6 | 6–7 | 810 | 5277 | 810 | 0 | 100.000% | 0.000% | 40.838% | 47.300% | 11.863% | 646/5234 (12.342%) |
| 2001 | S6 | 8–10 | 540 | 4843 | 540 | 0 | 100.000% | 0.000% | 38.241% | 46.748% | 15.011% | 664/4782 (13.885%) |
| 3001 | S2 | 1 | 1100 | 1100 | 1100 | 0 | 100.000% | 0.000% | 44.091% | 55.909% | 0.000% | 72/1087 (6.624%) |
| 3001 | S2 | 2 | 2195 | 4390 | 2195 | 0 | 100.000% | 0.000% | 43.645% | 50.706% | 5.649% | 418/4344 (9.622%) |
| 3001 | S2 | 3 | 1830 | 5490 | 1830 | 0 | 100.000% | 0.000% | 39.071% | 50.383% | 10.546% | 604/5440 (11.103%) |
| 3001 | S2 | 4 | 1100 | 4400 | 1100 | 0 | 100.000% | 0.000% | 39.000% | 51.000% | 10.000% | 524/4354 (12.035%) |
| 3001 | S2 | 5 | 585 | 2925 | 585 | 0 | 100.000% | 0.000% | 38.120% | 52.479% | 9.402% | 357/2901 (12.306%) |
| 3001 | S2 | 6–7 | 365 | 2381 | 365 | 0 | 100.000% | 0.000% | 34.103% | 52.583% | 13.314% | 314/2355 (13.333%) |
| 3001 | S2 | 8–10 | 145 | 1314 | 145 | 0 | 100.000% | 0.000% | 41.324% | 44.444% | 14.231% | 162/1301 (12.452%) |
| 3001 | S6 | 1 | 540 | 540 | 540 | 0 | 100.000% | 0.000% | 49.259% | 50.741% | 0.000% | 38/536 (7.090%) |
| 3001 | S6 | 2 | 1080 | 2160 | 1080 | 0 | 100.000% | 0.000% | 42.963% | 56.481% | 0.556% | 186/2134 (8.716%) |
| 3001 | S6 | 3 | 1080 | 3240 | 1080 | 0 | 100.000% | 0.000% | 40.833% | 53.241% | 5.926% | 339/3194 (10.614%) |
| 3001 | S6 | 4 | 810 | 3240 | 810 | 0 | 100.000% | 0.000% | 40.864% | 51.111% | 8.025% | 382/3197 (11.949%) |
| 3001 | S6 | 5 | 540 | 2700 | 540 | 0 | 100.000% | 0.000% | 40.370% | 50.741% | 8.889% | 306/2677 (11.431%) |
| 3001 | S6 | 6–7 | 810 | 5271 | 810 | 0 | 100.000% | 0.000% | 38.949% | 50.085% | 10.966% | 637/5228 (12.184%) |
| 3001 | S6 | 8–10 | 540 | 4849 | 540 | 0 | 100.000% | 0.000% | 35.492% | 47.350% | 17.158% | 718/4797 (14.968%) |

## Large-group focus: 1–5 versus 6–10

| Seed | Scenario | Size | Requests | Participations | Success | Rejection | Bad |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | S2 | 1–5 | 6810 | 18305 | 100.000% | 0.000% | 7.938% |
| 1001 | S2 | 6–10 | 510 | 3695 | 100.000% | 0.000% | 15.697% |
| 1001 | S6 | 1–5 | 4050 | 11880 | 100.000% | 0.000% | 6.566% |
| 1001 | S6 | 6–10 | 1350 | 10120 | 100.000% | 0.000% | 13.132% |
| 2001 | S2 | 1–5 | 6810 | 18305 | 100.000% | 0.000% | 8.342% |
| 2001 | S2 | 6–10 | 510 | 3695 | 100.000% | 0.000% | 13.721% |
| 2001 | S6 | 1–5 | 4050 | 11880 | 100.000% | 0.000% | 6.330% |
| 2001 | S6 | 6–10 | 1350 | 10120 | 100.000% | 0.000% | 13.370% |
| 3001 | S2 | 1–5 | 6810 | 18305 | 100.000% | 0.000% | 8.424% |
| 3001 | S2 | 6–10 | 510 | 3695 | 100.000% | 0.000% | 13.640% |
| 3001 | S6 | 1–5 | 4050 | 11880 | 100.000% | 0.000% | 5.926% |
| 3001 | S6 | 6–10 | 1350 | 10120 | 100.000% | 0.000% | 13.933% |

The separate 6–7 versus 8–10 comparison is in the group-size table above. Compare rates within each Seed; a prevalence-weighted total is not an individual group-size causal estimate.

## Rejections and contiguous-block pressure

Every rejection, when present, is included in JSON with day, simulation request identity, actual execution position/third, GroupSize, production reason, eligible capacity and block-found evidence. Grouped totals reconcile exactly by size, day, third and reason. Remaining total before a request is reconstructed from the clean 4,998-Spot inventory minus preceding successful persisted group memberships; it is not an added telemetry snapshot. Eligible-capacity evidence remains the baseline's independent pre-save read and does not guarantee a transactionally consistent snapshot.

| Seed | Rejected | Sufficient eligible capacity/no block | Insufficient eligible capacity | Unavailable diagnosis |
| --- | --- | --- | --- | --- |
| 1001 | 0 | 0 | 0 | 0 |
| 2001 | 0 | 0 | 0 | 0 |
| 3001 | 0 | 0 | 0 | 0 |

No-block rejection with eligible capacity >= GroupSize supports contiguous-block pressure. Eligible capacity > 0 but < GroupSize is insufficient eligible capacity. End-of-day residuals alone establish neither unusable capacity nor fragmentation causality.

## Actual execution position

Thirds use zero-based daily actual ExecutionSequence rank: floor(rank × 3 / daily request count). This is a descriptive control, with no order transformation.

| Seed | Scenario | Third | Requests | Participations | Success | Rejection | Bad |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | S2 | first | 2440 | 7384 | 100.000% | 0.000% | 0.000% |
| 1001 | S2 | middle | 2440 | 7289 | 100.000% | 0.000% | 0.000% |
| 1001 | S2 | last | 2440 | 7327 | 100.000% | 0.000% | 27.747% |
| 1001 | S6 | first | 1800 | 7419 | 100.000% | 0.000% | 0.000% |
| 1001 | S6 | middle | 1800 | 7197 | 100.000% | 0.000% | 0.000% |
| 1001 | S6 | last | 1800 | 7384 | 100.000% | 0.000% | 28.562% |
| 2001 | S2 | first | 2440 | 7257 | 100.000% | 0.000% | 0.000% |
| 2001 | S2 | middle | 2440 | 7371 | 100.000% | 0.000% | 0.000% |
| 2001 | S2 | last | 2440 | 7372 | 100.000% | 0.000% | 27.591% |
| 2001 | S6 | first | 1800 | 7322 | 100.000% | 0.000% | 0.000% |
| 2001 | S6 | middle | 1800 | 7314 | 100.000% | 0.000% | 0.000% |
| 2001 | S6 | last | 1800 | 7364 | 100.000% | 0.000% | 28.585% |
| 3001 | S2 | first | 2440 | 7243 | 100.000% | 0.000% | 0.000% |
| 3001 | S2 | middle | 2440 | 7436 | 100.000% | 0.000% | 0.000% |
| 3001 | S2 | last | 2440 | 7321 | 100.000% | 0.000% | 27.947% |
| 3001 | S6 | first | 1800 | 7375 | 100.000% | 0.000% | 0.000% |
| 3001 | S6 | middle | 1800 | 7364 | 100.000% | 0.000% | 0.000% |
| 3001 | S6 | last | 1800 | 7261 | 100.000% | 0.000% | 29.114% |

## Group size × execution position

All cells, including empty/N/A cells, are retained in JSON. The table shows S6 BadOutcomeRate and RejectionRate in each daily third. S2 cells are also retained in JSON for controlled comparison.

| Seed | Size | First Bad/rejection | Middle Bad/rejection | Last Bad/rejection | Last Bad/participations |
| --- | --- | --- | --- | --- | --- |
| 1001 | 1 | 0.000% / 0.000% | 0.000% / 0.000% | 0.000% / 0.000% | 0/175 |
| 1001 | 2 | 0.000% / 0.000% | 0.000% / 0.000% | 8.555% / 0.000% | 58/678 |
| 1001 | 3 | 0.000% / 0.000% | 0.000% / 0.000% | 17.460% / 0.000% | 198/1134 |
| 1001 | 4 | 0.000% / 0.000% | 0.000% / 0.000% | 21.961% / 0.000% | 224/1020 |
| 1001 | 5 | 0.000% / 0.000% | 0.000% / 0.000% | 29.557% / 0.000% | 300/1015 |
| 1001 | 6–7 | 0.000% / 0.000% | 0.000% / 0.000% | 34.857% / 0.000% | 633/1816 |
| 1001 | 8–10 | 0.000% / 0.000% | 0.000% / 0.000% | 45.019% / 0.000% | 696/1546 |
| 2001 | 1 | 0.000% / 0.000% | 0.000% / 0.000% | 0.000% / 0.000% | 0/184 |
| 2001 | 2 | 0.000% / 0.000% | 0.000% / 0.000% | 5.000% / 0.000% | 34/680 |
| 2001 | 3 | 0.000% / 0.000% | 0.000% / 0.000% | 15.040% / 0.000% | 171/1137 |
| 2001 | 4 | 0.000% / 0.000% | 0.000% / 0.000% | 25.758% / 0.000% | 272/1056 |
| 2001 | 5 | 0.000% / 0.000% | 0.000% / 0.000% | 30.220% / 0.000% | 275/910 |
| 2001 | 6–7 | 0.000% / 0.000% | 0.000% / 0.000% | 36.269% / 0.000% | 626/1726 |
| 2001 | 8–10 | 0.000% / 0.000% | 0.000% / 0.000% | 43.507% / 0.000% | 727/1671 |
| 3001 | 1 | 0.000% / 0.000% | 0.000% / 0.000% | 0.000% / 0.000% | 0/189 |
| 3001 | 2 | 0.000% / 0.000% | 0.000% / 0.000% | 1.648% / 0.000% | 12/728 |
| 3001 | 3 | 0.000% / 0.000% | 0.000% / 0.000% | 17.344% / 0.000% | 192/1107 |
| 3001 | 4 | 0.000% / 0.000% | 0.000% / 0.000% | 24.715% / 0.000% | 260/1052 |
| 3001 | 5 | 0.000% / 0.000% | 0.000% / 0.000% | 25.806% / 0.000% | 240/930 |
| 3001 | 6–7 | 0.000% / 0.000% | 0.000% / 0.000% | 36.652% / 0.000% | 578/1577 |
| 3001 | 8–10 | 0.000% / 0.000% | 0.000% / 0.000% | 49.583% / 0.000% | 832/1678 |

## Path fairness and large-group exposure

| Seed | Scenario | Evaluated | No path | Insufficient | Classifiable | Balanced | Acceptable | NotBalanced |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | S2 | 5354 | 0 | 232 | 5122 | 2903 | 1649 | 570 |
| 1001 | S6 | 5377 | 0 | 261 | 5116 | 2866 | 1654 | 596 |
| 2001 | S2 | 5359 | 0 | 244 | 5115 | 2880 | 1682 | 553 |
| 2001 | S6 | 5355 | 0 | 234 | 5121 | 2889 | 1615 | 617 |
| 3001 | S2 | 5344 | 0 | 218 | 5126 | 2890 | 1646 | 590 |
| 3001 | S6 | 5357 | 0 | 237 | 5120 | 2860 | 1634 | 626 |

For S6, each evaluated attendee is assigned to one exposure row by the **number of persisted Assignments in size 6–10 groups**. This does not assign a permanent group size. Rates use classifiable attendees in that row. Exposure count is also affected by total path length, so it is not a causal dose-response measure.

| Seed | Large-group Assignments | Evaluated | Classifiable | NotBalanced | NB rate |
| --- | --- | --- | --- | --- | --- |
| 1001 | 0 | 603 | 456 | 43 | 9.430% |
| 1001 | 1 | 1508 | 1394 | 163 | 11.693% |
| 1001 | 2 | 1686 | 1686 | 190 | 11.269% |
| 1001 | 3 | 1132 | 1132 | 147 | 12.986% |
| 1001 | 4 | 396 | 396 | 50 | 12.626% |
| 1001 | 5 | 52 | 52 | 3 | 5.769% |
| 2001 | 0 | 562 | 432 | 26 | 6.019% |
| 2001 | 1 | 1575 | 1471 | 165 | 11.217% |
| 2001 | 2 | 1627 | 1627 | 211 | 12.969% |
| 2001 | 3 | 1121 | 1121 | 144 | 12.846% |
| 2001 | 4 | 422 | 422 | 64 | 15.166% |
| 2001 | 5 | 48 | 48 | 7 | 14.583% |
| 3001 | 0 | 616 | 474 | 35 | 7.384% |
| 3001 | 1 | 1477 | 1382 | 150 | 10.854% |
| 3001 | 2 | 1677 | 1677 | 201 | 11.986% |
| 3001 | 3 | 1125 | 1125 | 169 | 15.022% |
| 3001 | 4 | 396 | 396 | 59 | 14.899% |
| 3001 | 5 | 66 | 66 | 12 | 18.182% |

Exposure counts do not form a universal monotonic NotBalanced gradient: read each count alongside its classifiable population, especially the small five-assignment category. The size-associated participation rates and attendee-level exposure rates answer different descriptive questions.

## Recovery and episode-opening groups

| Seed | Scenario | Episodes | Eligible | Recovered | Eligible unresolved | Ineligible unresolved |
| --- | --- | --- | --- | --- | --- | --- |
| 1001 | S2 | 1908 | 1447 | 1407 | 40 | 461 |
| 1001 | S6 | 1963 | 1502 | 1455 | 47 | 461 |
| 2001 | S2 | 1900 | 1449 | 1403 | 46 | 451 |
| 2001 | S6 | 1949 | 1482 | 1441 | 41 | 467 |
| 3001 | S2 | 1900 | 1450 | 1401 | 49 | 450 |
| 3001 | S6 | 1954 | 1488 | 1434 | 54 | 466 |

Opening-group attribution uses the stored Bad opening Assignment and its original daily group. It does not attribute later unresolved behavior solely to that group: membership can change. Opening rates below use persisted participations; eligible-unresolved rates use eligible episodes opening in that bucket. Every opening remains one episode, including consecutive Bad outcomes; detection and eligibility are never replayed.

| Seed | Scenario | Opening size | Openings/participations | Opening rate | Eligible openings | Eligible unresolved | Unresolved/eligible |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | S2 | 1 | 2/1100 | 0.182% | 2 | 0 | 0.000% |
| 1001 | S2 | 2 | 207/4390 | 4.715% | 139 | 2 | 1.439% |
| 1001 | S2 | 3 | 414/5490 | 7.541% | 298 | 8 | 2.685% |
| 1001 | S2 | 4 | 392/4400 | 8.909% | 300 | 7 | 2.333% |
| 1001 | S2 | 5 | 345/2925 | 11.795% | 246 | 6 | 2.439% |
| 1001 | S2 | 6–7 | 348/2379 | 14.628% | 294 | 12 | 4.082% |
| 1001 | S2 | 8–10 | 200/1316 | 15.198% | 168 | 5 | 2.976% |
| 1001 | S6 | 1 | 0/540 | 0.000% | 0 | 0 | N/A |
| 1001 | S6 | 2 | 56/2160 | 2.593% | 47 | 4 | 8.511% |
| 1001 | S6 | 3 | 178/3240 | 5.494% | 130 | 3 | 2.308% |
| 1001 | S6 | 4 | 210/3240 | 6.481% | 163 | 3 | 1.840% |
| 1001 | S6 | 5 | 280/2700 | 10.370% | 213 | 6 | 2.817% |
| 1001 | S6 | 6–7 | 575/5287 | 10.876% | 425 | 12 | 2.824% |
| 1001 | S6 | 8–10 | 664/4833 | 13.739% | 524 | 19 | 3.626% |
| 2001 | S2 | 1 | 0/1100 | 0.000% | 0 | 0 | N/A |
| 2001 | S2 | 2 | 272/4390 | 6.196% | 222 | 4 | 1.802% |
| 2001 | S2 | 3 | 454/5490 | 8.270% | 335 | 9 | 2.687% |
| 2001 | S2 | 4 | 408/4400 | 9.273% | 307 | 13 | 4.235% |
| 2001 | S2 | 5 | 297/2925 | 10.154% | 243 | 10 | 4.115% |
| 2001 | S2 | 6–7 | 224/2382 | 9.404% | 176 | 4 | 2.273% |
| 2001 | S2 | 8–10 | 245/1313 | 18.660% | 166 | 6 | 3.614% |
| 2001 | S6 | 1 | 0/540 | 0.000% | 0 | 0 | N/A |
| 2001 | S6 | 2 | 29/2160 | 1.343% | 24 | 0 | 0.000% |
| 2001 | S6 | 3 | 157/3240 | 4.846% | 134 | 5 | 3.731% |
| 2001 | S6 | 4 | 262/3240 | 8.086% | 194 | 8 | 4.124% |
| 2001 | S6 | 5 | 252/2700 | 9.333% | 188 | 2 | 1.064% |
| 2001 | S6 | 6–7 | 577/5277 | 10.934% | 431 | 16 | 3.712% |
| 2001 | S6 | 8–10 | 672/4843 | 13.876% | 511 | 10 | 1.957% |
| 3001 | S2 | 1 | 0/1100 | 0.000% | 0 | 0 | N/A |
| 3001 | S2 | 2 | 223/4390 | 5.080% | 171 | 12 | 7.018% |
| 3001 | S2 | 3 | 536/5490 | 9.763% | 400 | 11 | 2.750% |
| 3001 | S2 | 4 | 412/4400 | 9.364% | 328 | 11 | 3.354% |
| 3001 | S2 | 5 | 263/2925 | 8.991% | 179 | 4 | 2.235% |
| 3001 | S2 | 6–7 | 294/2381 | 12.348% | 219 | 8 | 3.653% |
| 3001 | S2 | 8–10 | 172/1314 | 13.090% | 153 | 3 | 1.961% |
| 3001 | S6 | 1 | 0/540 | 0.000% | 0 | 0 | N/A |
| 3001 | S6 | 2 | 10/2160 | 0.463% | 9 | 0 | 0.000% |
| 3001 | S6 | 3 | 173/3240 | 5.340% | 117 | 4 | 3.419% |
| 3001 | S6 | 4 | 238/3240 | 7.346% | 189 | 10 | 5.291% |
| 3001 | S6 | 5 | 221/2700 | 8.185% | 147 | 5 | 3.401% |
| 3001 | S6 | 6–7 | 548/5271 | 10.397% | 434 | 13 | 2.995% |
| 3001 | S6 | 8–10 | 764/4849 | 15.756% | 592 | 22 | 3.716% |

For a normalized large-group comparison, the next table contrasts their share of all persisted participations, episode openings, eligible openings and eligible-unresolved episodes. An excess share of openings shows concentration of initial Bad episodes; it does not itself demonstrate slower recovery after an opening.

| Seed | 6–10 share of participations | 6–10 share of openings | 6–10 share of eligible openings | 6–10 share of eligible unresolved |
| --- | --- | --- | --- | --- |
| 1001 | 46.000% | 63.118% | 63.182% | 65.957% |
| 2001 | 46.000% | 64.084% | 63.563% | 63.415% |
| 3001 | 46.000% | 67.144% | 68.952% | 64.815% |

Large groups are disproportionately represented in Bad episode openings relative to their participation share. Eligible-unresolved concentration must also be compared with eligible openings; the bucket-level unresolved rates do not show a consistent monotonic size penalty across Seeds.

## Daily capacity evolution

Vectors are remaining **Good / Medium / Bad / total** Spots after each drained daily plan. Totals are global for that FestivalDay. Identical total residuals can conceal different quality consumption or block geometry.

| Seed | Day | S2 G/M/B/total | S6 G/M/B/total | Delta G/M/B/total |
| --- | --- | --- | --- | --- |
| 1001 | 1 | 7 / 7 / 584 / 598 | 18 / 17 / 563 / 598 | 11 / 10 / -21 / 0 |
| 1001 | 2 | 4 / 0 / 594 / 598 | 13 / 15 / 570 / 598 | 9 / 15 / -24 / 0 |
| 1001 | 3 | 1 / 2 / 595 / 598 | 6 / 8 / 584 / 598 | 5 / 6 / -11 / 0 |
| 1001 | 4 | 8 / 7 / 583 / 598 | 8 / 10 / 580 / 598 | 0 / 3 / -3 / 0 |
| 1001 | 5 | 4 / 3 / 591 / 598 | 11 / 13 / 574 / 598 | 7 / 10 / -17 / 0 |
| 2001 | 1 | 5 / 4 / 589 / 598 | 12 / 12 / 574 / 598 | 7 / 8 / -15 / 0 |
| 2001 | 2 | 2 / 0 / 596 / 598 | 7 / 17 / 574 / 598 | 5 / 17 / -22 / 0 |
| 2001 | 3 | 4 / 4 / 590 / 598 | 7 / 12 / 579 / 598 | 3 / 8 / -11 / 0 |
| 2001 | 4 | 6 / 6 / 586 / 598 | 8 / 13 / 577 / 598 | 2 / 7 / -9 / 0 |
| 2001 | 5 | 5 / 8 / 585 / 598 | 14 / 13 / 571 / 598 | 9 / 5 / -14 / 0 |
| 3001 | 1 | 1 / 1 / 596 / 598 | 12 / 11 / 575 / 598 | 11 / 10 / -21 / 0 |
| 3001 | 2 | 8 / 8 / 582 / 598 | 10 / 15 / 573 / 598 | 2 / 7 / -9 / 0 |
| 3001 | 3 | 8 / 8 / 582 / 598 | 8 / 17 / 573 / 598 | 0 / 9 / -9 / 0 |
| 3001 | 4 | 7 / 7 / 584 / 598 | 15 / 15 / 568 / 598 | 8 / 8 / -16 / 0 |
| 3001 | 5 | 4 / 4 / 590 / 598 | 10 / 11 / 577 / 598 | 6 / 7 / -13 / 0 |

The existing evidence does not include per-request quality-specific inventory snapshots or block geometry. Exact Good/Medium feasibility-exhaustion times and unusable residual capacity therefore remain unestablished. Observed first Bad and last Good/Medium positions below describe consumption timing; last assignment is not an exhaustion proof.

| Seed | Scenario | Day | Daily requests | First Bad rank | Last Good rank | Last Medium rank |
| --- | --- | --- | --- | --- | --- | --- |
| 1001 | S2 | 1 | 1464 | 1281 | 1462 | 1459 |
| 1001 | S2 | 2 | 1464 | 1260 | 1461 | 1458 |
| 1001 | S2 | 3 | 1464 | 1226 | 1463 | 1449 |
| 1001 | S2 | 4 | 1464 | 1193 | 1462 | 1463 |
| 1001 | S2 | 5 | 1464 | 1263 | 1444 | 1455 |
| 1001 | S6 | 1 | 1080 | 902 | 1076 | 1077 |
| 1001 | S6 | 2 | 1080 | 878 | 1077 | 1075 |
| 1001 | S6 | 3 | 1080 | 900 | 1060 | 1047 |
| 1001 | S6 | 4 | 1080 | 927 | 1043 | 1057 |
| 1001 | S6 | 5 | 1080 | 930 | 1067 | 1079 |
| 2001 | S2 | 1 | 1464 | 1270 | 1459 | 1461 |
| 2001 | S2 | 2 | 1464 | 1294 | 1443 | 1452 |
| 2001 | S2 | 3 | 1464 | 1255 | 1461 | 1458 |
| 2001 | S2 | 4 | 1464 | 1286 | 1463 | 1446 |
| 2001 | S2 | 5 | 1464 | 1218 | 1446 | 1458 |
| 2001 | S6 | 1 | 1080 | 901 | 1079 | 1071 |
| 2001 | S6 | 2 | 1080 | 889 | 1074 | 1079 |
| 2001 | S6 | 3 | 1080 | 891 | 1053 | 1068 |
| 2001 | S6 | 4 | 1080 | 892 | 1061 | 1076 |
| 2001 | S6 | 5 | 1080 | 950 | 1078 | 1079 |
| 3001 | S2 | 1 | 1464 | 1239 | 1459 | 1450 |
| 3001 | S2 | 2 | 1464 | 1251 | 1463 | 1454 |
| 3001 | S2 | 3 | 1464 | 1262 | 1461 | 1459 |
| 3001 | S2 | 4 | 1464 | 1234 | 1456 | 1451 |
| 3001 | S2 | 5 | 1464 | 1252 | 1406 | 1459 |
| 3001 | S6 | 1 | 1080 | 903 | 1068 | 1073 |
| 3001 | S6 | 2 | 1080 | 891 | 1072 | 1046 |
| 3001 | S6 | 3 | 1080 | 883 | 1073 | 1060 |
| 3001 | S6 | 4 | 1080 | 892 | 1079 | 1074 |
| 3001 | S6 | 5 | 1080 | 914 | 1070 | 1068 |

## Reproduction

| Logical component | Equivalent |
| --- | --- |
| manifest | True |
| attempts | True |
| attendee-paths | True |
| recovery-observations | True |
| metrics | True |
| definition | True |
| checkpoints | True |
| runMetadataExceptRuntime | True |

| Byte-identical file | Equal SHA-256 |
| --- | --- |
| definition.json | True |
| manifest.json | True |
| checkpoints.json | True |
| metrics.json | True |

RunId and observed start/end/duration; generated durable request/Assignment IDs normalized by stable simulation-request and attendee/FestivalDay identities. All other logical fields must match. Reproduction is excluded from cross-Seed conclusions.

## Exploratory performance observation

Nearest-rank percentiles over existing exported duration values, in milliseconds. These include ordinary runtime effects and have no dedicated warm-up or controlled load. They are **exploratory observations, not a dedicated performance benchmark or production-performance evidence**.

| Seed | Scenario | Samples | p50 ms | p95 ms | p99 ms |
| --- | --- | --- | --- | --- | --- |
| 1001 | S2 | 7320 | 26.723 | 44.737 | 61.873 |
| 1001 | S6 | 5400 | 28.677 | 52.254 | 80.343 |
| 2001 | S2 | 7320 | 27.194 | 44.616 | 62.459 |
| 2001 | S6 | 5400 | 27.550 | 49.193 | 73.951 |
| 3001 | S2 | 7320 | 26.312 | 42.759 | 50.225 |
| 3001 | S6 | 5400 | 29.144 | 50.646 | 82.748 |

## Reproducing the derived reports

`python3 -B scripts/stage-5-representative-runs/analyze_s6_groups.py artifacts/simulations/s6-large-group-sensitivity-20261106-7e3855c30230448893bfeb561b8be700/index.json --output artifacts/analysis/s2-s6-repeat.json --markdown artifacts/analysis/s2-s6-repeat.md`

Choose fresh output names. Analysis verifies original hashes before and after reading. The JSON includes all per-Seed counts, denominators, cross-tabs, integrity audits, rejection events and provenance. Repeated derived JSON and Markdown are checked byte-for-byte. No wall-clock analysis timestamps are added.

## Interpretation boundaries and scope

These three analytical Seeds support sensitivity conclusions under provisional S2 operating conditions. They do not establish real festival distributions, all possible group mixes, fragmentation causality, concurrency behavior or production performance. Descriptive size/exposure associations retain confounding from exact rank, eligibility and history. No new materiality thresholds, Engine tuning or policy recommendation is implied by a metric worsening. Engine v1, fairness, S2/S5/S6 configuration, rubric and recovery are unchanged; S7/S8 and Decision Gate logic remain deferred.
