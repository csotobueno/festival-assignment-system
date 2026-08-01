# Festival Assignment System

Technical MVP for validating the viability of a fair, consistent, and scalable festival spot assignment system.

## Current stage

**Stage 3 — Persistence, Global Invariants and Concurrency: In progress**

Stage 2 validated the executable minimum assignment flow across Domain,
Application, and Infrastructure. Stage 3 is introducing the foundation needed
for durable relational persistence and concurrent request handling.

The current Stage 3 progress includes:

* the persistence model and transactional boundary have been defined;
* PostgreSQL has been selected as the relational database engine for the MVP;
* the EF Core model and initial PostgreSQL migration have been introduced;
* real PostgreSQL integration tests validate the current physical persistence
  foundation;
* PostgreSQL adapters implement the four existing Application persistence
  ports and stage new entities in a shared `FestivalDbContext`;
* the existing in-memory adapters remain available for fast technical
  validation.

Production persistence is not yet integrated into the assignment use case.
Repository `AddAsync` methods stage changes but do not commit them. Unit of Work,
transaction orchestration, concurrent assignment processing and production
exception translation have not been implemented. The current migration,
repositories and integration tests protect and validate the persistence
foundation, but do not make PostgreSQL persistence production-ready.

The in-memory adapters are validation tools. They lose their state when the
application stops and are not production persistence.

## Project status

**Stage 2 technically validated. Stage 3 in progress.**

The executable assignment flow is already validated using deterministic in-memory infrastructure. The project is now introducing PostgreSQL persistence with EF Core and preparing the protection of transactional and global consistency rules.


## Documentation

* [Project Operating Model](docs/project-operating-model.md)
* [Domain Glossary](docs/glossary.md)
* [Critical Invariants](docs/critical-invariants.md)
* [Domain Blueprint v1](docs/domain-blueprint-v1.md)
* [Stage 2 Technical Validation](docs/stage-2-technical-validation.md)
* [Stage 3 Persistence Model and Transactional Boundary](docs/stage-3-persistence-model-and-transaction-boundary.md)
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
