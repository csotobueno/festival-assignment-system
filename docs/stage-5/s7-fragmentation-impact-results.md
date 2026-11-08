# S7 fragmentation impact results

Execution/analysis date: **2026-11-07 (America/Lima)**. Runtime timestamps and directory dates use UTC. Simulated first date: **2026-08-15**, a fixture, not a confirmed festival date.

## Main finding

1001: 7.732% → 14.959%, +7.227 pp; 2001: 7.910% → 14.959%, +7.049 pp; 3001: 7.992% → 14.959%, +6.967 pp. Cross-Seed min/mean/max: 6.967 / 7.081 / 7.227 pp. Three deterministic Seeds describe variation; they do not estimate production occurrence probability.

Exact paired requests include 2918 Control successes that reject in Fragmented despite sufficient eligible capacity and no feasible block. Fragmented's larger residual inventory and changed quality/fairness populations must be read alongside that feasibility loss; these are results for the tested synthetic spatial pattern.

## Experimental answers

1. **How much does Fragmented increase RejectionRate?** 1001: 7.732% → 14.959%, +7.227 pp; 2001: 7.910% → 14.959%, +7.049 pp; 3001: 7.992% → 14.959%, +6.967 pp. Cross-Seed min/mean/max: 6.967 / 7.081 / 7.227 pp. Three deterministic Seeds describe variation; they do not estimate production occurrence probability.

2. **What proportion is ordinary capacity shortage?** Control / Fragmented: 1001: 268 (47.350%) / 0 (0.000%); 2001: 282 (48.705%) / 0 (0.000%); 3001: 245 (41.880%) / 0 (0.000%). Control has the same opening capacity pressure as Fragmented, but its rejections are not all capacity shortage either.

3. **What proportion is no contiguous block despite sufficient eligible capacity?** Control / Fragmented: 1001: 298 (52.650%) / 1095 (100.000%); 2001: 297 (51.295%) / 1095 (100.000%); 3001: 340 (58.120%) / 1095 (100.000%). These diagnoses require captured eligible capacity and a false block-found observation, not just the rejection code.

4. **Which group sizes are most affected?** 1001: 1: -2.909 pp, 2: -6.606 pp, 3: -8.306 pp, 4: -9.182 pp, 5: +88.376 pp, 6–7: +86.301 pp, 8–10: +87.586 pp; 2001: 1: -2.636 pp, 2: -7.745 pp, 3: -8.415 pp, 4: -9.727 pp, 5: +89.744 pp, 6–7: +91.507 pp, 8–10: +80.690 pp; 3001: 1: -1.818 pp, 2: -7.517 pp, 3: -10.164 pp, 4: -9.273 pp, 5: +90.940 pp, 6–7: +88.493 pp, 8–10: +88.276 pp. The observed shape is a step: sizes 1–4 have zero Fragmented rejection, sizes 5+ have 100%. Every opening largest available run is four; immutable occupancy cannot create a longer run later. This is specific to the tested pattern, not a universal group-size threshold.

5. **Does fragmentation cause rejection earlier?** 1001: Control first rank 1302 vs Fragmented 7; 2001: Control first rank 1299 vs Fragmented 9; 3001: Control first rank 1307 vs Fragmented 14. Daily thirds and all five daily first events are reported below. Early effects coexist with later natural inventory consumption.

6. **How much capacity remains at fragmentation-related rejection?** 1001: Control / Fragmented median total 7.0 / 2491, median eligible 7.0 / 2252; 2001: Control / Fragmented median total 8 / 2522, median eligible 8 / 2241; 3001: Control / Fragmented median total 7.0 / 2489, median eligible 7.0 / 2254. Min/median/max and missing counts are retained by cause. Total is reconstructed from 3,998 minus confirmed prior successes.

7. **Does Fragmented leave more unused capacity?** 1001: mean end-of-day remaining Control / Fragmented 0.0 / 922.0; 2001: mean end-of-day remaining Control / Fragmented 0.0 / 922.0; 3001: mean end-of-day remaining Control / Fragmented 0.0 / 922.0. The daily table separates residual total and quality inventory; more unused capacity with more rejection indicates lower spatial usability under this pattern.

8. **Does quality change among successfully assigned attendees?** 1001: Good/Medium/Bad deltas +8.281 pp / +10.582 pp / -18.863 pp; 2001: Good/Medium/Bad deltas +8.066 pp / +10.641 pp / -18.707 pp; 3001: Good/Medium/Bad deltas +8.138 pp / +10.953 pp / -19.091 pp. These are measured changes among persisted Assignments, with selection from rejection and changing history; no new materiality threshold is imposed.

9. **Does path fairness change or mainly participation coverage?** 1001: classifiable Control / Fragmented 5026 / 4552, NotBalanced delta -25.473 pp; 2001: classifiable Control / Fragmented 5026 / 4550, NotBalanced delta -24.860 pp; 3001: classifiable Control / Fragmented 5039 / 4553, NotBalanced delta -25.244 pp. Both the observed path-rate distributions and coverage change. Rates reflect altered successful-assignment composition and natural histories, not an independently established overall fairness improvement. Rejected attempts add no synthetic Bad or recovery opportunity.

10. **Does exact request pairing support fragmentation-induced infeasibility?** 1001: 959 Control-success/Fragmented-rejected requests with sufficient eligible capacity/no block; 2001: 976 Control-success/Fragmented-rejected requests with sufficient eligible capacity/no block; 3001: 983 Control-success/Fragmented-rejected requests with sufficient eligible capacity/no block. These are paired evidence of fragmentation-induced infeasibility under the tested setup. Reverse transitions remain visible; five-day effects include naturally diverging history/inventory.

11. **Should the later Decision Gate consider fragmentation mitigation?** Yes: the paired sufficient-capacity/no-block cases support considering fragmentation risk at the later Decision Gate. This is evidence for a later evaluation, not a final gate verdict, selected mitigation or authorization to change Engine v1.

## Execution matrix

