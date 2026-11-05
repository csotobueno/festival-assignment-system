# S2 NotBalanced paths and eligible unresolved recovery

## Observed evidence

Across the three official S2 runs, all persisted Bad outcomes occur in the last
third of daily requests. NotBalanced paths have many different shapes; 62–64%
end in Bad. Eligible unresolved episodes account for only 7–8% of NotBalanced
attendees. Larger groups have higher Bad rates, while FrontStanding exclusion
has no consistent association with the NotBalanced participation rate.

These are descriptive observations from sequential S2, not causal findings or a
Decision Gate verdict. Prioritize **S5 — Adverse Arrival Order** as the next
diagnostic scenario; retain S6 as a supported follow-up and the S7 feasibility
question as an evidence gap.

## Evidence and reproducibility

Only the three official S2 packages and the existing
[results report](s1-s2-representative-results.md) /
[structured results](s1-s2-representative-results.json) supply observations.
No S1, reproduction, regenerated or modified-parameter runs are included.

| Seed | RunId | Evidence directory under `artifacts/simulations/s2-expected-baseline/` |
| --- | --- | --- |
| 1001 | `official-s2-1001-b6c13b403b654deabc2a23302c2a7b5a` | `official-s2-1001-b6c13b403b654deabc2a23302c2a7b5a/` |
| 2001 | `official-s2-2001-38de76b7ac10424295d07ff3272f8333` | `official-s2-2001-38de76b7ac10424295d07ff3272f8333/` |
| 3001 | `official-s2-3001-50a58cfdae484c5d99139d63b0176545` | `official-s2-3001-50a58cfdae484c5d99139d63b0176545/` |

All eight files per package match the official results' SHA-256 checksums.
The source structured-results SHA-256 is
`e54ec777abf20c8ac9b3981d2d47d50b1afb94f8c3797cfff3c33186ec9c3a87`.
The source packages record Engine `v0.3.0`, Generator `v2`, Rubric `v1`,
Sequential execution and ConcurrencyLevel 1.

Run from the repository root, choosing a fresh derived output filename:

```bash
python3 -B scripts/stage-5-representative-runs/analyze_s2_paths.py \
  --output artifacts/analysis/s2-fairness-path-analysis-final.json
python3 -B -m unittest discover -s scripts/stage-5-representative-runs -p 'test_*.py'
```

The standard-library script validates evidence and reconciles populations before
analysis. It fails on missing/checksum-invalid evidence, population mismatches,
broken identity joins or inconsistent recorded outcomes. It never writes raw
packages, refuses output inside the S2 evidence directory, and refuses existing
output files. Output has no generated timestamp or machine-specific absolute path.

The ignored derived JSON contains every NotBalanced attendee's original
PathLength, quality sequence, OverallQuality, DirectionScore, OverallQualityBand,
Direction and classification, plus each Assignment's daily request identity,
membership, group size, eligibility, RequestedAt, execution position and captured
capacity. Every eligible unresolved episode retains its opening Assignment,
opening day/path position, opportunity count, later qualities and final state.
It also contains all distributions, numerators, denominators and stratified
tables used below. Raw packages and this detailed derivative remain uncommitted.

## Method and denominators

Classifications, scores and recovery episodes are consumed as stored; neither
PathClassificationRubric nor recovery detection is rerun. Paths retain their
persisted chronological order. Joins use unique attendee/FestivalDay membership
in the manifest and attempts; membership, day, sequence, selected quality and
persisted timestamp precision are checked. PostgreSQL timestamps have microsecond
precision while manifest timestamps can retain a seventh fractional digit.

A **participation** is one attendee in one daily request. All S2 attempts were
Successful, so all 22,000 participations per run have persisted Assignments.
The request population is 7,320 per run, 1,464 per day. A NotBalanced participation
means a participation of an attendee whose final stored path is NotBalanced;
it does not mean the individual Assignment or request has a rubric classification.

Participation rates use actual category participations as denominators. Distinct
attendee rates count each attendee once within a category; attendees can appear
in several categories and those denominators cannot be added. Path-length rates
use all paths of the same length. Short/absent histories are not extended with
synthetic outcomes. Percentages are rounded, with no significance threshold.

