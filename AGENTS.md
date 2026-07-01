# AGENTS.md

## Project

Festival Assignment System technical MVP.

The project validates the technical feasibility of assigning physically contiguous
festival spots fairly and consistently to temporary attendee groups.

## Required context

Before making changes, read the relevant project documentation:

* `README.md`
* `docs/project-operating-model.md`
* `docs/glossary.md`
* `docs/critical-invariants.md`
* `docs/domain-blueprint-v1.md`

Use the ubiquitous language defined in the glossary.

## Architecture

The backend follows these dependency rules:

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

Rules:

* `Festival.Domain` must not depend on another project.
* `Festival.Application` may depend on `Festival.Domain`.
* `Festival.Infrastructure` may depend on Application and Domain.
* `Festival.Api` is the composition root.
* Infrastructure concerns must not leak into Domain.
* Do not introduce circular project references.

## Working principles

* Follow Lean principles.
* Implement only the requested Issue.
* Keep every change small and focused.
* Do not add speculative functionality.
* Do not add abstractions until a demonstrated need exists.
* Do not add NuGet packages unless the task explicitly requires them.
* Preserve existing public behavior unless the task requests a change.
* Do not modify unrelated files.
* Do not create or switch branches.
* Do not commit or push changes.
* Leave Git operations to the developer.

## Domain conventions

* Use domain terminology from `docs/glossary.md`.
* Preserve the invariants documented in `docs/critical-invariants.md`.
* Prefer immutable Value Objects.
* Use private constructors and explicit factory methods when creation requires validation.
* Domain objects must not depend on EF Core, ASP.NET Core, logging, serialization, or database concerns.
* Do not use `DateTime.Now`, `DateTime.UtcNow`, or `DateTimeOffset.Now` inside Domain.
* Pass time explicitly when domain behavior depends on time.
* Do not expose public setters unless required by a validated use case.
* Do not create generic base classes such as `Entity`, `AggregateRoot`, or `ValueObject` without explicit approval.

## Code organization

Organize Domain code by business area:

```text
Festival.Domain/
├── Assignments/
├── Attendees/
├── FestivalDays/
├── Spots/
└── Zones/
```

Do not reorganize the project into generic folders such as:

```text
Entities/
ValueObjects/
Services/
```

unless explicitly requested.

## Testing

Domain behavior must be covered by unit tests in:

```text
tests/Festival.Domain.Tests
```

Tests should:

* describe observable behavior;
* cover valid and invalid states;
* avoid testing private implementation details;
* remain deterministic;
* avoid using the current system clock.

Before completing any task, run:

```bash
dotnet build Festival.sln
dotnet test Festival.sln
```

Report the result of both commands.

## Scope control

When a requested behavior depends on an unresolved business or architectural
decision:

1. do not invent the decision;
2. identify the ambiguity;
3. stop that part of the implementation;
4. report the decision that is required.

Do not silently broaden the scope.

## Completion report

At the end of a task, report:

1. summary of the implementation;
2. files added or modified;
3. tests added;
4. build result;
5. test result;
6. assumptions made;
7. remaining or deferred work;
8. any deviation from the requested scope.
