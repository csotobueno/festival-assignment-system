# S7 — High Fragmentation

## Status, authority and question

**Experimental definition only — implementation, execution and analysis pending.**
S7 asks: with equivalent remaining capacity, does spatial fragmentation materially
reduce contiguous-group feasibility? Future analysis must measure rejection
increase, affected group sizes, failure onset and capacity remaining at rejection.

The [S2 contract](representative-scenarios.md#s2--expected-baseline) owns the
workload, topology and generator v2 rules. The
[metrics framework](metrics-and-evaluation-criteria.md) remains authoritative for
populations, formulas, rubric and recovery; the
[simulation architecture](simulation-architecture.md) owns execution and evidence.
This definition changes none of those rules.

The [S2/S5 results](s2-s5-order-sensitivity-results.md) and
[S2/S6 results](s2-s6-large-group-sensitivity-results.md) retained 100% SuccessRate
and zero DomainRejected across the observed Seeds. S6 increased requests of size
6–10 from 7% to 25% without observed feasibility loss. Large-group prevalence
alone did not cause placement failure in those runs; S7 deliberately changes
spatial availability while retaining the S2 workload.

## Paired experiment and capacity equivalence

| Primary baseline | Experimental variant | Only intended intervention |
| --- | --- | --- |
| S7-Control | S7-Fragmented | Spatial distribution of equivalent pre-occupied capacity |

Both variants preserve the same total occupied count, remaining capacity,
per-Zone and per-Row occupied counts, remaining quality capacity, exact workload,
Engine, fairness policy and normal sequential processing order. S7-Control is
partially occupied but spatially healthy inventory. The empty S2 inventory is
contextual reference, not the primary causal control.

The initial pre-occupation hypothesis is **20% of modeled venue capacity —
Requires stakeholder validation**. This is an analytical stress input, not a
confirmed real festival condition. Round `4,998 × 0.20 = 999.6` to the nearest
integer, with midpoint ties away from zero: **1,000 occupied, 3,998 available**
per FestivalDay in each variant (approximately 20.008% realized occupancy).
Do not silently change this percentage or count during implementation.

Allocate the integer total deterministically, without randomness:

1. For each Zone of capacity `Cz`, begin with `floor(0.20 × Cz)` occupied Spots.
   Distribute the remainder needed to reach 1,000 by descending fractional
   remainder of `0.20 × Cz`; ties use ZoneType name in ordinal order.
2. Within each Zone, begin each Row of capacity `n` with `floor(0.20 × n)`.
   Distribute the remainder needed to reach that Zone's quota by descending
   fractional remainder of `0.20 × n`; ties use RowCode in ordinal order.
   Each selected Row receives one extra occupied Spot.
3. Use exactly those same Row quotas in both variants. Only positions change.

The fixed current topology gives these **opening counts in both variants**:

| ZoneType | Existing quality | Physical Spots | Pre-occupied | Remaining |
| --- | --- | --- | --- | --- |
| FrontStanding | Good | 750 | 150 | 600 |
| MiddleCenter | Good | 1,002 | 200 | 802 |
| MiddleLeft | Medium | 750 | 150 | 600 |
| MiddleRight | Medium | 750 | 150 | 600 |
| UpperCenter | Medium | 750 | 150 | 600 |
| UpperLeft | Bad | 498 | 100 | 398 |
| UpperRight | Bad | 498 | 100 | 398 |
| Total | | 4,998 | 1,000 | 3,998 |

| Quality | Physical Spots | Pre-occupied | Remaining in each variant |
| --- | --- | --- | --- |
| Good | 1,752 | 350 | 1,402 |
| Medium | 2,250 | 450 | 1,800 |
| Bad | 996 | 200 | 796 |
| Total | 4,998 | 1,000 | 3,998 |

Before dispatch on every FestivalDay, validate for the venue, each Zone and Row:

```text
OccupiedCount(Control) == OccupiedCount(Fragmented)
RemainingCapacity(Control) == RemainingCapacity(Fragmented)
```

Preserve Zone quality mapping, capacities, Row definitions, Spot identities and
SpotNumbers. Equal per-Zone capacity also preserves opening eligible capacity:
3,998 for FrontStanding-allowed requests and 3,398 for excluded requests. Setup
must not depend on request eligibility or identity. Equivalence is an opening
condition; different outcomes can cause inventory/history to diverge afterward.

Daily offered demand remains 4,400 Attendees, exceeding 3,998 available Spots by
402. Capacity pressure therefore exists even in Control. This is an attendee
capacity bound, not a predicted rejection count/rate. A Fragmented rejection
alone cannot isolate fragmentation; use the paired comparison and snapshot
diagnosis below.

## Deterministic pre-occupation patterns

Define **FragmentationPatternVersion = `s7-preoccupation-v1`**, covering rounding,
quotas, tie-breaks and both patterns. This is a future provenance identifier,
not an implemented metadata field or generic fragmentation framework.

Order each Row's Spots by ascending SpotNumber. The S2 topology has consecutive
positions; validate that precondition rather than treating gaps as contiguous.
Let `n` be Row capacity and `k` the common occupied quota.

**Control:** occupy the first `k` positions from the low-SpotNumber edge, leaving
one available run of length `n − k`. Do not introduce random holes.

**Fragmented:** divide the `n − k` available Spots into `k + 1` ordered gaps.
Set `q = floor((n − k) / (k + 1))` and `r = (n − k) mod (k + 1)`. Give the first
`r` gaps `q + 1` available Spots and all other gaps `q`. Insert one occupied Spot
between successive gaps and map the sequence onto the ordered Row. This gives
exactly `k` regularly dispersed occupied Spots and largest available run
`ceil((n − k) / (k + 1))`.

Example: `n = 15`, `k = 3`, `X = occupied`, `O = available`:

```text
Control:    XXXOOOOOOOOOOOO   (largest available run: 12)
Fragmented: OOOXOOOXOOOXOOO   (largest available run: 3)
```

No manual Spot-ID list or random placement is needed. Both patterns preserve
exact counts at every level. Current Row quotas satisfy `0 < k < n`, and
Fragmented shortens the opening largest run in every Row. Changed topology or
pattern requires an explicit contract/version revision. Later implementation
needs only a small explicit Stage 5 transformation/helper.

## Preserved S2 workload and processing

Inherit the complete [S2 generation contract](representative-scenarios.md#implemented-generation-contract--v2)
unchanged, including integer allocation, range sampling and exact daily demand.
These values identify the controls without redefining that contract:

| Parameter | Both S7 variants |
| --- | --- |
| RegisteredPopulation | 5,500 registered workload Attendees; supporting identities separate |
| DailyRequesterCount | Exactly 4,400 distinct planned requesters |
| FestivalDays | 5 |
| DailyParticipationContinuity | 90%; retain 3,960 and replace 440, independent of outcomes |
| FrontStandingAllowedRequestShare | 50% of requests/groups, not Attendees |
| ExecutionMode / ConcurrencyLevel | Sequential / 1 |
| Engine | Assignment Engine v1, `v0.3.0` baseline, real Application/PostgreSQL path |

S2 request-size targets remain **1: 15%; 2: 30%; 3: 25%; 4: 15%; 5: 8%;
6–7: 5%; 8–10: 2%**. Sizes 6–10 remain 7%; do not reuse S6's 25% profile.
Realized integer counts follow generator v2, not a new allocation rule.

Preserve the exact S2 arrival profile over requests: **07:00–09:00: 10%;
09:00–12:00: 40%; 12:00–15:00: 15%; 15:00–16:00: 20%; 16:00–18:00: 7%;
18:00–21:00: 5%; 21:00–23:00: 3%**. Retain half-open UTC simulated business-time
bands, the 07:00–23:00 window, RequestedAt and AssignedAt conventions.

Process RequestedAt ascending, then SimulationRequestId ascending, with
`BaselineSequence == ExecutionSequence`. Do not apply S5 reversal, concurrency,
delays, retries or workload adaptation after rejection. Inherited S2 assumptions
retain their provisional stakeholder-validation status. Preserve all Engine,
selection, scoring, quality, rubric, recovery and general metric behavior.

## Setup boundary and daily semantics

Pre-occupied Spots represent **inventory unavailable before the evaluated workload
begins**, not evaluated attendee participation or prior quality/rotation history.

The current PostgreSQL available-Spot provider excludes Spots through persisted
Assignments for the specified FestivalDay; there is no independent occupancy
flag. With production behavior unchanged, later S7 setup therefore requires
supporting persisted Assignments and valid completed setup requests, using the
existing `ScenarioSetup` separation. These are **supporting/setup data**, never
evaluated requests. Required persisted ExperienceQuality belongs only to that
supporting data; it must never become a synthetic evaluated quality outcome.

Supporting identities must be disjoint from the 5,500 registered workload
Attendees and day-scoped, so setup does not manufacture multi-day attendee
history. Exclude them from DailyPlans, evaluated paths, RotationScore and
GroupRotationScore inputs for evaluated groups, request/fairness/recovery metrics
and reporting populations. Account for their database rows separately in
integrity checks; physical inventory counts include their occupancy effect.
No invariant may be bypassed to create setup data.

Every FestivalDay begins with the **same deterministic pattern for its variant**:
exactly 1,000 day-scoped occupied Spots before that day's workload, leaving 3,998.
Setup may be persisted up front for all five days because availability is scoped
to FestivalDay. Do not delete/replace definitive Assignments or accumulate earlier
days' setup as extra occupancy/history for later evaluated groups.

Evaluated Day 1 starts without prior Assignment history. Days 2–5 retain each
variant's own naturally accumulated evaluated Assignments, including returning
Attendees' real history. Do not reset it or copy Control outcomes into Fragmented.
Divergent histories are consequences of the intervention. Planned daily cohorts
remain outcome-independent and paired.

## Seeds, pair integrity and reproducibility

Use **1001, 2001 and 3001** for both variants. For each Seed and the same FirstDate,
materialize the underlying S2 workload once with generator v2 and derive both
variants from that exact source. Do not generate independently under S7 IDs:
generator streams include ScenarioId, so equal Seeds alone would not pair them.
Future variant identities are `s7-control` and `s7-fragmented`; workload source
remains `s2-expected-baseline`.

Before execution compare exact request identities, FestivalDays/dates, daily
cohorts, group membership/sizes, eligibility, RequestedAt, AssignedAt,
BaselineSequence, ExecutionSequence and topology/catalog identities. All can
remain exact in the current sequential design. Apart from variant/provenance
labels and corresponding supporting setup graphs, only occupied arrangement
may differ. Production-generated request/Assignment IDs and timings are outputs
and need not match. Report any input mismatch before claiming paired causality.

Retain source definition/manifest checksum, Seed, FirstDate, GeneratorVersion
(`v2`), variant, FragmentationPatternVersion, exact setup and per-day/Zone/Row/
quality opening counts. The same variant + Seed + GeneratorVersion + pattern
version, with the same source/calendar/topology, must reproduce equivalent setup
and manifests. Preserve existing source revision and Engine/rubric provenance.

## Expected rejections and primary effect

`DomainRejected` is an expected experimental outcome, not automatically a failed
simulation. Reconcile it as persisted Rejected with zero Assignments. Technical
failures, known concurrency conflicts and invariant violations remain undesirable
and separately reported. Later validation must not require 100% SuccessRate.

Use the existing Stage 5 formula unchanged (`RejectionRate` is the report label
for AssignmentRejectionRate):

```text
NormallyProcessedRequests = Successful + DomainRejected
RejectionRate = DomainRejected / NormallyProcessedRequests × 100
FragmentationImpact = RejectionRate(S7-Fragmented) − RejectionRate(S7-Control)
```

Report percentage-point differences per same-Seed pair, with counts, denominators
and cross-Seed variation. Conflicts/technical failures remain outside this
denominator and visible separately; zero denominators are N/A. FragmentationImpact
is a derived S7 comparison, not a production metric or calculator change. Do not
predeclare a positive effect, materiality threshold or Decision Gate verdict.

## Fragmentation diagnosis and evidence requirements

The strongest physical-feasibility evidence is:

```text
DomainRejected
AND EligibleCapacity >= GroupSize
AND NoFeasibleContiguousBlock
```

EligibleCapacity means eligible available raw Spot count at the actual evaluation
snapshot. A feasible block fits the complete group in one eligible Zone, one Row
and consecutive SpotNumbers. Aggregate capacity across incompatible Rows/Zones
cannot satisfy that invariant. Use the existing production no-block reason
`NO_CONTIGUOUS_SPOTS_AVAILABLE`, without adding production rejection codes.

| Observed no-block rejection | Derived S7 diagnosis |
| --- | --- |
| EligibleCapacity < GroupSize | InsufficientEligibleCapacity |
| EligibleCapacity >= GroupSize and block-found observation is false | ContiguousBlockUnavailable |
| Missing capacity or feasibility observation | Unknown / N/A; report missing count |

Other domain reasons remain separate. Positive total capacity, closing inventory
or eligible capacity below GroupSize cannot by itself prove fragmentation.

Current evidence is **sufficient for the eligible-capacity/no-block distinction**:

- `RequestEvaluationObservation` captures the actual provider responses and
  derives EligibleAvailableSpotCount using the existing ZoneEligibilityPolicy.
- `SimulationAttemptRecord` and `attempts.json` retain GroupSize, membership,
  day/request identity, effective AttemptSequence, BaselineSequence, outcome,
  DiagnosticCode and nullable FeasibleContiguousBlockFound. The latter comes from
  the real success/no-block result, not a second block search.
- `manifest.json` retains exact setup/workload; `checkpoints.json` retains closing
  global quality/total capacity. Total capacity is not exported per request,
  but is reconstructible under sequential execution:

```text
RemainingTotalBeforeRequest(day, i)
  = 3,998 − sum(GroupSize of preceding confirmed Successful attempts that day)
```

At rejection report this total, captured eligible capacity, GroupSize, block-found
evidence, diagnostic source and execution position. Validate against actual daily
opening setup, persisted successes and closing checkpoint. Reconstruction requires
complete reconciled attempts, no external inventory writes and no uncertain
commits; otherwise report N/A rather than assuming a technical failure consumed
zero capacity. Do not reuse S6 analysis's empty-inventory opening value of 4,998.

**No additional per-request telemetry is required for this sequential experiment.**
The minimal future evidence addition is S7 setup/pair provenance: pattern version,
workload-source checksum and explicit opening quota validation, with occupied
Spots retained in the consumed setup manifest. Current workload-source/export
validation supports S5 specifically; a focused S7 extension is deferred. S7 setup
and provenance are requirements, not already executable capabilities. No broad
telemetry, second block search or fresh post-rejection snapshot is requested.

Missing evidence follows existing framework N/A/missing-observation rules,
including RawCapacityButNoFeasibleBlockRate. The baseline's independent pre-save
read consistency limitation remains; S7 adds no transactional-snapshot guarantee.

## Future size, execution-position and fairness analysis

For each variant/Seed, report request counts, normally processed denominators,
Successful, DomainRejected, SuccessRate and RejectionRate for **1, 2, 3, 4, 5,
6–7 and 8–10**. Apply existing request formulas within each bucket; each group
counts once. Report other attempt-category counts and N/A for empty denominators.
Compare paired rate deltas, not only rejection volumes. Supplement with **1–2,
3–4 and 5+** where useful, deriving rates from summed counts rather than averaging
bucket percentages.

Show the observed degradation curve: little/no impact for small groups, moderate
for medium and high for large is a hypothesis, not a prescribed result. Do not
assume only large groups fail or encode size thresholds in production behavior.

Within each FestivalDay sort by ExecutionSequence; assign daily rank `j` among
`N` requests to third `1 + floor(3 × (j − 1) / N)`: first, middle or last. Use
daily ranks rather than dividing global sequence values across days. Compare
size-by-third rates and diagnoses. Report the first rejection and first
ContiguousBlockUnavailable event per day/variant with rank, GroupSize and
remaining total/eligible capacity (or no event/N/A). These observations help
distinguish initial setup sensitivity from naturally generated fragmentation;
evolving inventory/history prevents thirds alone from proving a causal split.

Retain GoodOutcomeRate, MediumOutcomeRate, BadOutcomeRate, BalancedPathRate,
AcceptablePathRate, NotBalancedPathRate and RecoveryRate unchanged. Quality rates
use persisted evaluated Assignments, classification rates use classifiable paths,
and RecoveryRate uses eligible episodes. Rejection creates no synthetic
ExperienceQuality, participation or recovery opportunity. Zero-Assignment
attendees retain empty context paths. Reduced success can change participating
and classifiable populations; report coverage, path lengths and denominator counts.

Explicitly report **RegisteredAttendees, RequestingAttendees,
ParticipatingAttendees and AttendeesWithOnlyRejectedOrFailedAttempts**, using
existing definitions per run and relevant daily segment. RegisteredAttendees
remains 5,500; supporting identities are excluded. The last population contains
requesters with no persisted Assignment in the reporting period, including
conflict-only cases. Same-Seed inputs are paired, but successfully assigned
populations can differ. Better fairness rates among fewer survivors alone do
not establish improved overall fairness or access.

## Separation, interpretation and scope

- **S6:** increased large-group prevalence; no observed feasibility loss in its
  official runs. S7 does not extend its workload profile.
- **S7:** equivalent opening capacity, different spatial distribution, same S2
  workload; measure fragmentation-induced feasibility sensitivity.
- **S8:** concurrency / operational sensitivity; separate and undefined here.
  S7 remains Sequential with ConcurrencyLevel 1.

S7 can establish sensitivity **under the tested synthetic pre-occupation pattern**,
topology, workload and Seeds. It cannot alone establish real festival patterns,
production occurrence probability, all possible spatial arrangements or
concurrency behavior. The 20% input and pattern require stakeholder validation
before claims of operational representativeness. Five-day paired effects include
natural inventory/history consequences.

Evidence may inform whether fragmentation materially threatens group feasibility.
This task adds no mitigation recommendation, Engine change, block-preservation
strategy, allocation heuristic, reservation window, reordering or Decision Gate
logic/threshold. Implementation, focused tests, official paired runs and analysis
remain deferred. No S7ScenarioDefinition, transformation, setup code, telemetry,
analysis tooling or execution is included.

## Documentation contract validation

The definition requires these input checks before later execution; they are not
already observed S7 results:

- Both variants open each day with 1,000 occupied and 3,998 remaining Spots.
- Equal per-Row quotas reconcile to equal per-Zone counts and remaining quality
  capacity Good 1,402 / Medium 1,800 / Bad 796.
- Fragmented shortens available runs while preserving topology and identities.
- Exact S2 workload/arrival/eligibility inputs remain paired; S6's 25% profile and
  S5 reversal are absent; BaselineSequence equals ExecutionSequence.
- Daily setup repeats independently; evaluated history accumulates naturally and
  supporting data is excluded analytically.
- The 20% hypothesis remains provisional and versioned; missing observations are
  N/A and technical/invariant failures stay visible.
