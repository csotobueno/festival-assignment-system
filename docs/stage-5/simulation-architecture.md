# Stage 5 — Simulation Architecture and Reproducibility Strategy

## Status, Purpose and Authority

**Architecture agreed; minimal sequential development slice implemented;
full simulation platform and empirical evaluation remain pending.**
This document defines the minimum planned architecture for evaluating
`v0.3.0 — Fair Assignment Engine`. The current slice includes immutable manifests,
attempts, daily checkpoints, individual attendee-path reconstruction and
classification, attendee recovery observations and aggregate fairness/recovery
metrics, with a completed-run JSON evidence exporter. Other metric families and
Decision Gate verdicts remain deferred. No simulation analysis changes production behavior.

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
DailySimulationCheckpoint, AttendeePathRecord, AttendeeRecoveryRecord,
RecoveryEpisode and the analytical rubric belong
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

The full materialized plans are exported after execution as part of the completed
evidence package. Future replay will load an exported manifest directly, without
resampling from distributions or regenerating it with a newer generator. Replay
loading is not implemented. Validate references and basic input
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

1. Load or generate the manifest and seed its exact initial state.
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
analytical derivation from these same entries and cannot override the matrix.
FestivalDate remains evidence, without calendar weighting of recovery distance.

Thresholds remain analytical hypotheses requiring stakeholder calibration.
Retain original evidence/classifications when revising the rubric. Current
paths and aggregate fairness/recovery metrics are exported after execution;
analytical database persistence remains outside this tooling boundary.

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

## Attendee Recovery Observations

After reconstructing paths, the runner performs a separate in-memory analysis:

```text
AttendeePathRecord.Entries (ordered persisted ExperienceQuality)
→ AttendeeRecoveryAnalyzer
→ AttendeeRecoveryRecord.Episodes
→ SimulationResult.RecoveryObservations
```

The analyzer consumes only an existing AttendeePathRecord. It does not re-query
PostgreSQL or read ScenarioDefinition, manifest intent, attempts, request statuses
or Application return values. One immutable result per evaluated path retains
AttendeeId and an ImmutableArray of RecoveryEpisodes; no Bad means empty Episodes,
including zero-length paths. The runner analyzes only AttendeePaths, preserving
the evaluated population and exclusion of setup/supporting attendees.

A persisted Bad opens an episode. Additional Bad entries continue that episode;
the first later Medium or Good closes it as Recovered. RecoveryQuality preserves
Medium or Good. After closure a later Bad starts a new episode. B → B → M is one
episode; B → M → B → G is two. An open episode at the end is Unresolved, meaning
no later Medium/Good was observed during the evaluated path, not failed recovery.

Each episode retains AttendeeId and the opening persisted Assignment's ID,
FestivalDayId, FestivalDate and AssignedAt. Recovered episodes retain the same
fields for the closing Assignment, RecoveryQuality and OpportunitiesToRecovery.
Those recovery fields are null for Unresolved episodes. Get-only properties and
immutable collections preserve read-only evidence without copying attendee paths.

Every episode also retains non-negative ObservedOpportunities: later persisted
path entries after the opening Bad while the episode is open, including the
closing Medium/Good when present. For Unresolved episodes, count through the end
of the path. RecoveryEligible is computed as ObservedOpportunities > 0, without
extra statuses or stored mutable state. B is Unresolved with zero opportunities
and is not eligible; B → B is Unresolved with one and is eligible. Neither is a
definitive production failure. Recovered episodes have ObservedOpportunities equal
to OpportunitiesToRecovery; Unresolved episodes keep OpportunitiesToRecovery null.
The analyzer uses the same path-entry indices for both counts, with no new reads
or changes to SimulationResult.RecoveryObservations.

OpportunitiesToRecovery counts entries after the opening Bad through the closing
Medium/Good (closing index minus opening index). B → B → M has distance 2.
Absence and any rejected/conflicted/failed/invalid request without a persisted
Assignment add neither an entry nor a recovery opportunity. Thus Bad on Day 1
and Good on Day 3 with Day 2 absent has distance 1. Absence or failed requests
should not make the Assignment Engine appear slower when it did not have an
assigned opportunity to produce a new experience. Calendar distance is secondary
and deferred; retained dates remain evidence only.

