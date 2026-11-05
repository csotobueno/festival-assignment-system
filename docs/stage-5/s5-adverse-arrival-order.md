# S5 — Adverse Arrival Order

## Status, authority and question

**Counterfactual analytical scenario — contract defined; implementation and
execution pending.** S5 is an experiment under the exact S2 workload, not a
proposed production policy or a realistic chronological arrival simulation.

Its question is: keeping the exact same daily workload, how much do Engine v1
outcomes change when the request processing order is reversed?

The [S2 representative contract](representative-scenarios.md#s2--expected-baseline)
owns the baseline inputs. The [metrics framework](metrics-and-evaluation-criteria.md)
owns populations, formulas, rubric and recovery semantics; the
[simulation architecture](simulation-architecture.md) owns composition and
evidence flow. S5 changes none of those policies or analytical definitions.

## Evidence motivating the experiment

The [official S2 path analysis](s2-fairness-path-analysis.md) found that **100% of
persisted Bad outcomes occurred in the final third of daily requests** in each
of Seeds 1001, 2001 and 3001. Larger groups also had higher Bad rates, including
within that broad timing stratum. Timing/request position was the strongest
recurring association motivating this experiment.

This is motivation, not evidence that request position has already been proven
causal. S5 will test processing-order sensitivity with the other workload inputs
held fixed; it does not establish real-world arrival behavior.

## Exact S2 control

For each seed, use the exact S2 materialized daily workload. S5 must not generate
an independent workload or regenerate attendees, groups, membership, eligibility,
continuity, timestamps or arrival bands.

Preserve the S2 definition, Seed, GeneratorVersion and FirstDate used to produce
the paired baseline, including all stable catalog and SimulationRequestId
identities. Do not substitute an S5 ScenarioId into S2 materialization: generation
uses scenario identity, so that would produce different input. The S5 experiment
and its run can have their own labels, while retaining the S2 input provenance.

The unchanged controls include:

- RegisteredPopulation = 5,500; DailyRequesterCount = 4,400; FestivalDays = 5.
- DailyParticipationContinuity = 90%; FrontStandingAllowedRequestShare = 50%.
- ExecutionMode = Sequential; ConcurrencyLevel = 1.
- Exact registered and daily requesting Attendees, request/group identities,
  ordered membership, GroupSizes and request-level RequestEligibility.
- Exact RequestedAt and AssignedAt values, FestivalDay identities/dates/windows,
  original arrival bands and S2 arrival profile.
- Shared 4,998-Spot topology, Zone types, Spot identities/inventory and the
  existing Zone quality mapping.
- Engine v1 (`v0.3.0` baseline), fairness policies, rubric, recovery semantics,
  aggregate formulas and real Application/PostgreSQL composition.

The full group-size, arrival-profile and topology tables remain in the
[S2 contract](representative-scenarios.md); they are not duplicated or adjusted
here. Their stakeholder-validation status is unchanged.

Each run starts independently with clean Day 1 prior history. Days progress in
the same chronological order, preserving each run's natural prior-day Assignments
through Days 2–5. S5 must not import S2's resulting Assignments as its history or
reset history between days. Any divergence in later history, inventory consumption
or outcomes is a result of the ordering intervention, not another changed input.

## Deterministic daily reversal

For each FestivalDay independently, take requests in their exact normal S2
deterministic execution order, including its existing tie-breaks, and reverse
the complete ordered list:

```text
S2: R1 → R2 → ... → Rn
S5: Rn → ... → R2 → R1
```

An S2 daily position `i` maps to S5 daily execution position `n + 1 - i`.
FestivalDay order is unchanged. Await each request's terminal outcome before
presenting the next; do not add concurrency, delays, retries or regrouping.
Only the daily request processing order changes.

Full reversal is simple, deterministic, transparent and reproducible. It uses
neither outcome knowledge nor a sort key targeting group characteristics.
Do not replace it with largest-groups-last, FrontStanding-excluded-last or
worst-score-attendees-last. Those are different experiments that would target
particular workload characteristics and require separate contracts.

## Original timestamps versus execution order

Future execution and analysis must retain these distinct meanings:

| Term | S5 meaning |
| --- | --- |
| Original simulated arrival time / RequestedAt | The exact timestamp from S2; unchanged. |
| Original arrival band | The S2 band containing that RequestedAt; unchanged. |
| Baseline S2 sequence position | The original S2 request Sequence and its rank within its FestivalDay. The existing Sequence is global across days. |
| ExecutionSequence / S5 execution position | The actual order requests are presented to Engine v1 in S5, with an explicit per-day rank. Authoritative for S5 processing order. |
| AssignedAt | The original simulated S2 value, retaining AssignedAt = RequestedAt. |

`ExecutionSequence` is a contract term for actual dispatch order, not a new
implemented field in this task. Future evidence must preserve the baseline
sequence and distinguish it from the effective S5 sequence. The current runner
sorts requests by Sequence; merely reversing an array that it subsequently sorts
back into baseline order would not implement this contract.

For example, for a day whose first and last requests have these original times:

| Request | RequestedAt = AssignedAt | S2 position | S5 execution position |
| --- | --- | --- | --- |
| A | 09:00 | 1 | n |
| B | 18:00 | n | 1 |

This does not mean B was physically processed in the morning. S5 preserves
original simulated timestamps while executing requests in reverse order.
RequestedAt / AssignedAt must not be used as S5 processing-time chronology;
ExecutionSequence is authoritative. Persistence can therefore occur in a
different order from chronological AssignedAt. Do not rewrite either timestamp
to manufacture an execution chronology or a new hourly distribution.

Use wording such as “original arrival band = 18:00–21:00; S5 execution position =
first third,” not “processed at 18:00.” Observed wall-clock timings remain
separate runtime evidence and do not replace these simulated business values.

Every Attendee participates at most once per FestivalDay in the preserved
workload. Daily reversal introduces no additional same-day participation or
Assignment. Historical fairness remains driven by persisted prior-day Assignments;
no special intraday attendee-history logic is needed. Existing attendee-path
chronology and recovery interpretation remain unchanged across FestivalDays.

## Reproducibility and proof obligations

S5 is reproducible from the S2 ScenarioDefinition, Seed, GeneratorVersion and
the full daily reversal transformation, with the same explicit FirstDate and
initial-state controls as the baseline. A seed alone is insufficient.

Future implementation must retain the exact baseline manifest and identify its
paired source RunId/evidence and ordering transformation. Before treating a run
as S5 evidence, prove per FestivalDay:

1. The request set is identical, with each SimulationRequestId appearing once.
2. Each request's contents are identical: members and member order, group size,
   eligibility, RequestedAt, AssignedAt and FestivalDay. Catalog/topology and
   workload-generation metadata remain the same.
3. The effective execution sequence is the exact reverse of baseline order;
   only order/sequence differs, with baseline positions retained for comparison.

Using the S2 definition to materialize input must reproduce the exact retained
baseline; a changed workload must not silently replace the official control.
Runtime-generated persisted AssignmentRequestIds/AssignmentIds, RunIds and
observed durations can differ. They are outputs, not workload identity keys.
Manifest-loading/replay support and the concrete sequence/evidence representation
remain implementation work; this document does not claim they already exist.

## Paired comparison protocol

Keep each seed pair explicit, using the official S2 controls referenced by the
[results report](s1-s2-representative-results.md):

| Control | Counterfactual |
| --- | --- |
| S2 / 1001 | S5 / 1001 |
| S2 / 2001 | S5 / 2001 |
| S2 / 3001 | S5 / 3001 |

Report each pair's values, applicable populations/denominators and differences
before cross-seed summaries. Do not primarily compare scenario-level averages
that hide seed pairing. Using the existing formulas, compare at least:

- SuccessRate and RejectionRate.
- GoodOutcomeRate, MediumOutcomeRate and BadOutcomeRate.
- BalancedPathRate, AcceptablePathRate and NotBalancedPathRate.
- RecoveryRate, AverageOpportunitiesToRecovery and MedianOpportunitiesToRecovery.
- Daily remaining capacity, including available quality counts.
- Bad outcome position by actual execution sequence, separately from original
  S2 sequence, original arrival time and original arrival band.

Use the same declared daily-rank segments for both runs, such as the S2
analysis's first/middle/last thirds. Preserve existing null/N/A handling and
metric denominators; outcomes may change those denominators even though the
request workload is identical. No new fairness formula or threshold is defined.

### Individual outcome sensitivity

Similar aggregate rates can conceal a change in who receives favorable or
unfavorable outcomes. Future analysis must identify which Attendees receive
Good, Medium and Bad in each paired run, joining equivalent attendee/FestivalDay
participations and stable SimulationRequestIds rather than generated business IDs.

Where both paired participations have persisted Assignments, report same quality,
improved quality in S5 or worsened quality in S5 using the existing Good / Medium /
Bad ordering. Retain the transition counts and denominators. If either side has
no Assignment, report that outcome separately with its request status; do not
invent a quality, treat absence as Bad or silently drop unmatched outcomes.

Also compare each evaluated Attendee's stored S2 classification with its stored
S5 classification, retaining path-length/coverage differences and N/A context.
This is comparison of existing analytical outputs, not a change to the rubric
or recovery algorithms. These comparisons are requirements for later analysis,
not metrics implemented by this documentation task.

## Interpretation boundaries and other scenarios

S5 can support conclusions about sensitivity to processing order under the exact
S2 workload and these paired seeds. It cannot by itself establish realistic
operating chronology, real-world arrival behavior, arrival-time causality,
group-size causality or fragmentation causality. A similar aggregate result
would not establish identity-level stability; a changed result would not make
reverse order a proposed production operating mode.

S5 preserves the exact workload and changes processing order. S6 would change
the group-size workload to isolate large-group prevalence. Do not mix those
experiments. S5 does not change S2 arrival bands, reverse timestamps, change
hourly percentages, add fragmentation controls or introduce concurrency.

## Scope

This contract adds no S5 implementation or execution, telemetry, new fairness
formula, Engine tuning, Decision Gate logic or S6/S7/S8 implementation. No
ScenarioMaterializer, runner, manifest, generator, S1/S2 definition, production
layer, fairness policy, rubric, recovery semantics or aggregate implementation
changes. Existing S2 evidence remains intact. Implementation and paired execution
are deferred to a separately scoped task.
