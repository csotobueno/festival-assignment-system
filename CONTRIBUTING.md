# Contributing

## Development workflow

```text
Issue or task
→ make start
→ implementation
→ local validation
→ Pull Request
→ review
→ squash and merge
```

Do not develop directly on `main`. Every Pull Request starts from the latest
`origin/main`, and every change enters `main` through a Pull Request.

## Starting work

Start each contribution with a new branch:

```bash
make start branch=feature/add-fairness-policy
```

This command requires a clean working tree, validates the branch convention,
fetches the latest `origin/main`, rejects local or remote branch collisions, and
creates the new branch directly from that remote base. The contributor remains
responsible for deciding whether existing local work should be committed,
stashed, or discarded.

## Branch convention

Descriptions use lowercase kebab-case. Choose the prefix that reflects the
work:

| Prefix | Use |
| --- | --- |
| `feature/` | New capabilities |
| `fix/` | Corrections |
| `hotfix/` | Urgent production corrections |
| `chore/` | Tooling and repository maintenance |
| `docs/` | Documentation-only changes |
| `test/` | Test-only changes |
| `refactor/` | Behavior-preserving code improvements |

Branch prefixes and Conventional Commit types are independent:

```text
branch: feature/add-fairness-policy
commit: feat(application): add fairness policy
```

## Commit convention

Use Conventional Commits. Examples:

```text
feat(application): add fairness policy
fix(domain): correct rotation score
test(infrastructure): validate assignment concurrency
docs(stage-4): document fairness definition
refactor(domain): simplify assignment scoring
chore(workflow): standardize contributor commands
```

## Local validation

The contributor interface provides:

```bash
make build
make test-fast
make test-integration
make test
make verify
make sync-main
```

`make verify` is the normal pre-PR validation: it builds once, then runs the
fast tests and the PostgreSQL integration tests without rebuilding. Docker
Desktop must be running for integration tests, the complete suite, and
`make verify`. Use scope-appropriate validation; documentation-only changes do
not normally require PostgreSQL integration tests.

## Pull Requests

Keep each Pull Request focused and link its GitHub Issue when applicable. Run
the relevant local validation before requesting review, explain important
architectural trade-offs in the description, and avoid unrelated cleanup.
Address review comments in the same Pull Request rather than opening another
one solely for review fixes.

## Merge strategy

Normal contributions follow:

```text
Pull Request → review → squash and merge → one consolidated commit on main
```

Branch commits may be iterative while `main` retains a focused history. Do not
use merge commits for normal feature work.

## Main branch protection

The intended GitHub configuration requires Pull Requests before merging,
blocks force pushes and deletion of `main`, and enables squash and merge.
Required status checks can be enabled after CI is introduced; they are not
assumed to exist today. With one active contributor, multiple approvals are not
required. Repository settings remain a manual post-merge configuration step.

## Definition of Done

A contribution should normally satisfy all scope-relevant items:

- The requested behavior or documentation is complete.
- Relevant tests were added or updated.
- The build passes.
- Relevant automated tests pass.
- Architectural documentation is updated where required.
- No unrelated changes are included.
- The Pull Request explains its intent and important trade-offs.