| Variant | Seed | Role | RunId | Evidence path |
| --- | --- | --- | --- | --- |
| s7-control | 1001 | official | `official-s7-control-1001-ef7efbae9150480d9a38cea86cc921cb` | `artifacts/simulations/s7-control/official-s7-control-1001-ef7efbae9150480d9a38cea86cc921cb/` |
| s7-fragmented | 1001 | official | `official-s7-fragmented-1001-9ab3c9954481466bb2fd024a084acee5` | `artifacts/simulations/s7-fragmented/official-s7-fragmented-1001-9ab3c9954481466bb2fd024a084acee5/` |
| s7-control | 2001 | official | `official-s7-control-2001-3b140c9f06504104a2e403b7522a1e60` | `artifacts/simulations/s7-control/official-s7-control-2001-3b140c9f06504104a2e403b7522a1e60/` |
| s7-fragmented | 2001 | official | `official-s7-fragmented-2001-a679a3ed34cb42908ce12385ad90fbab` | `artifacts/simulations/s7-fragmented/official-s7-fragmented-2001-a679a3ed34cb42908ce12385ad90fbab/` |
| s7-control | 3001 | official | `official-s7-control-3001-0d972ff09a964da087b378916103694a` | `artifacts/simulations/s7-control/official-s7-control-3001-0d972ff09a964da087b378916103694a/` |
| s7-fragmented | 3001 | official | `official-s7-fragmented-3001-9ca831c2a0bb4ce39d780c15dd259c69` | `artifacts/simulations/s7-fragmented/official-s7-fragmented-3001-9ca831c2a0bb4ce39d780c15dd259c69/` |
| s7-control | 1001 | reproduction | `reproduction-s7-control-1001-11db7237164140da847a067c86589cff` | `artifacts/simulations/s7-control/reproduction-s7-control-1001-11db7237164140da847a067c86589cff/` |
| s7-fragmented | 1001 | reproduction | `reproduction-s7-fragmented-1001-8e44b550cee2485d8e497914f9a6957f` | `artifacts/simulations/s7-fragmented/reproduction-s7-fragmented-1001-8e44b550cee2485d8e497914f9a6957f/` |

Set index: `artifacts/simulations/s7-fragmentation-impact-20261108-8dddd92bd46a48269452998b0dfbd27a/index.json`. SHA-256: `f33bc18d0116bc4267186d3dd9fb59a9729bf9c49cbde41597f2b1dc3b8da47f`. All eight package hashes per run are in the adjacent JSON. Raw packages remain ignored and local; back them up before deleting the checkout.

## Pair and opening integrity

Every same-Seed pair passes exact definition, attendee/day catalog, daily cohort, request identity, ordered membership, size, eligibility, RequestedAt/AssignedAt, baseline/execution sequence, topology and Spot-catalog equality. Supporting identities/requests are also paired; only assigned Spot positions differ. All pairs use normal sequential S2 order, generator v2, Engine v0.3.0, rubric v1 and concurrency 1. No S2 control was executed.

Every daily opening has 4,998 total / 1,000 occupied / 3,998 available Spots, remaining Good 1,402 / Medium 1,800 / Bad 796, identical Row/Zone quotas and `s7-preoccupation-v1`. The 20% stress hypothesis **requires stakeholder validation**. Setup identities are disjoint and day-scoped; evaluated history accumulates naturally.

| Seed | Rows | Control sum largest runs | Fragmented sum | Control min/max | Fragmented min/max |
| --- | --- | --- | --- | --- | --- |
| 1001 | 82 | 3998 | 328 | 29/71 | 4/4 |
| 2001 | 82 | 3998 | 328 | 29/71 | 4/4 |
| 3001 | 82 | 3998 | 328 | 29/71 | 4/4 |

## Run integrity and primary comparison

Success/RejectionRate denominator: NormallyProcessedRequests = Successful + DomainRejected. FragmentationImpact = RejectionRate(Fragmented) − RejectionRate(Control), in percentage points. Relative increase divides the rate delta by Control's rate (N/A when zero). Conflicts/technical failures use AttemptedRequests. All category counts, exclusions and final-state findings remain visible. DomainRejected is expected experimental evidence.

| Seed | Variant | Attempted | Successful | Rejected | Conflict | Technical | Excluded | Findings | Success | Rejection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | control | 7320 | 6754 | 566 | 0 | 0 | 0 | 0 | 92.268% | 7.732% |
| 1001 | fragmented | 7320 | 6225 | 1095 | 0 | 0 | 0 | 0 | 85.041% | 14.959% |
| 2001 | control | 7320 | 6741 | 579 | 0 | 0 | 0 | 0 | 92.090% | 7.910% |
| 2001 | fragmented | 7320 | 6225 | 1095 | 0 | 0 | 0 | 0 | 85.041% | 14.959% |
| 3001 | control | 7320 | 6735 | 585 | 0 | 0 | 0 | 0 | 92.008% | 7.992% |
| 3001 | fragmented | 7320 | 6225 | 1095 | 0 | 0 | 0 | 0 | 85.041% | 14.959% |

| Seed | Control rejection | Fragmented rejection | Delta pp | Relative increase |
| --- | --- | --- | --- | --- |
| 1001 | 7.732% | 14.959% | +7.227 | 93.463% |
| 2001 | 7.910% | 14.959% | +7.049 | 89.119% |
| 3001 | 7.992% | 14.959% | +6.967 | 87.179% |

Final-state audits cover unique Spot/day and Attendee/day, valid setup and complete contiguous same-Zone evaluated results, immutable catalog identity, request terminal consistency, dispatch and checkpoints. They are not a complete temporal proof of INV-06. Supporting rows are counted separately; five opening states are checked against persisted day-specific inventory. Reproduction runs also have zero conflicts, technical failures and findings; their package counts are retained in JSON.

## Rejection decomposition and strong evidence

CapacityShortage requires eligible capacity below GroupSize with a no-block observation. ContiguousBlockUnavailable requires eligible capacity at least GroupSize and no block. Unobserved/other cases remain N/A. The existing rejection code alone does not distinguish these causes.

| Seed | Variant | Cause | Count | % rejected | % attempted |
| --- | --- | --- | --- | --- | --- |
| 1001 | control | CapacityShortage | 268 | 47.350% | 3.661% |
| 1001 | control | ContiguousBlockUnavailable | 298 | 52.650% | 4.071% |
| 1001 | control | N/A | 0 | 0.000% | 0.000% |
| 1001 | fragmented | CapacityShortage | 0 | 0.000% | 0.000% |
| 1001 | fragmented | ContiguousBlockUnavailable | 1095 | 100.000% | 14.959% |
| 1001 | fragmented | N/A | 0 | 0.000% | 0.000% |
| 2001 | control | CapacityShortage | 282 | 48.705% | 3.852% |
| 2001 | control | ContiguousBlockUnavailable | 297 | 51.295% | 4.057% |
| 2001 | control | N/A | 0 | 0.000% | 0.000% |
| 2001 | fragmented | CapacityShortage | 0 | 0.000% | 0.000% |
| 2001 | fragmented | ContiguousBlockUnavailable | 1095 | 100.000% | 14.959% |
| 2001 | fragmented | N/A | 0 | 0.000% | 0.000% |
| 3001 | control | CapacityShortage | 245 | 41.880% | 3.347% |
| 3001 | control | ContiguousBlockUnavailable | 340 | 58.120% | 4.645% |
| 3001 | control | N/A | 0 | 0.000% | 0.000% |
| 3001 | fragmented | CapacityShortage | 0 | 0.000% | 0.000% |
| 3001 | fragmented | ContiguousBlockUnavailable | 1095 | 100.000% | 14.959% |
| 3001 | fragmented | N/A | 0 | 0.000% | 0.000% |

