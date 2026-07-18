# Stage 3 — Persistence Model and Transactional Boundary

## 1. Context

Stage 2 validated the assignment flow using deterministic in-memory adapters.

The system can currently:

* receive an assignment request;
* resolve external attendee codes into internal attendee identities;
* build an assignment group;
* obtain available spots;
* execute the deterministic assignment engine;
* complete or reject the request;
* store the result using in-memory repositories.

Stage 3 introduces durable persistence, global invariant protection and concurrency handling.

Before implementing EF Core or selecting a database engine, this document defines:

* the minimum persistence model;
* the relationship between the domain model and the relational model;
* the information that must be preserved historically;
* the global invariants that require database support;
* the transactional boundary of the main assignment process;
* the minimum persistence contracts required by Application.

This document is intentionally independent of SQL Server and PostgreSQL whenever possible.

---

## 2. Design principles

The persistence model is not a direct copy of the domain model.

A domain object receives an independent table only when its information must:

* survive application restarts;
* support a business query;
* protect an invariant;
* preserve historical evidence;
* participate in a durable relationship;
* be reconstructed after persistence.

The following relationship is avoided:

```text
Domain class
→ automatically becomes a database table
```

The preferred relationship is:

```text
Business consistency, history or query requirement
→ persistence decision
```

The initial design also follows these principles:

* Keep the transactional boundary as small as reasonably possible.
* Protect local invariants in Domain.
* Coordinate the workflow in Application.
* Protect global uniqueness with database constraints.
* Use a transaction to prevent partial persistence.
* Avoid introducing abstractions without a demonstrated need.
* Defer database-specific optimizations until the database engine is selected.

---

## 3. Domain inventory and classification

| Concept                      |        Persisted? | Representation  | Reason                                 |
| ---------------------------- | ----------------: | --------------- | -------------------------------------- |
| `Attendee`                   |               Yes | Table           | Master data and attendee resolution    |
| `AttendeeCode`               |               Yes | Column          | External identifier submitted by users |
| `AttendeeId`                 |               Yes | Column          | Internal domain identity               |
| `FestivalDay`                |               Yes | Table           | Temporal assignment context            |
| `FestivalDayId`              |               Yes | Column          | Internal domain identity               |
| `AssignmentWindow`           | Not independently | Columns         | Part of `FestivalDay`                  |
| `Zone`                       |               Yes | Table           | Physical catalog                       |
| `ZoneId`                     |               Yes | Column          | Internal domain identity               |
| `Spot`                       |               Yes | Table           | Assignable physical position           |
| `SpotCode`                   |               Yes | Column          | Visible and stable Spot identifier     |
| `RowCode`                    |               Yes | Column          | Physical component of a Spot           |
| `SpotNumber`                 |               Yes | Column          | Physical component of a Spot           |
| `AssignmentRequest`          |               Yes | Table           | Audit and processing outcome           |
| `AssignmentRequestId`        |               Yes | Column          | Internal domain identity               |
| Requested attendee codes     |               Yes | Child table     | Original confirmed request input       |
| `AssignmentGroup`            |                No | Reconstructible | Temporary processing object            |
| `GroupSize`                  | Not independently | Derived         | Can be calculated from request members |
| `Assignment`                 |               Yes | Table           | Final result and historical evidence   |
| `AssignmentId`               |               Yes | Column          | Internal domain identity               |
| `AssignmentRequestRejection` | Not independently | Columns         | Part of `AssignmentRequest`            |
| `AssignmentRequestFailure`   | Not independently | Columns         | Part of `AssignmentRequest`            |
| `AssignmentRequestStatus`    | Not independently | Column          | Part of `AssignmentRequest`            |

---

## 4. Aggregate roots and consistency boundaries

### `Attendee`

`Attendee` is an independently persisted master-data concept.

It has its own identity and can be retrieved through its external `AttendeeCode`.

### `FestivalDay`

`FestivalDay` is independently persisted and owns its `AssignmentWindow`.

The window does not need an independent lifecycle or table.

### `Zone`

`Zone` is an independently persisted physical catalog concept.

### `Spot`

`Spot` is independently persisted and belongs to one `Zone`.

For the MVP, `SpotCode` is considered stable enough to serve as its persistent identity.

### `AssignmentRequest`

