# CLAUDE.md

## Project

Festival Assignment System technical MVP.

This project validates whether it is technically viable to assign physically contiguous festival spots to temporary attendee groups in a fair, consistent, and auditable way.

## Required context

Before making changes, read:

* `README.md`
* `AGENTS.md`
* `docs/project-operating-model.md`
* `docs/glossary.md`
* `docs/critical-invariants.md`
* `docs/domain-blueprint-v1.md`

Use the ubiquitous language defined in `docs/glossary.md`.

## Architecture rules

* `Festival.Domain` must not depend on any other project.
* `Festival.Application` may depend only on `Festival.Domain`.
* `Festival.Infrastructure` may depend on Application and Domain.
* `Festival.Api` is the composition root.
* Infrastructure concerns must not leak into Domain.
* Do not introduce circular references.

## Lean working principles

* Implement only the requested Issue.
* Keep changes small and focused.
* Do not add speculative functionality.
* Do not add abstractions without a demonstrated need.
* Do not add NuGet packages unless explicitly requested.
* Do not modify unrelated files.
* Do not create branches.
* Do not commit.
* Do not push.
* The developer owns Git operations.

## Domain conventions

* Use business terminology from the glossary.
* Preserve the invariants from `docs/critical-invariants.md`.
* Prefer immutable Value Objects.
* Use private constructors and static factory methods when creation requires validation.
* Domain objects must not depend on EF Core, ASP.NET Core, logging, serialization, or database concerns.
* Do not use `DateTime.Now`, `DateTime.UtcNow`, or `DateTimeOffset.Now` inside Domain.
* Pass time explicitly when domain behavior depends on time.
* Do not expose public setters unless required by a validated use case.
* Do not create generic `Entity`, `AggregateRoot`, or `ValueObject` base classes without explicit approval.

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

When a requested behavior depends on an unresolved business or architectural decision:

1. do not invent the decision;
2. identify the ambiguity;
3. stop that part of the implementation;
4. report the decision that is required.

## Completion report

At the end of a task, report:

1. summary of implementation;
2. files added or modified;
3. tests added;
4. build result;
5. test result;
6. assumptions made;
7. remaining or deferred work;
8. any deviation from the requested scope.