| Seed | Strong-evidence count delta | % rejected delta | % attempted delta |
| --- | --- | --- | --- |
| 1001 | 797 | +47.350 pp | +10.888 pp |
| 2001 | 798 | +48.705 pp | +10.902 pp |
| 3001 | 755 | +41.880 pp | +10.314 pp |

## Paired request outcomes and incremental harm

| Seed | Both success | Both rejected | Control success/F rejected | Control rejected/F success | Added harm: shortage/block/N/A | Reverse: shortage/block/N/A |
| --- | --- | --- | --- | --- | --- | --- |
| 1001 | 5795 | 136 | 959 | 430 | 0 / 959 / 0 | 206 / 224 / 0 |
| 2001 | 5765 | 119 | 976 | 460 | 0 / 976 / 0 | 223 / 237 / 0 |
| 3001 | 5752 | 112 | 983 | 473 | 0 / 983 / 0 | 189 / 284 / 0 |

Additional net rejection and individual added harm are distinct. The net equals forward minus reverse transitions. Only the forward sufficient-eligible-capacity/no-block subset is labelled paired fragmentation-induced infeasibility; total delta is an effect of the spatial intervention with natural subsequent history/inventory divergence.

## Group-size degradation and cause cross-tab

Each group counts once. Rates use the normally processed requests in that bucket; quality outcomes count persisted attendee Assignments. Empty denominators are N/A.

| Seed | Variant | Size | Requests | Successful | Rejected | Success | Rejection | Shortage | Block unavailable | N/A |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | control | 1 | 1100 | 1068 | 32 | 97.091% | 2.909% | 32 | 0 | 0 |
| 1001 | control | 2 | 2195 | 2050 | 145 | 93.394% | 6.606% | 53 | 92 | 0 |
| 1001 | control | 3 | 1830 | 1678 | 152 | 91.694% | 8.306% | 71 | 81 | 0 |
| 1001 | control | 4 | 1100 | 999 | 101 | 90.818% | 9.182% | 50 | 51 | 0 |
| 1001 | control | 5 | 585 | 517 | 68 | 88.376% | 11.624% | 34 | 34 | 0 |
| 1001 | control | 6–7 | 365 | 315 | 50 | 86.301% | 13.699% | 20 | 30 | 0 |
| 1001 | control | 8–10 | 145 | 127 | 18 | 87.586% | 12.414% | 8 | 10 | 0 |
| 1001 | fragmented | 1 | 1100 | 1100 | 0 | 100.000% | 0.000% | 0 | 0 | 0 |
| 1001 | fragmented | 2 | 2195 | 2195 | 0 | 100.000% | 0.000% | 0 | 0 | 0 |
| 1001 | fragmented | 3 | 1830 | 1830 | 0 | 100.000% | 0.000% | 0 | 0 | 0 |
| 1001 | fragmented | 4 | 1100 | 1100 | 0 | 100.000% | 0.000% | 0 | 0 | 0 |
| 1001 | fragmented | 5 | 585 | 0 | 585 | 0.000% | 100.000% | 0 | 585 | 0 |
| 1001 | fragmented | 6–7 | 365 | 0 | 365 | 0.000% | 100.000% | 0 | 365 | 0 |
| 1001 | fragmented | 8–10 | 145 | 0 | 145 | 0.000% | 100.000% | 0 | 145 | 0 |
| 2001 | control | 1 | 1100 | 1071 | 29 | 97.364% | 2.636% | 29 | 0 | 0 |
| 2001 | control | 2 | 2195 | 2025 | 170 | 92.255% | 7.745% | 77 | 93 | 0 |
| 2001 | control | 3 | 1830 | 1676 | 154 | 91.585% | 8.415% | 73 | 81 | 0 |
| 2001 | control | 4 | 1100 | 993 | 107 | 90.273% | 9.727% | 44 | 63 | 0 |
| 2001 | control | 5 | 585 | 525 | 60 | 89.744% | 10.256% | 25 | 35 | 0 |
| 2001 | control | 6–7 | 365 | 334 | 31 | 91.507% | 8.493% | 18 | 13 | 0 |
| 2001 | control | 8–10 | 145 | 117 | 28 | 80.690% | 19.310% | 16 | 12 | 0 |
| 2001 | fragmented | 1 | 1100 | 1100 | 0 | 100.000% | 0.000% | 0 | 0 | 0 |
| 2001 | fragmented | 2 | 2195 | 2195 | 0 | 100.000% | 0.000% | 0 | 0 | 0 |
| 2001 | fragmented | 3 | 1830 | 1830 | 0 | 100.000% | 0.000% | 0 | 0 | 0 |
| 2001 | fragmented | 4 | 1100 | 1100 | 0 | 100.000% | 0.000% | 0 | 0 | 0 |
| 2001 | fragmented | 5 | 585 | 0 | 585 | 0.000% | 100.000% | 0 | 585 | 0 |
| 2001 | fragmented | 6–7 | 365 | 0 | 365 | 0.000% | 100.000% | 0 | 365 | 0 |
| 2001 | fragmented | 8–10 | 145 | 0 | 145 | 0.000% | 100.000% | 0 | 145 | 0 |
| 3001 | control | 1 | 1100 | 1080 | 20 | 98.182% | 1.818% | 20 | 0 | 0 |
| 3001 | control | 2 | 2195 | 2030 | 165 | 92.483% | 7.517% | 62 | 103 | 0 |
| 3001 | control | 3 | 1830 | 1644 | 186 | 89.836% | 10.164% | 61 | 125 | 0 |
| 3001 | control | 4 | 1100 | 998 | 102 | 90.727% | 9.273% | 46 | 56 | 0 |
| 3001 | control | 5 | 585 | 532 | 53 | 90.940% | 9.060% | 25 | 28 | 0 |
| 3001 | control | 6–7 | 365 | 323 | 42 | 88.493% | 11.507% | 25 | 17 | 0 |
| 3001 | control | 8–10 | 145 | 128 | 17 | 88.276% | 11.724% | 6 | 11 | 0 |
| 3001 | fragmented | 1 | 1100 | 1100 | 0 | 100.000% | 0.000% | 0 | 0 | 0 |
| 3001 | fragmented | 2 | 2195 | 2195 | 0 | 100.000% | 0.000% | 0 | 0 | 0 |
| 3001 | fragmented | 3 | 1830 | 1830 | 0 | 100.000% | 0.000% | 0 | 0 | 0 |
| 3001 | fragmented | 4 | 1100 | 1100 | 0 | 100.000% | 0.000% | 0 | 0 | 0 |
| 3001 | fragmented | 5 | 585 | 0 | 585 | 0.000% | 100.000% | 0 | 585 | 0 |
| 3001 | fragmented | 6–7 | 365 | 0 | 365 | 0.000% | 100.000% | 0 | 365 | 0 |
| 3001 | fragmented | 8–10 | 145 | 0 | 145 | 0.000% | 100.000% | 0 | 145 | 0 |

