# Festival Assignment System

Technical MVP for validating the viability of a fair, consistent, and scalable festival spot assignment system.

## Current stage

**Stage 3 — Persistence, Global Invariants and Concurrency: Completed**

Stage 3 established PostgreSQL persistence, an atomic Unit of Work boundary,
database-backed global invariant protection, stable conflict translation and
deterministic validation of concurrent Spot and Attendee conflicts.

**Stage 4 — Assignment Engine + Fairness: Completed**

The deterministic online fairness baseline is integrated into Application,
including eligibility, inventory, complete-group placement and persisted
historical ExperienceQuality. The [authoritative completion document](docs/stage-4/completion.md)
records the verified exit criteria and validation evidence, including seven
deterministic integrated acceptance scenarios. The former five-day presimulation
requirement is explicitly deferred to Stage 5 to avoid duplicating its evaluation.

**Stage 5 — Simulation + Decision Gate: Pending**

Stage 5 will evaluate the baseline through simulation and a technical decision
gate, producing evidence for a proposal to the festival organization. Operational
and business considerations will then be refined with the organization.
The [Stage 5 evaluation framework](docs/stage-5/metrics-and-evaluation-criteria.md)
defines the analytical rubric, populations, metrics and scenario requirements
for evaluating `v0.3.0 — Fair Assignment Engine`. The
[simulation architecture](docs/stage-5/simulation-architecture.md) defines the
planned PostgreSQL execution, reproducibility and evidence flow. The first
executable slice now exercises a deterministic two-day recovery scenario;
the full simulation platform and evaluation remain pending.
The MVP validates technical feasibility and reliability; it does not deliver
the complete operational system.

## Project status

**Stage 4 Completed. Stage 5 — Simulation + Decision Gate: Pending.**


## Documentation

See [CONTRIBUTING.md](CONTRIBUTING.md) for the development workflow.

* [Project Operating Model](docs/project-operating-model.md)
* [Domain Glossary](docs/glossary.md)
* [Critical Invariants](docs/critical-invariants.md)
* [Domain Blueprint v1](docs/domain-blueprint-v1.md)
* [Stage 2 Technical Validation](docs/stage-2-technical-validation.md)
* [Stage 3 Persistence Model and Transactional Boundary](docs/stage-3-persistence-model-and-transaction-boundary.md)
* [Stage 3 Closure](docs/stages/stage-3-closure.md)
* [Stage 4 — Assignment Engine and Fairness](docs/stage-4/README.md)
* [Stage 4 Completion](docs/stage-4/completion.md)
* [Stage 5 — Metrics and Evaluation Criteria](docs/stage-5/metrics-and-evaluation-criteria.md)
* [Stage 5 — Simulation Architecture and Reproducibility](docs/stage-5/simulation-architecture.md)
* [Stage 3 Technical Lead Learnings](docs/learning/stage-3-technical-lead-learnings.md)
* [PostgreSQL Relational Model](docs/architecture/postgresql-relational-model.md)
* [ADR 0001: Select the MVP Database Engine](docs/adr/0001-select-mvp-database-engine.md)

## Repository structure

```text
src/
├── Festival.Api
├── Festival.Application
├── Festival.Domain
├── Festival.Infrastructure
└── Festival.Simulation (tooling executable)

tests/
├── Festival.Application.Tests
├── Festival.Domain.Tests
├── Festival.Infrastructure.IntegrationTests
├── Festival.Infrastructure.Tests
└── Festival.Simulation.Tests
```

The backend follows an inward dependency direction:

```text
Festival.Api
├── Festival.Application
└── Festival.Infrastructure

Festival.Infrastructure
├── Festival.Application
└── Festival.Domain

Festival.Application
└── Festival.Domain

Festival.Domain
└── no project dependencies
```

## Running tests

The fast test projects do not require Docker or another external resource:

```bash
dotnet test Festival.FastTests.slnf
```

Run the real PostgreSQL integration suite explicitly:

```bash
dotnet test tests/Festival.Infrastructure.IntegrationTests
```