## Population reconciliation

Eligible unresolved count is checked against both the official structured result
and `RecoveryEligibleEpisodeCount - RecoveredEpisodeCount` in exported metrics.

| Seed | NotBalanced expected / analyzed | Eligible unresolved expected / analyzed | Classifiable paths |
| --- | --- | --- | --- |
| 1001 | 570 / 570 | 40 / 40 | 5,122 |
| 2001 | 553 / 553 | 46 / 46 | 5,115 |
| 3001 | 590 / 590 | 49 / 49 | 5,126 |

All reconcile. Pooled counts describe **run-attendee observations**, not unique
people deduplicated across seeds.

## Path shapes and stored rubric outputs

`G`, `M`, `B` denote persisted Good, Medium and Bad. There are 111, 112 and 114
distinct NotBalanced shapes by seed. Each seed's ten most frequent shapes explain
178/570 (31.2%), 173/553 (31.3%) and 184/590 (31.2%) respectively. A small set of
exact shapes therefore does not explain most cases.

The six most frequent pooled shapes are below. Scores are stored values, rounded
to four decimals; every listed shape has the same stored cell across its cases.

| Shape | Counts 1001 / 2001 / 3001 | Pooled count / 1,713 | Length | OverallQuality | DirectionScore | Band / Direction |
| --- | --- | --- | --- | --- | --- | --- |
| M M B | 21 / 21 / 22 | 64 (3.7%) | 3 | -0.3333 | -0.5333 | Low / Negative |
| M B | 27 / 18 / 15 | 60 (3.5%) | 2 | -0.5 | -0.8 | Low / Negative |
| M M G M B | 16 / 20 / 14 | 50 (2.9%) | 5 | 0 | -0.22 | Medium / Negative |
| M M M M B | 17 / 15 / 17 | 49 (2.9%) | 5 | -0.2 | -0.32 | Low / Negative |
| M B M | 12 / 11 / 25 | 48 (2.8%) | 3 | -0.3333 | -0.1333 | Low / Neutral |
| G G B | 21 / 13 / 14 | 48 (2.8%) | 3 | 0.3333 | -0.4667 | Medium / Negative |

The ten pooled leaders explain 499/1,713 (29.1%). Seed-specific leaders differ:
MB (1001), MMBMM (2001), MBM (3001). MMB is common in all three.

| PathLength | 1001: NB / all same-length paths | 2001 | 3001 |
| --- | --- | --- | --- |
| 2 | 44/453 (9.7%) | 40/435 (9.2%) | 28/436 (6.4%) |
| 3 | 115/712 (16.2%) | 109/743 (14.7%) | 137/743 (18.4%) |
| 4 | 129/1,059 (12.2%) | 126/1,028 (12.3%) | 141/1,054 (13.4%) |
| 5 | 282/2,898 (9.7%) | 278/2,909 (9.6%) | 284/2,893 (9.8%) |

Five-entry paths comprise 49.5%, 50.3%, 48.1% of NotBalanced populations, but
three-entry paths have the highest within-length NotBalanced rate in every seed.
The stored OverallQuality range is -1 to 0.3333 and DirectionScore -1 to 0.18 in
each run. Stored rubric cells, without reinterpretation:

| Band / Direction | 1001 | 2001 | 3001 |
| --- | --- | --- | --- |
| Low / Negative | 183 | 164 | 160 |
| Low / Neutral | 174 | 171 | 184 |
| Medium / Negative | 213 | 218 | 246 |

## Bad outcome position

All NotBalanced paths contain a Bad outcome. These analytical flags overlap;
they are not new rubric categories. “Middle” excludes the first and last entry.

| Flag: count (% of NotBalanced paths) | 1001 | 2001 | 3001 |
| --- | --- | --- | --- |
| First entry Bad | 107 (18.8%) | 97 (17.5%) | 107 (18.1%) |
| Any middle entry Bad | 270 (47.4%) | 264 (47.7%) | 268 (45.4%) |
| Last entry Bad | 363 (63.7%) | 344 (62.2%) | 370 (62.7%) |
| At least two Bad entries | 204 (35.8%) | 187 (33.8%) | 188 (31.9%) |