## Supplementary sizes

| Seed | Variant | Size | Requests | Success | Rejection |
| --- | --- | --- | --- | --- | --- |
| 1001 | control | 1–2 | 3295 | 94.628% | 5.372% |
| 1001 | control | 3–4 | 2930 | 91.365% | 8.635% |
| 1001 | control | 5+ | 1095 | 87.580% | 12.420% |
| 1001 | fragmented | 1–2 | 3295 | 100.000% | 0.000% |
| 1001 | fragmented | 3–4 | 2930 | 100.000% | 0.000% |
| 1001 | fragmented | 5+ | 1095 | 0.000% | 100.000% |
| 2001 | control | 1–2 | 3295 | 93.961% | 6.039% |
| 2001 | control | 3–4 | 2930 | 91.092% | 8.908% |
| 2001 | control | 5+ | 1095 | 89.132% | 10.868% |
| 2001 | fragmented | 1–2 | 3295 | 100.000% | 0.000% |
| 2001 | fragmented | 3–4 | 2930 | 100.000% | 0.000% |
| 2001 | fragmented | 5+ | 1095 | 0.000% | 100.000% |
| 3001 | control | 1–2 | 3295 | 94.385% | 5.615% |
| 3001 | control | 3–4 | 2930 | 90.171% | 9.829% |
| 3001 | control | 5+ | 1095 | 89.772% | 10.228% |
| 3001 | fragmented | 1–2 | 3295 | 100.000% | 0.000% |
| 3001 | fragmented | 3–4 | 2930 | 100.000% | 0.000% |
| 3001 | fragmented | 5+ | 1095 | 0.000% | 100.000% |

## Daily execution thirds

Thirds use floor(3 × zero-based daily execution rank / daily request count), not global sequence cutoffs. Block-unavailable rate below uses normally processed requests; attempted and rejection denominators are also retained in JSON.

| Seed | Variant | Third | Requests | Success | Rejection | Block unavailable |
| --- | --- | --- | --- | --- | --- | --- |
| 1001 | control | first | 2440 | 100.000% | 0.000% | 0.000% |
| 1001 | control | middle | 2440 | 100.000% | 0.000% | 0.000% |
| 1001 | control | last | 2440 | 76.803% | 23.197% | 12.213% |
| 1001 | fragmented | first | 2440 | 84.713% | 15.287% | 15.287% |
| 1001 | fragmented | middle | 2440 | 85.205% | 14.795% | 14.795% |
| 1001 | fragmented | last | 2440 | 85.205% | 14.795% | 14.795% |
| 2001 | control | first | 2440 | 100.000% | 0.000% | 0.000% |
| 2001 | control | middle | 2440 | 100.000% | 0.000% | 0.000% |
| 2001 | control | last | 2440 | 76.270% | 23.730% | 12.172% |
| 2001 | fragmented | first | 2440 | 85.000% | 15.000% | 15.000% |
| 2001 | fragmented | middle | 2440 | 84.877% | 15.123% | 15.123% |
| 2001 | fragmented | last | 2440 | 85.246% | 14.754% | 14.754% |
| 3001 | control | first | 2440 | 100.000% | 0.000% | 0.000% |
| 3001 | control | middle | 2440 | 100.000% | 0.000% | 0.000% |
| 3001 | control | last | 2440 | 76.025% | 23.975% | 13.934% |
| 3001 | fragmented | first | 2440 | 85.205% | 14.795% | 14.795% |
| 3001 | fragmented | middle | 2440 | 84.344% | 15.656% | 15.656% |
| 3001 | fragmented | last | 2440 | 85.574% | 14.426% | 14.426% |

## Size × execution third