Docker is required by the integration and simulation test projects. Testcontainers
starts and disposes an isolated PostgreSQL container automatically, so no manual
PostgreSQL installation, fixed host port or local database credentials are required. The
first execution may take longer while Docker downloads and starts the pinned
PostgreSQL image.

Older Docker daemons may expose an API below Testcontainers' default. For
example, Docker 24 (API 1.43) can run the suite with:

```bash
DOCKER_API_VERSION=1.43 dotnet test tests/Festival.Infrastructure.IntegrationTests
```

Run all using

```bash
dotnet test Festival.sln
```

or

```bash
DOCKER_API_VERSION=1.43 dotnet test Festival.sln
```

Because `Festival.sln` includes the integration and simulation test projects,
`dotnet test Festival.sln` also executes both PostgreSQL-backed suites and
therefore requires Docker. CI should keep the three fast projects in its default
job and execute the PostgreSQL-backed projects in a separate Docker-enabled job.

## Minimal Stage 5 simulation

Run the two-day development scenario explicitly (Docker required):

```bash
dotnet run --project src/Festival.Simulation
```

The runner owns a disposable `postgres:16.3-alpine` container, applies the real
migrations once, and executes one group of two attendees on both days through
`AddApplication()`, `AddPostgreSqlPersistence(...)` and the real use case, with
a fresh scope/context per request. All three Zones and their contiguous two-Spot
blocks are seeded once. Supporting attendees occupy the Good and Medium blocks
through four Day 1 Assignments and two Completed setup requests, leaving only
Bad available for the evaluated group. These occupancy graphs remain persisted;
availability is scoped to FestivalDay, so the same Good and Medium Spots are
available on Day 2 without changing topology or resetting state. Persisted Bad history makes
the engine choose Good; empty history would choose Medium in that inventory.
`MinimalRecoveryScenarioDefinition.Create()` declares the small population,
three contiguous blocks and daily occupancy/request intent. `ScenarioMaterializer`
turns it into a read-only `ScenarioManifest` with metadata, a shared catalog,
separate supporting setup and explicitly sequenced daily requests. The runner
seeds and executes that exact input. All manifest collections are immutable;
execution state stays in PostgreSQL and observation records.

Generator `v1` derives IDs from SHA-256 of stable scenario/entity keys, orders
days and requests explicitly, and derives UTC business timestamps from the
definition's first date (setup after 13:00, evaluated requests from 14:00 in
two-minute steps). The seed is recorded but this nonrandom scenario needs no
sampling. The same definition, seed and generator version reproduce equivalent
manifests, including topology and setup. Evaluated attendees have no initial
history. Generated production request/Assignment IDs remain observed outputs.
Definition/materialization/manifest execution uses in-memory objects; the exact
consumed manifest is now exported as JSON after the completed run. Loading an
exported manifest for replay remains deferred.

The executable returns zero when both attempts reconcile as Successful, and
one otherwise; the automated test verifies stable topology and persisted Bad → Good
recovery, two evaluated Completed requests and four evaluated Assignments. The raw
observations also retain the two setup requests and four occupancy Assignments
(four Completed requests/eight Assignments total). After the result is complete,
the executable exports eight JSON evidence files under
`artifacts/simulations/<scenario>/<run>/` and prints the completed directory.
Run IDs are generated per invocation. Paths are relative to the working directory.

Each evaluated request retains an in-memory attempt record with scenario/request
identity, attendee membership, observed UTC start/end, monotonic processing duration,
outcome, confirmed persisted request ID, stable diagnostic code/type and selected
persisted quality. Timing covers the Application call through return/exception;
scope preparation, reconciliation and checkpoints are outside that interval.
Simulation-scoped pass-through providers capture the actual Zone/available-Spot
inputs used by the request. Eligible capacity reuses `ZoneEligibilityPolicy`;
block-found evidence comes from the real success/no-block result without another
block search. Unobserved capacity/feasibility remains null. These observations
retain the baseline's independent pre-save-read consistency limitation.

One immutable end-of-day checkpoint follows each drained daily plan. Its counts
include only that day's evaluated attempts; remaining Good/Medium/Bad/total
capacity is global for that FestivalDay and includes setup occupancy effects.
Attempts, checkpoints, attendee paths and recovery observations remain in
`SimulationResult`, alongside derived fairness/recovery metrics, and are exported
after execution. No analytical tables or latency summaries are generated.