Of final-Bad NotBalanced paths, 282/363, 278/344 and 287/370 end on FestivalDay 5.
The remainder end on Days 2–4: last persisted entry is not necessarily the final
festival day. Final Bad alone is not an additional fairness verdict. There are
323, 298 and 321 final-Bad NotBalanced attendees without an eligible unresolved
episode: their final episode has no later persisted opportunity. Conversely,
207, 209 and 220 NotBalanced paths end in Medium/Good.

## Group size

The seven buckets are the existing S2 definition's buckets. Requests per run
are 1,100 / 2,195 / 1,830 / 1,100 / 585 / 365 / 145 respectively. These request
counts differ substantially from attendee exposure.

| GroupSize | 1001: NB participations / all (%) | 2001 | 3001 |
| --- | --- | --- | --- |
| 1 | 57/1,100 (5.2%) | 72/1,100 (6.5%) | 72/1,100 (6.5%) |
| 2 | 448/4,390 (10.2%) | 406/4,390 (9.2%) | 418/4,390 (9.5%) |
| 3 | 562/5,490 (10.2%) | 554/5,490 (10.1%) | 604/5,490 (11.0%) |
| 4 | 483/4,400 (11.0%) | 490/4,400 (11.1%) | 524/4,400 (11.9%) |
| 5 | 349/2,925 (11.9%) | 326/2,925 (11.1%) | 357/2,925 (12.2%) |
| 6–7 | 294/2,379 (12.4%) | 245/2,382 (10.3%) | 314/2,381 (13.2%) |
| 8–10 | 166/1,316 (12.6%) | 208/1,313 (15.8%) | 162/1,314 (12.3%) |

Distinct-attendee exposure gives the same broad contrast: size 1 has
53/998, 69/1,009, 69/1,020 NotBalanced participants; size 8–10 has
151/1,191, 183/1,186, 147/1,192. These overlapping populations are not permanent
groups. Group size changes across the persisted histories of 561/570, 544/553
and 581/590 NotBalanced attendees.

All-population Bad rates by size also differ: size 2 is 5.1%, 6.6%, 5.6%, versus
15.8%, 20.6%, 14.2% for size 8–10. Within the last daily third, those rates are
15.7%, 20.3%, 16.7% for size 2 versus 56.1%, 55.0%, 47.3% for size 8–10.
Thus the larger-group association remains within that broad timing stratum.
The relationship is not strictly monotonic across intermediate sizes, and the
stratum does not control exact order, eligibility or mixed member histories.

## FrontStanding eligibility

There are exactly 3,660 allowed and 3,660 excluded requests per run. Eligibility
belongs to each request and may change for the same attendee on another day.

| AllowsFrontStanding | 1001: NB participations / all (%) | 2001 | 3001 |
| --- | --- | --- | --- |
| false | 1,177/10,888 (10.8%) | 1,181/10,939 (10.8%) | 1,201/10,868 (11.1%) |
| true | 1,182/11,112 (10.6%) | 1,120/11,061 (10.1%) | 1,250/11,132 (11.2%) |

All-population Bad rates are 10.0%/8.5%, 10.3%/8.2%, 9.3%/9.3% for excluded/
allowed requests' participations. NotBalanced participation differences are
small and reverse in Seed 3001; these data do not show a consistent strong
association with exclusion. Eligibility changes across the histories of
477, 479 and 506 NotBalanced attendees. No causal conclusion is supported.

## FestivalDay, arrival bands and execution position

Sequence thirds use the daily rank: `floor((rank - 1) * 3 / dailyRequestCount)`.
For 1,464 requests/day, ranks 1–488, 489–976 and 977–1,464 are first/middle/last.
The stored Sequence is global across days; it is retained separately and is not
incorrectly treated as a daily rank. Each third has 2,440 requests per run.

| Third | NB participation rates: 1001 / 2001 / 3001 | Bad outcomes / all participations: 1001 / 2001 / 3001 |
| --- | --- | --- |
| First | 8.4% / 8.2% / 8.5% | 0/7,384 / 0/7,257 / 0/7,243 |
| Middle | 7.8% / 7.6% / 8.0% | 0/7,289 / 0/7,371 / 0/7,436 |
| Last | 16.0% / 15.6% / 16.9% | 2,033/7,327 / 2,034/7,372 / 2,046/7,321 |

