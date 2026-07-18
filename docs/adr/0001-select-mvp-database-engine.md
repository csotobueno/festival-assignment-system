# ADR 0001: Select the MVP database engine

## Status

Accepted

## Date

2026-07-18

## Context

Stage 3 replaces the in-memory adapters with relational durable persistence and introduces database-backed protection for global invariants and concurrent requests. The persistence design defines `AssignmentRequest` and its resulting `Assignments` as one atomic business outcome, persisted through one shared EF Core context and one `SaveChangesAsync` call.

The MVP must run locally on macOS using Docker and remain suitable for a possible real deployment by a festival organization. The database choice must therefore consider correctness, .NET integration, local reproducibility, production operations, portability, licensing and the developer's ability to operate the system.

The developer has substantially more SQL Server experience and limited PostgreSQL experience. This favors SQL Server for immediate delivery confidence. Conversely, the system has no known SQL Server-specific requirement, and PostgreSQL is initially preferred for its permissive license, broad deployment choices, portability and learning value. The initial preference is a hypothesis, not the decision: either engine is acceptable only if it can protect the Stage 3 persistence boundary and critical invariants.

This ADR selects the engine only. Provider installation, containers, EF Core mappings, migrations, repositories, transaction implementation, cloud provider selection, high availability and concurrency policy remain outside its scope.

## Decision drivers

- Protect the Stage 3 atomic persistence boundary and global invariants with relational transactions and database constraints.
- Support concurrent request handling without relying on generic or unverified performance claims.
- Integrate reliably with .NET and EF Core.
- Provide reproducible macOS development through Docker.
- Offer practical production deployment, monitoring, backup and restore options.
- Keep database license obligations and likely organizational costs understandable.
- Preserve deployment portability and avoid an unnecessary platform dependency.
- Account honestly for existing SQL Server experience and the learning risk of PostgreSQL.
- Prefer standard relational features unless a database-specific feature has demonstrated project value.

## Options considered

### PostgreSQL

PostgreSQL is an open-source relational database released under the permissive PostgreSQL License. For EF Core, the project would use the independently maintained `Npgsql.EntityFrameworkCore.PostgreSQL` provider. PostgreSQL supplies transactions, composite unique constraints, standard isolation levels, MVCC-based concurrency control and explicit locking when required.

Its main advantages here are licensing simplicity, deployment portability, availability from many hosting models and a straightforward containerized local environment. Its principal disadvantage is the developer's limited PostgreSQL operational and diagnostic experience, including provider-specific exception handling, query analysis, backup/restore and production maintenance.

### SQL Server

SQL Server is Microsoft's relational database and uses the Microsoft-maintained `Microsoft.EntityFrameworkCore.SqlServer` provider. It supplies transactions, composite unique indexes or constraints, multiple isolation levels, lock-based concurrency control and optional row-versioning modes.

Its main advantages here are the developer's substantially greater experience, mature Microsoft .NET integration and familiar administration and diagnostics. Its disadvantages are edition and production licensing considerations, a comparatively stronger platform/vendor dependency, and local container limitations that matter on macOS: Microsoft supports SQL Server Linux container images on Intel and AMD x86-64 hosts, while emulation or translation environments are not tested or supported. This is a material concern for Apple Silicon development even though clients on macOS can connect to a supported remote or Docker-hosted instance.

## Comparison matrix

Scoring: 1 = unfavorable, 2 = acceptable with disadvantages, 3 = adequate, 4 = favorable, 5 = very favorable.