Attendee-path reconstruction selects distinct participants from the manifest's
evaluated `DailyPlans` workload, excluding setup/supporting attendees. One
read-only batched PostgreSQL query joins Assignments to FestivalDays, retaining
persisted dates and quality in `AssignedAt`, then `AssignmentId` order. The
Stage 5 analytical rubric v1 derives OverallQuality, participation-weighted
DirectionScore and the classification matrix in memory. Evaluated attendees
without Assignments retain zero-length context records with null scores and
classification; singletons are InsufficientEvidence without directional scores.
Absence, rejection, conflict and technical failure add no synthetic outcomes.
The independent `AttendeeRecoveryAnalyzer` derives recovery episodes only from
those reconstructed path entries. Bad opens an episode, consecutive Bad keeps it
open, and the first later Medium/Good closes it as Recovered while preserving
recovery quality. A later Bad starts another episode; an open episode at path end
is Unresolved, not failed recovery. `ObservedOpportunities` counts later persisted
path entries while the episode is open; `RecoveryEligible` is computed as that
count being greater than zero. Thus B has no later opportunity, while B → B has
one; both remain Unresolved. `OpportunitiesToRecovery` is populated only for
Recovered episodes, equals their `ObservedOpportunities`, and counts later
persisted Assignments through recovery, excluding the opening Bad and any absent
days or requests without Assignments. Both evaluated attendees in the minimal
scenario have one Bad → Good episode with distance 1. Immutable attendee recovery
records, including empty Episodes for attendees without Bad, are exposed through
`SimulationResult.RecoveryObservations`; supporting attendees remain excluded.
Recovery does not influence path classification. `SimulationMetricsCalculator`
consumes these existing paths and recovery records without additional database
queries or reinterpretation. `SimulationResult.Metrics.Fairness` exposes population
coverage, stored classification counts/rates and persisted quality counts/rates.
`Metrics.Recovery` exposes episode/eligibility/recovery-quality counts, recovery
rate, decimal average/median distance and immutable ascending opportunity buckets.
Rates are nullable decimal ratios (0.85 = 85%); zero denominators are null/N/A.
RecoveryRate uses eligible episodes only, while speed uses recovered episodes
only. Bad outcomes and recovery episodes remain separate counts. Calendar-day
recovery metrics, other aggregate families and Decision Gate verdicts remain deferred.

The evidence package separates scenario intent, exact input and observed results:

```text
artifacts/simulations/minimal-recovery/<run>/
├── run-metadata.json          # reproducibility context
├── definition.json            # compact scenario intent
├── manifest.json              # exact materialized input consumed by the runner
├── attempts.json              # raw request execution evidence
├── checkpoints.json           # raw daily counts and remaining capacity
├── attendee-paths.json        # individual persisted paths and classification
├── recovery-observations.json # individual recovery episodes
└── metrics.json               # aggregate fairness and recovery observations
```

JSON is indented UTF-8 without BOM, with camelCase properties, named enums and
UTC timestamps. **JSON null = analytical N/A**. Observations have deterministic
ordering; the manifest retains its original collection order. Existing run
directories are rejected, and files are staged before publishing the complete
directory. Export errors propagate and prevent a success message/exit.
`artifacts/` is already ignored by Git; selected final runs may later be
intentionally retained as Decision Gate evidence. Generated test runs are not
committed. Set `FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA` explicitly when a
reliable source revision is available. Missing SHA, definition version and
metrics-framework version are null; the engine label is the documented `v0.3.0`
baseline and RubricVersion is the existing `v1`. A supplied SHA alone does not
identify uncommitted changes. See the
[evidence contract](docs/stage-5/simulation-architecture.md#artifacts-and-evidence-versioning)
for the metadata and export boundary.

```bash
dotnet test tests/Festival.Simulation.Tests
```

On Docker 24, prefix either command with `DOCKER_API_VERSION=1.43`.
This development slice is not the five-day Stage 5 evaluation or Decision Gate.
