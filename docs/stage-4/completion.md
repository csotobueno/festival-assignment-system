# Stage 4 — Assignment Engine and Fairness: Completion

## Status and Closure Decision

**Stage 4: Completed.**
**Stage 5: Pending.**

This is the authoritative Stage 4 completion document, reviewed on
**2026-10-21** against current code, tests and documentation. All remaining
Stage 4 exit criteria are satisfied. The seven deterministic integrated
acceptance scenarios establish functional closure evidence.

An explicit Lean scope decision revises the former Step 26 requirement:
[five-day evaluation belongs to Stage 5](implementation-plan.md#step-26--defer-five-day-evaluation-to-stage-5).
No five-day presimulation was run or claimed as passed. It is no longer a Stage 4
exit criterion. There is no remaining formal closure blocker.

Stage 4 is formally complete and ready to be tagged as the Stage 5 baseline.
This task does not create a tag or begin Stage 5.

## Lean Decision on Five-Day Evaluation

The small five-day presimulation was originally intended to detect obvious
model failures before Stage 5. After completing the integrated implementation
and acceptance suite, a separate presimulation no longer reduces enough
additional uncertainty to justify its setup and interpretation cost. Running
a smaller five-day simulation immediately before the fuller Stage 5 simulation
would duplicate setup, scenarios and interpretation work.

Therefore, Step 26 is explicitly deferred to Stage 5 rather than executed as
a Stage 4 prerequisite. Stage 4 acceptance evidence establishes correctness of
representative deterministic behavior; Stage 5 simulation evaluates multi-day
quality and technical viability. Functional tests remain Stage 4 evidence.
This scope decision changes no fairness rule, formula or production policy.

## Objective and Implemented Baseline

Stage 4 builds the core assignment engine and fairness baseline and integrates
it into the real Application flow. The delivered strategy is **online,
deterministic, known-state only, fairness-aware, group-preserving,
eligibility-aware, inventory-aware, physically feasible and historically stable**.
It is a minimum credible baseline, without a claim of global optimality,
statistical fairness, scalability or operational readiness.

Detailed definitions remain in [Fairness Definition v1](fairness-definition-v1.md),
[RotationScore v1](rotation-score-v1.md), [Assignment Strategy v1](assignment-strategy-v1.md)
and the [Application cutover decision](../architecture/stage-4-application-cutover-boundary.md).
The official fairness-rule count remains **19**; block selection is a capacity
heuristic and adds no fairness rule.

## Final Runtime Architecture

```text
AssignmentRequest
        ↓
resolve complete attendee group
        ↓
FairnessHistory (one per member)
        ↓
RotationScore (individual recovery need)
        ↓
GroupRotationScore (arithmetic mean)
        ↓
TargetQuality
        +
Zone eligibility + RemainingInventoryState + available Spots + GroupSize
        ↓
AssignmentSelectionPolicy
        ↓
AssignmentSelection
        ↓
AssignmentEngine
        ↓
Assignments with persisted ExperienceQuality
        ↓
request outcome + all Assignments → one atomic UnitOfWork save
```

This is conceptual data flow; Application prepares eligibility, scores, target
and inventory as separate inputs. Incomplete attendee resolution rejects the
whole request before group creation or selection. External callers supply
business-request data, never a prepared selection. A null selection rejects
without Assignments using `NO_CONTIGUOUS_SPOTS_AVAILABLE`.

The active orchestration is
[`ProcessAssignmentRequestUseCase`](../../src/Festival.Application/Assignments/ProcessAssignmentRequest/ProcessAssignmentRequestUseCase.cs).
Domain owns the pure policies, Application owns preparation and outcome
orchestration, Infrastructure implements the read/write ports, and API remains
the composition root. The Stage 3 atomic persistence boundary is preserved.

## Fairness and Historical Experience

| Concept | Implemented responsibility | Detailed definition |
| --- | --- | --- |
| ExperienceQuality | `Good`, `Medium`, `Bad`; actual recorded outcome. | [Global Zone mapping](assignment-strategy-v1.md#agreed-zone-taxonomy-and-global-mapping) |
| FairnessHistory | Ordered real recorded outcomes only; absence contributes no entry. | [Recorded history](assignment-strategy-v1.md#2-load-historical-fairness-state) |
| RotationScore | Individual recovery need, independent of eligibility and inventory. | [Baseline formula](rotation-score-v1.md#baseline-formula) |
| GroupRotationScore | Arithmetic mean of member scores; neutral members contribute zero. | [Group score](rotation-score-v1.md#group-rotationscore) |
| TargetQuality | `Good` or `Medium`, using the existing inclusive threshold; reference, neither entitlement nor ceiling. | [Target policy](assignment-strategy-v1.md#3-determine-target-quality) |
| AssignmentSelectionPolicy | Actual Zone/block placement and actual quality. | [Selection composition](assignment-strategy-v1.md#pure-assignment-selection-composition) |

The final invariant is:

```text
every valid Assignment → one explicit immutable ExperienceQuality
```

Quality is mandatory at creation, persisted as required enum-name text and
explicitly restored through the validated Assignment constructor during EF
rehydration. Historical quality remains stable and is never reconstructed from
current Zone classification. The batched `IFairnessHistoryProvider` read boundary
supplies recorded outcomes in AssignedAt order, then AssignmentId for timestamp
ties. It performs no current Zone lookup.

```text
no reliable recorded history → FairnessHistory = [] → RotationScore = 0
```

The neutral target does not fabricate a Medium experience. Only a successful
assignment records actual selected quality; absences and rejected requests
create no synthetic outcomes. Disposable pre-cutover data without reliable
quality has no synthetic backfill, as specified by the
[bootstrap and transition decision](../architecture/stage-4-application-cutover-boundary.md#assignment-experiencequality-transition-and-bootstrap).
Historical stability is exercised by
[`AssignmentPersistenceTests`](../../tests/Festival.Infrastructure.IntegrationTests/Persistence/AssignmentPersistenceTests.cs)
and recorded-history reads by
[`PostgreSqlFairnessHistoryProviderTests`](../../tests/Festival.Infrastructure.IntegrationTests/Assignments/PostgreSql/PostgreSqlFairnessHistoryProviderTests.cs).

## Placement, Eligibility and Physical Feasibility

`AssignmentSelectionPolicy` is the only production placement decision path. It
composes Zone ordering, discovery of every complete block in the first feasible
ordered Zone, and block selection. `AssignmentEngine` only materializes that
selection, checks its size against GroupSize and validates the complete result.
It no longer finds the first contiguous block, evaluates Zones, calculates
fairness or chooses placement. Legacy placement and its helpers are removed.

`RequestEligibility` filters Zone opportunities through `ZoneEligibilityPolicy`.
The implemented request-wide Front Standing exclusion does not change ZoneType,
reclassify ExperienceQuality or create request-relative Zone quality. Other
operational eligibility policies require concrete requirements.

`RemainingInventoryState` describes current available raw inventory through
ZoneAvailabilityState entries and derived quality totals. Application currently
calculates it over the current catalog and supplied available Spots; eligibility
filters participating Zones separately. `FeasibleSpotBlock` and the selection
pipeline establish complete-group physical feasibility:

```text
raw capacity != physical feasibility
```

Same Zone, same Row, consecutive SpotNumbers and exact GroupSize remain mandatory.
The selected Zone follows the existing
[Zone Evaluation Strategy](assignment-strategy-v1.md#11-zone-evaluation-strategy-v1).
Within it, the [block policy](assignment-strategy-v1.md#block-selection-policy-v1)
preserves the largest remaining contiguous run in the affected Row, with the
documented deterministic ties. This local heuristic does not optimize all future
capacity. Groups are never split or restructured automatically.

## Global Assignment State and Previously Blocked Issues

`CurrentGlobalAssignmentState` **was not implemented**. Stage 4 does not consume
global assigned-count state as an input to placement. Persisted
`Assignment.ExperienceQuality` supplies the source from which such state could
later be derived if Stage 5 evidence justifies it. Its shape and population/time
scope remain unresolved, including whether granularity should be per
ExperienceQuality, per Zone, global totals or some combination. Earlier sketches
are not an agreed contract. `ZoneAssignmentState` is also unimplemented.

| Previously blocked issue | Final Stage 4 disposition |
| --- | --- |
| Record Experience Quality on Assignment | Implemented during the coordinated Stage 4 cutover. |
| Create Assignment from Stage 4 Selection | Implemented during the coordinated Stage 4 cutover. |
| Build Current Global Assignment State by Experience Quality | Not required by the Stage 4 baseline; intentionally deferred pending Stage 5 evidence. Not implemented. |

## Integrated Acceptance Evidence

[`ProcessAssignmentRequestUseCasePostgreSqlTests`](../../tests/Festival.Infrastructure.IntegrationTests/Assignments/PostgreSql/ProcessAssignmentRequestUseCasePostgreSqlTests.cs)
contains **7 deterministic scenarios**, selected with `Category=Stage4Acceptance`:

1. Neutral bootstrap without recorded history.
2. Historical recovery using persisted unfavorable outcomes.
3. Eligibility restriction with unchanged selected Zone quality.
4. Better-than-target assignment when Good inventory is abundant.
5. Degraded assignment when preferred quality lacks a complete block.
6. Raw capacity without contiguous feasibility rejects the whole group.
7. Complete group materialization and per-member persisted quality/history.

These exercise the real Application flow with PostgreSQL, committed outcomes
and historical reads. They establish representative deterministic correctness
and are the final functional acceptance evidence for Stage 4. Statistical
fairness, scalability and five-day simulation results remain Stage 5 evaluation.

## Validation Evidence

Revalidated on **2026-10-21** using .NET 8 and Docker 24 / API 1.43 with isolated
PostgreSQL Testcontainers:

| Validation | Verified result |
| --- | --- |
| `dotnet build Festival.sln` (the `make build` target) | Passed; 0 warnings, 0 errors. |
| `DOCKER_API_VERSION=1.43 dotnet test Festival.sln` | Passed; 696 passed, 0 failed, 0 skipped. |
| Fast-project subtotal in that full run | 619 passed: Domain 490, Application 43, Infrastructure 86. |
| PostgreSQL integration project in that full run | 77 passed, including existing concurrency regressions. |
| `Category=Stage4Acceptance` filtered integration run | Passed; 7/7, 0 failed, 0 skipped. |
| `git diff --check` | Passed. |
| Changed Markdown local links/anchors | Passed; 146 local links/anchors across 10 Markdown files, including the Step 26 scope revision. |

The supplied 619/77/696 baseline counts remain current. Separate `make test-fast`
and `make test-integration` invocations were not needed: the full solution run
executed their constituent projects. The full run additionally used a TRX logger
and `/tmp/festival-stage4-closure-results` for reviewable test results. The first
sandboxed test attempt could not bind MSBuild sockets; the successful rerun had
the required socket and Docker access. No tests were added or modified.
The repository provides no Markdown/local-link validation target; a one-off
read-only check verified local destinations and heading anchors in all changed
Markdown files, including this new checkpoint.

## Exit Criteria Audit

Checked criteria are supported by current implementation and the passing suites
above. Source/test directories identify the evidence without copying formulas
or implementation details into this checkpoint.

- [x] Fairness Definition v1 documented; official count remains 19.
- [x] ExperienceQuality and ordered FairnessHistory implemented.
- [x] RotationScore and its reference recovery scenarios implemented and tested.
- [x] GroupRotationScore arithmetic mean implemented and tested.
- [x] TargetQuality policy implemented for individuals and groups.
- [x] ZoneType and global Zone quality classification implemented.
- [x] Request eligibility implemented before placement without reclassification.
- [x] Remaining inventory model implemented and used in selection.
- [x] Zone evaluation implements the quality/count/ZoneId ordering.
- [x] Feasible block discovery generates complete same-Zone, same-Row blocks.
- [x] First ordered Zone with feasible blocks is authoritative.
- [x] Block selection strategy implemented before engine materialization.
- [x] AssignmentSelectionPolicy composes the production placement path.
- [x] Stage 4 selection integrated into Application.
- [x] Assignment.ExperienceQuality persisted and explicitly rehydrated.
- [x] Recorded fairness-history read boundary implemented; history affects decisions.
- [x] AssignmentEngine reduced to materialization; legacy placement removed.
- [x] Complete-group semantics, atomic persistence and database protections preserved.
- [x] Better-than-target and degradation behavior supported without speculative reservation or artificial punishment.
- [x] Existing concurrency regression tests passing.
- [x] Integrated Stage 4 acceptance suite passing.
- [x] CurrentGlobalAssignmentState excluded from v1 and explicitly deferred.
- [x] Known trade-offs, provisional policies and Stage 5 questions documented.
- [x] Five-day evaluation explicitly assigned to Stage 5 by the Lean scope decision; removed as a Stage 4 execution requirement.
- [x] Formal Stage 4 closure and completed-baseline handoff documented.

Implementation evidence is in
[`Festival.Domain/Assignments`](../../src/Festival.Domain/Assignments),
[`Zones`](../../src/Festival.Domain/Zones),
[`Spots`](../../src/Festival.Domain/Spots), their
[`Domain tests`](../../tests/Festival.Domain.Tests), the
[Application use case](../../src/Festival.Application/Assignments/ProcessAssignmentRequest/ProcessAssignmentRequestUseCase.cs)
and the [PostgreSQL integration suite](../../tests/Festival.Infrastructure.IntegrationTests).
The [implementation plan](implementation-plan.md) retains Step 26 as an explicit
deferral recording the changed scope and rationale. All remaining Stage 4 exit
criteria are verified; no simulation result is inferred from acceptance tests.

## Accepted Trade-offs and Known Limitations

The [trade-offs document](trade-offs-and-open-questions.md#accepted-trade-offs)
owns the rationale. Stage 4 accepts online assignment instead of festival-wide
optimization; no daily batch or hybrid model; no unknown future requests or
speculative reservation; incremental fairness; three quality levels;
provisional RotationScore weights and TargetQuality threshold; deterministic
tie-breaking; and no automatic group restructuring. Group averaging may hide
individual recovery differences. Closure does not reopen these decisions.

- **Arrival order:** online final decisions consume capacity, so request order
  may affect final outcomes. Stage 5 must measure the magnitude.
- **Read snapshot consistency:** Zones, available Spots and fairness histories
  may be read at different moments. There is no explicit consistent read
  snapshot across them; one scoped DbContext does not supply that guarantee.
- **Concurrency:** database integrity protections and regression tests exist.
  Realistic concurrent behavior, conflict frequency and operational rejection
  characteristics still require Stage 5 evaluation. A higher recovery score
  does not guarantee winning a concurrent commit race.
- **Global fairness:** each decision uses known current state without computing
  a festival-wide optimum or consuming global assigned-count inputs.
- **Policy calibration:** weights and thresholds remain empirical hypotheses.
- **Fragmentation:** raw capacity is not group feasibility; local block ranking
  does not guarantee optimal future capacity or eliminate deterministic bias.

## Stage 5 Boundary and Deferred Work

**Stage 4:** Does the implemented assignment strategy behave correctly?

**Stage 5:** How well does the strategy behave across five days, scale,
variation, capacity pressure, arrival order, performance and concurrency?

Stage 5 remains **Pending**. Its evaluation covers five-day simulation, large
attendee populations, arrival-order sensitivity, fairness distributions,
recovery statistics, group-size effects, inventory consumption, fragmentation
at scale, performance, concurrency/stress, algorithm comparison and the final
technical decision gate. The concerns formerly assigned to Step 26 are evaluated
within that fuller simulation:

- persistent Bad paths;
- attendees never receiving Good;
- unused Good capacity and Good-capacity utilization;
- excessive early Good-capacity consumption;
- pathological group behavior across populations;
- arrival-order effects;
- unexpected score behavior across multiple days and populations.

Statistical fairness analysis and policy calibration remain Stage 5 work. These
evaluations were deferred; the seven functional acceptance scenarios and existing
unit, Application, persistence and concurrency regression tests remain Stage 4
evidence. No separate Stage 4 presimulation is required.

Possible responses to measured findings include CurrentGlobalAssignmentState,
ZoneAssignmentState, weighted randomness, batch or hybrid assignment, global
optimization, stronger fragmentation optimization or additional global fairness
inputs. These are candidates, not planned implementation requirements. Define
their minimum scope only when evidence demonstrates a real need.

## Scope and Assumptions

This checkpoint documents existing work only. No production code, tests, EF
mappings, migrations, DI, orchestration or policies changed. No simulator,
performance test, stress test or concurrency mechanism was introduced. Current
repository code and executed suites support implementation claims. The explicit
scope revision moves five-day evaluation to Stage 5; no presimulation evidence
is fabricated or inferred from seeded history. Stage 5 measurements and
organization approval are not assumed.

The glossary's original request-relative quality wording and stale
not-yet-implemented target-policy statements are aligned with the already
agreed global mapping and implemented cutover. No definition or policy is newly
designed here. Earlier speculative global-state sketches are replaced with an
explicit unresolved-granularity boundary, without implementing that state.

The only revision to the original Stage 4 validation scope is the explicitly
authorized Step 26 deferral. There is no remaining or deferred Stage 4 closure
work; Stage 5 owns the empirical evaluations listed above.
