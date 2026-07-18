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
* the EF Core and Npgsql Infrastructure foundation is being introduced;
* the existing in-memory adapters remain available for fast technical
  validation.

Durable PostgreSQL persistence is not yet implemented. The EF Core model is not
complete, and repositories, migrations, database constraints, transaction
handling and PostgreSQL integration tests have not been implemented or
validated. Global invariants are therefore not yet protected by the database.

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