The deterministic minimal scenario produces one Recovered episode per evaluated
attendee from persisted Bad → Good, with RecoveryQuality = Good and
ObservedOpportunities = OpportunitiesToRecovery = 1 and RecoveryEligible = true.
Supporting occupancy remains excluded. Pure
analytical tests cover episode derivation; the existing PostgreSQL-backed run
verifies integration with persisted paths.

Recovery never changes OverallQuality, DirectionScore, OverallQualityBand,
Direction, Classification or PathClassificationRubric v1. Observations remain in
SimulationResult and the completed-run `recovery-observations.json`; there is no
recovery database persistence or scenario acceptance/gate logic. Separate
aggregation consumes this evidence.

## Implemented Aggregate Fairness and Recovery Metrics

```text
AttendeePaths + RecoveryObservations
→ SimulationMetricsCalculator
→ SimulationMetrics (Fairness + Recovery)
→ SimulationResult.Metrics
```

The runner calls this pure calculator once after reconstruction and recovery
analysis. It reads only the existing analytical records for the same evaluated
population and period, without PostgreSQL access, new path reconstruction,
request-status inspection or scenario intent. Classification is counted from
AttendeePathRecord.Classification; recovery status, eligibility, quality and
distance come from supplied RecoveryEpisodes. The rubric and analyzer remain
unchanged. Raw attempts, checkpoints, paths and recovery records remain available.

ScenarioFairnessMetrics retains evaluated/no-path/insufficient/classifiable
counts and coverage ratios over all evaluated attendees, classification counts
and ratios over classifiable paths only, and Good/Medium/Bad outcome counts and
ratios over persisted path entries only. No-path attendees stay in the population;
setup/supporting attendees remain excluded upstream.

ScenarioRecoveryMetrics retains total/recovered/unresolved/eligible/ineligible
episode counts, RecoveryRate = recovered / eligible, Medium/Good recovery counts,
decimal average/median opportunity distance and an immutable ascending opportunity
distribution. Eligibility is ObservedOpportunities > 0. Distribution rates use
RecoveredEpisodeCount; unresolved episodes never enter speed calculations. Buckets
have no hard-coded maximum. A final Bad still contributes to BadOutcomeCount,
while consecutive Bad outcomes can belong to one recovery episode.

