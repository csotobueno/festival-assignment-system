# Stage 5 — Simulation Architecture and Reproducibility Strategy

## Status, Purpose and Authority

**Architecture agreed; minimal sequential development slice implemented;
full simulation platform and empirical evaluation remain pending.**
This document defines the minimum planned architecture for evaluating
`v0.3.0 — Fair Assignment Engine`. The current slice includes immutable manifests,
attempts, daily checkpoints and individual attendee-path reconstruction and
classification in memory. Aggregate metrics, recovery observations and artifact
export remain deferred. No simulation analysis changes production behavior.

The [Stage 5 metrics and evaluation framework](metrics-and-evaluation-criteria.md)
is authoritative for analytical outcomes, populations, denominators,
PathClassificationRubric v1, formulas, thresholds, scenarios and the Decision
Gate. This document owns simulation architecture and evidence flow. The
[Stage 4 completion checkpoint](../stage-4/completion.md) owns the implemented
baseline and its acceptance evidence; the [glossary](../glossary.md) and
[critical invariants](../critical-invariants.md) retain their responsibilities.
The official fairness-rule count remains **19**.

The architecture must exercise real assignment behavior, preserve reproducible
inputs and explain aggregate results through individual observations. Small
development scenarios and festival-scale evidence use the same execution path.

## Project Boundary and Composition

`Festival.Simulation` will be a dedicated tooling executable. Its responsibilities
are scenario loading/generation, materialization, daily request planning,
execution, observation, evaluation and export. SimulationAttemptRecord,
DailySimulationCheckpoint, AttendeePathRecord and the analytical rubric belong
to this tooling boundary. They are not production Domain concepts or business
database tables.

Planned project references preserve the existing inward dependency direction:

```text
Festival.Simulation
├── Festival.Application → Festival.Domain
├── Festival.Infrastructure → Festival.Application + Festival.Domain
└── Festival.Domain (existing factories/types used to materialize inputs)

Festival.Domain → no project dependencies
```

There is no reference to `Festival.Api`,
`Festival.Infrastructure.IntegrationTests` or xUnit, and no production project
references Simulation. The API remains the HTTP composition root; Simulation
owns composition for its tooling process.

> Simulation reuses the real Application composition and the implemented
> PostgreSQL Infrastructure composition intended for persistence evidence,
> without depending on API/HTTP hosting or test infrastructure.

Concretely, use [AddApplication()](../../src/Festival.Application/DependencyInjection.cs)
and [AddPostgreSqlPersistence(configuration)](../../src/Festival.Infrastructure/DependencyInjection.cs),
supplying `ConnectionStrings:FestivalDatabase`. Reuse the existing repositories,
providers, FestivalDbContext, EF mappings, migrations and EfCoreUnitOfWork.
The [current API](../../src/Festival.Api/Program.cs) selects
`AddInMemoryInfrastructure()`; this plan does not claim identical API persistence
composition. **PostgreSQL/Testcontainers is the only planned simulation
persistence mode.**

The evaluated path is:

```text
ScenarioManifest → DailyRequestPlan → ScenarioExecutor
                                           ↓
                          ProcessAssignmentRequestUseCase
                                           ↓
                          real Application + Domain policies
                                           ↓
                          real PostgreSQL Infrastructure
                                           ↓
                                      PostgreSQL

attempt observations + checkpoints + final persisted evidence
                          ↓
                    MetricEvaluator
                          ↓
                    ScenarioResult → artifact export
```

The simulator never implements an alternative assignment algorithm. RotationScore,
GroupRotationScore, TargetQuality, Zone evaluation, feasible block search, block
selection and AssignmentEngine run through the existing Application flow.
Execution and metric evaluation are separate responsibilities, without requiring
a generic executor/evaluator plugin system or an interface for every concept.

## PostgreSQL/Testcontainers Lifecycle

