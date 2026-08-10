# Stage 3 — Technical Lead Learnings

## Context

Stage 3 moved the Festival Assignment System from deterministic in-memory
validation to durable PostgreSQL persistence. The central engineering question
was not simply whether EF Core could store an `AssignmentRequest`. It was
whether the complete request outcome could remain correct when independent
operations observe valid state and then compete to persist it.

The implementation and its tests provide a concrete decision trail:

```text
decision
→ reasoning
→ trade-off
→ evidence
```

Detailed model and implementation context remains in the
[Stage 3 persistence design](../stage-3-persistence-model-and-transaction-boundary.md)
and [PostgreSQL relational model](../architecture/postgresql-relational-model.md).

## 1. Protect invariants at the authoritative boundary

**Decision.** Domain and Application express the rules and reject invalid state
they can observe. PostgreSQL constraints provide the final global guarantee for:

```text
one Spot per FestivalDay
one Attendee per FestivalDay
one Attendee per AssignmentRequest
```

**Reasoning.** A single use-case execution can validate its local
`AssignmentGroup`, requested Attendees and available Spots, but it cannot see
uncommitted work in another independent request. Only the shared durable store
can arbitrate all competing commits.

**Trade-off.** The model contains deliberate enforcement at more than one
boundary. This is complementary rather than redundant: Domain and Application
produce meaningful behavior early, while physical unique constraints preserve
global correctness even when local observations become stale. Not every Domain
invariant is represented by a database constraint; group completeness, Zone and
contiguity rules still depend on Domain behavior and the atomic persistence
boundary.

**Evidence.** Real concurrent tests let two scoped requests prepare locally
valid state for the same Spot and, separately, for the same Attendee. PostgreSQL
allowed one complete graph to commit and rejected the competing graph without
creating a duplicate durable Assignment.

## 2. Pre-checks reduce conflicts but do not guarantee correctness

**Decision.** Availability and existing-assignment checks remain useful, but
are advisory under concurrency.

```text
check current availability
→ another request commits
→ persist Assignment using stale knowledge
```

**Reasoning.** A pre-check can be correct at the instant it executes and stale
before `SaveChangesAsync`. Removing the pre-check would worsen normal behavior;
treating it as a guarantee would leave a race condition.

**Trade-off.** The use case retains early validation for clear outcomes and
avoided work, while accepting that the durable save can still produce a
controlled conflict. This separates user-facing behavior optimization from
correctness enforcement.

**Evidence.** The same-Spot and same-Attendee concurrency scenarios coordinate
both operations before the durable save. Both pass their local preparation, and
the database constraint resolves the conflict.

## 3. Transactions must represent business units

**Decision.** The durable unit is the complete outcome graph:

```text
AssignmentRequest
+ AssignmentRequestAttendees
+ Assignments
= complete graph or no durable graph
```

**Reasoning.** Persisting any element independently could leave a Completed
request without all Assignments, Assignments without a consistent request, or a
partial `AssignmentGroup`. The transaction boundary therefore follows
`AssignmentRequest` outcome consistency rather than table or repository
boundaries.

**Trade-off.** All changes for one execution must share one scoped
`FestivalDbContext` and one confirmation point. This couples their durability
without coupling Application to EF Core.

**Evidence.** PostgreSQL integration tests verify both successful complete-graph
persistence and complete rollback when Spot, Attendee or duplicate-request
uniqueness fails.

## 4. Repositories stage; Unit of Work confirms

**Decision.** PostgreSQL repository operations map and stage state. They do not
call `SaveChangesAsync`.

```text
Repository.AddAsync
→ maps and stages persistence state

IUnitOfWork.SaveChangesAsync
→ confirms all staged changes durably
```

**Reasoning.** A repository-level save would divide the business transaction by
storage concern. The Unit of Work gives `ProcessAssignmentRequestUseCase` one
explicit durable boundary after it has staged the final Completed or Rejected
outcome.

**Trade-off.** `AddAsync` does not mean durable completion, so repository
contracts and tests must make staging semantics clear. In exchange, atomicity is
visible and independently verifiable.