`AssignmentRequest` is the main process record.

It owns:

* the original confirmed attendee codes;
* its processing status;
* its resolution timestamp;
* its rejection information;
* its failure information when persistence of a technical failure is possible.

### `Assignment`

`Assignment` represents a final individual result.

Assignments are persisted together with the outcome of their originating `AssignmentRequest`.

### `AssignmentGroup`

`AssignmentGroup` is not independently persisted.

It is a temporary domain object used to:

* represent the resolved group;
* validate internal attendee identity uniqueness;
* validate the complete assignment result;
* prevent partial group assignment.

It does not have a lifecycle independent from the assignment process.

---

## 5. Minimum relational model

## 5.1 Table: `Attendees`

### Responsibility

Store the minimum master data required to identify and resolve an attendee.

### Columns

| Column         | Conceptual type | Nullable | Description                       |
| -------------- | --------------- | -------: | --------------------------------- |
| `AttendeeId`   | `Guid`          |       No | Internal domain identifier        |
| `AttendeeCode` | `string`        |       No | External attendee code            |
| `Name`         | `string`        |       No | Name returned during verification |

### Primary key

* `AttendeeId`

### Foreign keys

* None.

### Unique constraints

* `AttendeeCode`

### Value Object mappings

* `AttendeeId` → `Guid`
* `AttendeeCode` → `string`

### Minimum indexes

* Unique index on `AttendeeCode`.

---

## 5.2 Table: `FestivalDays`

### Responsibility

Store each assignable festival day and its assignment window.

### Columns

| Column                  | Conceptual type | Nullable | Description             |
| ----------------------- | --------------- | -------: | ----------------------- |
| `FestivalDayId`         | `Guid`          |       No | Internal day identifier |
| `Date`                  | `date`          |       No | Festival calendar date  |
| `AssignmentWindowStart` | `time`          |       No | Assignment window start |
| `AssignmentWindowEnd`   | `time`          |       No | Assignment window end   |

### Primary key

* `FestivalDayId`

### Foreign keys

* None.

### Unique constraints

* `Date`

### Value Object mappings

* `FestivalDayId` → `Guid`
* `AssignmentWindow.Start` → `time`
* `AssignmentWindow.End` → `time`

### Minimum indexes

* Unique index on `Date`.

### Notes

`AssignmentWindow` is stored as part of `FestivalDays` because it has no independent identity or lifecycle.

---

## 5.3 Table: `Zones`

### Responsibility

Store the physical zones that contain assignable Spots.

### Columns

| Column   | Conceptual type | Nullable | Description              |
| -------- | --------------- | -------: | ------------------------ |
| `ZoneId` | `Guid`          |       No | Internal zone identifier |
| `Name`   | `string`        |       No | Visible zone name        |

### Primary key

* `ZoneId`

### Foreign keys

* None.

### Unique constraints

No uniqueness constraint on `Name` is required unless the business explicitly guarantees that zone names cannot repeat.

### Value Object mappings

* `ZoneId` → `Guid`

### Minimum indexes

* None beyond the primary key for the initial MVP.

### Notes

Zone quality, priority and fairness-related attributes are deferred.

---

## 5.4 Table: `Spots`

### Responsibility

Store the physical catalog of assignable positions.

### Columns

| Column       | Conceptual type | Nullable | Description                        |
| ------------ | --------------- | -------: | ---------------------------------- |
| `SpotCode`   | `string`        |       No | Visible and stable Spot identifier |
| `ZoneId`     | `Guid`          |       No | Zone containing the Spot           |
| `RowCode`    | `string`        |       No | Physical row                       |
| `SpotNumber` | `int`           |       No | Position within the row            |

### Primary key

* `SpotCode`

### Foreign keys

* `ZoneId` → `Zones.ZoneId`

### Unique constraints

* `SpotCode`
* `(ZoneId, RowCode, SpotNumber)`

### Value Object mappings

* `SpotCode` → `string`
* `ZoneId` → `Guid`
* `RowCode` → `string`
* `SpotNumber` → `int`

### Minimum indexes

* Unique index on `SpotCode`.
* Unique composite index on `(ZoneId, RowCode, SpotNumber)`.

### Decision

`SpotCode` will be used as the initial primary key because it is currently considered a stable business identifier.