The runner owns its container lifetime and uses a pinned PostgreSQL image,
initially matching the integration suite's `postgres:16.3-alpine`. Record the
actual image/database version in run metadata. Docker availability is an
execution prerequisite; startup failures are environment/setup evidence and do
not fabricate request failures for requests that never executed.

```text
start container
→ build Application/PostgreSQL service provider
→ apply existing migrations once in a short-lived setup scope
→ for each independent scenario:
    reset application data
    → materialize manifest data
    → execute every planned FestivalDay, retaining all earlier Assignments
    → drain requests and reconcile final persistence
    → evaluate and export evidence
→ dispose service provider and container, including on failure
```

Reset the seven application tables while preserving schema and migration history:
AssignmentRequestAttendees, Assignments, AssignmentRequests, Attendees, Spots,
FestivalDays and Zones. The existing test fixture demonstrates truncation with
`CASCADE`; the runner owns its small reset operation without importing or
extracting the xUnit fixture. Update the table list when the schema changes.
Reset only an owned disposable simulation database with no active request scopes.
Independent scenarios initially execute serially against that database.

**Never reset between FestivalDays.** Earlier Assignments supply later fairness
history. Never recreate the container, database or schema per request. Small
scenarios can amortize startup/migrations over several scenarios in one process.
Existing structural migrations assume disposable databases; reuse across schema
revisions must respect their documented empty/legacy-data limitations.

Use **one DI scope / FestivalDbContext per request**, with all request adapters
and EfCoreUnitOfWork sharing that scope. Use separate short-lived scopes for
seeding, checkpoints and final reads. No scenario-wide tracked DbContext exists.
Leave connection management to the existing EF/Npgsql configuration.