**Evidence.** Repository integration tests observe staged state, while Unit of
Work and full-flow tests confirm that the graph becomes durable only at the one
save boundary.

## 5. Keep Application independent from persistence technology

**Decision.** Application owns `IUnitOfWork` and stable conflict concepts;
Infrastructure owns EF Core and PostgreSQL details.

```text
Application
→ SpotAlreadyAssigned
→ AttendeeAlreadyAssigned
→ DuplicateRequestAssignment

Infrastructure
→ DbUpdateException
→ PostgresException
→ SQLSTATE 23505
→ physical constraint names
```

**Reasoning.** The use case needs to describe a persistence conflict in the
language of the problem, not the mechanism used by one provider.

**Trade-off.** Infrastructure must maintain an explicit translation table for
known physical constraints. That small provider-specific responsibility keeps
upper layers stable if low-level exception shapes or persistence technology
change.

**Evidence.** Project references preserve the inward dependency direction, and
Application contracts contain no EF Core, Npgsql, SQLSTATE or physical index
names.

## 6. Translate infrastructure failures into stable application concepts

**Decision.** `EfCoreUnitOfWork` translates only recognized PostgreSQL unique
violations into `AssignmentPersistenceConflictException`. Unknown EF Core and
PostgreSQL errors propagate unchanged.

**Reasoning.** Broad exception conversion would erase diagnostic meaning and
could misclassify outages or defects as business conflicts. Precise translation
lets upper layers depend on a stable contract without pretending every database
failure has the same meaning.

**Trade-off.** Physical constraint names form an internal mapping contract that
must remain aligned with migrations. This is preferable to leaking those names
through Application.

**Evidence.** Unit tests cover recognized and unrecognized translation cases;
real PostgreSQL rollback tests verify all three stable conflict classifications,
and an integration test verifies propagation of an unknown database error.

## 7. Design for failure, not only for the happy path

**Decision.** Stage 3 required evidence for both outcomes:

```text
SaveChangesAsync succeeds
→ complete commit

SaveChangesAsync fails
→ complete rollback
```

**Reasoning.** A successful insert proves mapping, but not transactional
correctness. The highest-risk state is the partially durable graph after a late
failure.

**Trade-off.** Failure-path tests need deliberate conflicting seed data and
post-failure database inspection. They cost more than unit tests but directly
validate INV-03 and INV-08 at the physical boundary.

**Evidence.** Integration tests inspect PostgreSQL after known conflicts and
confirm that the losing `AssignmentRequest`, its attendee rows and its
Assignments are all absent.

## 8. Discard failed scoped persistence contexts

**Decision.** A failed scoped operation discards its `FestivalDbContext`; Stage
3 performs no retry in the same context.

```text
failed scoped operation
→ discard FestivalDbContext
→ no same-context retry
```

**Reasoning.** After a failed save, tracked entities still represent an
unsuccessful pending graph. Repairing that tracker correctly is a separate
workflow with its own risks and no current requirement.

**Trade-off.** Stage 3 deliberately avoids `ChangeTracker.Clear`, selective
detachment, tracked-state repair, automatic retry and a second
`SaveChangesAsync` in the same scope. A future retry policy must recreate and
rerun the complete business operation when idempotency and operational evidence
justify it.

**Evidence.** The concurrent tests bound each operation to an independent scope,
verify different `FestivalDbContext` instances and dispose the losing scope
after its controlled conflict.

## 9. Test concurrency deterministically

**Decision.** Concurrency tests use test-only asynchronous coordination after
both graphs are staged and immediately before their durable saves.

**Reasoning.** `Task.WhenAll(...)` starts concurrent tasks, but scheduling and
database timing can allow one operation to finish before the other reaches the
critical boundary. Such a test may pass without exercising the intended race.

**Trade-off.** The coordination wrapper adds test complexity, but no production
lock or hook. It makes the contested state observable and repeatable.

**Evidence.** `AsyncPreSaveCoordinator` releases both independent operations
only after both reach the save boundary. The confidence run completed 10/10
rounds, covering 20/20 concurrent scenarios with no detected flakiness.

The general lesson is: concurrency tests should control the race rather than
hope timing creates one.

## 10. Prefer the simplest mechanism that guarantees correctness

