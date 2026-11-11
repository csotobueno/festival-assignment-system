# First official S1/S2 representative set

Run from the repository root with Docker available:

```bash
dotnet build Festival.sln
DOCKER_API_VERSION=1.43 dotnet test Festival.sln
git diff --check
dotnet build scripts/stage-5-representative-runs/RepresentativeRuns.csproj
```

Read `git rev-parse HEAD` and supply that SHA explicitly:

```bash
DOCKER_API_VERSION=1.43 FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA=<sha> \
  dotnet run --no-build --project scripts/stage-5-representative-runs/RepresentativeRuns.csproj
```

This task-specific harness runs S1 seeds 1001, 2001, 3001, then S2 with the same
seeds, then a separately labelled reproduction of S1/1001. There are six official
runs; the seventh is verification and is excluded from cross-seed aggregates.
Each run calls the existing factories, materializer, PostgreSQL Testcontainers
runner and eight-file exporter unchanged. Each run owns a clean disposable
database; all five FestivalDays retain earlier days' real Assignments. No
requests or scenarios execute concurrently.

The supplied simulated FirstDate is 2026-08-15, matching the existing test
fixture; it is not a confirmed festival date. The shared request eligibility
share remains 50%. The harness does not tune parameters, retry poor results or
alter production/rubric/recovery/metric behavior. Business rejections are valid
observations, not a failed execution. Invariant findings stop the protocol after
preserving the affected package and audit; technical failures remain recorded.

The final `COMPLETE` line identifies the set's `index.json`. It records the exact
six official paths, the reproduction path, final persisted-state audits and
SHA-256 of all eight files per package. Audit checks include Spot/day and
Attendee/day uniqueness; complete, single-Zone, contiguous request results;
catalog identity and persisted outcome consistency. This is a final-state
audit, not a complete temporal audit of INV-06. The unchanged Application path
does not perform replacement/cancellation operations. The runner already
reconciles each returned outcome against persistence in a fresh read scope.

Analyze the completed set from the repository root:

```bash
python3 scripts/stage-5-representative-runs/summarize.py artifacts/simulations/<set>/index.json
```

The script parses and hashes every exported file, verifies the fixed execution
matrix, five-day populations and generator/mode, and writes `summary.json`
alongside the index. Request rates follow framework sections 8–13: success and
rejection use NormallyProcessedRequests; technical failures/conflicts use
AttemptedRequests. Fairness/recovery aggregates are copied from `metrics.json`.
Cross-seed min/mean/max use unweighted run values; N/A runs are counted and
excluded, never converted to zero. Timing uses existing durations and the
documented nearest-rank p50/p95 over AttemptedRequests. No warm-up, load test or
performance optimization is added.

Three focused comparison regression tests can be run with:

```bash
python3 -m unittest discover -s scripts/stage-5-representative-runs -p 'test_*.py'
```

Reproduction compares the exact manifest, attempt outcomes/diagnostics/capacity,
paths/classifications, recovery and metrics. Observed attempt times/durations
are excluded; generated persisted request and Assignment IDs are matched by
stable simulation-request and attendee/FestivalDay identities. Other logical
fields must match. A mismatch leaves evidence intact and exits unsuccessfully.

Keep all official packages intact under ignored `artifacts/simulations/`.
There is no mandatory evidence-promotion mechanism in the repository. The
selected summary/results document records exact paths and checksums; raw
packages remain local, are not committed, and require separate backup before
deleting a checkout. No retention subsystem is introduced.

## S2 path analysis

Analyze the existing three official S2 packages without running a simulation:

```bash
python3 -B scripts/stage-5-representative-runs/analyze_s2_paths.py \
  --output artifacts/analysis/s2-fairness-path-analysis-final.json
```