Every NotBalanced attendee has at least one last-third participation. This does
not mean every participation of those attendees was late or Bad.

RequestedAt uses simulated UTC business time, not observed wall-clock StartedAt.
Arrival bands are the definition's existing half-open intervals, including start
and excluding end; no calendar/time-zone conversion is introduced.

| Arrival band | Requests/run | Bad participation rates: 1001 / 2001 / 3001 | NB participation rate range across seeds |
| --- | --- | --- | --- |
| 07:00–09:00 | 730 | 0% / 0% / 0% | 9.3–11.0% |
| 09:00–12:00 | 2,930 | 0% / 0% / 0% | 7.2–8.0% |
| 12:00–15:00 | 1,100 | 0% / 0% / 0% | 7.5–7.9% |
| 15:00–16:00 | 1,465 | 0.8% / 0.2% / 0.4% | 7.1–8.3% |
| 16:00–18:00 | 510 | 28.3% / 26.1% / 28.7% | 15.2–18.4% |
| 18:00–21:00 | 365 | 83.9% / 89.0% / 87.3% | 32.8–35.3% |
| 21:00–23:00 | 220 | 94.1% / 95.1% / 95.3% | 33.4–37.4% |

Per-day counts below use 4,400 participation exposure per cell. First number is
all NotBalanced participations that day; second is their Bad subset.

| FestivalDay | 1001: NB / NB-Bad | 2001 | 3001 |
| --- | --- | --- | --- |
| 1 | 462 / 99 | 441 / 75 | 491 / 82 |
| 2 | 473 / 115 | 464 / 112 | 499 / 125 |
| 3 | 480 / 140 | 472 / 146 | 511 / 152 |
| 4 | 475 / 155 | 471 / 150 | 488 / 155 |
| 5 | 469 / 282 | 453 / 278 | 462 / 287 |

Daily all-population Bad rates remain approximately 9.1–9.4%; their association
with the final NotBalanced population grows toward Day 5. The population was
selected by the final stored classification, so this is a retrospective pattern.

## Capacity and feasibility evidence

Bad Assignments associated with NotBalanced paths come from 396, 405 and 415
distinct requests. Every such attempt captures `feasibleContiguousBlockFound=true`
and non-null eligible available capacity. Capacity ranges are 598–1,283,
598–1,226 and 598–1,270 Spots respectively. Counts are requests, not attendees.

This supports that a feasible block existed and the persisted selected quality
was Bad. The capacity is a total over eligible qualities. It does **not** show
whether any Good/Medium eligible capacity remained, whether those qualities had
a sufficiently large contiguous block, or whether only Bad blocks were feasible.
No end-of-day checkpoint is used to infer an instantaneous pre-request inventory.
There are no rejected S2 attempts to supply no-block cases.

## Eligible unresolved recovery

All selected episodes retain their stored `Status=Unresolved` and
`RecoveryEligible=true`. Each belongs to a distinct attendee in these runs.
Opening and subsequent entries are checked against the original persisted path.

| ObservedOpportunities | 1001 | 2001 | 3001 |
| --- | --- | --- | --- |
| 1 | 35 (87.5%) | 44 (95.7%) | 45 (91.8%) |
| 2 | 4 (10.0%) | 2 (4.3%) | 4 (8.2%) |
| 3+ | 1 (2.5%; exactly 3) | 0 | 0 |

Later persisted qualities are respectively B, BB or BBB; all final path states
are Bad. This characterizes existing episodes rather than detecting new ones.
Opportunity count is the number of later persisted Assignments, never a
calendar-day distance. The single three-opportunity case occurs only in 1001.

Opening-day cells show unresolved / all eligible episodes opening on that day:

| Opening FestivalDay | 1001 | 2001 | 3001 |
| --- | --- | --- | --- |
| 1 | 2/406 (0.5%) | 1/394 (0.3%) | 2/396 (0.5%) |
| 2 | 2/360 (0.6%) | 4/365 (1.1%) | 6/379 (1.6%) |
| 3 | 12/345 (3.5%) | 5/349 (1.4%) | 7/346 (2.0%) |
| 4 | 24/336 (7.1%) | 36/341 (10.6%) | 34/329 (10.3%) |

