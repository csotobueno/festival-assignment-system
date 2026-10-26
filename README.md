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
for evaluating `v0.3.0 — Fair Assignment Engine`; simulation remains pending.
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
* [Stage 3 Technical Lead Learnings](docs/learning/stage-3-technical-lead-learnings.md)
* [PostgreSQL Relational Model](docs/architecture/postgresql-relational-model.md)
* [ADR 0001: Select the MVP Database Engine](docs/adr/0001-select-mvp-database-engine.md)

## Repository structure

```text
src/
├── Festival.Api
├── Festival.Application
├── Festival.Domain
└── Festival.Infrastructure

tests/
├── Festival.Application.Tests
├── Festival.Domain.Tests
├── Festival.Infrastructure.IntegrationTests
└── Festival.Infrastructure.Tests
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

Docker is required only by the integration project. Testcontainers starts and
disposes an isolated PostgreSQL container automatically, so no manual PostgreSQL
installation, fixed host port or local database credentials are required. The
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

Because `Festival.sln` includes the integration project,
`dotnet test Festival.sln` also executes the PostgreSQL integration suite and
therefore requires Docker. CI should keep the three fast projects in its default
job and execute the integration project in a separate Docker-enabled job.