Choose a fresh output filename when repeating the command. The standard-library
helper reads the official structured results and verifies all eight package
checksums, target populations and attendee/day joins. It consumes stored rubric
and recovery outputs. Its deterministic derived JSON includes aggregate tables,
every NotBalanced path and every eligible unresolved episode with traceability
to recorded daily requests. It refuses existing outputs and destinations within
the raw S2 evidence directory. Raw packages are never modified.

The [S2 analysis report](../../docs/stage-5/s2-fairness-path-analysis.md) defines
denominators, observations, interpretation, scenario guidance and evidence gaps.
The unittest discovery command above also runs ten focused S2 analysis tests.

## Official paired S5 order-sensitivity protocol

Keep the retained official S2 packages in their recorded locations. With the
current repository SHA explicitly supplied, run:

```bash
dotnet build scripts/stage-5-representative-runs/RepresentativeRuns.csproj
DOCKER_API_VERSION=1.43 FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA=<sha> \
  dotnet run --no-build --project scripts/stage-5-representative-runs/RepresentativeRuns.csproj -- --s5
```

This option executes only S5/1001, S5/2001, S5/3001, then a separately labelled
S5/1001 reproduction. It never executes an S2 control. Before any run, it parses
and checks the retained S2 file hashes, then proves that each in-memory S2
materialization and source definition equal the retained input. Comparison uses
the existing exporter's UTC representation; only absent baseline-default
sequence/provenance additions are omitted. A mismatch stops execution. The
unchanged S5 transformation consumes those verified in-memory manifests.

Every package retains both sequences and all eight normal files. The set index
records the exact paired control, persisted-state audit and eight SHA-256 hashes
per S5 package. Attempt dispatch is audited against ExecutionSequence, while
BaselineSequence must equal the preserved source Sequence.

Analyze the completed index without executing another simulation:

```bash
python3 -B scripts/stage-5-representative-runs/analyze_s5_order.py \
  artifacts/simulations/<s5-set>/index.json \
  --output artifacts/analysis/s2-s5-order-sensitivity-repeat.json
python3 -B -m unittest discover -s scripts/stage-5-representative-runs -p 'test_*.py'
```

The analysis proves complete workload equality and exact daily reversal before
joining outcomes through stable AttendeeId/FestivalDay and SimulationRequestId.
It compares stored quality, classifications and recovery without replaying the
rubric or episode detection. Missing Assignments retain separate counts and
request statuses. InsufficientEvidence and NoPath remain unranked. Identity
overlap uses intersection/union; churn uses symmetric difference/union, with
both directional counts and denominators retained. Thirds and arrival bands
reuse the S2 analysis helpers. RequestedAt/AssignedAt retain original simulated
meaning and never stand for S5 dispatch chronology.

All raw JSON/checksums are verified before and after analysis. Reproduction
reuses the existing logical normalization of generated IDs and runtime timings;
definition, manifest and metrics also require equal file hashes. Output must be
fresh and outside raw simulations. The 15 S5 regression tests cover workload
changes, missing outcomes, transition direction, unranked paths and corrupt or
incomplete evidence. The selected concise derivative and human interpretation
are in [the S2/S5 results](../../docs/stage-5/s2-s5-order-sensitivity-results.md).

## Official S6 large-group sensitivity protocol

With the retained S2 controls intact, build the harness and supply the current
source revision explicitly:

```bash
dotnet build scripts/stage-5-representative-runs/RepresentativeRuns.csproj
DOCKER_API_VERSION=1.43 FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA=<sha> \
  dotnet run --no-build --project scripts/stage-5-representative-runs/RepresentativeRuns.csproj -- --s6
```

This option executes only S6/1001, S6/2001, S6/3001 and a distinct S6/1001
reproduction. It reuses the S5 protocol's retained-S2 checksum and input
verification, normal runner/exporter and final persisted-state audit. It does
not execute S2 or apply the S5 transformation. Each S6 definition retains its
own identity and changes only the configured group-size profile relative to S2.
The index links each S6 run to its retained same-Seed control; comparison is at
scenario level, without requiring equal requests or daily cohorts.