| Criterion | Weight | PostgreSQL | Weighted | SQL Server | Weighted | Rationale |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Transactions and invariant protection | 20% | 5 | 1.00 | 5 | 1.00 | Both provide atomic transactions and composite uniqueness enforcement. |
| Concurrency | 15% | 4 | 0.60 | 4 | 0.60 | Both provide mature isolation and conflict mechanisms; project-specific correctness still depends on application and transaction design. |
| EF Core and .NET integration | 15% | 4 | 0.60 | 5 | 0.75 | Npgsql is a capable EF Core provider; SQL Server has the Microsoft-maintained provider and the most familiar .NET path for this developer. |
| Operation and maintenance | 15% | 3 | 0.45 | 4 | 0.60 | Both are production-capable; limited PostgreSQL experience increases initial operational risk. |
| Cost and licensing | 15% | 5 | 0.75 | 3 | 0.45 | PostgreSQL has a permissive no-fee database license; SQL Server production edition and deployment choices require license evaluation. |
| Deployment and portability | 10% | 5 | 0.50 | 3 | 0.30 | PostgreSQL is broadly portable; supported SQL Server containers have an x86-64 host limitation relevant to Apple Silicon. |
| Existing experience and learning value | 10% | 3 | 0.30 | 5 | 0.50 | SQL Server experience reduces delivery risk; PostgreSQL adds useful learning but requires deliberate validation. |
| **Total** | **100%** |  | **4.20 / 5** |  | **4.20 / 5** | The equal result makes the trade-off explicit; it does not make the decision. |

The matrix deliberately does not convert uncertain performance or concurrency assumptions into a differentiator. SQL Server scores higher for provider ownership, existing experience and operational familiarity. PostgreSQL scores higher for licensing and portability. The decision therefore rests on the absence of a SQL Server-specific need and on whether PostgreSQL's experience risk can be contained through implementation evidence.

## Transaction and concurrency analysis

Both engines can support the required model:

| Requirement | PostgreSQL | SQL Server |
| --- | --- | --- |
| INV-01 — Unique Spot per FestivalDay | Supported by a composite unique constraint or unique index on `Assignments(FestivalDayId, SpotCode)`. A conflicting insert fails. | Supported by a composite unique constraint or unique index on `Assignments(FestivalDayId, SpotCode)`. A conflicting insert fails. |
| INV-02 — Unique Attendee per FestivalDay | Supported by a composite unique constraint or unique index on `Assignments(FestivalDayId, AttendeeId)`. A conflicting insert fails. | Supported by a composite unique constraint or unique index on `Assignments(FestivalDayId, AttendeeId)`. A conflicting insert fails. |
| INV-03 — Complete AssignmentGroup | Supported by committing the completed `AssignmentRequest` and every `Assignment` in one transaction; any failed insert rolls back the whole unit. | Supported by the same one-transaction design; any failed insert rolls back the whole unit. |
| `ProcessAssignmentRequestUseCase` atomic boundary | Supported with one shared Npgsql-backed `DbContext` and one transactional `SaveChangesAsync`, subject to integration validation. | Supported with one shared SQL Server-backed `DbContext` and one transactional `SaveChangesAsync`, subject to integration validation. |

Neither database constraint alone proves INV-03. Group completeness also depends on Domain validation, Application orchestration, registering the request and all assignments in the same context, and committing them once. Similarly, the availability query is advisory: two requests may select the same block, and the uniqueness constraints are the final defense against duplicate committed assignments.

PostgreSQL uses MVCC and its default `Read Committed` level gives each command a snapshot of committed data. It also offers Repeatable Read and Serializable; stronger levels can produce serialization failures for which the application must retry the complete transaction when retry is appropriate. SQL Server defaults to lock-based `Read Committed` in SQL Server and can use row versioning when `READ_COMMITTED_SNAPSHOT` is enabled; it also offers Snapshot, Repeatable Read and Serializable. SQL Server's stronger lock-based levels can increase blocking, while row-versioned modes have their own configuration and resource costs.

These factual differences do not establish that either engine is generally faster or better at concurrency. For this system, correctness and observed behavior depend on:

- the exact transaction boundary and duration;
- the selected isolation level and database configuration;
- the two non-null composite unique constraints;
- how constraint, serialization and deadlock conflicts are classified;
- whether and how the application rejects or retries a losing request;
- keeping all decision logic inside the transaction when a complete retry is required;
- project-specific concurrent integration tests.

The initial isolation level and any explicit locking strategy remain implementation decisions. They must be selected from real query behavior and concurrent tests rather than from a generic database preference.

## EF Core and .NET integration analysis

Both engines are listed EF Core relational providers. SQL Server uses the Microsoft-maintained `Microsoft.EntityFrameworkCore.SqlServer` provider. PostgreSQL uses `Npgsql.EntityFrameworkCore.PostgreSQL`, maintained by the Npgsql project; it follows general EF Core patterns while exposing PostgreSQL-specific capabilities where needed.

EF Core can define composite unique indexes through the model. With a provider that supports transactions, EF Core applies all changes in one `SaveChanges` call transactionally and rolls them back if a change fails. That behavior matches the current Stage 3 assumption, but must be proven against the selected provider and real database rather than mocked or inferred from in-memory tests.

SQL Server has an advantage in Microsoft ownership of the provider and the developer's prior experience with its conventions, tooling and exception behavior. PostgreSQL's provider is an additional dependency whose EF Core/.NET version compatibility and release support must be checked before each upgrade. No required mapping in the Stage 3 model is known to need a SQL Server-only feature or to exceed Npgsql's normal relational support.

Provider-neutral Domain and Application code must be preserved. Provider-specific mappings, SQLSTATE handling, naming, data types and migrations belong in Infrastructure.

## Local development analysis

Both engines can be accessed by .NET applications running on macOS and can be containerized. PostgreSQL is the more portable local choice for this project because it has established container images across common development architectures and does not add a commercial EULA to the database engine.

SQL Server Linux containers are a workable option on supported x86-64 hosts, and macOS tools can connect to them. However, Microsoft's current documentation states that the images are supported only on Intel and AMD x86-64 hosts and that emulation or translation environments are not tested or supported. This makes the exact experience hardware-dependent on macOS and weakens reproducibility for Apple Silicon.

The PostgreSQL advantage is conditional on implementation: Stage 3 must provide a pinned, reproducible Docker configuration, persistent local storage where appropriate, health checks or readiness handling, schema migration instructions and documented reset/troubleshooting steps. Those artifacts are not created by this ADR.

## Production deployment and operational analysis

Both engines can be self-hosted or consumed through managed services, and both have mature backup, restore, monitoring and high-availability ecosystems. A final hosting provider and high-availability design are deliberately not selected here.

PostgreSQL offers broad portability among self-hosted, containerized and managed offerings without coupling the logical database selection to one cloud. SQL Server also has strong production options, especially in Microsoft-oriented environments, and may be operationally preferable if the festival organization already has SQL Server licenses, administrators, monitoring and backup standards.

PostgreSQL's portability does not eliminate operational work. Before production, the organization must assign ownership for upgrades, security patching, credentials, monitoring, capacity, backups, restore drills and incident response. Managed hosting can reduce some work but introduces provider-specific service and cost considerations. The same distinction applies to SQL Server.

Given current repository evidence, neither engine has a proven project-specific performance or maintenance advantage. Operational suitability must be validated with the intended deployment model and representative workloads.

## Cost and licensing analysis

PostgreSQL is released under the permissive PostgreSQL License and does not impose a database-engine license fee. This does not make a PostgreSQL deployment free: compute, storage, backups, networking, monitoring, support and staff time still cost money.

SQL Server has free editions for development or constrained use, but Developer editions are not licensed for production. Production use may involve Express limitations, paid Standard or Enterprise licensing, or a managed service whose price incorporates database licensing. The appropriate edition and use rights must be confirmed for the actual production topology; this ADR does not provide legal advice or assume a price.

Exact cloud pricing is not compared because no provider, region, capacity, availability target, support level or deployment model has been selected. Hosting and operational costs are separate from the database engine's license cost and must be estimated when those inputs are known.

