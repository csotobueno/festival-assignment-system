# Stage 3 — Closure

> This is the historical delivery record for Stage 3. Its evidence and decisions
> are preserved. The [Stage 4 guide](../stage-4/README.md) develops the next
> assignment and fairness proposal; it does not change what Stage 3 delivered.

## Objective

Establish durable relational persistence and prove that global assignment
invariants remain protected when independent requests fail or compete.

## Status

**Status: Completed**

## Delivered capabilities

Stage 3 delivered:

- PostgreSQL persistence through EF Core mappings and the initial migration;
- a persistence-specific `AssignmentRequestRow` model and request-attendee
  rows;
- PostgreSQL implementations of the existing Application persistence ports;
- one shared scoped `FestivalDbContext` and an EF Core Unit of Work;
- one durable `SaveChangesAsync` boundary for Completed and Rejected outcomes;
- physical uniqueness constraints, complete rollback validation and stable
  conflict translation;
- real PostgreSQL validation of concurrent Spot and Attendee conflicts.

The [Stage 3 persistence design](../stage-3-persistence-model-and-transaction-boundary.md)
and [PostgreSQL relational model](../architecture/postgresql-relational-model.md)
contain the detailed model and implementation rationale.

## Global invariants protected

- **One Spot per FestivalDay:** Domain/Application availability checks improve
  behavior; `Assignments(FestivalDayId, SpotCode)` provides global enforcement.
- **One Attendee per FestivalDay:** Application rejects known prior
  Assignments; `Assignments(FestivalDayId, AttendeeId)` provides global
  enforcement.
- **One Attendee per AssignmentRequest:** Domain protects duplicate requested
  codes and resolved Attendees; PostgreSQL also enforces request-level
  uniqueness in persisted attendee and Assignment relationships.
- **Complete outcome:** Domain validates a complete `AssignmentGroup`, while one
  transactional save guarantees a complete durable graph or no durable graph.

These physical constraints do not represent every Domain invariant. Single
Zone, contiguity and other group rules remain Domain responsibilities.

## Persistence boundary

```text
Application
→ repositories stage state
→ shared FestivalDbContext
→ IUnitOfWork
→ one SaveChangesAsync
→ PostgreSQL
```

Repositories do not commit. The Unit of Work defines the durable boundary for
the `AssignmentRequest`, its attendees and its Assignments.

## Concurrency model

```text
request A → independent scope → FestivalDbContext A
request B → independent scope → FestivalDbContext B

both prepare locally valid state
→ PostgreSQL unique constraints arbitrate
→ one commit
→ one rollback
```

The winner is not predetermined. Pre-checks are advisory under concurrency, and
the database constraints are the final correctness boundary. The losing graph
is completely rolled back, and its failed context is discarded with the scope.
No application-level lock is required for current correctness; the model makes
no claim that it optimizes high contention.

## Key architectural decisions

- PostgreSQL is the MVP persistence engine, as recorded in
  [ADR 0001](../adr/0001-select-mvp-database-engine.md).
- Global uniqueness is encoded as physical constraints.
- Repository staging is separate from durable Unit of Work confirmation.
- Each use-case execution invokes one Unit of Work save.
- EF Core owns the transaction for that single save; no explicit transaction is
  required.
- Infrastructure translates known PostgreSQL conflicts into stable Application
  conflicts; unknown persistence errors propagate unchanged.
- A failed scoped context is discarded, with no speculative lock or retry
  mechanism.

## Testing evidence

The Stage 3 closure evidence is locally validated, not CI evidence:

```text
Build:
0 errors
0 warnings

Fast tests:
243/243

PostgreSQL integration tests:
44/44

Complete suite:
287/287

Concurrency confidence:
10/10 rounds
20/20 concurrent scenarios
0 flakiness detected
```

Fast tests cover isolated behavior. PostgreSQL integration tests cover physical
persistence, constraints and rollback. Deterministic concurrency tests cover
real competing transactions after test-only coordination at the durable save
boundary.

## Intentionally deferred

- **Stage 4 — Assignment Engine + Fairness:** Fairness Definition v1,
  `RotationScore`, justified assignment policy abstractions, Zone and contiguous
  Spot selection, and a deterministic weighted MVP strategy.
- **Stage 5 — Simulation + Decision Gate:** deterministic simulations, fairness
  metrics, distribution and edge-case analysis, scenario comparison and the
  technical viability conclusion.
- **Future productization:** API/HTTP conflict mapping, attendee-code validation
  workflow, CRUD and administration endpoints, assignment-query APIs not needed
  for validation, frontend, authentication, authorization, operational
  dashboard and deployment hardening.
- **Evidence-driven operational evolution:** automatic retries, locking,
  serializable isolation, idempotency, persistent failed-attempt audit,
  observability, and load/performance testing.

These items are not missing Stage 3 acceptance criteria. Current persistence
and concurrency correctness is already satisfied.

## Exit criteria

- [x] Real PostgreSQL persistence exists.
- [x] Successful graph persistence is atomic.
- [x] Failed persistence rollback is physically verified.
- [x] Global assignment constraints exist.
- [x] Known assignment conflicts are translated.
- [x] Unknown persistence errors remain unaltered.
- [x] Concurrent Spot conflicts preserve correctness.
- [x] Concurrent Attendee conflicts preserve correctness.
- [x] Independent scopes receive independent `FestivalDbContext` instances.
- [x] Failed contexts are not reused.
- [x] No unnecessary locking or retry mechanism was added.
- [x] Stage 3 documentation is current.
- [x] The complete test suite is green.

## Version milestone

- `v0.1.0 — Executable Domain Core` represents Stage 2: executable Domain and
  Application, in-memory Infrastructure and the core assignment flow without a
  durable persistence requirement.
- `v0.2.0 — Durable Assignment Core` represents Stage 3: PostgreSQL, atomic
  persistence, global invariants, rollback, conflict translation and concurrent
  correctness.

This document records the intended milestones only. Tags and releases are not
created as part of Stage 3 closure; they follow the closure and contributor
workflow merges.

## Next stage

**Stage 4 — Assignment Engine + Fairness** will build and refine the core
assignment strategy and define measurable fairness. It targets a deterministic
weighted strategy because reproducibility improves tests, scenario comparison
and fairness analysis during validation. Deterministic behavior is not
automatically fair: Stage 4 must define fairness, and Stage 5 must evaluate it.

The reduced technical MVP asks:

> Can the festival assign valid locations using a sufficiently fair assignment
> policy while preserving the required business invariants?

It retains the Assignment Engine, fairness, persistence correctness,
concurrency correctness, simulation and decision gate. Product administration
surfaces and operational hardening remain outside the current MVP surface.

## Conclusion

Stage 3 is closed. The system now has a durable assignment core with explicit
transaction ownership, database-backed global invariant enforcement, controlled
conflict semantics and repeatable evidence that concurrent losers leave no
partial durable state. Stage 4 can build the fairness strategy on that verified
foundation.