| Seed | Variant | Size/third | Requests | Rejection | Block unavailable |
| --- | --- | --- | --- | --- | --- |
| 1001 | control | 1/first | 377 | 0.000% | 0.000% |
| 1001 | control | 1/middle | 350 | 0.000% | 0.000% |
| 1001 | control | 1/last | 373 | 8.579% | 0.000% |
| 1001 | control | 2/first | 716 | 0.000% | 0.000% |
| 1001 | control | 2/middle | 772 | 0.000% | 0.000% |
| 1001 | control | 2/last | 707 | 20.509% | 13.013% |
| 1001 | control | 3/first | 611 | 0.000% | 0.000% |
| 1001 | control | 3/middle | 592 | 0.000% | 0.000% |
| 1001 | control | 3/last | 627 | 24.242% | 12.919% |
| 1001 | control | 4/first | 363 | 0.000% | 0.000% |
| 1001 | control | 4/middle | 365 | 0.000% | 0.000% |
| 1001 | control | 4/last | 372 | 27.151% | 13.710% |
| 1001 | control | 5/first | 199 | 0.000% | 0.000% |
| 1001 | control | 5/middle | 201 | 0.000% | 0.000% |
| 1001 | control | 5/last | 185 | 36.757% | 18.378% |
| 1001 | control | 6–7/first | 111 | 0.000% | 0.000% |
| 1001 | control | 6–7/middle | 119 | 0.000% | 0.000% |
| 1001 | control | 6–7/last | 135 | 37.037% | 22.222% |
| 1001 | control | 8–10/first | 63 | 0.000% | 0.000% |
| 1001 | control | 8–10/middle | 41 | 0.000% | 0.000% |
| 1001 | control | 8–10/last | 41 | 43.902% | 24.390% |
| 1001 | fragmented | 1/first | 377 | 0.000% | 0.000% |
| 1001 | fragmented | 1/middle | 350 | 0.000% | 0.000% |
| 1001 | fragmented | 1/last | 373 | 0.000% | 0.000% |
| 1001 | fragmented | 2/first | 716 | 0.000% | 0.000% |
| 1001 | fragmented | 2/middle | 772 | 0.000% | 0.000% |
| 1001 | fragmented | 2/last | 707 | 0.000% | 0.000% |
| 1001 | fragmented | 3/first | 611 | 0.000% | 0.000% |
| 1001 | fragmented | 3/middle | 592 | 0.000% | 0.000% |
| 1001 | fragmented | 3/last | 627 | 0.000% | 0.000% |
| 1001 | fragmented | 4/first | 363 | 0.000% | 0.000% |
| 1001 | fragmented | 4/middle | 365 | 0.000% | 0.000% |
| 1001 | fragmented | 4/last | 372 | 0.000% | 0.000% |
| 1001 | fragmented | 5/first | 199 | 100.000% | 100.000% |
| 1001 | fragmented | 5/middle | 201 | 100.000% | 100.000% |
| 1001 | fragmented | 5/last | 185 | 100.000% | 100.000% |
| 1001 | fragmented | 6–7/first | 111 | 100.000% | 100.000% |
| 1001 | fragmented | 6–7/middle | 119 | 100.000% | 100.000% |
| 1001 | fragmented | 6–7/last | 135 | 100.000% | 100.000% |
| 1001 | fragmented | 8–10/first | 63 | 100.000% | 100.000% |
| 1001 | fragmented | 8–10/middle | 41 | 100.000% | 100.000% |
| 1001 | fragmented | 8–10/last | 41 | 100.000% | 100.000% |
| 2001 | control | 1/first | 396 | 0.000% | 0.000% |
| 2001 | control | 1/middle | 343 | 0.000% | 0.000% |
| 2001 | control | 1/last | 361 | 8.033% | 0.000% |
| 2001 | control | 2/first | 745 | 0.000% | 0.000% |
| 2001 | control | 2/middle | 741 | 0.000% | 0.000% |
| 2001 | control | 2/last | 709 | 23.977% | 13.117% |
| 2001 | control | 3/first | 556 | 0.000% | 0.000% |
| 2001 | control | 3/middle | 632 | 0.000% | 0.000% |
| 2001 | control | 3/last | 642 | 23.988% | 12.617% |
| 2001 | control | 4/first | 377 | 0.000% | 0.000% |
| 2001 | control | 4/middle | 355 | 0.000% | 0.000% |
| 2001 | control | 4/last | 368 | 29.076% | 17.120% |
| 2001 | control | 5/first | 200 | 0.000% | 0.000% |
| 2001 | control | 5/middle | 198 | 0.000% | 0.000% |
| 2001 | control | 5/last | 187 | 32.086% | 18.717% |
| 2001 | control | 6–7/first | 125 | 0.000% | 0.000% |
| 2001 | control | 6–7/middle | 121 | 0.000% | 0.000% |
| 2001 | control | 6–7/last | 119 | 26.050% | 10.924% |
| 2001 | control | 8–10/first | 41 | 0.000% | 0.000% |
| 2001 | control | 8–10/middle | 50 | 0.000% | 0.000% |
| 2001 | control | 8–10/last | 54 | 51.852% | 22.222% |
| 2001 | fragmented | 1/first | 396 | 0.000% | 0.000% |
| 2001 | fragmented | 1/middle | 343 | 0.000% | 0.000% |
| 2001 | fragmented | 1/last | 361 | 0.000% | 0.000% |
| 2001 | fragmented | 2/first | 745 | 0.000% | 0.000% |
| 2001 | fragmented | 2/middle | 741 | 0.000% | 0.000% |
| 2001 | fragmented | 2/last | 709 | 0.000% | 0.000% |
| 2001 | fragmented | 3/first | 556 | 0.000% | 0.000% |
| 2001 | fragmented | 3/middle | 632 | 0.000% | 0.000% |
| 2001 | fragmented | 3/last | 642 | 0.000% | 0.000% |
| 2001 | fragmented | 4/first | 377 | 0.000% | 0.000% |
| 2001 | fragmented | 4/middle | 355 | 0.000% | 0.000% |
| 2001 | fragmented | 4/last | 368 | 0.000% | 0.000% |
| 2001 | fragmented | 5/first | 200 | 100.000% | 100.000% |
| 2001 | fragmented | 5/middle | 198 | 100.000% | 100.000% |
| 2001 | fragmented | 5/last | 187 | 100.000% | 100.000% |
| 2001 | fragmented | 6–7/first | 125 | 100.000% | 100.000% |
| 2001 | fragmented | 6–7/middle | 121 | 100.000% | 100.000% |
| 2001 | fragmented | 6–7/last | 119 | 100.000% | 100.000% |
| 2001 | fragmented | 8–10/first | 41 | 100.000% | 100.000% |
| 2001 | fragmented | 8–10/middle | 50 | 100.000% | 100.000% |
| 2001 | fragmented | 8–10/last | 54 | 100.000% | 100.000% |
| 3001 | control | 1/first | 389 | 0.000% | 0.000% |
| 3001 | control | 1/middle | 368 | 0.000% | 0.000% |
| 3001 | control | 1/last | 343 | 5.831% | 0.000% |
| 3001 | control | 2/first | 725 | 0.000% | 0.000% |
| 3001 | control | 2/middle | 726 | 0.000% | 0.000% |
| 3001 | control | 2/last | 744 | 22.177% | 13.844% |
| 3001 | control | 3/first | 591 | 0.000% | 0.000% |
| 3001 | control | 3/middle | 604 | 0.000% | 0.000% |
| 3001 | control | 3/last | 635 | 29.291% | 19.685% |
| 3001 | control | 4/first | 374 | 0.000% | 0.000% |
| 3001 | control | 4/middle | 360 | 0.000% | 0.000% |
| 3001 | control | 4/last | 366 | 27.869% | 15.301% |
| 3001 | control | 5/first | 201 | 0.000% | 0.000% |
| 3001 | control | 5/middle | 195 | 0.000% | 0.000% |
| 3001 | control | 5/last | 189 | 28.042% | 14.815% |
| 3001 | control | 6–7/first | 121 | 0.000% | 0.000% |
| 3001 | control | 6–7/middle | 125 | 0.000% | 0.000% |
| 3001 | control | 6–7/last | 119 | 35.294% | 14.286% |
| 3001 | control | 8–10/first | 39 | 0.000% | 0.000% |
| 3001 | control | 8–10/middle | 62 | 0.000% | 0.000% |
| 3001 | control | 8–10/last | 44 | 38.636% | 25.000% |
| 3001 | fragmented | 1/first | 389 | 0.000% | 0.000% |
| 3001 | fragmented | 1/middle | 368 | 0.000% | 0.000% |
| 3001 | fragmented | 1/last | 343 | 0.000% | 0.000% |
| 3001 | fragmented | 2/first | 725 | 0.000% | 0.000% |
| 3001 | fragmented | 2/middle | 726 | 0.000% | 0.000% |
| 3001 | fragmented | 2/last | 744 | 0.000% | 0.000% |
| 3001 | fragmented | 3/first | 591 | 0.000% | 0.000% |
| 3001 | fragmented | 3/middle | 604 | 0.000% | 0.000% |
| 3001 | fragmented | 3/last | 635 | 0.000% | 0.000% |
| 3001 | fragmented | 4/first | 374 | 0.000% | 0.000% |
| 3001 | fragmented | 4/middle | 360 | 0.000% | 0.000% |
| 3001 | fragmented | 4/last | 366 | 0.000% | 0.000% |
| 3001 | fragmented | 5/first | 201 | 100.000% | 100.000% |
| 3001 | fragmented | 5/middle | 195 | 100.000% | 100.000% |
| 3001 | fragmented | 5/last | 189 | 100.000% | 100.000% |
| 3001 | fragmented | 6–7/first | 121 | 100.000% | 100.000% |
| 3001 | fragmented | 6–7/middle | 125 | 100.000% | 100.000% |
| 3001 | fragmented | 6–7/last | 119 | 100.000% | 100.000% |
| 3001 | fragmented | 8–10/first | 39 | 100.000% | 100.000% |
| 3001 | fragmented | 8–10/middle | 62 | 100.000% | 100.000% |
| 3001 | fragmented | 8–10/last | 44 | 100.000% | 100.000% |

