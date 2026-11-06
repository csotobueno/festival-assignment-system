# S6 — Large Groups

## Status, authority and question

**Analytical sensitivity contract only — S6 implementation and execution pending.**
S6 uses S2 — Expected Baseline as its control and changes only the target
group-size distribution, increasing the prevalence of requests with 6–10 Attendees.

Its question is: if large groups become substantially more common, how do
feasibility, quality and fairness change under the same representative operating
conditions?

The [S2 contract](representative-scenarios.md#s2--expected-baseline) owns the
preserved inputs and [generation contract](representative-scenarios.md#implemented-generation-contract--v2).
The [metrics framework](metrics-and-evaluation-criteria.md) remains authoritative
for populations, formulas, PathClassificationRubric v1 and recovery semantics;
the [simulation architecture](simulation-architecture.md) owns execution and
evidence flow. This contract changes none of those definitions.

S2 is a **representative working hypothesis**. S6 is a **large-group prevalence
sensitivity experiment**, not a confirmed real festival distribution. Its
percentages are analytical inputs and remain subject to stakeholder validation
where applicable; the provisional status of the inherited S2 inputs is unchanged.

## Motivation and experimental variable

The [S2/S5 order-sensitivity analysis](s2-s5-order-sensitivity-results.md)
showed that processing order strongly affects individual outcomes. Group-size
associations were visible, but their magnitude and direction across paired Seeds
were not stable enough to establish group-size causality. S6 isolates
large-group prevalence while retaining normal S2 processing order.

The only intended changed dimension is **group-size distribution**. Daily attendee
demand, arrival behavior, eligibility prevalence, topology, fairness policy and
processing-order semantics remain controlled. A different request count and
different grouping are consequences of redistributing the same attendee demand,
not independently tuned workload parameters.

## S2 control and preserved conditions

| Parameter | S2 and future S6 |
| --- | --- |
| RegisteredPopulation | 5,500 Attendees |
| DailyRequesterCount | Exactly 4,400 distinct planned requesters per FestivalDay |
| FestivalDays | 5 |
| DailyParticipationContinuity | 90% |
| FrontStandingAllowedRequestShare | 50% of daily requests/groups |
| ExecutionMode | Sequential |
| ConcurrencyLevel | 1 |
| PhysicalCapacity | 4,998 Spots, with the exact shared S2 row topology |
| Engine | Assignment Engine v1 (`v0.3.0` baseline), real Application/PostgreSQL path |
| Initial history | Clean Day 1, with no prior persisted Assignments or synthetic history |
| Days 2–5 history | Each run's own naturally accumulated persisted Assignments |

Preserve the existing Zone quality mapping, all fairness policies, RotationScore,
GroupRotationScore, TargetQuality, AssignmentSelectionPolicy, BlockSelectionPolicy,
rubric, recovery semantics and metric formulas. Do not copy S2 outcomes/history
into S6 or reset history between days. Diverging histories and inventory are
observed consequences, not additional input interventions.

Participation continuity retains exactly 3,960 prior-day planned requesters and
replaces 440 from the registered complement, using the existing deterministic
procedure independently of Assignment outcomes. Preserve the current generation
infrastructure and its versioned deterministic algorithms, integer allocation,
range sampling and population reconciliation; no generator redesign is required.

The preserved request-level FrontStanding share uses the existing largest-remainder
allocation and seeded shuffle. Integer rounding may prevent a perfectly even
split for an odd request count. Do not change the 50% target, rounding contract
or eligibility policy, or reinterpret it as an attendee share.

## Group-size transformation

Percentages count **requests/groups**, each counted once regardless of GroupSize;
they do not count Attendees. Combined ranges are target buckets, with the existing
seeded selection of concrete sizes inside each range.

| GroupSize | S2 control | S6 target |
| --- | --- | --- |
| 1 | 15% | 10% |
| 2 | 30% | 20% |
| 3 | 25% | 20% |
| 4 | 15% | 15% |
| 5 | 8% | 10% |
| 6–7 | 5% | 15% |
| 8–10 | 2% | 10% |
| Total | 100% | 100% |

Large groups (6–10) increase from **7% to 25% of requests**: an 18 percentage-point
increase in the target profile. No other workload distribution is changed.

For later implementation, exactly **4,400 requesters per FestivalDay** remains a
hard invariant, not an approximate goal. Group-size percentages remain request-level
targets. The existing deterministic group generator must preserve the exact
attendee population, use valid sizes 1..10, and produce deterministic output while
approximating the target profile as closely as possible under that contract.
Do not force impossible exact percentages, fix the request count to S2's count,
or change the generation contract to fit this profile. Future evidence must
report realized request and attendee counts by category alongside target shares.

## Unchanged arrival profile and processing order

Use the exact S2 arrival-band targets over daily requests:

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

Preserve RequestedAt generation, half-open UTC simulated business-time bands,
the 07:00–23:00 window, integer allocation and existing AssignedAt convention.
Different request counts may produce different concrete band counts/timestamps;
the arrival profile and generation semantics remain unchanged.

Normal S2 order is RequestedAt ascending, then SimulationRequestId ascending,
with baseline Sequence and effective ExecutionSequence in that same order:

```text
BaselineSequence == ExecutionSequence
```

Do not apply the S5 reversal. S6 is not an arrival-order experiment and adds
neither concurrency nor a new processing-order rule.

## Unchanged venue topology

Reuse the [shared S2 topology](representative-scenarios.md#shared-quality-capacity-and-physical-topology),
including row geometry, Spot identities and the existing Zone quality mapping:

| ZoneType | Existing quality | Spots |
| --- | --- | --- |
| FrontStanding | Good | 750 |
| MiddleCenter | Good | 1,002 |
| MiddleLeft | Medium | 750 |
| MiddleRight | Medium | 750 |
| UpperCenter | Medium | 750 |
| UpperLeft | Bad | 498 |
| UpperRight | Bad | 498 |
| Total | | 4,998 |

Good = 1,752, Medium = 2,250 and Bad = 996 Spots, unchanged across scenarios and
all five days. Preserve full-group placement in one Zone, one Row and consecutive
SpotNumbers. Total venue capacity does not guarantee eligible or contiguous capacity.

## Reproducibility and comparison limits

Future execution uses the same Seeds as S2 and S5:

```text
S2 / 1001 ↔ S6 / 1001
S2 / 2001 ↔ S6 / 2001
S2 / 3001 ↔ S6 / 3001
```

Compare each same-Seed pair before summarizing cross-Seed variation. Preserve
the same FirstDate, five-day calendar, generator version and preserved baseline
parameters. Record definitions, exact consumed manifests, initial state, engine,
repository/rubric versions and existing evidence provenance under the architecture's
reproducibility contract; a Seed alone is insufficient.

These are **controlled scenario-level comparisons, not exact request-level pairs**.
Unlike S5, S6 intentionally changes grouping. Do not require the same request
identities, group membership, request count or attendee/group composition.
Preserve the registered population, daily requester count, continuity rule,
arrival profile, eligibility share, topology, Engine and execution mode.

Generator streams include ScenarioId as well as Seed; equal Seeds alone do not
guarantee equal daily requester cohorts, membership, eligibility flags or timestamps.
Where deterministic attendee cohorts are demonstrably comparable, they may be
analyzed using verified stable identities and explicit intersection/coverage
counts. Do not imply exact request equivalence or assume identical daily cohorts.

## Future metrics and diagnostic evidence

Use existing Stage 5 formulas unchanged. Report numerator/denominator counts and
normalized rates per scenario and same-Seed comparison, with N/A for unavailable
evidence or zero denominators. Do not substitute raw counts for rates when request
counts and group composition differ.

| Comparison | Required metrics and existing denominator semantics |
| --- | --- |
| Feasibility | SuccessRate / RejectionRate (the report labels for AssignmentSuccessRate / AssignmentRejectionRate); Successful / DomainRejected requests over NormallyProcessedRequests. Report conflicts and technical failures separately. |
| Quality | GoodOutcomeRate, MediumOutcomeRate, BadOutcomeRate over persisted evaluated Assignments. |
| Path fairness | BalancedPathRate, AcceptablePathRate, NotBalancedPathRate over ClassifiablePaths; retain no-path and insufficient-evidence coverage. |
| Recovery | RecoveryRate over recovery-eligible episodes; AverageOpportunitiesToRecovery and MedianOpportunitiesToRecovery over recovered episodes, using assigned opportunities rather than calendar days. |

Explicitly inspect DomainRejectedRequests, existing rejection reasons, eligible
capacity, block-found observations and remaining total capacity. The key question
is: **do larger groups cause rejection even while total Spot capacity remains?**
Use the state actually observed at the request evaluation, distinguishing
insufficient eligible capacity from no complete feasible block despite sufficient
eligible raw capacity. Missing observations remain unavailable; no new telemetry
or production rejection codes are introduced by this definition.

Remaining venue-wide capacity alone does not establish contiguous-block pressure
or fragmentation. Apply the framework's existing physical-feasibility diagnosis
only when eligible-capacity and block-found evidence supports it, retaining the
baseline's independent pre-save-read consistency limitation. Do not classify a
rejection as fragmentation from group size or end-of-day totals alone.

### Outcomes by group-size category

Future analysis must report categories **1, 2, 3, 4, 5, 6–7 and 8–10**. For each,
report request count and attendee participation count, SuccessRate, BadOutcomeRate
and NotBalanced association where evidence permits. Keep planned requester
memberships separate from persisted attendee participations so rejected groups
remain visible. Use category-specific denominators for SuccessRate and
BadOutcomeRate under the existing formulas.

For NotBalanced association, relate category participations to the Attendee's
stored final path classification; do not classify individual requests as
NotBalanced. Report the classified population and denominator explicitly,
separating no-path/insufficient evidence. Attendees can change group-size category
across days, so distinct category populations are not additive. This descriptive
association does not establish an individual group-size causal effect.

### Daily capacity evolution

Compare existing S2/S6 end-of-day checkpoints after each drained daily plan:
**remaining Good, remaining Medium, remaining Bad and remaining total**.
Inspect quality-specific consumption, remaining inventory and rejection behavior
alongside request observations. Identical residual totals can hide different
quality consumption or block geometry; end-of-day capacity cannot establish the
exact fragmentation state.

## S6 versus S7 and interpretation boundaries

S6 changes **large-group prevalence** under the S2 operating baseline. S7 will
later isolate **high fragmentation**; its detailed behavior is not defined here.
S6 may expose fragmentation-related signals, but it is not itself the
fragmentation experiment.

S6 may support conclusions about sensitivity to large-group prevalence under
these controlled, provisional S2 operating conditions. It cannot alone prove a
real festival group distribution, fragmentation causality, arrival-order causality
or concurrency behavior. Same-Seed variation and descriptive group-size
associations must retain those limits.

## Validation and scope boundary

Contract checks: S2 and S6 group shares each sum to 100%; their 6–10 shares are
7% and 25%; daily demand remains exactly 4,400; the arrival profile sums to 100%
and matches S2; request-level eligibility remains 50%; normal baseline/execution
order is retained; topology remains 4,998 Spots with unchanged quality capacity.
These are documentation checks, not S6 execution results.

This task adds no S6 ScenarioDefinition, implementation or execution. It modifies
no generation, materialization, manifest, runner, production layer, assignment
policy, fairness formula, rubric, recovery semantics or metrics calculator.
S2/S5 behavior remains unchanged. S7/S8 implementation, concurrency, fairness
tuning, new telemetry and Decision Gate logic/thresholds remain outside scope.
Implementation, execution and evidence-based S2/S6 analysis are deferred.