A separate `SpotId` may be introduced later if Spot codes become editable or externally managed.

---

## 5.5 Table: `AssignmentRequests`

### Responsibility

Store each formal assignment request and its final processing outcome.

### Columns

| Column                | Conceptual type  | Nullable | Description                    |
| --------------------- | ---------------- | -------: | ------------------------------ |
| `AssignmentRequestId` | `Guid`           |       No | Request identifier             |
| `FestivalDayId`       | `Guid`           |       No | Requested festival day         |
| `RequestedAt`         | `datetimeoffset` |       No | Request creation timestamp     |
| `Status`              | `string`         |       No | Request status                 |
| `ResolvedAt`          | `datetimeoffset` |      Yes | Final resolution timestamp     |
| `RejectionCode`       | `string`         |      Yes | Business rejection code        |
| `RejectionMessage`    | `string`         |      Yes | Business rejection description |
| `FailureCode`         | `string`         |      Yes | Technical failure code         |
| `FailureMessage`      | `string`         |      Yes | Technical failure description  |

### Primary key

* `AssignmentRequestId`

### Foreign keys

* `FestivalDayId` → `FestivalDays.FestivalDayId`

### Unique constraints

* None beyond the primary key.

### Value Object mappings

* `AssignmentRequestId` → `Guid`
* `FestivalDayId` → `Guid`
* `AssignmentRequestStatus` → `string`
* `AssignmentRequestRejection`:

  * `RejectionCode`
  * `RejectionMessage`
* `AssignmentRequestFailure`:

  * `FailureCode`
  * `FailureMessage`

### Minimum indexes

* Composite index on `(FestivalDayId, RequestedAt)`.

An index on `Status` should only be added if an operational query requires it.

### Status consistency

The expected states are:

#### `Received`

* `ResolvedAt` is null.
* Rejection fields are null.
* Failure fields are null.

#### `Completed`

* `ResolvedAt` is required.
* Rejection fields are null.
* Failure fields are null.

#### `Rejected`

* `ResolvedAt` is required.
* Rejection fields are required.
* Failure fields are null.

#### `Failed`

* `ResolvedAt` is required.
* Failure fields are required.
* Rejection fields are null.

These rules are initially protected by Domain and Application.

Database `CHECK` constraints may be evaluated after the database engine is selected.

---

## 5.6 Table: `AssignmentRequestAttendees`

### Responsibility

Preserve the attendee codes included in the formal and confirmed request.

### Columns

| Column                | Conceptual type | Nullable | Description                          |
| --------------------- | --------------- | -------: | ------------------------------------ |
| `AssignmentRequestId` | `Guid`          |       No | Parent request                       |
| `Position`            | `int`           |       No | Original position within the request |
| `AttendeeCode`        | `string`        |       No | Confirmed attendee code              |

### Primary key

* `(AssignmentRequestId, Position)`

### Foreign keys

* `AssignmentRequestId` → `AssignmentRequests.AssignmentRequestId`

### Unique constraints

* `(AssignmentRequestId, AttendeeCode)`

### Value Object mappings

* `AssignmentRequestId` → `Guid`
* `AttendeeCode` → `string`

### Minimum indexes

* Index on `AssignmentRequestId`.
* Unique composite index on `(AssignmentRequestId, AttendeeCode)`.

### Notes

The MVP persists only formal requests that have passed the preliminary attendee verification step.

Therefore, duplicate attendee codes are not expected inside a formal request.

The domain still rejects duplicated codes to prevent invalid construction from other clients, direct API calls or programming errors.

---

## 5.7 Table: `Assignments`

### Responsibility

Store the final assignment of one attendee to one Spot for a specific festival day.

### Columns

| Column                | Conceptual type  | Nullable | Description                |
| --------------------- | ---------------- | -------: | -------------------------- |
| `AssignmentId`        | `Guid`           |       No | Assignment identifier      |
| `AssignmentRequestId` | `Guid`           |       No | Originating request        |
| `FestivalDayId`       | `Guid`           |       No | Assigned festival day      |
| `AttendeeId`          | `Guid`           |       No | Assigned attendee          |
| `SpotCode`            | `string`         |       No | Assigned Spot code         |
| `ZoneId`              | `Guid`           |       No | Historical assigned zone   |
| `RowCode`             | `string`         |       No | Historical assigned row    |
| `SpotNumber`          | `int`            |       No | Historical assigned number |
| `AssignedAt`          | `datetimeoffset` |       No | Assignment timestamp       |