## Decision

Use **PostgreSQL** as the relational database engine for the MVP, with the Npgsql EF Core provider.

The hypothesis is confirmed because no critical PostgreSQL limitation was identified for the defined persistence model, atomic boundary or invariants. Both engines meet the correctness requirements, and the scoring is equal. PostgreSQL is selected because, in the absence of a SQL Server-specific requirement, its permissive licensing and greater deployment portability better preserve options for a possible festival organization and for macOS-based development.

This is not a conclusion that PostgreSQL is faster or generally better at concurrency. SQL Server remains the lower-learning-risk option and has stronger current developer familiarity and Microsoft-maintained EF Core integration. PostgreSQL is justified only with the validation and operational mitigations below.

## Positive consequences

- The MVP uses a permissively licensed relational engine with no engine license fee.
- The system retains broad self-hosted and managed deployment options.
- Local development is less dependent on host architecture than supported SQL Server Linux containers.
- PostgreSQL supports the required transactions and composite uniqueness constraints.
- The developer gains relevant PostgreSQL and Npgsql experience.
- The design has no need to introduce a vendor-specific feature at selection time.

## Negative consequences

- The developer has less PostgreSQL experience, increasing initial delivery, diagnosis and operational effort.
- The application depends on the Npgsql EF Core provider rather than Microsoft's SQL Server provider.
- Provider-specific exceptions, SQLSTATE values, migrations, types and tooling differ from previous SQL Server projects.
- PostgreSQL backup, restore, monitoring, upgrades and query analysis require additional learning and documentation.
- Switching to SQL Server later would require provider and migration changes and could expose database-specific assumptions.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Limited PostgreSQL/Npgsql experience causes incorrect configuration or slow diagnosis. | Pin compatible versions, document setup and common diagnostics, keep the first persistence implementation small, and review database-specific choices. |
| In-memory or SQLite tests give false confidence about PostgreSQL behavior. | Run database integration and concurrency tests against a real PostgreSQL instance. |
| Concurrent requests produce uniqueness, serialization or deadlock failures that leak as technical errors or cause unsafe retries. | Classify PostgreSQL SQLSTATEs, define controlled Application outcomes, retry only complete idempotent transactions when justified, and test collisions deterministically. |
| A long transaction increases contention or invalidates assumptions. | Measure transaction duration, keep non-shared validation outside the transaction where safe, and include all mutable availability decisions inside it. |
| Local environments drift. | Provide a pinned, reproducible Docker configuration and documented migration/reset workflow. |
| Production data cannot be restored when needed. | Document backup and restore procedures and perform a restore drill before production use. |
| PostgreSQL-specific features create avoidable lock-in. | Use standard relational features unless a PostgreSQL-specific capability provides demonstrated value and is documented. |
| Npgsql and EF Core versions become incompatible. | Use compatible major versions, review provider release notes during upgrades and verify migrations and integration tests. |
| Hosting cost is assumed rather than measured. | Estimate the selected provider and topology separately, including compute, storage, backups, traffic, support and operations. |

## Validation required during implementation

Stage 3 is not complete until implementation provides evidence for all of the following:

1. Select and pin mutually compatible .NET, EF Core, Npgsql provider and PostgreSQL versions.
2. Provide reproducible Docker-based local PostgreSQL setup on the developer's macOS hardware.
3. Map the Stage 3 relational model and review generated PostgreSQL column types, lengths, nullability, foreign keys and delete behavior.
4. Create and inspect migrations for the two required unique constraints:
   - `Assignments(FestivalDayId, SpotCode)` for INV-01;
   - `Assignments(FestivalDayId, AttendeeId)` for INV-02.
