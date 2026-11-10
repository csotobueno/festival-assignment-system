# Arrival concurrency — observer-audit stop and resumption record

Recorded before any replacement execution. Original ledger: `artifacts/simulations/arrival-concurrency-20261110-2216981442fb4823afbea2d2de6b10d2/index.json`.
SHA-256: `be6872a78a471ebb442624a100272044895a1aa119d17144497ea54aa32c721f`.

The original three-execution set stopped after BurstLike/1001
`official-arrival-burstlike-1001-7669f0983bf74b06adb043b70d495743`. Its eight files and original ledger remain immutable.
The original audit count **3** is retained: one orphan-request correlation finding
and two "Conflict/failure loser partial graph" findings. No count is overwritten.

Investigation found two attempts (sequences 2179 and 5146), both singleton groups,
with `RECONCILIATION_FAILED`, null confirmed request identity and a real successful
use-case result (`FeasibleContiguousBlockFound = true`). Their final exported
attendee paths each contain one durable Assignment on the correct day. The
observer's read failed after the use case returned. The legacy audit incorrectly
assumed every UnexpectedTechnicalFailure meant no persisted graph, conflating
observer failure with persisted corruption. The other 1,024 unexpected outcomes
remain recorded measured-path failures; they are not reclassified as conflicts.

The original disposable database is already gone. Its per-request statuses for
these two unmatched graphs were not exported separately, so a complete independent
retrospective request-state audit cannot be claimed. Preserve the three original
findings as a limitation; do not call that package a clean correctness result or
include it in the valid comparison. No confirmed duplicate Spot/day, duplicate
Attendee/day or partial singleton allocation was identified in retained evidence.

The correction performs read-only correlation against each future run's final
persisted snapshot, requiring an actual Completed request, exact members/day/time,
one request graph, complete single-Zone contiguous placement and Assignment
identity. It does not change attempt outcomes, business execution, arrivals,
conflict classification, retry count, transactions or persistence. Focused tests
include valid committed observer failure, partial graph, inconsistent status and
known-conflict cases; the full solution and harness must pass before resumption.

After validation, preserve Moderate/1001 and /2001, whose audits passed. Replace
only the stopped BurstLike/1001 evidence collection and execute the still-unrun
BurstLike/2001. Record both in a new ledger chained to the stopped ledger, with
source hashes proving every prior src file is unchanged and only the read-only
resolver is added. The old harness hashes and excluded package remain linked.

This gives four valid initial-matrix runs and **five actual official execution
attempts**, including the excluded stopped run. It is a documented harness-audit
replacement, not an adaptive Seed 3001 run. No additional execution beyond the
six-attempt cap is authorized. Decide any remaining adaptive expansion only after
reviewing the completed valid four-run matrix. A real unresolved structural
invariant finding still stops execution and cannot be averaged away.

## Validation and resumption outcome

Before resumption, solution/harness builds passed with zero warnings/errors;
the full solution passed 990 .NET tests with zero failures/skips, and 89 Python
tests passed. The correction also rejects partial graphs, inconsistent request
status and known losing conflicts in automated PostgreSQL-backed coverage.

The continuation ledger is
`artifacts/simulations/arrival-concurrency-20261110-2ba8335287484fd69d6cf26eb98f456a/index.json`.
Its embedded remediationReason retains the exact pre-execution version of this
record. Both corrected Burst-like collections completed and passed their full
final-state audits; their unexpected outcomes remain recorded, including four
and three independently correlated post-commit observer failures respectively.
No new observation query or business-request retry was introduced.

There are four valid initial-matrix runs and five actual executions including
the original excluded collection. The [analysis](production-like-arrival-concurrency-analysis.md)
retains all identifiers, checksums, original findings, interpretation limits and
the pattern-specific decision not to execute Seed 3001. The original package is
not retrospectively certified clean; its lost request-status audit coverage is
an explicit evidence limitation, not averaged away by the replacement.
