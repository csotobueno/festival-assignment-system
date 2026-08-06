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
* a minimal `IUnitOfWork` is implemented by `EfCoreUnitOfWork` and confirms all
  changes staged in that shared context through one `SaveChangesAsync` call;
* `ProcessAssignmentRequestUseCase` stages final Completed or Rejected outcomes
  and invokes `IUnitOfWork.SaveChangesAsync` exactly once before returning;
* known PostgreSQL assignment uniqueness violations are translated at the Unit
  of Work boundary into stable Application persistence conflicts;
* real PostgreSQL tests prove that each recognized conflict rolls back the
  complete new request graph;
* deterministic real PostgreSQL concurrency tests prove that overlapping
  requests for the same Spot or Attendee produce one complete commit and one
  complete rollback through independent scoped contexts;
* `AddPostgreSqlPersistence` explicitly registers the PostgreSQL resolver,
  provider, repositories, context and Unit of Work with scoped lifetimes;
* the existing in-memory adapters remain available for fast technical
  validation.

Repository `AddAsync` methods continue to stage changes without committing
them. `IUnitOfWork.SaveChangesAsync` is the single durable boundary for all
pending changes in one shared `FestivalDbContext`; EF Core owns the transaction
for that save, and no manual transaction API has been introduced. The API demo
continues selecting the separate in-memory configuration; its
`InMemoryUnitOfWork` is a no-op and is not a durable transaction. Selecting
`AddPostgreSqlPersistence` makes Completed and Rejected use-case outcomes
durable through PostgreSQL. No legitimate Failed branch currently exists, and
recognized assignment uniqueness conflicts propagate as Application exceptions.
Unknown PostgreSQL and EF Core errors continue propagating unchanged. The
current concurrency policy relies on PostgreSQL uniqueness constraints without
retry or locking. API mapping remains pending.

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