5. Confirm through real-database integration tests that each duplicate insert fails and leaves no invalid committed state.
6. Confirm that one `SaveChangesAsync` atomically persists the `AssignmentRequest`, confirmed attendee codes and every `Assignment`, and that an injected or constraint failure rolls back the complete outcome for INV-03 and INV-08.
7. Confirm that all repositories and the Unit of Work share the same scoped `DbContext`.
8. Exercise concurrent requests that contend for the same Spot and the same Attendee, verifying one valid winner and a controlled complete outcome for the loser.
9. Select and document the initial isolation level from test evidence; verify whether explicit transaction control or locking is needed around the read-decide-write flow.
10. Define handling for unique violations, serialization failures, deadlocks, cancellation and connection failures, including which cases are rejected, failed or retried.
11. If retries are introduced, verify that the entire decision transaction is rerun safely and that application behavior is idempotent enough for the chosen policy.
12. Measure transaction duration and inspect contention under representative concurrent scenarios; do not infer performance from database brand.
13. Verify status consistency for Completed, Rejected and Failed outcomes, including the accepted limitation that a database outage may prevent persisting Failed.
14. Document migrations, local troubleshooting and database diagnostics.
15. Before production use, select the deployment model, estimate total cost, establish monitoring and security ownership, document backup/restore, and complete a restore drill.

## Conditions that would justify reconsidering the decision

Reconsider PostgreSQL if evidence shows any of the following:

- Npgsql cannot correctly map or transactionally persist the defined model on the project's supported .NET/EF Core version.
- Required concurrency behavior cannot be implemented reliably after reasonable transaction and conflict-handling changes.
- The festival organization mandates SQL Server because of existing licenses, operational staff, support contracts, security policy or platform standards.
- The selected production environment offers materially better supported reliability or total cost with SQL Server, based on a documented topology and estimate.
- Apple Silicon or other required development environments cannot run the chosen PostgreSQL setup reproducibly.
- A future requirement depends on a SQL Server-specific capability and the value of that capability exceeds migration cost.
- PostgreSQL/Npgsql operational risk remains unacceptable after the listed mitigations and implementation validations.

A reconsideration must use project-specific evidence. A generic benchmark, preference or claim that one database is “better at concurrency” is insufficient.

## References

### Repository

- [README](../../README.md)
- [Project operating model](../project-operating-model.md)
- [Domain glossary](../glossary.md)
- [Critical invariants](../critical-invariants.md)
- [Domain blueprint v1](../domain-blueprint-v1.md)
- [Stage 2 technical validation](../stage-2-technical-validation.md)
- [Stage 3 persistence model and transactional boundary](../stage-3-persistence-model-and-transaction-boundary.md)

### Official external documentation

- [PostgreSQL License](https://www.postgresql.org/about/licence/)
- [PostgreSQL constraints](https://www.postgresql.org/docs/current/ddl-constraints.html)
- [PostgreSQL concurrency control](https://www.postgresql.org/docs/current/mvcc.html)
- [PostgreSQL transaction isolation](https://www.postgresql.org/docs/current/transaction-iso.html)
- [PostgreSQL serialization failure handling](https://www.postgresql.org/docs/current/mvcc-serialization-failure-handling.html)
- [Npgsql EF Core provider](https://www.npgsql.org/efcore/)
- [Npgsql basic usage and transactions](https://www.npgsql.org/doc/basic-usage.html)
- [EF Core database providers](https://learn.microsoft.com/en-us/ef/core/providers/)
- [EF Core SQL Server provider](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)
- [EF Core indexes](https://learn.microsoft.com/en-us/ef/core/modeling/indexes)
- [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- [SQL Server transaction locking and row versioning guide](https://learn.microsoft.com/en-us/sql/relational-databases/sql-server-transaction-locking-and-row-versioning-guide)
- [SQL Server Linux containers with Docker](https://learn.microsoft.com/en-us/sql/linux/install-upgrade/quickstart-install-docker)
- [SQL Server licensing guidance](https://www.microsoft.com/licensing/guidance/SQL)