Analyze a completed S6 set without executing another simulation:

```bash
python3 -B scripts/stage-5-representative-runs/analyze_s6_groups.py \
  artifacts/simulations/<s6-set>/index.json \
  --output artifacts/analysis/s2-s6-repeat.json \
  --markdown artifacts/analysis/s2-s6-repeat.md
python3 -B -m unittest discover -s scripts/stage-5-representative-runs -p 'test_*.py'
```

Use fresh output names outside raw simulations. Analysis reuses the existing
checksum reader, normal request metrics, stable joins and reproduction
normalization. Fairness classifications and recovery episodes/aggregates are
consumed as stored. Size/third tables count the same request and Assignment
populations with explicit denominators. Raw checksums are checked before/after;
the reproduction includes checkpoints and requires byte-identical definition,
manifest, checkpoints and metrics. Runtime times/durations and generated durable
IDs are excluded only from logical comparison, using stable identity joins.

The deterministic derived reports are
[S2/S6 results](../../docs/stage-5/s2-s6-large-group-sensitivity-results.md) and
its adjacent JSON. They record exact raw paths, per-Seed comparisons, exposure
and episode-opening attribution, diagnostic limits and exploratory timing.
The new tests cover controlled inputs, non-paired workloads, population/order
failures, attendee-weighted quality, N/A segments, changing group exposure,
opening-group attribution, rejection diagnosis, rendering and reproduction
boundaries. The original S1/S2 and S5 execution options remain unchanged.

## Official S7 paired fragmentation protocol

Build the existing harness and supply the protected source revision explicitly:

```bash
dotnet build scripts/stage-5-representative-runs/RepresentativeRuns.csproj
DOCKER_API_VERSION=1.43 FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA=<sha> \
  dotnet run --no-build --project scripts/stage-5-representative-runs/RepresentativeRuns.csproj -- --s7
```

This option runs Control then Fragmented for Seeds 1001, 2001 and 3001, followed
by a freshly constructed 1001 reproduction pair with distinct RunIds. It does
not execute S2. All three pairs are verified before dispatch: exact shared S2
workload/catalog, normal sequences, equal Row/Zone/quality opening quotas and
the fixed `s7-preoccupation-v1` setup. Each run uses the unchanged real runner
and eight-file exporter. The index links Fragmented to its exact paired Control,
records source/setup provenance, harness hash and all package checksums, and
audits persisted opening availability and supporting/evaluated graphs separately.
Technical failures, conflicts or invariant findings stop the S7 protocol.

Analyze a completed eight-run set offline:

```bash
python3 -B scripts/stage-5-representative-runs/analyze_s7_fragmentation.py \
  artifacts/simulations/<s7-set>/index.json \
  --output artifacts/analysis/s7-repeat.json \
  --markdown artifacts/analysis/s7-repeat.md
python3 -B -m unittest discover -s scripts/stage-5-representative-runs -p 'test_*.py'
```

Choose fresh outputs outside raw evidence. The analyzer checks pair integrity
before outcome interpretation, joins stable request identities and reuses existing
request metric helpers and stored fairness/recovery outputs. It diagnoses each
rejection from captured eligible capacity and block-found evidence, reconstructs
total capacity from the verified 3,998 opening, and reconciles closing inventory.
Forward/reverse request transitions, normalized size/third/cause tables, population
coverage and exploratory existing timing percentiles remain separate.

Reproduction requires byte-identical definition, manifest/setup, checkpoints and
metrics plus logical equality of attempts, paths and recovery using the existing
stable-ID normalization. The repeated pair's request transition/harm analysis
must also match. RunIds and observed timing are excluded only where documented.
Raw checksums are checked before/after analysis; derived JSON and Markdown are
replayed twice for byte equality without analysis wall-clock timestamps.