## First rejection in each run

| Seed | Variant | Day | ExecutionSequence | Daily rank | Third | Size | Total left | Eligible left | Cause |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | control | 1 | 1302 | 1302 | last | 10 | 127 | 127 | ContiguousBlockUnavailable |
| 1001 | fragmented | 1 | 7 | 7 | first | 6 | 3987 | 3987 | ContiguousBlockUnavailable |
| 2001 | control | 1 | 1299 | 1299 | last | 7 | 104 | 104 | ContiguousBlockUnavailable |
| 2001 | fragmented | 1 | 9 | 9 | first | 6 | 3979 | 3379 | ContiguousBlockUnavailable |
| 3001 | control | 1 | 1307 | 1307 | last | 9 | 70 | 70 | ContiguousBlockUnavailable |
| 3001 | fragmented | 1 | 14 | 14 | first | 5 | 3958 | 3958 | ContiguousBlockUnavailable |

## Rejection-time capacity

Total capacity is reconstructed from the verified 3,998 opening minus preceding confirmed successful group sizes. Eligible capacity and block-found are captured from the real evaluation. Reconstruction requires complete reconciled sequential evidence; no uncertain commits or external inventory writes occurred. Independent pre-save reads retain the baseline's snapshot-consistency limitation.

| Seed | Variant | Cause | Total min/median/max | Eligible min/median/max | Missing total/eligible |
| --- | --- | --- | --- | --- | --- |
| 1001 | control | CapacityShortage | 0 / 0.0 / 6 | 0 / 0.0 / 6 | 0 / 0 |
| 1001 | control | ContiguousBlockUnavailable | 2 / 7.0 / 127 | 2 / 7.0 / 127 | 0 / 0 |
| 1001 | fragmented | CapacityShortage | N/A / N/A / N/A | N/A / N/A / N/A | 0 / 0 |
| 1001 | fragmented | ContiguousBlockUnavailable | 922 / 2491 / 3996 | 877 / 2252 / 3996 | 0 / 0 |
| 2001 | control | CapacityShortage | 0 / 0.0 / 8 | 0 / 0.0 / 8 | 0 / 0 |
| 2001 | control | ContiguousBlockUnavailable | 2 / 8 / 159 | 2 / 8 / 159 | 0 / 0 |
| 2001 | fragmented | CapacityShortage | N/A / N/A / N/A | N/A / N/A / N/A | 0 / 0 |
| 2001 | fragmented | ContiguousBlockUnavailable | 922 / 2522 / 3998 | 884 / 2241 / 3998 | 0 / 0 |
| 3001 | control | CapacityShortage | 0 / 0 / 6 | 0 / 0 / 6 | 0 / 0 |
| 3001 | control | ContiguousBlockUnavailable | 2 / 7.0 / 159 | 2 / 7.0 / 159 | 0 / 0 |
| 3001 | fragmented | CapacityShortage | N/A / N/A / N/A | N/A / N/A / N/A | 0 / 0 |
| 3001 | fragmented | ContiguousBlockUnavailable | 928 / 2489 / 3998 | 890 / 2254 / 3998 | 0 / 0 |

## Daily capacity utilization and quality evolution

Remaining vectors are Good / Medium / Bad / total. Each day starts with equal quality capacity; closing differences are observed consequences. Assignments below exclude the 1,000 setup occupants. Unused capacity divides closing total by 3,998. First-rejection consumption is 3,998 minus its total-left value.

