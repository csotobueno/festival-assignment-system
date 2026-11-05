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
