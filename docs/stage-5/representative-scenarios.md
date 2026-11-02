# Stage 5 — S1 and S2 Representative Scenario Contracts

## Status and Authority

**Documentation contract only; representative generation and execution are pending.**
S1 and S2 define simulation intent and provisional inputs for later deterministic
materialization. They provide no measured results or scenario pass/fail expectations.

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

## Shared Baseline

| Parameter | S1 and S2 |
| --- | --- |
| FestivalDays | 5 |
| RegisteredPopulation | 5,500 Attendees |
| PhysicalCapacity | Approximately 4,998; the provisional row model totals exactly 4,998 Spots |
| ExecutionMode | Sequential |
| ConcurrencyLevel | 1 |
| DailyParticipationContinuity | 90% overlap of planned daily requesting populations between consecutive FestivalDays |
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
versioned before implementation; no selection algorithm is defined here.

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
eligibility remains the implemented behavior. The workload's eligibility mix is
still an open input decision, distinct from the existing eligibility policy.

## Simulated Arrival Order and Reproducibility

Later implementation will allocate deterministic integer request counts per time
band, generate pseudo-random timestamps within each band from the scenario
`Seed`, order requests by simulated `RequestedAt`, and process them immediately
in simulation time with one request in flight. Time affects ordering, not
execution duration: tests and simulations must not use real delays, timers or
wall-clock waiting to reproduce these arrival profiles.

Reproducibility requires the same definition, Seed, GeneratorVersion and initial
state, with exact input retained in the manifest, as defined by the
[architecture contract](simulation-architecture.md#deterministic-generation-and-replay-limits)
and [framework requirements](metrics-and-evaluation-criteria.md#27-reproducibility-requirements).
The later generator must record exact daily membership, groups, eligibility,
band counts, timestamps, stable identities and request order. A Seed alone
does not specify the generation algorithm. Observed processing time remains
separate from simulated business time.

These inputs need explicit decisions before materialization; no undocumented
defaults should be embedded in code:

| Unresolved input | Required decision |
| --- | --- |
| Integer group allocation | Choose deterministic rounding that reconciles the approximate distributions with daily attendee totals; specify individual sizes within combined buckets. |
| Participation selection | Define deterministic Day 1 selection and later retention/replacement within the planned requesting population, including any additional replacement-eligibility criteria. |
| Eligibility workload | Specify a shared distribution of unrestricted and FrontStanding-excluded requests and how eligibility is assigned to groups. Do not invent a percentage. |
| Arrival materialization | Specify deterministic band-count rounding, time-band boundary treatment, timestamp precision and tie ordering. |
| Reproduction configuration | Record the concrete Seed, dates, time-zone/UTC conversion, assignment windows, identity procedure and GeneratorVersion before executing representative runs. |

## Comparison Intent and Later Scenarios

S1 and S2 intentionally hold venue topology, quality capacity distribution,
fairness policy, five-day duration, participation continuity and execution mode
constant. They primarily differ in daily demand, group-size distribution and
arrival concentration. The remaining eligibility/generation choices must be
resolved consistently to preserve that comparison.

This enables meaningful comparison of the combined workload conditions. Because
several workload dimensions change together, an outcome difference cannot be
attributed to one of them independently. S5 will later isolate adverse
arrival-order sensitivity.

The remaining scenarios retain only their high-level purposes here:

| Scenario | Purpose |
| --- | --- |
| S3 — Stress | Examine behavior under operating pressure. |
| S4 — Worst Plausible | Examine the practical operating boundary. |
| S5 — Adverse Arrival Order | Isolate sensitivity to adverse request order. |
| S6 — Large Groups | Examine sensitivity to larger groups. |
| S7 — High Fragmentation | Examine sensitivity to fragmentation. |
| S8 — Higher Concurrency | Examine sensitivity to concurrency. |

S1/S2 evidence will inform later calibration. No detailed S3–S8 parameters,
Decision Gate thresholds or new acceptance criteria are defined here.

## Assumptions Requiring Stakeholder Validation

Every row below is **Simulation hypothesis — Requires stakeholder validation**.

| Hypothesis | Validation needed |
| --- | --- |
| 5,500 registered Attendees | Confirm the relevance of this growth / oversubscription population. |
| S1 60% / approximately 3,300 and S2 80% / approximately 4,400 daily requesters | Validate demand assumptions and their applicability across five days. |
| 90% daily participation continuity | Validate the overlap assumption for planned daily requesters and replacement eligibility; selection precedes Assignment outcomes and returning participants are permitted. |
| S1 and S2 group-size distributions | Validate the proposed shares of requests/groups and composition of combined-size buckets. |
| S1 and S2 arrival distributions and 07:00–23:00 window | Validate the smoother S1 profile and S2 peaks/tail against operational understanding. |
| Good 35% / Medium 45% / Bad 20% capacity | Validate approximate quality-capacity proportions. |
| Per-Zone shares and capacities, approximately 4,998 total | Validate the detailed allocation and usable venue capacity. |
| Grandstand RowCount = 12 | Validate the approximation for each seated Zone. |
| GrowthPerRow = +1 and Zone-specific starting capacities | Validate the progressive semicircular approximation. |
| FrontStanding: 10 logical rows × 75 Spots | Validate the technical discretization and its suitability for contiguous group placement. |

## Scope Boundary

This task adds documentation only. It adds no ScenarioDefinition code changes,
scenario materialization or execution, timestamp generator, 5,500-Attendee run,
artifact run, S3–S8 detailed design, Decision Gate logic/thresholds or performance
optimization. Representative implementation remains deferred until the explicit
input decisions above are resolved and documented.