| Seed | Variant | Day | Successful Assignments | Remaining G/M/B/total | Unused | Used before first rejection | First rejection rank / total / eligible |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | control | 1 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3871 | 1302 / 127 / 127 |
| 1001 | control | 2 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3916 | 1303 / 82 / 82 |
| 1001 | control | 3 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3895 | 1309 / 103 / 103 |
| 1001 | control | 4 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3911 | 1306 / 87 / 86 |
| 1001 | control | 5 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3953 | 1314 / 45 / 45 |
| 1001 | fragmented | 1 | 3076 | 68 / 86 / 768 / 922 | 23.062% | 11 | 7 / 3987 / 3987 |
| 1001 | fragmented | 2 | 3076 | 70 / 96 / 756 / 922 | 23.062% | 12 | 6 / 3986 / 3386 |
| 1001 | fragmented | 3 | 3076 | 73 / 87 / 762 / 922 | 23.062% | 14 | 6 / 3984 / 3984 |
| 1001 | fragmented | 4 | 3076 | 52 / 98 / 772 / 922 | 23.062% | 2 | 2 / 3996 / 3996 |
| 1001 | fragmented | 5 | 3076 | 80 / 81 / 761 / 922 | 23.062% | 8 | 5 / 3990 / 3390 |
| 2001 | control | 1 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3894 | 1299 / 104 / 104 |
| 2001 | control | 2 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3884 | 1294 / 114 / 114 |
| 2001 | control | 3 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3899 | 1297 / 99 / 99 |
| 2001 | control | 4 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3839 | 1286 / 159 / 159 |
| 2001 | control | 5 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3879 | 1298 / 119 / 119 |
| 2001 | fragmented | 1 | 3076 | 77 / 92 / 753 / 922 | 23.062% | 19 | 9 / 3979 / 3379 |
| 2001 | fragmented | 2 | 3076 | 79 / 82 / 761 / 922 | 23.062% | 0 | 1 / 3998 / 3998 |
| 2001 | fragmented | 3 | 3076 | 77 / 82 / 763 / 922 | 23.062% | 7 | 5 / 3991 / 3991 |
| 2001 | fragmented | 4 | 3076 | 65 / 98 / 759 / 922 | 23.062% | 0 | 1 / 3998 / 3398 |
| 2001 | fragmented | 5 | 3076 | 78 / 85 / 759 / 922 | 23.062% | 3 | 3 / 3995 / 3395 |
| 3001 | control | 1 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3928 | 1307 / 70 / 70 |
| 3001 | control | 2 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3918 | 1314 / 80 / 80 |
| 3001 | control | 3 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3861 | 1285 / 137 / 137 |
| 3001 | control | 4 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3883 | 1292 / 115 / 115 |
| 3001 | control | 5 | 3998 | 0 / 0 / 0 / 0 | 0.000% | 3839 | 1278 / 159 / 159 |
| 3001 | fragmented | 1 | 3076 | 63 / 82 / 777 / 922 | 23.062% | 40 | 14 / 3958 / 3958 |
| 3001 | fragmented | 2 | 3076 | 73 / 88 / 761 / 922 | 23.062% | 23 | 10 / 3975 / 3975 |
| 3001 | fragmented | 3 | 3076 | 73 / 71 / 778 / 922 | 23.062% | 0 | 1 / 3998 / 3398 |
| 3001 | fragmented | 4 | 3076 | 78 / 69 / 775 / 922 | 23.062% | 0 | 1 / 3998 / 3998 |
| 3001 | fragmented | 5 | 3076 | 78 / 81 / 763 / 922 | 23.062% | 5 | 3 / 3993 / 3393 |

## Fairness population and coverage

| Seed | Variant | Registered | Requesting | Participating | Only rejected/failed | No path | Insufficient | Classifiable |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | control | 5500 | 5354 | 5331 | 23 | 23 | 305 | 5026 |
| 1001 | fragmented | 5500 | 5354 | 5205 | 149 | 149 | 653 | 4552 |
| 2001 | control | 5500 | 5359 | 5329 | 30 | 30 | 303 | 5026 |
| 2001 | fragmented | 5500 | 5359 | 5195 | 164 | 164 | 645 | 4550 |
| 3001 | control | 5500 | 5344 | 5325 | 19 | 19 | 286 | 5039 |
| 3001 | fragmented | 5500 | 5344 | 5193 | 151 | 151 | 640 | 4553 |

Supporting attendees are excluded. Requesting includes everyone with an evaluated attempt; Participating requires at least one persisted Assignment. NoPath remains visible and unclassified. Rejected attempts generate no synthetic quality or recovery opportunity. Changes among survivors must be read alongside participation and classifiability coverage.

## Quality, path fairness and recovery deltas

Quality denominators are persisted evaluated Assignments; path denominators are classifiable paths; RecoveryRate uses eligible episodes, and speed uses recovered episodes. Stored rubric/recovery outputs are consumed unchanged. Deltas are Fragmented minus Control.

| Seed | Metric | Control | Fragmented | Delta |
| --- | --- | --- | --- | --- |
| 1001 | goodOutcomeRate | 35.068% | 43.349% | +8.281 pp |
| 1001 | mediumOutcomeRate | 45.023% | 55.605% | +10.582 pp |
| 1001 | badOutcomeRate | 19.910% | 1.047% | -18.863 pp |
| 1001 | balancedPathRate | 42.459% | 57.162% | +14.702 pp |
| 1001 | acceptablePathRate | 30.860% | 41.630% | +10.771 pp |
| 1001 | notBalancedPathRate | 26.681% | 1.208% | -25.473 pp |
| 1001 | classifiablePathRate | 93.874% | 85.021% | -8.853 pp |
| 1001 | recoveryEpisodeCount | 3392.000000 | 161.000000 | -3231.000000 |
| 1001 | recoveredEpisodeCount | 2332.000000 | 108.000000 | -2224.000000 |
| 1001 | unresolvedEpisodeCount | 1060.000000 | 53.000000 | -1007.000000 |
| 1001 | recoveryEligibleEpisodeCount | 2511.000000 | 108.000000 | -2403.000000 |
| 1001 | recoveryRate | 92.871% | 100.000% | +7.129 pp |
| 1001 | averageOpportunitiesToRecovery | 1.159091 | 1.000000 | -0.159091 |
| 1001 | medianOpportunitiesToRecovery | 1.000000 | 1.000000 | +0.000000 |
| 2001 | goodOutcomeRate | 35.068% | 43.134% | +8.066 pp |
| 2001 | mediumOutcomeRate | 45.023% | 55.663% | +10.641 pp |
| 2001 | badOutcomeRate | 19.910% | 1.203% | -18.707 pp |
| 2001 | balancedPathRate | 43.275% | 56.835% | +13.560 pp |
| 2001 | acceptablePathRate | 30.283% | 41.582% | +11.300 pp |
| 2001 | notBalancedPathRate | 26.442% | 1.582% | -24.860 pp |
| 2001 | classifiablePathRate | 93.786% | 84.904% | -8.882 pp |
| 2001 | recoveryEpisodeCount | 3420.000000 | 185.000000 | -3235.000000 |
| 2001 | recoveredEpisodeCount | 2356.000000 | 128.000000 | -2228.000000 |
| 2001 | unresolvedEpisodeCount | 1064.000000 | 57.000000 | -1007.000000 |
| 2001 | recoveryEligibleEpisodeCount | 2549.000000 | 128.000000 | -2421.000000 |
| 2001 | recoveryRate | 92.428% | 100.000% | +7.572 pp |
| 2001 | averageOpportunitiesToRecovery | 1.141341 | 1.000000 | -0.141341 |
| 2001 | medianOpportunitiesToRecovery | 1.000000 | 1.000000 | +0.000000 |
| 3001 | goodOutcomeRate | 35.068% | 43.205% | +8.138 pp |
| 3001 | mediumOutcomeRate | 45.023% | 55.975% | +10.953 pp |
| 3001 | badOutcomeRate | 19.910% | 0.819% | -19.091 pp |
| 3001 | balancedPathRate | 43.917% | 55.436% | +11.519 pp |
| 3001 | acceptablePathRate | 29.609% | 43.334% | +13.725 pp |
| 3001 | notBalancedPathRate | 26.474% | 1.230% | -25.244 pp |
| 3001 | classifiablePathRate | 94.293% | 85.198% | -9.094 pp |
| 3001 | recoveryEpisodeCount | 3356.000000 | 125.000000 | -3231.000000 |
| 3001 | recoveredEpisodeCount | 2286.000000 | 77.000000 | -2209.000000 |
| 3001 | unresolvedEpisodeCount | 1070.000000 | 48.000000 | -1022.000000 |
| 3001 | recoveryEligibleEpisodeCount | 2500.000000 | 77.000000 | -2423.000000 |
| 3001 | recoveryRate | 91.440% | 100.000% | +8.560 pp |
| 3001 | averageOpportunitiesToRecovery | 1.156605 | 1.012987 | -0.143618 |
| 3001 | medianOpportunitiesToRecovery | 1.000000 | 1.000000 | +0.000000 |