Day 4 accounts for 60.0%, 78.3%, 69.4% of eligible unresolved openings. There
are no eligible episodes opening on Day 5. Opening path-position counts for
positions 1/2/3/4 are 2/6/12/20, 6/5/7/28 and 3/7/13/26. Day number and path
position differ when attendance is discontinuous. The shorter observation
horizon near the festival end limits interpretation of opening-day differences.

## Population overlap

| Seed | NB ∩ eligible-unresolved attendees | % of NB | % of eligible-unresolved attendees | NB without eligible unresolved |
| --- | --- | --- | --- | --- |
| 1001 | 40 | 7.0% | 100% | 530 |
| 2001 | 46 | 8.3% | 100% | 507 |
| 3001 | 49 | 8.3% | 100% | 541 |

NotBalanced attendees with at least one **Recovered** episode number 287, 274
and 288. Successful recovery and NotBalanced classification can coexist; recovery
status and trajectory classification measure different things.

## Interpretation and cross-seed stability

The consistent late-request Bad concentration is the clearest common pattern.
Larger groups also face less favorable recorded outcomes, including within the
last third, but intermediate-size rankings vary by seed. NotBalanced path-shape
leaders vary, while final-Bad frequency, dispersed exact shapes and the small
eligible-unresolved overlap are stable. FrontStanding exclusion does not explain
the approximately 11% NotBalanced result consistently.

NotBalanced therefore appears to reflect broader trajectory behavior, including
low/neutral histories and deterioration after better outcomes, rather than mostly
eligible unresolved recovery. Final-Bad histories with no later opportunity are
an observation-horizon limitation, not proof of a new fairness failure. None of
these descriptive comparisons establishes that order, size or eligibility caused
the final classification. Group members share outcomes and individuals recur
across days; the observations are not independent experimental samples.

## Hypothesis for later Stage 5 scenarios

Prioritize **S5 — Adverse Arrival Order** to isolate the sensitivity suggested by
the all-seed concentration of every Bad outcome in the final daily third. The
hypothesis is that arrival position under S2 inventory/history conditions
contributes to less favorable trajectories; the current evidence cannot establish
the alternative result under another order.

**S6 — Large Groups** is the next supported candidate because the size association
survives a broad last-third comparison. **S7 — High Fragmentation** remains
plausible but is less directly supported: the captured evidence cannot separate
quality-specific exhaustion from fragmentation. S8 concurrency is not an
explanation supplied by these sequential runs. No later scenario is implemented.

## Evidence gaps and deferred work

- Instantaneous eligible capacity and feasible-block inventory by Good/Medium/Bad.
- Exact pre-request fragmentation and largest block size in each eligible Zone.
- The counterfactual outcome under changed request order, grouping or eligibility.
- Isolation of exact sequence position, group member histories and eligibility;
  the descriptive strata here do not control all their interactions.
- Recovery beyond the last observed participation/festival day, or on absent days.
- Stakeholder-calibrated inputs or generalization beyond these three fixed S2 seeds.

The first two gaps need a separately scoped telemetry/scenario decision. No new
telemetry, reconstructed inventory or block-search algorithm is added here.

## Validation and scope

`dotnet build Festival.sln`: passed, 0 warnings/errors.
`DOCKER_API_VERSION=1.43 dotnet test Festival.sln`: 883 passed, 0 failed/skipped.
Python discovery: 13 passed (10 new analytical regression tests plus 3 existing
metadata tests). Tests cover exposure denominators with changing groups,
sequence/arrival boundaries, global sequence versus daily rank, persisted timestamp
precision, invalid joins, population/checksum failures and episode opportunities
across absent days. Full-evidence replay produces byte-identical derived JSON;
all official package hashes are rechecked. `git diff --check`: passed.

Only analysis code/tests/documentation change. No production, Engine, fairness,
recovery, rubric, aggregate formulas, scenario definitions or concurrency behavior
changes. The harness remains outside Festival.sln. No branch switch, commit or
push is performed; the report is prepared for the developer's commit on the
existing `test/stage-5-s2-fairness-paths` branch.
