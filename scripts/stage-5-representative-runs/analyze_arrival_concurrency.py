"""Read-only arrival experiment analysis; reuse Stage 5 accounting and timing."""

import argparse
import copy
import hashlib
import json
import math
from collections import Counter
from pathlib import Path
from statistics import mean

from analyze_s8_concurrency import (MEASURED, outcome_metrics, performance, read_package,
                                    retained_s2_proof, ticks, useful_throughput)
from analyze_s2_paths import require, uid


def offset_ms(pattern, ordinal):
    require(ordinal >= 0 and pattern in ("Moderate", "BurstLike"), "Invalid schedule")
    return 25 * ordinal if pattern == "Moderate" else 65 * (ordinal // 4) + 5 * (ordinal % 4)


def max_active(intervals):
    events = []
    for start, duration in intervals:
        require(start >= 0 and duration >= 0, "Invalid actual interval")
        if duration:
            events.extend(((start, 1), (start + duration, -1)))
    active = peak = 0
    for _, delta in sorted(events):
        active += delta  # End (-1) before start (+1): half-open intervals.
        peak = max(peak, active)
    require(active == 0, "Unbalanced interval accounting")
    return peak


def timing_diagnostics(attempts):
    measured = [a for a in attempts if a["outcome"] in MEASURED]
    intervals, lag = [], []
    for attempt in measured:
        require(attempt.get("processingStartedOffset") is not None and attempt.get("scheduledArrivalOffset") is not None,
                "Missing actual/scheduled timing")
        start, scheduled, duration = (ticks(attempt[k]) for k in ("processingStartedOffset", "scheduledArrivalOffset", "duration"))
        require(start >= scheduled >= 0, "Started before due arrival")
        intervals.append((start, duration))
        lag.append((start - scheduled) / 10_000)
    if not intervals:
        return dict(maxActiveRequests=0, meanSchedulingLagMs=None, p95SchedulingLagMs=None,
                    maxSchedulingLagMs=None, meanActiveConcurrency=None)
    elapsed = max(s + d for s, d in intervals) - min(s for s, _ in intervals)
    return dict(maxActiveRequests=max_active(intervals), meanSchedulingLagMs=mean(lag),
                p95SchedulingLagMs=sorted(lag)[math.ceil(.95 * len(lag)) - 1], maxSchedulingLagMs=max(lag),
                meanActiveConcurrency=sum(d for _, d in intervals) / elapsed if elapsed else None)


def validate_schedule(pattern, day, attempts):
    requests = sorted(day["requests"], key=lambda r: r["sequence"])
    require([a["simulationRequestId"] for a in attempts] == [r["simulationRequestId"] for r in requests], "Request loss/order change")
    if not attempts:
        return
    origin = ticks(attempts[0]["scheduledArrivalOffset"])
    for ordinal, (attempt, request) in enumerate(zip(attempts, requests)):
        require(attempt["attemptSequence"] == attempt["baselineSequence"] == request["sequence"] == request["executionSequence"],
                "Baseline sequence changed")
        require(attempt["attendeeIds"] == [m["id"] for m in request["members"]], "Membership changed")
        require(ticks(attempt["scheduledArrivalOffset"]) == origin + offset_ms(pattern, ordinal) * 10_000, "Schedule drift/configuration mismatch")
        require(attempt.get("windowNumber") is None and attempt.get("windowPosition") is None, "Synchronized window provenance present")


def summarize(entry, data):
    pattern = entry["pattern"]
    meta, manifest, attempts = data["run-metadata"], data["manifest"], data["attempts"]
    expected = dict(pattern=pattern, version="arrival-v1", interArrivalMilliseconds=25 if pattern == "Moderate" else None,
                    clusterSize=4 if pattern == "BurstLike" else None, withinClusterMilliseconds=5 if pattern == "BurstLike" else None,
                    betweenClustersMilliseconds=50 if pattern == "BurstLike" else None)
    require(meta["arrivalSchedule"] == manifest["metadata"]["arrivalSchedule"] == expected, "Wrong authoritative schedule configuration")
    require(meta["executionMode"] == manifest["metadata"]["executionMode"] == "ArrivalDriven"
            and meta["concurrencyLevel"] == manifest["metadata"]["concurrencyLevel"] == 0, "Wrong execution mode/cap")
    require(meta["scenarioId"] == "s2-expected-baseline" and meta["seed"] == entry["seed"]
            and meta["generatorVersion"] == "v2" and meta["engineVersion"] == "v0.3.0" and meta["rubricVersion"] == "v1", "Provenance mismatch")
    candidate = copy.deepcopy(data)
    candidate["manifest"]["metadata"].pop("arrivalSchedule")
    proof = retained_s2_proof(entry["seed"], candidate)
    require(len(attempts) == len({a["simulationRequestId"] for a in attempts}) == 7320, "Duplicate/missing attempt or retry")
    require(len(manifest["dailyPlans"]) == len(data["checkpoints"]) == entry["integrity"]["completedFestivalDays"] == 5, "Incomplete run")
    day_rows = []
    previous_end = -1
    for day in sorted(manifest["dailyPlans"], key=lambda d: d["dayNumber"]):
        observed = [a for a in attempts if a["festivalDayId"] == day["festivalDayId"]]
        validate_schedule(pattern, day, observed)
        require(min(ticks(a["scheduledArrivalOffset"]) for a in observed) >= previous_end, "Next day scheduled before previous drain")
        previous_end = max(ticks(a["processingStartedOffset"]) + ticks(a["duration"]) for a in observed)
        day_rows.append(dict(day=day["dayNumber"], **outcome_metrics(observed), **timing_diagnostics(observed), performance=performance(observed)))
    outcomes = outcome_metrics(attempts)
    outcomes["unexpectedTechnicalFailureRate"] = outcomes["technicalErrorRate"]
    require(entry["outcomes"] == dict(Counter(a["outcome"] for a in attempts)), "Ledger outcomes mismatch")
    require(entry["integrity"]["invariantViolationCount"] == len(entry["integrity"]["violations"]), "Audit count mismatch")
    observation_commits = {r["simulationRequestId"]:r for r in entry.get("postCommitObservationRequests", [])}
    require(outcomes["normallyProcessedRequests"] + len(observation_commits) == entry["integrity"]["persistedRequestCount"], "Durable request mismatch")
    paths, fair = data["attendee-paths"], data["metrics"]["fairness"]
    evaluated = {uid(m["id"]) for d in manifest["dailyPlans"] for r in d["requests"] for m in r["members"]}
    requesting = {uid(m) for a in attempts if a["outcome"] in MEASURED for m in a["attendeeIds"]}
    participating = {uid(p["attendeeId"]) for p in paths if p["pathLength"] > 0}
    require({uid(p["attendeeId"]) for p in paths} == evaluated, "Evaluated population changed")
    require(sum(p["pathLength"] for p in paths) == fair["persistedOutcomeCount"] == entry["integrity"]["persistedAssignmentCount"]
            == sum(a["groupSize"] for a in attempts if a["outcome"] == "Successful")
               + sum(r["persistedAssignmentCount"] for r in observation_commits.values()), "Durable Assignment/path reconciliation failed")
    require(sum(p["pathLength"] == 0 for p in paths) == fair["noPersistedPathCount"], "NoPersistedPath mismatch")
    entries = {(uid(p["attendeeId"]), uid(e["festivalDayId"])): e for p in paths for e in p["entries"]}
    require(len(entries) == fair["persistedOutcomeCount"], "Duplicate persisted attendee/day")
    for a in attempts:
        persisted = [entries.get((uid(m), uid(a["festivalDayId"]))) for m in a["attendeeIds"]]
        if a["simulationRequestId"] in observation_commits:
            correlation = observation_commits[a["simulationRequestId"]]
            require(a["outcome"] == "UnexpectedTechnicalFailure" and a["diagnosticCode"] == "RECONCILIATION_FAILED"
                    and a["feasibleContiguousBlockFound"] is True and correlation["persistedRequestStatus"] == "Completed"
                    and correlation["persistedAssignmentCount"] == a["groupSize"]
                    and all(e is not None for e in persisted)
                    and {uid(e["assignmentId"]) for e in persisted} == set(correlation["persistedAssignmentIds"]),
                    "Invalid post-commit observation correlation")
            continue  # Outcome remains unexpected; final durable allocation is separate.
        require(all(e is not None and e["experienceQuality"] == a["selectedExperienceQuality"] for e in persisted)
                if a["outcome"] == "Successful" else all(e is None for e in persisted), "Synthetic/missing persisted participation")
        if a.get("databaseSqlState") == "40P01":
            require(a["outcome"] == "KnownConcurrencyConflict" and a["diagnosticCode"] == "DeadlockDetected", "Deadlock classification changed")
        if a["outcome"] == "KnownConcurrencyConflict":
            require(a["persistedRequestId"] is None, "Conflict claims persisted graph")
    diagnostics = timing_diagnostics(attempts)
    require(diagnostics["maxActiveRequests"] == entry["maxActiveRequests"], "Independent peak calculation disagrees")
    timing = performance(attempts)
    timing.update(useful_throughput(outcomes["successful"], fair["persistedOutcomeCount"], timing["executionSeconds"]))
    timing["wallClockDurationSeconds"] = timing["executionSeconds"]
    conflict_diagnostics = Counter((a.get("databaseSqlState"), a.get("diagnosticCode")) for a in attempts if a["outcome"] == "KnownConcurrencyConflict")
    failure_diagnostics = Counter((a.get("databaseSqlState"), a.get("diagnosticCode"), a.get("diagnosticType"), a.get("diagnostic"))
                                  for a in attempts if a["outcome"] == "UnexpectedTechnicalFailure")
    return dict(pattern=pattern, seed=entry["seed"], runId=entry["runId"], evidencePath=entry["evidencePath"], metadata=meta,
                sourceManifestSha256=entry["sourceManifestSha256"], integrity=entry["integrity"], outcomes=outcomes,
                postCommitObservationRequests=list(observation_commits.values()),
                performance=timing, concurrency=diagnostics,
                population=dict(requestingAttendees=len(requesting), participatingAttendees=len(participating),
                    noPersistedPath=fair["noPersistedPathCount"], insufficientEvidence=fair["insufficientEvidenceCount"], classifiable=fair["classifiablePathCount"]),
                fairness=fair, recovery=data["metrics"]["recovery"], days=day_rows, retainedS2InputIntegrity=proof,
                conflictDiagnostics=[dict(databaseSqlState=s, diagnosticCode=c, count=n) for (s,c),n in sorted(conflict_diagnostics.items(), key=str)],
                failureDiagnostics=[dict(databaseSqlState=s, diagnosticCode=c, diagnosticType=t, diagnostic=m, count=n)
                    for (s,c,t,m),n in sorted(failure_diagnostics.items(), key=str)])


def analyze(index_path):
    raw = Path(index_path).read_bytes()
    index = json.loads(raw)
    require(index["role"] == "arrival-concurrency" and index["status"] == "Completed" and index["retryCount"] == 0, "Official matrix incomplete")
    configurations = {(e["pattern"], e["seed"]) for e in index["runs"]}
    initial = {(p,s) for p in ("Moderate", "BurstLike") for s in (1001,2001)}
    require(4 <= len(index["runs"]) == len(configurations) <= 6 and initial <= configurations
            and configurations <= initial | {("Moderate",3001),("BurstLike",3001)}, "Wrong official matrix")
    if len(configurations) > 4:
        require(index["expansionReason"] and index["priorIndex"], "Undocumented expansion")
        require(hashlib.sha256(Path(index["priorIndex"]).read_bytes()).hexdigest() == index["priorIndexSha256"], "Prior ledger changed")
    excluded = index.get("excludedExecutions", [])
    require(len(index["runs"]) + len(excluded) <= 6, "Actual execution cap exceeded")
    if excluded:
        require(index.get("remediationReason") and index.get("priorIndex"), "Missing stopped-run remediation record")
        require(hashlib.sha256(Path(index["priorIndex"]).read_bytes()).hexdigest() == index["priorIndexSha256"], "Stopped ledger changed")
        for stopped in excluded:
            read_package(stopped["entry"])
        for path, digest in index["preservedRunSourceSha256"].items():
            if path.startswith("src/"):
                require(index["sourceSha256"][path] == digest, "Arrival execution source changed during audit correction")
    reference_path = Path(index["referenceAnalysisPath"])
    require(hashlib.sha256(reference_path.read_bytes()).hexdigest() == index["referenceAnalysisSha256"], "Reference analysis changed")
    reference = json.loads(reference_path.read_bytes())
    runs = [summarize(e, read_package(e)) for e in index["runs"]]
    refs = [r for r in reference["runs"] if r["seed"] in {e["seed"] for e in index["runs"]}]
    for row in refs:
        read_package(row)
    for seed in {r["seed"] for r in runs}:
        paired = [r for r in runs if r["seed"] == seed]
        require(len({r["sourceManifestSha256"] for r in paired}) == 1, "Pattern source input mismatch")
    for entry in index["runs"]:
        read_package(entry)  # Read-only checksum boundary, checked again after analysis.
    return dict(indexPath=str(index_path), indexSha256=hashlib.sha256(raw).hexdigest(), officialRunCount=len(runs),
                actualOfficialExecutionCount=len(runs)+len(excluded), excludedExecutions=excluded,
                priorIndex=index.get("priorIndex"), priorIndexSha256=index.get("priorIndexSha256"),
                remediationReason=index.get("remediationReason"), preservedRunSourceSha256=index.get("preservedRunSourceSha256"),
                sourceSha256=index["sourceSha256"], repositoryCommitSha=index["repositoryCommitSha"],
                buildConfiguration=index["buildConfiguration"], warmup=index["warmup"], retryCount=0,
                analysisSourceSha256={n: hashlib.sha256((Path(__file__).parent/n).read_bytes()).hexdigest()
                    for n in ("analyze_arrival_concurrency.py","analyze_s8_concurrency.py","analyze_s6_groups.py",
                              "analyze_s5_order.py","analyze_s2_paths.py","summarize.py")},
                runs=runs, references=[dict(seed=r["seed"], concurrencyLevel=r["concurrencyLevel"], runId=r["runId"],
                    outcomes=r["outcomes"], performance=r["performance"], population=r["population"],
                    evidencePath=r["evidencePath"], integrity=r["integrity"], metadata=r["metadata"]) for r in refs],
                timingBoundary="Last terminal offset minus first actual processing-start offset over five days; monotonic; includes intervening gaps",
                variabilityBoundary="One run per pattern/Seed; same Seed does not fix scheduler interleavings; hypotheses are not production forecasts")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("index")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    output = Path(args.output)
    require(not output.exists() and "simulations" not in output.parts, "Refusing to overwrite or write into raw simulation evidence")
    result = analyze(args.index)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, indent=2, ensure_ascii=False, allow_nan=False) + "\n")
    print(output)


if __name__ == "__main__":
    main()