| Seed | Variant | Persisted outcomes | Classifiable | Episodes | Eligible | Recovered | Eligible unresolved |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | control | 19990 | 5026 | 3392 | 2511 | 2332 | 179 |
| 1001 | fragmented | 15380 | 4552 | 161 | 108 | 108 | 0 |
| 2001 | control | 19990 | 5026 | 3420 | 2549 | 2356 | 193 |
| 2001 | fragmented | 15380 | 4550 | 185 | 128 | 128 | 0 |
| 3001 | control | 19990 | 5039 | 3356 | 2500 | 2286 | 214 |
| 3001 | fragmented | 15380 | 4553 | 125 | 77 | 77 | 0 |

## Rejected-attendee later exposure

| Seed | Variant | Ever rejected | Successfully assigned on a later day | Share |
| --- | --- | --- | --- | --- |
| 1001 | control | 1710 | 1310 | 76.608% |
| 1001 | fragmented | 3998 | 3070 | 76.788% |
| 2001 | control | 1733 | 1331 | 76.803% |
| 2001 | fragmented | 4023 | 3071 | 76.336% |
| 3001 | control | 1724 | 1313 | 76.160% |
| 3001 | fragmented | 3988 | 3015 | 75.602% |

Distinct people are counted once per run. Later success means any successful request on a strictly later day than a rejection. A person can be rejected again afterward; no later success may also reflect no later request. This is descriptive exposure, not a new fairness classification.

## Reproduction and deterministic report replay

| Variant | Logical components equal | Byte-identical definition/manifest/checkpoints/metrics |
| --- | --- | --- |
| s7-control | True | True |
| s7-fragmented | True | True |

Only RunId, observed start/end/duration and generated durable IDs are excluded from logical comparison. Definition/manifest (including setup), checkpoints and metrics require byte equality. The repeated 1001 pair produces the same paired request matrix and harm diagnosis. Reproduction is excluded from cross-Seed statistics. Derived analysis has no wall-clock analysis timestamps and is replayed twice for byte-identical JSON and Markdown.

## Exploratory performance observation

Existing attempt durations, nearest-rank percentiles in milliseconds. These are **exploratory observations, not a dedicated performance benchmark**; serial run order, runtime warm-up and local Docker conditions are uncontrolled. They do not establish production performance.

| Seed | Variant | Samples | p50 ms | p95 ms | p99 ms |
| --- | --- | --- | --- | --- | --- |
| 1001 | control | 7320 | 20.930 | 36.117 | 42.257 |
| 1001 | fragmented | 7320 | 23.998 | 35.170 | 41.839 |
| 2001 | control | 7320 | 21.124 | 37.702 | 57.098 |
| 2001 | fragmented | 7320 | 24.028 | 34.889 | 40.751 |
| 3001 | control | 7320 | 21.110 | 35.452 | 41.346 |
| 3001 | fragmented | 7320 | 23.635 | 34.117 | 39.341 |

## Evidence provenance and replay

Harness SHA-256: `27c5e9a490519cc419aa6963aaa597b3a1135a99a40f6584589279a9bbb54266`. Analyzer SHA-256: `9ae1309d888f47d750d50fa47d277eccc67c18c6b7bdd0756d826e62bf2f973a`. Metadata identifies the unchanged protected source revision; harness/analyzer changes are local task work. Every package checksum is verified before and after analysis.

`python3 -B scripts/stage-5-representative-runs/analyze_s7_fragmentation.py artifacts/simulations/s7-fragmentation-impact-20261108-8dddd92bd46a48269452998b0dfbd27a/index.json --output artifacts/analysis/s7-repeat.json --markdown artifacts/analysis/s7-repeat.md`

Use fresh output paths outside raw evidence. The adjacent JSON retains per-Seed denominators, cause counts/rates, all cross-tabs, daily first events and capacity summaries, population coverage, reproduction and raw checksums.

## Interpretation boundaries and scope

These results concern the tested synthetic 20% pre-occupation pattern under S2 workload and topology. They do not establish real festival pre-occupation behavior, production fragmentation probability, every spatial arrangement, concurrency behavior or production performance. The pattern is a deliberate analytical stress input requiring stakeholder validation. No new materiality threshold or final Decision Gate is imposed. Engine v1, fairness, scoring, selection, rubric, recovery, S2/S5/S6 and S7 configuration/pattern are unchanged; no mitigation or S8 is implemented.

Evidence-supported next task: define the separate S8 concurrency/operational-sensitivity contract before its later implementation/execution, then integrate the remaining independent scenario evidence at the Decision Gate. No such execution or policy change begins here.