Rates are nullable decimal ratios in [0, 1] (0.85 means 85%). Zero denominators
produce null/N/A; no recovered episodes produce null average/median and an empty
distribution. This result is exported directly as `metrics.json`, without new
derivations, aggregate database storage, thresholds, verdicts or production behavior changes. Other metric
families and the broader ScenarioResult below remain planned.

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
artifacts/simulations/minimal-recovery/run-001/
├── run-metadata.json
├── definition.json
├── manifest.json
├── attempts.json
├── checkpoints.json
├── attendee-paths.json
├── recovery-observations.json
└── metrics.json
```

The general layout is `artifacts/simulations/<scenario>/<run>/`.
`SimulationEvidenceExportRequest` supplies Definition, the exact Manifest consumed
by the runner, the completed Result, RunMetadata and DestinationRoot (normally
`artifacts/simulations`). `SimulationEvidenceExporter` uses only those inputs;
it never regenerates the manifest, queries PostgreSQL, reads Git/global state or
recomputes analytical results. The executable calls it once after RunAsync has
returned the complete result, then prints the published directory. Export failure
propagates to a nonzero process exit without claiming package completion.

| File | Responsibility |
| --- | --- |
| `run-metadata.json` | Reproducibility context and observed run lifecycle. |
| `definition.json` | Compact ScenarioDefinition intent: what was requested. |
| `manifest.json` | Exact ScenarioManifest execution input, including catalog, IDs, timestamps, setup/supporting occupancy and ordered workload. This is the authoritative reproduction input. |
| `attempts.json` | Raw SimulationAttemptRecord execution evidence, including timing, diagnostics, membership, persisted request identity, capacity/feasibility and selected quality. |
| `checkpoints.json` | Raw DailySimulationCheckpoint evidence: FestivalDay, daily evaluated request counts and global Good/Medium/Bad/Total remaining capacity. |
| `attendee-paths.json` | Individual AttendeePathRecords, retaining all persisted Assignment/FestivalDay entries, scores, bands, direction, classification and RubricVersion. |
| `recovery-observations.json` | Individual AttendeeRecoveryRecords and RecoveryEpisodes, retaining opening/recovery Assignment evidence, status, quality, observed opportunities, eligibility and distance. |
| `metrics.json` | Existing SimulationResult.Metrics fairness/recovery aggregates, including nullable rates and recovery distance. |

No summary, verdict, archive, dashboard or database analytics is produced.
Supporting attendees remain in the exact setup manifest but outside evaluated
path/recovery exports; the exporter preserves the runner's population boundary.
Existing path/recovery fields are exported in full, without quality-letter
compression or additional derivations.

Metadata uses camelCase JSON properties for these explicit model fields:

| Fields | Source / unavailable behavior |
| --- | --- |
| RunId | Explicit safe identifier; executable supplies a fresh GUID in N format. |
| ScenarioId, GeneratorVersion, Seed, ExecutionMode, ConcurrencyLevel | Copied from the consumed manifest; provenance must agree with definition and metadata. |
| ScenarioDefinitionVersion | null: no explicit definition version exists in code yet. |
| EngineVersion | `v0.3.0`, the documented Stage 4 conceptual baseline; not a claim of source identity. |
| RepositoryCommitSha | Optional explicit run input; executable reads `FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA`. No Git command/discovery is required; absence is null and never prevents export. |
| RubricVersion | Existing PathClassificationRubric.Version (`v1`). |
| MetricsFrameworkVersion | null: the framework is described as v1 in documentation but has no explicit code version value yet. |
| StartedAt, CompletedAt | Observed UTC lifecycle around the full runner invocation, including setup, analysis and teardown; excludes export and is separate from request latency and deterministic business timestamps. |

A supplied SHA alone cannot identify uncommitted executed changes. Capturing
working-tree diffs, content fingerprints and broader benchmark environment
metadata remains future evidence work; unavailable values are not fabricated.
Callers can supply explicit known version values in RunMetadata without a new
versioning system. No replay loader or schema migration contract is added.

One explicit, read-only System.Text.Json configuration writes indented UTF-8
without BOM, camelCase property names, enums as their declared names (not numeric
values), and all DateTimeOffset timestamps as UTC ISO 8601 with seven fractional
digits and `Z`. DateOnly/TimeOnly retain their ISO representations. Domain value
objects keep their public structure (for example, an identity has a `value`).
JSON uses LF and a final newline. Nulls are always included:
**JSON null = analytical N/A**. Zero denominators never become 0, -1, strings or NaN.

Ordering is deterministic: attempts by AttemptSequence then SimulationRequestId;
checkpoints by manifest FestivalDate then FestivalDayId; paths/recovery records by
AttendeeId; path entries by AssignedAt then AssignmentId; episodes by
StartAssignedAt then StartAssignmentId; opportunity buckets by Opportunities
ascending. The exact definition and manifest retain their original collection
order, including execution order and member order. Sorting copies observations,
without mutating source evidence or recalculating classifications/metrics.
Equivalent supplied evidence/metadata produces identical bytes; runtime-generated
business IDs, observed timestamps and durations naturally differ between runs.

ScenarioId and RunId must contain 1..128 ASCII letters/digits, hyphens or
underscores, starting with a letter/digit. Unsafe identifiers are rejected instead
of rewritten. **Any existing run destination is rejected**, even an empty
directory; individual files are never overwritten. All eight files are written
to a unique hidden sibling temporary directory before one final directory move.
Failures/cancellation propagate and trigger best-effort temporary cleanup. An
abrupt process termination may leave a `.tmp` sibling, which is not a completed
run directory. There is no transactional filesystem framework.

The existing `.gitignore` already ignores `artifacts/`; no duplicate rule is
needed. Routine outputs and generated test evidence are not committed. Selected
final runs may later be intentionally retained as Decision Gate evidence;
publication and retention workflow are deferred.

Pure temporary-directory tests cover serialization, ordering, immutable inputs,
safe paths, existing runs and failed/cancelled export cleanup. The existing real
PostgreSQL deterministic recovery test also exports a complete package and checks
the consumed manifest, raw records, persisted Bad → Good paths, recovered episodes,
aggregate result equality and supporting-attendee exclusion.

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