Repositories stage a complete outcome and one Unit of Work save confirms it.
Preserve the [existing persistence contract](../stage-3-persistence-model-and-transaction-boundary.md#12-persistence-contract-evaluation):
no manual scenario transaction, stronger isolation, retry or locking is added.
A failed request scope/context is discarded. Earlier reads are not guaranteed
to form one transactionally consistent pre-save snapshot.

## ScenarioDefinition and ScenarioManifest

### Compact Analytical Definition

`ScenarioDefinition` describes the hypothesis and configurable inputs. Its human
readable form includes ScenarioId and definition version, registered population,
physical topology/capacity parameters, FestivalDays/evaluated period, daily
request-participation assumptions, group-size and eligibility distributions,
initial-history distribution, arrival-order strategy, Seed, ExecutionMode and
numeric ConcurrencyLevel. It also identifies relevant scenario assumptions.

Do not hand-author thousands of Attendees or requests. Population, participation,
quality capacity and fragmentation are separate controls. Approximately 5,000
people is the current physical-capacity reference; approximately 5,500 registered
Attendees is the identified growth/oversubscription variant, not confirmed venue
capacity. Actual demand depends on daily participation. Preserve the framework's
S1–S8 scope and focused variants without inventing organization distributions.

### Exact Materialized Execution Input

```text
ScenarioDefinition + Seed + GeneratorVersion
                     ↓
            deterministic generation
                     ↓
              ScenarioManifest
                     ↓
              materialize and execute
```

`ScenarioManifest` is the immutable exact input once generated. It contains:

| Input area | Materialized evidence needed |
| --- | --- |
| Identity and population | Exact AttendeeIds, normalized AttendeeCodes and required registration data. |
| FestivalDays | Exact identities, dates, chronological indices, assignment windows and evaluated period. |
| Venue | Exact ZoneIds and ZoneTypes, RowCodes, SpotNumbers and SpotCodes; physical and assignable capacity, with explicit out-of-pool exclusions. SpotCode is the implemented Spot identity; no new SpotId is introduced. |
| Initial state | Explicit empty histories or exact prior persisted Assignments and required supporting request/catalog rows, including identities, timestamps and recorded quality. |
| Daily workload | Exact request-participation membership, changing daily groups, ordered members/codes, request eligibility and stable simulation request identities. |
| Request plans | Exact order, deterministic business timestamps and any arrival schedule; execution mode and numeric concurrency limit. |
| Provenance | Definition/version, Seed, GeneratorVersion and applicable baseline/configuration identifiers. |

The full materialized plans are exported before relying on their results. Replay
loads an exported manifest directly, without resampling from distributions or
regenerating it with a newer generator. Validate references and basic input
structure before seeding; never silently repair or reorder the manifest.

Physical exclusions must be represented in the assignable catalog supported by
the baseline and recorded explicitly. This does not implement reservations,
accessibility policies or a new production availability model. Materialize initial
history through existing domain factories and persistence capabilities; it is
declared synthetic scenario input, not a reconstruction from current Zone quality.
Keep initial history outside the evaluated period.

### Deterministic Generation and Replay Limits

A seed alone is insufficient. The generator version must identify the random
generation procedure, ID derivation, ordering and timestamp rules. The same
definition, seed and generator version must reproduce the same AttendeeIds/codes,
FestivalDayIds, ZoneIds, SpotCodes/topology, participation, daily groups,
eligibility, initial histories, request order and simulated timestamps.

Specify stable enumeration and tie-breaking before consuming randomness or
serializing inputs. Do not use uncontrolled Guid.NewGuid(), wall-clock time or
unordered collection iteration to choose scenario inputs. ZoneId matters because
production selection uses it for exact ties. Exact ID derivation and generator
implementation remain later implementation decisions, versioned when chosen.

Keep three time concepts separate: deterministic business timestamps passed to
the use case, observed UTC timestamps for run/log correlation, and monotonic
timestamps for elapsed processing time. RunId and observed wall time may vary
without changing the scenario. They never determine business inputs or ordering.

The real use case and engine generate runtime AssignmentRequestIds and
AssignmentIds. Preserve that behavior. Stable SimulationRequestId/CorrelationId
links comparison runs; generated business IDs are observed outputs. Replay aims
to reproduce sequential placement/quality outcomes, not byte-identical database
rows, IDs or measured durations. Production history orders by AssignedAt and then
AssignmentId: provide unambiguous chronological business timestamps for each
attendee's history. If a scenario deliberately includes exact ties, record the
random-ID tie-break limitation rather than claiming deterministic history order.

## DailyRequestPlan and Sequential Execution

Each FestivalDay has an ordered `DailyRequestPlan` from the manifest. Each entry
identifies a stable simulation request/correlation ID, AttemptSequence, ordered
members/codes, request-wide eligibility and explicit RequestedAt/AssignedAt.
Record arrival scheduling separately from business time; the sequential runner
does not need real-time sleeping to represent another FestivalDay. Groups contain
1..10 members and may change daily. No permanent group identity is required.

Initially `ExecutionMode = Sequential` and `ConcurrencyLevel = 1`:

1. Load or generate/export the manifest and seed its exact initial state.
2. For Day 1, capture opening inventory facts and execute each planned request in
   order, awaiting its terminal outcome before the next request.
3. Give each invocation a fresh DI scope, resolve the real use case and execute
   its command with the manifest's business timestamps. Observe the outcome and
   dispose the scope, including after exceptions.
4. Capture the drained day's closing checkpoint, then repeat for Days 2–5 without
   clearing persistence. A request rejection or recorded conflict does not cause
   a retry or implicit regrouping.
5. After the last day, reconcile attempts against persisted requests/Assignments,
   export the required persisted facts, reconstruct paths, evaluate and summarize.

The complete fairness scenario covers all five FestivalDays. Smaller populations
(tens for development, hundreds for medium scenarios, representative festival
populations for evidence) use this same pipeline. Short development runs may
exercise fewer days but must label their horizon and evidence limits. They do
not establish completion of the five-day evaluation or normal concurrent load.
Simulation execution is explicit tooling work; it does not automatically run as
part of `dotnet test`.

## Raw Observations and SimulationAttemptRecord

Every executed attempt, including excluded input, yields exactly one
`SimulationAttemptRecord` outside production business tables. Conceptually retain:

- RunId, ScenarioId, FestivalDayId/index, AttemptSequence and stable
  SimulationRequestId/CorrelationId linking to the manifest entry;
- requested members/codes, identifiable AttendeeIds and GroupSize;
- observed UTC start/end, monotonic entry/terminal offsets and duration when
  measured, separate from deterministic business timestamps;
- exactly one analytical outcome, available persisted AssignmentRequestId and
  Assignment references, existing rejection reason or typed conflict/failure
  diagnostics; an exclusion also records its validation boundary and reason;
- minimal facts needed for physical diagnosis, with their observation source
  and explicit unavailable values.

Outcome interpretation follows the framework unchanged:

| Analytical outcome | Required handling |
| --- | --- |
| Successful | Completed request and complete Assignments durably persisted; reconcile returned results with persistence. |
| DomainRejected | Persisted Rejected request and zero Assignments, including the real use case's attendee-resolution rejection. |
| KnownConcurrencyConflict | Recognized Application persistence-conflict contract; losing graph rolls back, no persisted Rejected request/losing Assignments, no retry. Discard the failed scope. |
| UnexpectedTechnicalFailure | Other unexpected processing/persistence errors or timeouts; retain diagnostics and reconcile any uncertain durable state. |
| ExcludedInvalidInput | Known basic input/precondition boundary prevents measured assignment evaluation; record separately, without hiding valid rejections or technical errors. |

Recognize conflicts through the existing typed Application contract, not exception
text or arbitrary database errors. Do not convert every ArgumentException into
excluded input. A failed attempt need not have a persisted request ID, and a
staged/generated ID alone is not proof of persistence. Correlate structured
runner logs with the same attempt ID, including rolled-back attempts. Logging
and observations are planned tooling instrumentation, not capabilities added by
this document.

For physical no-block diagnosis, observe the actual Zone and available-Spot
inputs returned to that invocation. Runner-local pass-through observation around
existing read ports can retain the minimum counts from those values while
delegating unchanged to the real providers. Reuse production eligibility policy
when deriving eligible counts; do not duplicate eligibility or block-search
logic. The real no-block result supplies the no-feasible-block fact for that
baseline evaluation. Other cases without evidence remain unavailable.

For `NO_CONTIGUOUS_SPOTS_AVAILABLE`, compare eligible available raw Spot count
with GroupSize as defined by the framework. Never substitute total venue capacity
or a fresh post-rejection read. Preserve the pre-save snapshot-consistency
limitation under contention. Missing observations produce the framework's N/A
result and missing count; they are not guessed as zero.

Keep only diagnostic facts required by the selected metrics, such as eligible
available count and selected quality/Zone when known. Do not copy the database
per request, alter provider results, rerun a second assignment algorithm, or
accumulate aggregate rates inside the execution loop.

## DailySimulationCheckpoint

Capture opening and closing facts for each FestivalDay, with a checkpoint ID,
day/index and request-plan boundary. At minimum retain persisted Assignment and
terminal-attempt counts by outcome, total available Spots and Good/Medium/Bad
capacity remaining. State whether counts are daily or cumulative and whether
inventory is global or request-eligible. Closing Good remaining supports unused
Good-capacity diagnosis; raw counts do not prove group feasibility.

The framework also needs Good consumption over request-order segments. Declare
the small set of segment boundaries in the manifest and capture the same minimal
inventory facts there when required. Detailed fragmentation distributions remain
deferred; add only the summary needed for a selected diagnostic. These factual
counts are observations; rates and interpretation belong to MetricEvaluator.

Use fresh read scopes at drained boundaries. Future concurrent checkpoints must
identify a drained boundary or label their observation interval and overlap;
they must not claim a coherent snapshot during active commits. Checkpoints do
not replace the actual request-time evidence needed for no-block diagnosis.

## Persisted Evidence and AttendeePathRecord

At scenario completion, the evaluated population is the distinct attendee
membership in ScenarioManifest.DailyPlans' request workload. It is not all
registered attendees or all Assignment owners. Setup/supporting attendees outside
that workload are excluded even when their requests/Assignments are persisted.
PostgreSQL Assignments are the source of truth for real experiences:

```text
ScenarioManifest.DailyPlans → distinct EvaluatedAttendees
→ persisted Assignments for that population in evaluated FestivalDays
→ group by AttendeeId
→ chronological entries ordered by AssignedAt ASC, AssignmentId ASC
→ EvaluatedPath
→ OverallQuality + DirectionScore → PathClassificationRubric v1 matrix
→ AttendeePathRecord
```

AttendeePathReconstructor performs one no-tracking batched Assignment/FestivalDay
join for all evaluated attendees, filtered to the FestivalDays in DailyPlans.
FestivalDate comes from persisted FestivalDay.Date in that projection, without
N+1 queries or using FestivalDayId as chronological order. Grouping and analytical
derivations happen in memory after the read. Initial history outside those days
can influence production decisions but is excluded from evaluated paths.

Each immutable AttendeePathEntry retains AssignmentId, FestivalDayId,
FestivalDate, AssignedAt and recorded ExperienceQuality. AttendeePathRecord
retains AttendeeId, ordered Entries, PathLength, OverallQuality, DirectionScore,
OverallQualityBand, Direction, Classification and RubricVersion. Other placement
facts, including AssignmentRequestId and Spot/location, remain in the existing
raw persisted Assignment observations. Do not replace persisted evidence with
Application return values, attempts, scenario intent or current Zone quality.

SimulationResult.AttendeePaths contains one record per evaluated attendee.
Only non-empty records represent EvaluatedPaths: zero-length records preserve
population context with null scores, bands and classification. PathLength = 1 is
InsufficientEvidence, with diagnostic OverallQuality/band and null DirectionScore
and Direction. ClassifiablePaths have length >= 2. A representative manifest
may evaluate all 5,000 attendees; no fixed limit or population size is assumed.

The framework defines the exhaustive matrix and inclusive thresholds. Quality
values Good/Medium/Bad map to +1/0/-1 only in Simulation. Direction accounts for
current quality and normalized change with participation-transition weights
1, 2, 3, ...; calendar gaps never weight classification. Absence, rejection,
conflict and technical failure add no synthetic outcomes. Recovery is a separate
deferred diagnostic, cannot override the matrix, and a final Bad without later
participation is not automatically failed recovery. Retained FestivalDate will
support later FestivalDay progression/recovery calculations.

Thresholds remain analytical hypotheses requiring stakeholder calibration.
Retain original evidence/classifications when revising the rubric. Current
paths remain in memory; analytical database persistence, aggregate rates and
the planned attendee-paths.json export are not implemented in this slice.

Final reconciliation reads existing business tables in fresh scopes. Public
DbSets and simulation-side read-only projections/SQL can supply the required
facts; internal request-row access granted to integration tests is not a
Simulation dependency. Do not add production query APIs or expose internals
without a demonstrated production need. Preserve persisted request status,
membership and assignment-count evidence in the corresponding attempt projection,
and report unmatched/unexpected rows explicitly. Invariant checks remain separate
from outcome classification; missing reads or uncertain commit state cannot be
reported as confirmed zero violations. Preserve partial artifacts and mark an
interrupted/unreconciled run incomplete instead of claiming final gate evidence.

## MetricEvaluator and ScenarioResult

Execution produces facts; MetricEvaluator consumes the manifest, attempt records,
checkpoints and reconciled persisted outcomes. It derives paths, populations,
metrics and diagnostic summaries after execution. Re-evaluation of retained facts
can apply an explicitly revised analytical rubric without rerunning assignments;
new metrics needing facts not captured require another run, not invented data.

`ScenarioResult` contains execution metadata and completeness/observation limits,
population summary, request accounting, fairness, feasibility, reliability /
consistency, performance and relevant diagnostics. It follows the
[common result structure](metrics-and-evaluation-criteria.md#28-common-scenario-result-structure)
without duplicating all raw records. Report RegisteredAttendees,
RequestingAttendees, ParticipatingAttendees,
AttendeesWithOnlyRejectedOrFailedAttempts, EvaluatedPaths, ClassifiablePaths,
InsufficientEvidencePaths and all five attempt categories.

Keep the framework's named denominators: NormallyProcessedRequests for normal
success/rejection rates, AttemptedRequests for technical errors/conflicts and
latency, ClassifiablePaths for the three fairness rates, and the specified
event/path/Assignment populations for the other metrics. ExcludedInvalidInput
remains separately reported. Every rate retains numerator/denominator counts,
scope/cohort and N/A reason, including zero-denominator and missing-observation
cases. No formulas or thresholds are redefined here.

Traceability must survive container disposal:

```text
ScenarioResult metric + population/period/cohort + numerator/denominator
→ evidence artifact and selection rule / record keys
→ attempt or checkpoint or attendee persisted outcome
→ SimulationRequestId + observed business IDs
→ exact manifest members, topology and request-plan entry
```

Persist invariant findings with supporting row/attempt references, including
evidence of zero findings and reconciliation coverage. An outcome label cannot
hide a correctness failure. A conflict-free run cannot prove exercised rollback
handling. Performance and fairness hypotheses remain provisional as specified;
invariants remain hard criteria. Per-run summaries supply initial Decision Gate
observations, not an automated final conclusion across S1–S8 or organization
approval. Continue / Tune / Pivot / Stop retain their existing meaning.

## Performance Boundary

Measure entry into the evaluated Application use-case execution through observing
its terminal Successful, DomainRejected, KnownConcurrencyConflict or
UnexpectedTechnicalFailure outcome. Use monotonic elapsed time in milliseconds;
ExcludedInvalidInput is outside benchmark summaries. Persistence work and waits
inside the use case remain included. Later reconciliation verifies durable
evidence without extending the original observed terminal latency.

Exclude generation, manifest serialization, container startup, migrations,
scenario reset/seeding, scope preparation, frontend/HTTP/network outside the
use case, external queueing, artifact export and final metric evaluation.
Record warm-up and exclude it from steady-state evidence; restore the exact
manifest initial state after any warm-up that writes business data. Report
intentional cold-start measurements separately.

TotalScenarioProcessingTime uses the first measured use-case entry through the
last measured terminal outcome for the declared workload/days, including overlap
and inter-request scheduling gaps. Do not sum request durations or subtract gaps
to compute throughput. Checkpoint collection between those endpoints contributes
to that interval even though it is outside individual request latency; record
its cadence/overhead. Serialize result artifacts after the measured workload.
Report attempt counts/outcome mix, workload interval and the framework's
nearest-rank percentiles so fast failures cannot imply successful performance.

Record runtime/build configuration, machine resources, database version and
configuration/placement, warm-up policy, arrival schedule, concurrency and enabled
observation overhead. Repeated all-available-Spot reads and history query/index
behavior remain visible as measured latency/throughput concerns. Do not optimize
them or introduce database profiling infrastructure before evidence justifies it.

## Artifacts and Evidence Versioning

Default output is outside production source directories:

```text
artifacts/simulations/<scenario-id>/<run-id>/
├── scenario-definition.json
├── scenario-manifest.json
├── attempts.json
├── daily-checkpoints.json
├── attendee-paths.json
├── scenario-result.json
└── summary.md
```

Definition and manifest preserve configuration and actual execution input.
Attempts, checkpoints and path records preserve detailed evidence; result and
summary aggregate and interpret it. Replays get a new RunId and retain the source
manifest identity. The existing `.gitignore` already ignores `artifacts/`.
Routine outputs are not versioned by default. Selected Decision Gate evidence
may later be copied to an explicit documentation/evidence location; publication
and retention workflow are deferred.

Every run's metadata identifies:

```text
RunId, ScenarioId, ScenarioDefinitionVersion, GeneratorVersion, Seed
EngineVersion (e.g. v0.3.0), RepositoryCommitSha
PathClassificationRubricVersion, MetricsFrameworkVersion
ExecutionMode, ConcurrencyLevel, StartedAt, CompletedAt
```

Include the evaluated period, manifest identity/content fingerprint, rubric
formulas/thresholds, benchmark environment and run completeness. EngineVersion
identifies the conceptual baseline/release; RepositoryCommitSha identifies the
source revision. Record working-tree state and preserve a source diff/content
identity for a dirty evidence run: a commit SHA alone cannot identify modified
executed code. Raw artifact references must identify the same run/manifest;
later evaluation revisions retain their original inputs and prior results.
Version artifact formats when compatibility requires it, without assigning
semantic versions to every internal simulation class.

Use a stable `summary.md` structure: Scenario; Execution Metadata; Population;
Request Outcomes; Fairness; Feasibility; Reliability / Consistency; Performance;
Relevant Diagnostics; Initial Decision Gate Observations. Include N/A reasons,
unresolved judgments and links to detailed run artifacts, rather than replacing
them with favorable summary values.

## Future Concurrent Execution

The same manifest, records and evaluators support a later execution strategy:

```text
ScenarioManifest → DailyRequestPlan
                         ├── SequentialScenarioExecutor (first implementation)
                         └── ConcurrentScenarioExecutor (future)
                                      ↓
                         same observations/evaluation/artifacts
```

`ExecutionMode = Concurrent` and numeric `ConcurrencyLevel = N` will control
dispatch using the manifest's arrival schedule. AttemptSequence identifies planned
order, not completion or commit order. Observe actual entry/completion timing;
each request has an independent scope/context, and the runner drains the day
before advancing. Future modes must not silently fall back to sequential while
claiming concurrency evidence.

Known conflicts preserve complete rollback, no persisted Rejected request or
losing Assignments, no retries and disposal of the failed context. Do not add
artificial barriers/delays solely to force races, or change production isolation,
locking or serialization. Concurrency reproducibility means reproducing scenario,
workload, configuration, arrival schedule and concurrency level. Seeds do not
determine exact transaction winners, observed snapshots or final concurrent
placements. Record actual outcomes and their limits; a sequential run is not
evidence for the framework's normal/high concurrent-load scenarios.

## Accepted Trade-offs and Deferred Decisions

One PostgreSQL path preserves persistence/conflict fidelity and accepts Docker
startup cost for small scenarios. Runner-owned lifecycle keeps the existing
integration-test fixture test-specific; no shared Testcontainers framework is
needed. Explicit manifests and linear per-attempt/per-assignment evidence cost
more storage than aggregate-only output but enable replay and audit without
per-request database snapshots.

The baseline remains online and arrival-order sensitive, with independent
pre-save reads, provisional analytical judgments and runtime-generated business
IDs. Stage 5 measures these limits. No simulation concept changes the 19 fairness
rules, Domain policies, persistence statuses or Stage 4 acceptance evidence.

Remaining implementation decisions are limited to exact JSON schemas and
compatibility checks; deterministic ID/random-generator algorithms; concrete
materialization and pass-through observation mechanics; minimal checkpoint
fields/segment cadence and read projections; artifact retention/publication;
future concurrent scheduling; and later multi-scenario comparison tooling.
Rubric thresholds and real organization distributions follow the metrics
framework's stakeholder calibration process, not new architecture rules.

No generic simulation plugin system, shared test framework, analytics database,
dashboard, event sourcing, production global-state abstraction, new request
status, retry framework or automated S1–S8 comparison is required by this plan.