### Primary key

* `AssignmentId`

### Foreign keys

* `AssignmentRequestId` → `AssignmentRequests.AssignmentRequestId`
* `FestivalDayId` → `FestivalDays.FestivalDayId`
* `AttendeeId` → `Attendees.AttendeeId`
* `SpotCode` → `Spots.SpotCode`

### Unique constraints

* `(FestivalDayId, SpotCode)`
* `(FestivalDayId, AttendeeId)`
* `(AssignmentRequestId, AttendeeId)`

### Value Object mappings

* `AssignmentId` → `Guid`
* `AssignmentRequestId` → `Guid`
* `FestivalDayId` → `Guid`
* `AttendeeId` → `Guid`
* `SpotCode` → `string`
* `ZoneId` → `Guid`
* `RowCode` → `string`
* `SpotNumber` → `int`

### Minimum indexes

* Unique composite index on `(FestivalDayId, SpotCode)`.
* Unique composite index on `(FestivalDayId, AttendeeId)`.
* Index on `AssignmentRequestId`.

### Historical snapshot

The following fields are preserved as a snapshot of the assignment result:

* `SpotCode`
* `ZoneId`
* `RowCode`
* `SpotNumber`

This prevents a future modification to the Spot catalog from changing the historical meaning of an existing Assignment.

The foreign key to `Spots` represents the source catalog entry, while the snapshot represents the result as it existed when assigned.

---

## 6. Objects without independent tables

### `AssignmentGroup`

`AssignmentGroup` does not need an independent table.

For a completed request, its relevant information can be reconstructed from:

* `AssignmentRequests`;
* `AssignmentRequestAttendees`;
* `Assignments`.

For a rejected request, the original attendee codes remain auditable, but the resolved internal attendee identities are not persisted.

This is an intentional Lean decision because no current requirement needs the exact resolved internal group of a rejected request.

### `GroupSize`

`GroupSize` is derived from:

* the number of rows in `AssignmentRequestAttendees`; or
* the number of Assignments generated by a completed request.

It will not be persisted as an independent value unless a future query or performance requirement justifies it.

### `AssignmentWindow`

Stored as:

* `AssignmentWindowStart`;
* `AssignmentWindowEnd`;

inside `FestivalDays`.

### `AssignmentRequestRejection`

Stored as:

* `RejectionCode`;
* `RejectionMessage`;

inside `AssignmentRequests`.

### `AssignmentRequestFailure`

Stored as:

* `FailureCode`;
* `FailureMessage`;

inside `AssignmentRequests`.

### `AssignmentRequestStatus`

Stored as a string column in `AssignmentRequests`.

Using a string improves direct database readability during the MVP.

The column must have:

* a bounded length;
* only known status values;
* explicit conversion through the persistence mapping.

---

## 7. Preliminary attendee verification

Before a formal `AssignmentRequest` is created, the user performs a verification step.

The expected user flow is:

```text
1. Enter one or more AttendeeCodes.
2. Select Verify.
3. The system validates the codes.
4. The system returns the attendee names.
5. The user reviews the attendees.
6. The user confirms the assignment request.
```

The verification operation checks:

* valid code format;
* allowed group size;
* duplicate attendee codes;
* existence of every attendee code;
* attendee names associated with those codes.

Invalid verification attempts do not create formal `AssignmentRequests` in the MVP.

Therefore:

```text
Invalid verification
→ no AssignmentRequest
→ no persistence
```

A formal request begins only after the user confirms a previously verified selection:

```text
Successful verification
→ user confirmation
→ AssignmentRequest creation
```

The preliminary verification improves user experience and reduces typing errors, but it is not a security or consistency boundary.

The backend continues protecting structural domain invariants because:

* the API may be called directly;
* another client may be introduced;
* the submitted payload may be modified;
* the state may change between verification and confirmation;
* programming errors may bypass the UI flow.

`AssignmentRequest` therefore continues rejecting:

* empty attendee lists;
* groups outside the allowed size;
* duplicate `AttendeeCodes`.

