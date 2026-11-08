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