The [S7 results report](../../docs/stage-5/s7-fragmentation-impact-results.md)
and adjacent JSON retain the selected official set, exact paths/checksums,
per-Seed findings, pair-level harm/reverse transitions, population effects and
interpretation boundaries. Raw evidence remains ignored under
`artifacts/simulations/`; source and analytical changes are not production policy
changes or a final Decision Gate.

## S8 initial concurrency protocol

Build the harness, then run the mandatory non-official smoke with the current
repository revision supplied explicitly:

```bash
dotnet build scripts/stage-5-representative-runs/RepresentativeRuns.csproj
DOCKER_API_VERSION=1.43 FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA=<sha> \
  dotnet run --no-build --project scripts/stage-5-representative-runs/RepresentativeRuns.csproj -- --s8-smoke
```

Smoke uses 48 registered / 40 requesting Attendees, twenty groups of two, four
Spots and one FestivalDay at both C4 and C16. C16 includes a partial final window.
The last request has an explicitly invalid resolution timestamp to exercise
UnexpectedTechnicalFailure through the real use case; contention is natural.
This is a negative control in non-official input, never an official S2 input or
an injected persistence exception. The smoke requires conflict capture, exactly
that one technical failure, normal export and zero audit findings before passing.

Use the successful smoke index printed by COMPLETE:

```bash
DOCKER_API_VERSION=1.43 FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA=<sha> \
  dotnet run --no-build --project scripts/stage-5-representative-runs/RepresentativeRuns.csproj -- \
  --s8 artifacts/simulations/<successful-s8-smoke>/index.json
```

This option executes exactly C4/1001, C4/2001, C4/3001, C16/1001, C16/2001 and
C16/3001, Repetition 1. It materializes S2 once per Seed and reuses that exact
catalog, empty setup and daily plans at both levels through S8ConcurrentExecution.
Each run owns a clean PostgreSQL container and retains real history across five
days. The runner, readiness gate, windows, transactions and no-retry behavior
remain unchanged. No C1 or follow-up repetitions are dispatched automatically.

The task-specific S8Runs harness checks the smoke packages and identical
execution-source hashes before official execution. The set index records source
and harness hashes, normal package checksums, outcome counts and final durable
state audits: Spot/day and Attendee/day uniqueness, complete contiguous groups,
request/catalog identity, no orphan/fake requests or losing partial graphs,
checkpoint reconciliation, exact window membership and non-overlap. Findings
stop the protocol after preserving evidence. This is final-state validation,
not a full temporal mutation audit. Unfavorable outcomes are preserved.

No warm-up is performed. First calls remain in latency samples; configurations
run serially in the same process, with fresh containers. Local performance is
environment-specific, and process warmness/host activity are limitations.
RunId is fresh per execution; the harness refuses to treat smoke as official.

Analyze only a completed six-run index:

```bash
python3 -B scripts/stage-5-representative-runs/analyze_s8_concurrency.py \
  artifacts/simulations/<s8-initial-concurrency-set>/index.json \
  --output artifacts/analysis/s8-initial-results.json
python3 -B -m unittest discover -s scripts/stage-5-representative-runs -p 'test_*.py'
```

The read-only analyzer verifies all package hashes, exact C4/C16 input pairing
and equality with retained official S2 inputs (normalizing only additive baseline
metadata/sequence defaults). It reuses the framework's denominators, stored
fairness/recovery and daily baseline thirds. Throughput uses terminal Attempts
over max(ProcessingStartedOffset + Duration) minus min(ProcessingStartedOffset),
with exact 100 ns parsing; request p50/p95/p99 use nearest rank. Window timing
and observed overlap are reconstructed from attempts. Cross-Seed min/mean/max
describe Seed-level observations, not repetition-level scheduler variability.
It never executes a simulation or chooses/executes adaptive follow-up runs.