**Decision.** The current correctness mechanism is:

```text
one SaveChangesAsync
+ PostgreSQL unique constraints
```

EF Core owns the transaction for the single save. Stage 3 introduced no manual
transaction, pessimistic lock, serializable isolation, PostgreSQL advisory lock,
automatic retry, idempotency mechanism, dedicated failure-audit persistence or
ChangeTracker recovery.

**Reasoning.** The present use case has one durable flush, and the database can
arbitrate its global uniqueness rules. Additional mechanisms would solve
operational or workflow requirements not yet demonstrated.

**Trade-off.** This design guarantees current correctness but does not claim to
optimize high contention. The deferred mechanisms remain valid options if load,
latency, retry semantics or multi-save workflows later justify them.

**Evidence.** Real constraint, rollback and concurrent-transaction tests pass
without application-level locks or retry behavior.

## 11. Build evidence at multiple testing levels

**Decision.** Stage 3 uses distinct test layers for distinct claims:

```text
fast tests
→ isolated Domain, Application and Infrastructure behavior

PostgreSQL integration tests
→ real mappings, constraints, transactions and exception behavior

concurrency integration tests
→ real competing transactions with a controlled race
```

**Reasoning.** Fast tests give precise feedback but cannot reproduce PostgreSQL
constraint and transaction semantics. Database tests give physical confidence
but should not replace focused isolated coverage.

**Trade-off.** The integration suite requires Docker and runs more slowly. It is
kept separate so fast feedback remains available while provider-specific claims
are still proven against the selected database.

**Evidence.** The final locally validated Stage 3 evidence recorded at closure
is:

```text
Build: 0 errors, 0 warnings
Fast tests: 243/243
PostgreSQL integration tests: 44/44
Complete suite: 287/287
Concurrency confidence: 10/10 rounds, 20/20 scenarios, 0 flakiness detected
```

## 12. Technical Lead perspective

Stage 3 demonstrates a sequence of architecture decisions grounded in risk:

- identify PostgreSQL as the boundary that can actually enforce uniqueness
  across independent requests;
- define the transaction from the complete `AssignmentRequest` business
  outcome, not from repository calls;
- keep provider-specific errors below the Application boundary and translate
  only failures with known meaning;
- choose the smallest mechanism that proves correctness before optimizing
  contention;
- require controlled, observable evidence before claiming concurrent safety;
- record deferred operational decisions so simplicity is intentional rather
  than accidental.

This leaves an architecture another developer can review and extend: the
business boundary, ownership of technical details, failure semantics and
evidence are explicit.

## Decisions deliberately deferred

These decisions are not missing Stage 3 acceptance criteria. The current MVP
correctness requirement is satisfied without them.

- **Automatic retry:** requires complete-operation retry semantics and evidence
  that the operation is sufficiently idempotent.
- **Locking strategy and PostgreSQL advisory locks:** no application-level lock
  is required for current correctness; contention evidence may justify one
  later.
- **Serializable isolation:** stronger isolation has costs and retry behavior
  that the current single-save design does not require.
- **Idempotency:** needs an external retry or duplicate-submission requirement
  before its contract can be designed responsibly.
- **Persistent audit of failed attempts:** the same failed database path cannot
  reliably record itself; a separate durable mechanism needs an explicit audit
  requirement.
- **API/HTTP conflict mapping:** Application exposes stable conflicts, but the
  external API surface is outside Stage 3.
- **Observability:** structured production metrics, tracing and alerting belong
  to operational productization.
- **Load and performance testing:** Stage 5 simulation and later representative
  workloads should provide inputs; correctness tests do not establish capacity.
- **ChangeTracker recovery:** the current policy is to discard a failed scoped
  context, so tracker repair adds no present value.

## Summary

Stage 3 aligned business invariants with the boundaries that can enforce them.
Domain and Application preserve meaningful rules and orchestration; PostgreSQL
guarantees global uniqueness; one Unit of Work save protects the complete
durable graph; Infrastructure translates only known provider failures; and
deterministic tests prove both commit and rollback behavior under real
competition. The result is a small persistence design whose correctness and
limitations are both explicit.