`AssignmentGroup` continues rejecting duplicate `AttendeeIds` after code resolution.

These protections are complementary:

```text
AssignmentRequest
→ protects the external confirmed input.

AssignmentGroup
→ protects the resolved internal identities.
```

---

## 8. Domain reconstruction

## 8.1 Can `AssignmentRequest` be reconstructed?

Yes.

It requires:

* `AssignmentRequests` for identity, day, timestamps, status and outcome;
* `AssignmentRequestAttendees` for the original confirmed attendee codes and their order.

## 8.2 Can `Assignment` be reconstructed?

Yes.

One row in `Assignments` contains the complete information required to reconstruct the domain object.

## 8.3 Can `AssignmentGroup` be reconstructed?

For completed requests, yes.

It can be reconstructed using:

* the request identity;
* the festival day;
* the attendee identities in `Assignments`.

For rejected requests, the original external attendee codes remain available, but the resolved internal attendee identities are not guaranteed to be persisted.

Exact reconstruction of the internal rejected group is not required by the MVP.

## 8.4 Can rejected requests be audited?

Yes.

The following information remains available:

* request identity;
* festival day;
* original confirmed attendee codes;
* original attendee order;
* request timestamp;
* resolution timestamp;
* rejection code;
* rejection message.

This requires both:

* `AssignmentRequests`;
* `AssignmentRequestAttendees`.

---

## 9. Historical information

The persistence model distinguishes between current references and historical snapshots.

### Current references

The following values connect the result to current master data:

* `AssignmentRequestId`
* `FestivalDayId`
* `AttendeeId`
* `SpotCode`

### Historical snapshot

The following values preserve the assignment as it existed when confirmed:

* `SpotCode`
* `ZoneId`
* `RowCode`
* `SpotNumber`
* `AssignedAt`

A future change to the Spot catalog must not alter the historical assignment result.

For the MVP, attendee names are not duplicated inside `Assignments`.

The Assignment retains `AttendeeId`, and the current attendee name is resolved from `Attendees`.

A separate attendee-name snapshot may be introduced only if the business later requires historically immutable attendee display data.

---

## 10. Global invariants

## INV-01 — Unique Spot per FestivalDay

A Spot cannot be assigned more than once on the same FestivalDay.

Protection:

```text
Unique constraint:
Assignments(FestivalDayId, SpotCode)
```

The assignment flow may check availability before insertion, but the database constraint is the final protection against concurrent requests.

---

## INV-02 — Unique Attendee per FestivalDay

An Attendee cannot receive more than one Spot on the same FestivalDay.

Protection:

```text
Unique constraint:
Assignments(FestivalDayId, AttendeeId)
```

The invariant uses `AttendeeId`, not `AttendeeCode`, because internal identity determines whether two external codes represent the same attendee.

---

## INV-03 — Complete AssignmentGroup

A group must be persisted completely or not persisted at all.

Protection:

* domain result validation;
* one persistence unit shared by the request and assignments;
* one atomic `SaveChangesAsync`;
* rollback if persistence cannot complete.

Expected outcome:

```text
Completed AssignmentRequest
+
all Assignments
```

The following result must never be committed:

```text
Completed AssignmentRequest
+
partial Assignments
```

---

## INV-06 — Final Assignment

An Assignment is final after confirmation.

Initial protection:

* immutable domain object;
* no repository update operation;
* no repository delete operation;
* no application use case for replacement or removal.

Database-level immutability mechanisms are deferred until a real modification requirement appears.

---

## INV-08 — AssignmentRequest outcome consistency

A request outcome must agree with its persisted result.

### Completed

```text
Status = Completed
ResolvedAt is present
Rejection is absent
Failure is absent
One complete Assignment exists for each resolved group member
```

### Rejected

```text
Status = Rejected
ResolvedAt is present
Rejection is present
Failure is absent
No Assignments exist
```

### Failed

```text
Status = Failed
ResolvedAt is present
Failure is present
Rejection is absent
No partial Assignments exist
```

This invariant spans multiple records and cannot be completely protected by a simple unique constraint.

Its primary protection is:

* Domain;
* Application orchestration;
* the persistence transaction;
* database uniqueness constraints.

---

## 11. Transactional boundary

## 11.1 Business operation

The transactional boundary represents the durable outcome of processing one formal assignment request.

