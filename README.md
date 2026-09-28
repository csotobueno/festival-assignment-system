# Festival Assignment System

Technical MVP for validating the viability of a fair, consistent, and scalable festival spot assignment system.

## Current stage

**Stage 3 — Persistence, Global Invariants and Concurrency: Completed**

Stage 3 established PostgreSQL persistence, an atomic Unit of Work boundary,
database-backed global invariant protection, stable conflict translation and
deterministic validation of concurrent Spot and Attendee conflicts.

**Stage 4 — Assignment Engine + Fairness: Next**

Stage 4 has an [initial implementation guide](docs/stage-4/README.md); the
fairness-aware engine is the next implementation stage. It will build a
reproducible online baseline using attendee history across festival days.
Stage 5 will evaluate that baseline through simulation and a technical decision
gate, producing evidence for a proposal to the festival organization. Operational
and business considerations will then be refined with the organization.
The MVP validates technical feasibility and reliability; it does not deliver
the complete operational system.

## Project status

**Stage 3 completed. Stage 4 — Assignment Engine + Fairness is next. Stage 5 — Simulation + Decision Gate is pending.**


## Documentation

See [CONTRIBUTING.md](CONTRIBUTING.md) for the development workflow.

* [Project Operating Model](docs/project-operating-model.md)
* [Domain Glossary](docs/glossary.md)
* [Critical Invariants](docs/critical-invariants.md)
* [Domain Blueprint v1](docs/domain-blueprint-v1.md)
* [Stage 2 Technical Validation](docs/stage-2-technical-validation.md)
* [Stage 3 Persistence Model and Transactional Boundary](docs/stage-3-persistence-model-and-transaction-boundary.md)
* [Stage 3 Closure](docs/stages/stage-3-closure.md)
* [Stage 4 — Initial Implementation Guide](docs/stage-4/README.md)
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
