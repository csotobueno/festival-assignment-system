# Festival Assignment System

Technical MVP for validating the viability of a fair, consistent, and scalable festival spot assignment system.

## Current stage

**Stage 2 — Backend Foundation and Executable Domain: Completed**

The project currently has an executable minimum assignment flow across Domain, Application, and Infrastructure.

The system can:

* create and resolve an assignment request;
* form an indivisible attendee group;
* find a contiguous block of available spots;
* generate one assignment per attendee;
* complete a valid request;
* reject a request when no sufficiently large contiguous block is available;
* store the final result using in-memory infrastructure adapters.

The current implementation is intended for technical validation. It does not yet include production persistence, concurrency protection, fairness, or HTTP endpoints.

The next stage will focus on persistence, transactional boundaries, global invariants, and concurrency.

## Project status

**Stage 2 technically validated. Preparing Stage 3.**

Validated using deterministic in-memory data:

* 10 Attendees;
* 1 FestivalDay;
* 2 Zones;
* 2 Rows per Zone;
* 4 Spots per Row;
* 16 Spots total.

Validated flows:

* successful assignment for a group of 3 Attendees;
* rejected assignment for a group of 5 Attendees when no sufficiently large contiguous block exists.

## Documentation

* [Project Operating Model](docs/project-operating-model.md)
* [Domain Glossary](docs/glossary.md)
* [Critical Invariants](docs/critical-invariants.md)
* [Domain Blueprint v1](docs/domain-blueprint-v1.md)
* [Stage 2 Technical Validation](docs/stage-2-technical-validation.md)

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