The transaction does not exist merely to group repository calls.

It exists to protect one complete business state change:

```text
Formal AssignmentRequest
→ Completed with all Assignments
```

or:

```text
Formal AssignmentRequest
→ Rejected with no Assignments
```

---

## 11.2 Operations before the transaction

Operations that do not depend on mutable shared assignment state may occur before the transaction:

* receive the confirmed request command;
* normalize primitive input when applicable;
* create `AssignmentRequest`;
* validate structural request invariants;
* resolve attendee codes;
* create `AssignmentGroup`;
* validate local group invariants.

These operations do not modify persistent state.

The exact placement of attendee resolution may be adjusted during implementation if it must use the same database context.

---

## 11.3 Operations within the transaction

The transaction must include operations that depend on current shared state or produce the durable result:

1. Read the current assignments relevant to the FestivalDay.
2. Validate that the Attendees are not already assigned.
3. Determine the currently available Spots.
4. Execute the assignment engine using the current availability.
5. Produce either a completed or rejected domain outcome.
6. Register the `AssignmentRequest`.
7. Register its confirmed attendee codes.
8. Register every generated Assignment when successful.
9. Call `SaveChangesAsync` once.
10. Commit the transaction.

Database unique constraints remain the final protection if another concurrent transaction changes availability after it was read.

---

## 11.4 Successful flow

```text
Create formal AssignmentRequest
→ resolve AttendeeIds
→ create AssignmentGroup
→ read current availability
→ execute AssignmentEngine
→ create all Assignments
→ mark AssignmentRequest Completed
→ register request and assignments
→ SaveChangesAsync
→ commit
```

The request and all Assignments become durable together.

---

## 11.5 Rejected flow

Examples of rejection after formal confirmation include:

* an Attendee is already assigned for the FestivalDay;
* there is no sufficiently large contiguous block;
* availability changed after preliminary verification;
* the assignment window is no longer valid.

Flow:

```text
Create formal AssignmentRequest
→ evaluate current business state
→ no valid assignment result
→ mark AssignmentRequest Rejected
→ register request and confirmed attendee codes
→ register zero Assignments
→ SaveChangesAsync
→ commit
```

A business rejection is an expected result and is committed normally.

It is not treated as a technical rollback.

---

## 11.6 Rollback conditions

Rollback occurs when the system cannot complete the expected durable state change.

Examples:

* database connection failure;
* unexpected database exception;
* constraint violation caused by a concurrent request;
* persistence failure during `SaveChangesAsync`;
* operation cancellation;
* unexpected technical exception;
* partial persistence cannot be completed.

A constraint violation caused by concurrency must later be translated into a controlled Application result.

The exact user-facing result is deferred to the concurrency implementation tasks.

---

## 11.7 Technical failure handling

For the initial MVP:

```text
Technical failure before commit
→ rollback
→ structured log
→ technical error result
```

Persistence of `AssignmentRequestStatus.Failed` is not guaranteed when the database itself is unavailable.

This is accepted as an initial limitation.

The MVP does not persist a `Received` request in a separate initial transaction.

Therefore, an interrupted or rolled-back technical request may exist only in application logs.

A future version may evaluate:

* persisting `Received` before processing;
* updating the request in a second transaction;
* recovery of abandoned requests;
* timeout detection;
* external audit or event storage.

An unfinished request must not automatically be interpreted as `Failed`, because it could represent:

* a request still being processed;
* an interrupted process;
* a timeout;
* an infrastructure outage;
* an application crash.

---

## 12. Persistence contract evaluation

The current contracts are:

```csharp
public interface IAssignmentRequestRepository
{
    Task AddAsync(
        AssignmentRequest assignmentRequest,
        CancellationToken cancellationToken = default);
}
```

```csharp
public interface IAssignmentRepository
{
    Task AddAsync(
        IEnumerable<Assignment> assignments,
        CancellationToken cancellationToken = default);
}
```

These contracts register changes but do not explicitly confirm them.

To coordinate the atomic persistence operation, the following minimal abstraction is proposed:

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
```

### Decision

The initial persistence implementation will use:

* `IAssignmentRequestRepository`;
* `IAssignmentRepository`;
* one shared persistence context;
* one `IUnitOfWork`;
* one call to `SaveChangesAsync`.

Conceptually:

```text
Repository AddAsync
→ registers pending changes in the shared context.

