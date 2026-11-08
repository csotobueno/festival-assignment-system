# Stage 5 — S1 and S2 Representative Scenario Contracts

## Status and Authority

**Deterministic generation and the first six official representative runs completed;
the wider Stage 5 evaluation remains pending.**
S1 and S2 define simulation intent and provisional inputs materialized through
the existing ScenarioDefinition → ScenarioMaterializer → ScenarioManifest flow.
These definitions describe inputs and do not prescribe scenario pass/fail results.
The [first official S1/S2 set](s1-s2-representative-results.md) records observations
separately: Seeds 1001, 2001 and 3001 for each scenario, six official runs in total.
Both use
the conservative initial Stage 5 baseline `FrontStandingAllowedRequestShare = 50%`
of requests. This simulation input is not an observed festival value and
requires stakeholder validation.

Keep four kinds of information distinct:

| Kind | Meaning in this contract |
| --- | --- |
| Existing domain/system behavior | The implemented Stage 4 baseline and existing Stage 5 analytical framework, referenced below. Scenarios do not redefine them. |
| Known festival context | Registration is separate from assignment; requests contain temporary groups of 1–10 Attendees. The current physical-capacity reference is approximately 5,000 people, as recorded in the [operating model](../project-operating-model.md#9-métricas-de-éxito). This does not confirm the detailed venue model below. |
| Simulation hypothesis | A provisional workload or venue input selected for simulation, explicitly identified in the validation register. |
| Stakeholder-validation item | A hypothesis that requires organization confirmation or calibration before being presented as representative operational evidence. |

The [metrics and evaluation framework](metrics-and-evaluation-criteria.md)
remains authoritative for analytical populations, metrics, rubric and evaluation
criteria. This document supplies the concrete S1/S2 input hypotheses within its
[scenario envelope](metrics-and-evaluation-criteria.md#19-scenario-envelope).
The [simulation architecture](simulation-architecture.md) owns execution,
reproducibility and evidence flow. The contracts do not claim that S2 is an
organization-validated forecast or that sequential execution represents normal
concurrent operational load.

The [S5 Adverse Arrival Order contract](s5-adverse-arrival-order.md) defines a
counterfactual reversal of the exact S2 daily workload. The explicit transformation,
runner dispatch and evidence support are implemented; official S5 runs and paired
analysis remain pending. S1/S2 inputs below are unchanged.

## Shared Baseline

| Parameter | S1 and S2 |
| --- | --- |
| FestivalDays | 5 |
| RegisteredPopulation | 5,500 Attendees |
| PhysicalCapacity | Approximately 4,998; the provisional row model totals exactly 4,998 Spots |
| ExecutionMode | Sequential |
| ConcurrencyLevel | 1 |
| DailyParticipationContinuity | 90% overlap of planned daily requesting populations between consecutive FestivalDays |
| FrontStandingAllowedRequestShare | 50% of each FestivalDay's requests/groups |
| RequestGenerationWindow | 07:00–23:00, simulated time |
| Initial history | No prior persisted Assignments on Day 1 |

**RegisteredPopulation = 5,500 is a Simulation hypothesis — Requires stakeholder
validation.** It is a growth / oversubscription population, separate from current
physical capacity. Both scenarios deliberately use this population hypothesis;
it does not turn 5,500 into confirmed current registration or venue capacity.
Daily demand remains below this registered population.

`DailyRequestingPopulation` denotes distinct Attendees included in the planned
daily request workload, counting every group member. It is not a request count
or a guarantee of successful Assignments. Actual RequestingAttendees and
ParticipatingAttendees retain the
[framework's reporting definitions](metrics-and-evaluation-criteria.md#32-reporting-populations).

### FrontStanding Request Eligibility

`FrontStandingAllowedRequestShare = 50%` is the shared request-level simulation
parameter, stored as `RepresentativeWorkloadDefinition.AllowsFrontStandingRequestShare`
(decimal ratio `0.50`). Each daily request receives its own
`RequestEligibility.AllowsFrontStanding`: approximately/exactly half are true
and half false, using the deterministic integer allocation described below.
Each request/group counts once, regardless of its number of Attendees.

Eligibility is generated independently for each FestivalDay's requests; it is
not a persistent property of an Attendee. The same Attendee can therefore join
an allowed request on Day 1, an excluded request on Day 2 and an allowed request
on Day 3. No attendee preference-transition logic or forced cross-day changes
are needed. Eligibility allocation does not change DailyRequesterCount, group
membership or group-size distribution, arrivals or participation continuity.

| Item | Provisional value / rationale |
| --- | --- |
| Current business hypothesis | Approximately 75–80% of requests may allow FrontStanding. |
| Initial Stage 5 simulation baseline | 50% of requests for both S1 and S2. |
| Reason | Conservative initial baseline while stakeholder data is unavailable. |
| Status | Requires stakeholder validation. Neither percentage is an observed festival value. |

## S1 — Best Plausible

Purpose: validate Assignment Engine v1 under favorable but still plausible
operating conditions.

| Demand parameter | Value |
| --- | --- |
| DailyRequestingPopulation | Approximately 3,300 Attendees |
| DailyDemandRatio | Approximately 60% of RegisteredPopulation |
| CapacityUtilization | Approximately 66% (3,300 / 4,998 ≈ 66.03%) |

These demand values are **Simulation hypothesis — Requires stakeholder validation**.
`CapacityUtilization` here describes offered attendee demand relative to physical
capacity, not measured occupancy or successful assignment rate.

Group-size distribution: **Simulation hypothesis — Requires stakeholder validation**.
Percentages count daily requests/groups, with each request counted once regardless
of GroupSize. They do not measure shares of Attendees. Ranges are combined buckets,
not a specified distribution among the individual sizes inside them.

| GroupSize | Share |
| --- | --- |
| 1 | 25% |
| 2 | 35% |
| 3 | 25% |
| 4 | 10% |
| 5–10 | 5% |
| Total | 100% |

Arrival distribution, as shares of daily requests:
**Simulation hypothesis — Requires stakeholder validation**.

| Simulated time band | Share |
| --- | --- |
| 07:00–09:00 | 15% |
| 09:00–12:00 | 25% |
| 12:00–15:00 | 20% |
| 15:00–18:00 | 20% |
| 18:00–21:00 | 15% |
| 21:00–23:00 | 5% |
| Total | 100% |

Analytical intent: lower capacity pressure, smaller groups, a smoother arrival
profile and low natural fragmentation. These describe conditions to examine,
not guaranteed outcomes, prescribed initial occupied gaps or pass/fail criteria.

## S2 — Expected Baseline

Purpose: represent the current best working hypothesis of normal festival usage.
This remains provisional, including the shared growth-population assumption and
sequential execution boundary.

| Demand parameter | Value |
| --- | --- |
| DailyRequestingPopulation | Approximately 4,400 Attendees |
| DailyDemandRatio | Approximately 80% of RegisteredPopulation |
| CapacityUtilization | Approximately 88% (4,400 / 4,998 ≈ 88.04%) |

These demand values are **Simulation hypothesis — Requires stakeholder validation**.
The utilization label has the same offered-demand meaning as S1.

Group-size distribution: **Simulation hypothesis — Requires stakeholder validation**.
Percentages count daily requests/groups, as in S1. The distribution among individual
sizes within combined buckets remains to be specified before materialization.

| GroupSize | Share |
| --- | --- |
| 1 | 15% |
| 2 | 30% |
| 3 | 25% |
| 4 | 15% |
| 5 | 8% |
| 6–7 | 5% |
| 8–10 | 2% |
| Total | 100% |

Arrival distribution, as shares of daily requests:
**Simulation hypothesis — Requires stakeholder validation**. It reflects current
operational understanding and requires stakeholder confirmation.

| Simulated time band | Share |
| --- | --- |
| 07:00–09:00 | 10% |
| 09:00–12:00 | 40% |
| 12:00–15:00 | 15% |
| 15:00–16:00 | 20% |
| 16:00–18:00 | 7% |
| 18:00–21:00 | 5% |
| 21:00–23:00 | 3% |
| Total | 100% |

This models a strong morning peak, a secondary afternoon peak, lower request
volume through the rest of the day and a small request tail during the event.
The profile does not establish confirmed event times or production assignment
windows. Resulting fragmentation develops naturally from real assignments.

## Shared Quality Capacity and Physical Topology

The capacities, proportions and row representations in this section are all
**Simulation hypothesis — Requires stakeholder validation**. The quality labels
reuse the existing Zone mapping; only the quantities/topology are provisional
scenario inputs.

| ZoneType | Existing quality | Provisional capacity share | Provisional row model | Spots |
| --- | --- | --- | --- | --- |
| FrontStanding | Good | 15% | 10 logical rows × 75 | 750 |
| MiddleCenter | Good | 20% | 12 rows: 78, 79, 80, …, 89 | 1,002 |
| MiddleLeft | Medium | 15% | 12 rows: 57, 58, 59, …, 68 | 750 |
| MiddleRight | Medium | 15% | 12 rows: 57, 58, 59, …, 68 | 750 |
| UpperCenter | Medium | 15% | 12 rows: 57, 58, 59, …, 68 | 750 |
| UpperLeft | Bad | 10% | 12 rows: 36, 37, 38, …, 47 | 498 |
| UpperRight | Bad | 10% | 12 rows: 36, 37, 38, …, 47 | 498 |
| Total | | 100% | | 4,998 |

| Quality | Provisional capacity share | Spots from row model | Actual model share (rounded) |
| --- | --- | --- | --- |
| Good | 35% | 1,752 | 35.05% |
| Medium | 45% | 2,250 | 45.02% |
| Bad | 20% | 996 | 19.93% |
| Total | 100% | 4,998 | 100.00% |

The percentages are approximate targets. The explicit integer row model explains
the small differences; later materialization must preserve these Spot counts
rather than independently rounding percentages into a different topology.

All six seated/grandstand Zones share the conceptual semicircular shape: lower
rows have fewer Spots, and capacity increases progressively toward upper rows.
Use `Grandstand RowCount = 12` and `GrowthPerRow = +1 spot`. For row index
`r = 1..12`, capacity is `StartingCapacity + (r - 1)`. The arithmetic-series
totals are `12 × (78 + 89) / 2 = 1,002`,
`12 × (57 + 68) / 2 = 750`, and `12 × (36 + 47) / 2 = 498`.
This approximates semicircular grandstand behavior; it is not an exact geometric
reconstruction or a confirmed seating survey.

FrontStanding uses uniform **logical** rows: 10 rows of 75 Spots, total 750.
They are a technical discretization for the existing contiguity invariant:
same Zone, same Row and consecutive SpotNumbers for the complete GroupSize.
They do not represent physical seated rows and do not use the increasing-row
grandstand pattern. Physical suitability of this discretization requires
stakeholder validation. See [critical invariants](../critical-invariants.md#inv-05--contiguous-spots).

The exact same venue topology is used for both scenarios and all five days.
Request eligibility can reduce available options; physical capacity does not
imply that every Spot is eligible or feasible for every group.

## Daily Participation Continuity and Historical State

`DailyParticipationContinuity = 90%` is a **Simulation hypothesis — Requires
stakeholder validation**: approximately 90% of the planned daily requesting
population on one FestivalDay remain in the next day's planned requesting
population. Membership is determined before Assignment outcomes are known;
continuity models participation/request intent and does not depend on Assignment
success. It does not use the analytical ParticipatingAttendees population, which
counts Attendees with successfully persisted Assignments.

The remaining 10% are replaced deterministically from eligible registered
Attendees outside the immediately previous day's planned requesting population,
including those who may have requested on earlier days. A rejected or failed
request does not remove its members from that previous day's planned population
or make them eligible as next-day replacements. Replacements may be returning
Attendees, not necessarily first-time participants. This is a daily overlap,
not a claim that the same 90% attend all five days.

At the nominal daily totals, each S1 transition retains 2,970 planned requesters
and replaces 330; each S2 transition retains 3,960 and replaces 440.
The deterministic retention/replacement procedure must be documented and
versioned. The implemented v2 procedure is specified below.

Both independent scenarios begin from a clean festival baseline:

- **Day 1:** no prior persisted Assignment history; no synthetic fairness history
  is pre-seeded.
- **Days 2–5:** retain previous simulated days' real persisted Assignments;
  history evolves naturally, including for returning Attendees. Never reset
  persistence between days or manufacture an outcome for absence/rejection.

The minimal two-day proof scenario's supporting Attendees, occupancy Assignments
and setup requests are not inputs to S1/S2. Clean history does not mean repeating
empty history on every day.

## Shared System and Analytical Behavior

S1 and S2 use the same authoritative behavior and versions:

| Shared behavior | Authority |
| --- | --- |
| Assignment Engine v1 (`v0.3.0` baseline), real Application/PostgreSQL path | [Stage 4 completion](../stage-4/completion.md), [simulation composition](simulation-architecture.md#project-boundary-and-composition) |
| All 19 Stage 4 fairness rules | [Fairness Definition v1](../stage-4/fairness-definition-v1.md) |
| RotationScore and GroupRotationScore | [RotationScore v1](../stage-4/rotation-score-v1.md) |
| TargetQuality, Zone quality mapping, FrontStanding eligibility behavior, inventory and contiguous placement | [Assignment Strategy v1](../stage-4/assignment-strategy-v1.md) |
| PathClassificationRubric v1 | [Stage 5 rubric](metrics-and-evaluation-criteria.md#41-pathclassificationrubric-v1) |
| Recovery model | [Recovery observations](metrics-and-evaluation-criteria.md#65-attendee-recovery-observations), [recovery distance](metrics-and-evaluation-criteria.md#66-opportunitiestorecovery) |
| Aggregate metrics framework | [Stage 5 metrics](metrics-and-evaluation-criteria.md), [implemented aggregates](simulation-architecture.md#implemented-aggregate-fairness-and-recovery-metrics) |

No rule, formula, quality classification, recovery definition or metric
denominator is redefined by the scenario parameters. Request-wide FrontStanding
eligibility remains the implemented behavior. The shared 50% workload parameter
materializes that existing input without changing the eligibility policy.

## Simulated Arrival Order and Reproducibility

Generator v2 allocates deterministic integer request counts per time
band, generates pseudo-random timestamps within each band from the scenario
`Seed`, and orders requests by simulated `RequestedAt`, then SimulationRequestId.
It records that order as increasing Sequence values for the existing runner to
process immediately in simulation time with one request in flight. Time affects ordering, not
execution duration: tests and simulations must not use real delays, timers or
wall-clock waiting to reproduce these arrival profiles.

Reproducibility requires the same definition, Seed, GeneratorVersion and initial
state, with exact input retained in the manifest, as defined by the
[architecture contract](simulation-architecture.md#deterministic-generation-and-replay-limits)
and [framework requirements](metrics-and-evaluation-criteria.md#27-reproducibility-requirements).
The generated manifest records exact daily membership, groups, eligibility,
timestamps, stable identities and request order; band counts can be calculated
directly from those timestamps. A Seed alone
does not specify the generation algorithm. Observed processing time remains
separate from simulated business time.

### Implemented Generation Contract — v2

`S1BestPlausibleScenarioDefinition.Create` and
`S2ExpectedBaselineScenarioDefinition.Create` require an explicit Seed and FirstDate.
Both default `allowsFrontStandingRequestShare` to `0.50`, the shared initial
Stage 5 request-level baseline; the existing explicit-share input remains available.
They share the same registered catalog, topology, five-day plan and generation
algorithms.
The small proof scenario retains generator v1 and its existing identifiers,
timestamps and supporting setup.

`GroupDistributionGenerator` treats daily attendee count as a **hard invariant**:
exactly 3,300 for S1 or 4,400 for S2, every day. Group percentages are targets
over requests. The generator estimates the request count from the weighted
midpoint group size and rounds to the nearest integer, with midpoint ties away
from zero. It allocates band counts by largest remainder, breaking equal
remainders by declared band order. Concrete sizes inside ranges are seeded
pseudo-random integers. This adds no fixed subrange percentage distribution.

After shuffling groups, membership totals are reconciled inside their existing
bands first, preserving the allocated band counts whenever feasible. If those
counts cannot fit the exact attendee total (for example, tiny or fixed-size
inputs), sizes change one member at a time within 1–10, choosing the smallest
incremental absolute band-count deviation, with shuffled group order breaking
ties. Sizes outside declared bands count as a separate deviation bucket.
The correction is bounded by the original membership difference; no sampling
retry loop is used. A final seeded shuffle determines concrete grouping order.
Representative tests allow less than one request of deviation from each ideal
band quota after reconciliation; they do not demand impossible exact percentages.

Day 1 requesters are selected from a seeded shuffle of all 5,500 registered
Attendees. On later days, the prior requesting cohort and its registered
complement are shuffled separately: retain exactly 2,970 / 3,960 and add exactly
330 / 440 replacements for S1 / S2. The shared procedure uses the floor of
`DailyRequesterCount × DailyParticipationContinuity` for retention; the supplied
representative values are already integers. A separate membership shuffle
assigns every daily requester to exactly one group, without duplication. All
registered Attendees outside the immediately prior cohort are candidate
replacements; no extra business-eligibility filters have been invented.

`ArrivalDistributionGenerator` allocates the exact daily request count across
bands by largest remainder with declared-order ties. It shuffles groups before
assigning them to bands and samples timestamps at DateTime tick precision
inside each `[Start, End)` interval. Profile clock times are interpreted as
**UTC simulated business time** for the supplied FestivalDate, with a
07:00–23:00 assignment window. This is a technical time representation, not a
claim about confirmed venue local operating hours. AssignedAt equals RequestedAt
as an input convention; real processing duration remains separately observed.
Final ordering is RequestedAt ascending, then SimulationRequestId ascending,
with scenario-wide Sequence values matching that exact order.

Independent daily streams for participation, membership, group sizes,
eligibility and arrivals derive from SHA-256 of a length-prefixed ScenarioId,
GeneratorVersion, Seed, DayNumber and stream name. Generator v2 specifies
SplitMix64, multiply-high bounded integer sampling and Fisher–Yates shuffling;
it uses neither runtime Random/hash behavior nor current wall-clock time.
Existing SHA-256 identity derivation is preserved for v1. V2 shares the
`representative-population-v1` attendee namespace and `representative-venue-v1`
Zone namespace across S1/S2 and Seeds; day/request identities remain scenario
specific. Identical catalogs preserve identical Zone tie-break inputs.

Each FestivalDay's FrontStanding share is allocated by `PercentageAllocation`
using largest remainder into allowed/excluded requests, then shuffled using its
independent daily eligibility stream. Equal remainders favor the declared bucket
order (allowed first): the 50% baseline produces 667 allowed / 666 excluded for
S1's 1,333 daily requests and 732 / 732 for S2's 1,464. There is no special
eligibility rounding rule and no adjustment to groups or attendee counts.
The stream depends on ScenarioId, Seed, GeneratorVersion and DayNumber, not
previous eligibility or attendee preferences. Existing request-wide
`AllowsFrontStanding` and production quality policies remain unchanged.
A representative run must record the shared eligibility mix, Seed and FirstDate. Any
future local-time mapping or additional replacement eligibility rules require
an explicit contract revision.

Validation rejects invalid percentages/totals, invalid or overlapping group
ranges, unordered/overlapping/invalid arrival bands, nonpositive requester
counts, infeasible replacement pools, invalid continuity/eligibility ratios,
nonconsecutive days and supporting setup or explicit requests combined with a
generated workload. All validation stays in Simulation. The immutable manifest
contains all concrete input; the runner does not regenerate workload details.

## Comparison Intent and Later Scenarios

S1 and S2 intentionally hold venue topology, quality capacity distribution,
fairness policy, five-day duration, participation continuity, 50% request-level
FrontStanding eligibility baseline and execution mode constant. They primarily
differ in daily demand, group-size distribution and
arrival concentration. Eligibility is held constant as a workload parameter
rather than adding another differing variable.

This enables meaningful comparison of the combined workload conditions. Because
several workload dimensions change together, an outcome difference cannot be
attributed to one of them independently. The
[S5 contract](s5-adverse-arrival-order.md) defines a controlled processing-order
counterfactual over the exact S2 workload. Its transformation is implemented;
official paired execution and analysis remain pending.

The [S6 Large Groups contract](s6-large-groups.md) defines a separate controlled
S2 sensitivity experiment: only the target group-size distribution changes,
raising requests of size 6–10 from 7% to 25%. Normal S2 processing order and
all other baseline parameters remain controlled. `S6LargeGroupsScenarioDefinition.Create`
copies S2 and replaces only scenario identity and GroupSizeProfile, reusing
generator v2 unchanged. Definition/materialization tests and the
[official S2/S6 execution and analysis](s2-s6-large-group-sensitivity-results.md)
are completed, including S6/1001 reproduction. Same-Seed S2/S6
comparisons are at scenario level, not exact request-level pairs.

The [S7 High Fragmentation contract](s7-high-fragmentation.md) defines a paired
S2-workload experiment with 1,000 pre-occupied Spots and 3,998 remaining in each
variant. S7-Control concentrates occupancy; S7-Fragmented disperses the same
per-Row quotas. Opening capacity remains equal by Zone and quality every day.
The 20% pre-occupation hypothesis requires stakeholder validation.
`S7ScenarioPair.Create(seed, firstDate)` materializes S2 once and derives both
variants with isolated day-scoped setup and versioned provenance. Definition/
materialization and focused tests are implemented; official runs and analysis
remain pending.

The later-scenario matrix links the detailed S5, S6 and S7 contracts; the other
scenarios retain only their high-level purposes here:

| Scenario | Purpose |
| --- | --- |
| S3 — Stress | Examine behavior under operating pressure. |
| S4 — Worst Plausible | Examine the practical operating boundary. |
| [S5 — Adverse Arrival Order](s5-adverse-arrival-order.md) | Reverse the exact S2 daily request processing order, preserving original timestamps and baseline sequence; implemented, official runs pending. |
| [S6 — Large Groups](s6-large-groups.md) | Baseline: S2. Changed dimension: group-size distribution; requests of size 6–10 increase from 7% → 25%. Definition/materialization and first official execution/analysis completed. |
| [S7 — High Fragmentation](s7-high-fragmentation.md) | Primary baseline: S7-Control. Experimental variant: S7-Fragmented. Changed dimension: spatial distribution of equivalent pre-occupied capacity. Workload: exact paired S2. Processing: normal sequential order. Definition/materialization implemented; official runs and analysis pending. |
| S8 — Higher Concurrency | Examine sensitivity to concurrency. |

S1/S2 evidence will inform later calibration. Detailed S3/S4/S8 parameters,
Decision Gate thresholds and new acceptance criteria remain undefined here.

## Assumptions Requiring Stakeholder Validation

Every row below is **Simulation hypothesis — Requires stakeholder validation**.

| Hypothesis | Validation needed |
| --- | --- |
| 5,500 registered Attendees | Confirm the relevance of this growth / oversubscription population. |
| S1 60% / approximately 3,300 and S2 80% / approximately 4,400 daily requesters | Validate demand assumptions and their applicability across five days. |
| 90% daily participation continuity | Validate the overlap assumption for planned daily requesters and replacement eligibility; selection precedes Assignment outcomes and returning participants are permitted. |
| FrontStanding request eligibility: 75–80% business hypothesis; 50% initial Stage 5 baseline | Validate the request-level business hypothesis and calibrate the conservative shared simulation input when stakeholder data becomes available. Neither value is observed festival evidence. |
| S1 and S2 group-size distributions | Validate the proposed shares of requests/groups and composition of combined-size buckets. |
| S1 and S2 arrival distributions and 07:00–23:00 window | Validate the smoother S1 profile and S2 peaks/tail against operational understanding. |
| Good 35% / Medium 45% / Bad 20% capacity | Validate approximate quality-capacity proportions. |
| Per-Zone shares and capacities, approximately 4,998 total | Validate the detailed allocation and usable venue capacity. |
| Grandstand RowCount = 12 | Validate the approximation for each seated Zone. |
| GrowthPerRow = +1 and Zone-specific starting capacities | Validate the progressive semicircular approximation. |
| FrontStanding: 10 logical rows × 75 Spots | Validate the technical discretization and its suitability for contiguous group placement. |

## Scope Boundary

Generation materializes the unchanged representative inputs described above.
The first official six-run execution protocol, separate reproduction check and
retained evidence are documented in the
[results report](s1-s2-representative-results.md) and
[task harness](../../scripts/stage-5-representative-runs/README.md).
S5 adds only its explicit ordering transformation, sequence/provenance evidence
and focused tests; no official S5 execution is included.
S6 adds its declarative definition and focused generation/export-compatibility
tests under the linked experimental contract; the separate official execution
and analysis report retains observations without changing these inputs.
S7 adds its explicit paired materialization, isolated supporting setup, minimal
provenance/export support and focused tests under the linked contract; official
execution and analysis remain deferred.
No S3/S4/S8 implementation or detailed design, Decision Gate logic/thresholds,
concurrency, replay loading, generic comparison engine or performance optimization
is added.
Stakeholder hypotheses remain provisional; simulation observations are not
measured festival data and materialization tests are not empirical evidence.