The [S8 analysis](../../docs/stage-5/s8-concurrency-analysis.md) and adjacent JSON
retain the initial six-run set and its separate smoke results. The initial
adaptive decision selected the sequential baseline protocol below.

## S8 sequential baseline and combined analysis

The authorized C1 follow-up executes exactly Seeds 1001 / 2001 / 3001 once each,
Repetition 1. It uses the existing direct sequential runner path, fresh owned
PostgreSQL containers, and unchanged S2 inputs. No concurrent start gate is used.
Do not rerun this command to reproduce an analysis: every invocation executes
three new official runs. Offline analysis below reuses retained evidence.

```bash
DOCKER_API_VERSION=1.43 FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA=<sha> \
  dotnet run --no-build --project scripts/stage-5-representative-runs/RepresentativeRuns.csproj -- \
  --s8-c1 artifacts/simulations/<completed-s8-initial-concurrency-set>/index.json
```

Build the harness first. The preflight verifies all retained initial-package
checksums, unchanged production/Simulation source hashes, exact per-Seed source
definitions/catalog/setup/daily plans and matching captured execution environment.
The harness changes are fingerprinted separately; a new concurrent smoke or
C4/C16 run is not part of this follow-up. The C1 ledger links the initial index
by checksum and records all three packages, final-state audits and no-overlap
checks. RetryCount remains zero. Unexpected outcomes are retained for analysis;
invariant findings stop execution without an automatic repeat.

```bash
python3 -B scripts/stage-5-representative-runs/analyze_s8_concurrency.py \
  artifacts/simulations/<completed-s8-initial-concurrency-set>/index.json \
  --sequential-index artifacts/simulations/<completed-s8-sequential-baseline-set>/index.json \
  --output artifacts/analysis/s8-combined-results.json
```

The analyzer checks exactly three C1 entries, paired with all six retained C4/C16
entries, and compares all nine without running simulations. It preserves the
original counts/timings and adds SuccessfulRequestThroughput = Successful /
executionSeconds and SuccessfulAssignmentThroughput = durable Assignment count /
executionSeconds, using the same monotonic interval as terminal Attempts/s.
Counts reconcile to persisted paths and ledger audits before deriving delivery.
It exports per-Seed comparisons including population and across-Seed min/mean/max
for all three levels. Missing denominators remain null/N/A. Analysis replay is
deterministic for unchanged inputs and analyzer sources; outputs refuse overwrite
and remain outside raw evidence. Regenerate to a fresh path, compare bytes, then
select the reviewed JSON/report for repository history. Do not add raw packages.

## Production-like Arrival Concurrency

**Historical contract/command name retained. Open-loop High-rate Arrival Stress
= Completed; Plausible Festival Arrival Concurrency = Defined / Pending Execution.** Post-run analysis
identified sustained overload: 40 / ≈ 61.5 offered req/s exceed the retained C1
mean of ≈ 29.6 successful req/s. The [corrected interpretation](../../docs/stage-5/production-like-arrival-concurrency-analysis.md)
preserves every measurement and the stopped collection/remediation history.
The commands below document the original execution protocol; no new Moderate,
Burst-like, Seed 3001 or C1/C4/C16 run is required for this correction. Next:
**Perform MVP Technical Feasibility Gate** using existing evidence. The
[scope decision](../../docs/stage-5/pre-proposal-validation-scope.md) moves Plausible
Festival Arrival Concurrency and Late Large Groups with Reconfiguration to
conditional post-proposal validations, dependent on actual operational inputs.
The [bounded contract](../../docs/stage-5/plausible-festival-arrival-concurrency.md)
remains defined, unimplemented/unexecuted and technically useful; the commands
below remain the historical stress harness, not a way to execute the new patterns.

The [arrival contract](../../docs/stage-5/production-like-arrival-concurrency.md)
uses exact S2 inputs, Moderate/25 ms and BurstLike/4 requests with 5 ms internal
spacing and a 50 ms last-to-next-cluster gap. Build the solution and this harness,
then pass the full automated suites before official execution:

```bash
DOCKER_API_VERSION=1.43 FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA=<sha> \
  dotnet run --no-build --project scripts/stage-5-representative-runs/RepresentativeRuns.csproj -- \
  --arrival
```

This executes exactly Moderate/1001, Moderate/2001, BurstLike/1001 and
BurstLike/2001, one five-day run each, with a new owned PostgreSQL container per
run. It verifies retained C1/S2 package checksums and exact business inputs before
dispatch; production Domain/Application/Infrastructure/API source hashes must
match retained S8. Only simulation tooling changes are expected. The ledger
fingerprints execution sources/harness, audits final persisted state and records
all eight package checksums. Setup/harness failures stop the set with a retained
failure record; no automatic retries or reference reruns occur.

ArrivalDriven metadata retains the S2 ScenarioId and adds ArrivalSchedule;
ConcurrencyLevel = 0 means no configured worker cap, not zero actual activity.
Each attempt retains its scheduled run-relative offset and actual processing
offset/duration. The first scheduled offset of each day is its observed origin.
No redundant terminal-offset or scheduling-lag field is exported: derive both.

```bash
python3 -B scripts/stage-5-representative-runs/analyze_arrival_concurrency.py \
  artifacts/simulations/<completed-arrival-set>/index.json \
  --output artifacts/analysis/arrival-results.json
```

The analyzer verifies hashes, original S2 equivalence, schedule arithmetic,
request/Assignment/path reconciliation, independent half-open overlap counts and
unchanged conflict semantics. It reports actual peak/mean concurrency and lag
separately from use-case latency, plus useful throughput/participation and retained
C1/C4/C16 references. Outputs refuse overwrite and stay outside raw packages.
The selected documentation JSON has an additive `interpretation` note for the
post-run correction; the unchanged analyzer regenerates measurements/provenance,
not this editorial metadata. All original fields remain unchanged.

Only after analyzing the initial four, document a pattern-specific ambiguity
before any conditional execution:

```bash
DOCKER_API_VERSION=1.43 FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA=<sha> \
  dotnet run --no-build --project scripts/stage-5-representative-runs/RepresentativeRuns.csproj -- \
  --arrival-add <latest-completed-index.json> <Moderate-or-BurstLike> <reason-file>
```

An expansion adds Seed 3001 for that pattern only and chains the prior ledger
by checksum. Reusing an already-expanded pattern is rejected; the planned total
is capped at six. A confirmed integrity violation must be investigated, never
averaged away. The hypotheses and results do not estimate production traffic.

The completed [arrival analysis](../../docs/stage-5/production-like-arrival-concurrency-analysis.md)
records a stopped collection exposing a post-commit observer-audit assumption.
The [remediation record](../../docs/stage-5/arrival-observer-audit-remediation.md)
was written before replacing that collection. Valid committed graphs whose
observer read fails are correlated from the existing final snapshot, with no
new DB read/retry and no change to attempt outcomes or business execution.

```bash
DOCKER_API_VERSION=1.43 FESTIVAL_SIMULATION_REPOSITORY_COMMIT_SHA=<sha> \
  dotnet run --no-build --project scripts/stage-5-representative-runs/RepresentativeRuns.csproj -- \
  --arrival-resume <stopped-index.json> docs/stage-5/arrival-observer-audit-remediation.md
```

This specific repair retains the two valid Moderate entries, preserves the
stopped BurstLike/1001 entry/checksums/findings as excluded, and executes only
its documented replacement plus BurstLike/2001. The new ledger links the stopped
ledger by checksum and retains the original source phase. Existing src files
must match byte-for-byte; only the read-only resolver is added. The completed
matrix has four valid runs and five actual executions; neither pattern required
Seed 3001. The cap includes excluded executions, so a later expansion cannot
silently exceed six actual executions.