UnitOfWork.SaveChangesAsync
→ persists the complete outcome atomically.
```

When using EF Core, one `SaveChangesAsync` call wraps the pending changes in a transaction when the provider supports transactions.

This is sufficient for the initial flow because it requires only one durable persistence operation.

The first version does not require explicit Application contracts for:

* `BeginTransactionAsync`;
* `CommitAsync`;
* `RollbackAsync`.

Those operations should be introduced only if future implementation requires:

* multiple `SaveChangesAsync` calls;
* multiple database contexts;
* multiple transactional resources;
* an isolation level controlled by Application;
* database-specific locking.

### Why `UnitOfWork` is justified here

`UnitOfWork` is not introduced only because it is a common repository pattern.

It addresses a concrete need:

> `AssignmentRequest` and all generated `Assignments` must be persisted as one consistency unit.

---

## 13. Lean decisions

The following decisions keep the initial persistence design small:

* `SpotCode` is the primary key of `Spots`.
* `AssignmentGroup` has no independent table.
* `GroupSize` is derived.
* `AssignmentWindow` is stored inside `FestivalDays`.
* Request rejection and failure details are stored inside `AssignmentRequests`.
* Resolved `AttendeeIds` are not persisted for rejected requests.
* Invalid preliminary verification attempts are not persisted.
* Formal request data is persisted only after user confirmation.
* One shared persistence context is used.
* One `SaveChangesAsync` call represents the initial atomic commit.
* No explicit transaction API is added to Application initially.
* No advanced locking strategy is selected before concurrency tests provide evidence.
* No speculative indexes are added beyond identity, uniqueness and known queries.

---

## 14. Decisions deferred

The following decisions are intentionally deferred:

* SQL Server or PostgreSQL.
* EF Core provider.
* Concrete database column types and lengths.
* Naming conventions for tables and constraints.
* Database schemas.
* Delete behavior for foreign keys.
* `CHECK` constraints for request status consistency.
* Explicit transaction isolation level.
* Pessimistic locking.
* Optimistic concurrency tokens.
* Translation of database constraint violations.
* Retry strategy for concurrency conflicts.
* Persistence of technical failures when the primary database is unavailable.
* Recovery of requests left in `Received`.
* Attendee-name historical snapshots.
* Spot catalog versioning.
* Fairness.
* `RotationScore`.
* API design for preliminary attendee verification.

---

## 15. Risks and implementation considerations

### Verification does not reserve state

The preliminary attendee verification step confirms identity only.

It does not reserve:

* Attendees;
* Spots;
* a contiguous block;
* the assignment window.

The assignment result must be recalculated during formal request processing.

### Availability can change concurrently

Two requests may read the same available Spots.

Unique constraints protect the final database state, but the application must later translate the losing transaction into a controlled result.

### Spot snapshots duplicate catalog data

`Assignments` intentionally duplicates Spot location information.

This is acceptable because the duplicated values preserve historical meaning rather than representing accidental denormalization.

### One `SaveChangesAsync` is an initial assumption

The proposed Unit of Work is sufficient while the complete outcome can be registered and persisted in one call.

The decision must be revisited if the implementation requires more than one persistence flush.

### Failed requests are not always durable

If persistence infrastructure fails, the system may be unable to store the failure using the same database.

Structured logging is the accepted MVP fallback.

---

## 16. Outcome

The initial persistence model consists of:

```text
Attendees
FestivalDays
Zones
Spots
AssignmentRequests
AssignmentRequestAttendees
Assignments
```

The main atomic business operation is:

```text
Persist one formal AssignmentRequest outcome
+
persist all generated Assignments when completed
+
commit both together
```

Global uniqueness is backed by:

```text
Unique(FestivalDayId, SpotCode)
Unique(FestivalDayId, AttendeeId)
```

Group completeness is backed by:

```text
Domain validation
+
Application orchestration
+
one shared persistence context
+
one UnitOfWork.SaveChangesAsync
```

The design is sufficiently defined to continue with:

1. selecting the MVP database engine;
2. adding EF Core and database test infrastructure;
3. implementing the persistence model;
4. validating constraints and transaction behavior;
5. testing concurrent assignment conflicts.
