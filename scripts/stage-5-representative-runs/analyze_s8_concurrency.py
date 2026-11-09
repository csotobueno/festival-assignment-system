"""Read-only S8 initial-matrix analysis; no runner, rubric or recovery replay."""

import argparse
import copy
import hashlib
import json
import math
from collections import Counter
from datetime import datetime
from pathlib import Path
from statistics import mean

from analyze_s2_paths import SEEDS, THIRDS, rate, require, sequence_third, uid
from analyze_s6_groups import BUCKETS
from summarize import FILES

MEASURED = ("Successful", "DomainRejected", "KnownConcurrencyConflict", "UnexpectedTechnicalFailure")


def ticks(value):
    """Parse .NET TimeSpan exactly at its 100 ns evidence precision."""
    hours, minutes, seconds = value.split(":")
    days, hours = hours.split(".") if "." in hours else ("0", hours)
    seconds, _, fraction = seconds.partition(".")
    return ((int(days) * 24 + int(hours)) * 3600 + int(minutes) * 60 + int(seconds)) * 10_000_000 + int(fraction.ljust(7, "0"))


def outcome_metrics(attempts):
    counts = Counter(a["outcome"] for a in attempts)
    require(set(counts) <= set(MEASURED) | {"ExcludedInvalidInput"}, "Unknown analytical outcome")
    attempted = sum(counts[k] for k in MEASURED)
    normal = counts["Successful"] + counts["DomainRejected"]
    return dict(attemptedRequests=attempted, normallyProcessedRequests=normal,
        successful=counts["Successful"], domainRejected=counts["DomainRejected"],
        knownConcurrencyConflict=counts["KnownConcurrencyConflict"], unexpectedTechnicalFailure=counts["UnexpectedTechnicalFailure"],
        excludedInvalidInput=counts["ExcludedInvalidInput"], successRate=rate(counts["Successful"], normal),
        rejectionRate=rate(counts["DomainRejected"], normal), knownConcurrencyConflictRate=rate(counts["KnownConcurrencyConflict"], attempted),
        technicalErrorRate=rate(counts["UnexpectedTechnicalFailure"], attempted))


def performance(attempts):
    measured = [a for a in attempts if a["outcome"] in MEASURED]
    if not measured:
        return dict(sampleCount=0, p50Ms=None, p95Ms=None, p99Ms=None, executionSeconds=None, throughput=None)
    require(all(a.get("processingStartedOffset") is not None for a in measured), "Missing monotonic offset")
    durations = sorted(ticks(a["duration"]) for a in measured)
    require(durations[0] >= 0, "Negative request duration")
    start = min(ticks(a["processingStartedOffset"]) for a in measured)
    end = max(ticks(a["processingStartedOffset"]) + ticks(a["duration"]) for a in measured)
    elapsed = (end - start) / 10_000_000
    return dict(sampleCount=len(measured),
        **{f"p{p}Ms": durations[math.ceil(p / 100 * len(durations)) - 1] / 10_000 for p in (50, 95, 99)},
        executionSeconds=elapsed, throughput=rate(len(measured), elapsed),
        firstEntryOffset= start / 10_000_000, lastTerminalOffset=end / 10_000_000)


def group_bucket(size):
    return str(size) if size <= 5 else "6–7" if size <= 7 else "8–10"


def read_package(entry):
    directory = Path(entry["evidencePath"])
    require(sorted(p.name for p in directory.iterdir()) == sorted(FILES), "Eight-file package incomplete")
    require(set(entry["integrity"]["sha256"]) == set(FILES), "Incomplete checksum inventory")
    data = {}
    for name in FILES:
        raw = (directory / name).read_bytes()
        require(hashlib.sha256(raw).hexdigest() == entry["integrity"]["sha256"][name], f"Corrupt evidence: {directory}/{name}")
        data[name[:-5]] = json.loads(raw)
    require(data["run-metadata"] == entry["metadata"], "Index/run metadata mismatch")
    return data


def retained_s2_proof(seed, data):
    """Read historical controls only; normalize additive baseline defaults."""
    official = json.loads(Path("docs/stage-5/s1-s2-representative-results.json").read_text())
    entry = next(r for r in official["officialRuns"] if r["scenario"] == "s2" and r["seed"] == seed)
    source = read_package(entry)
    candidate = copy.deepcopy(data["manifest"])
    candidate["metadata"].update(scenarioId="s2-expected-baseline", executionMode="Sequential", concurrencyLevel=1)
    candidate["metadata"].pop("repetition", None)
    candidate["metadata"].pop("workloadSourceScenarioId", None)
    candidate["metadata"].pop("orderingTransformation", None)
    for day in candidate["dailyPlans"]:
        for request in day["requests"]:
            require(request.pop("executionSequence") == request["sequence"], "Changed source dispatch")
    require(candidate == source["manifest"] and data["definition"] == source["definition"], "S8 differs from retained official S2 input")
    return dict(seed=seed, evidencePath=entry["evidencePath"], allEightFileChecksumsVerified=True,
                exactBusinessInputEqual=True)


def summarize_run(entry, data):
    manifest, meta = data["manifest"], data["run-metadata"]
    level = entry["concurrencyLevel"]
    require(level in (4, 16) and entry["seed"] in SEEDS and entry["repetition"] == 1, "Wrong initial configuration")
    require(meta["executionMode"] == "Concurrent" and meta["concurrencyLevel"] == level and meta["repetition"] == 1, "Wrong execution metadata")
    for field in ("scenarioId", "seed", "generatorVersion", "executionMode", "concurrencyLevel", "repetition", "workloadSourceScenarioId"):
        require(meta[field] == manifest["metadata"][field], f"Manifest metadata mismatch: {field}")
    require(meta["generatorVersion"] == "v2" and meta["engineVersion"] == "v0.3.0" and meta["rubricVersion"] == "v1", "Version mismatch")
    require(meta["workloadSourceScenarioId"] == data["definition"]["scenarioId"] == "s2-expected-baseline", "Wrong S2 source")
    require(data["definition"]["executionMode"] == "Sequential" and data["definition"]["concurrencyLevel"] == 1, "Source definition changed")
    require(len(manifest["catalog"]["attendees"]) == 5500 and len(manifest["catalog"]["spots"]) == 4998, "Population/topology changed")
    require(not any(manifest["setup"].values()), "S8 must have normal empty S2 setup")
    attempts = data["attempts"]
    require(len({a["simulationRequestId"] for a in attempts}) == len(attempts), "Duplicate execution / retry")
    days = sorted(manifest["dailyPlans"], key=lambda d: d["dayNumber"])
    require(len(days) == len(data["checkpoints"]) == entry["integrity"]["completedFestivalDays"] == 5, "Incomplete days")
    positions = {}
    evaluated = set()
    for day in days:
        requests = sorted(day["requests"], key=lambda r: r["sequence"])
        members = [uid(m["id"]) for r in requests for m in r["members"]]
        require(len(members) == len(set(members)) == 4400, "Daily requester membership changed")
        evaluated.update(members)
        observed = [a for a in attempts if a["festivalDayId"] == day["festivalDayId"]]
        require([a["simulationRequestId"] for a in observed] == [r["simulationRequestId"] for r in requests], "Request identity/order mismatch")
        for i, (a, r) in enumerate(zip(observed, requests)):
            require(a["baselineSequence"] == a["attemptSequence"] == r["sequence"] == r["executionSequence"], "Sequence semantics changed")
            require(a["windowNumber"] == i // level + 1 and a["windowPosition"] == i % level + 1, "Wrong window provenance")
            require(a["attendeeIds"] == [m["id"] for m in r["members"]] and a["groupSize"] == len(r["members"]), "Attempt membership changed")
            positions[a["simulationRequestId"]] = (day["dayNumber"], sequence_third(i + 1, len(requests)))
            if a.get("databaseSqlState") == "40P01":
                require(a["outcome"] == "KnownConcurrencyConflict" and a["diagnosticCode"] == "DeadlockDetected", "Deadlock misclassified")
            if a["outcome"] == "KnownConcurrencyConflict":
                require(a["persistedRequestId"] is None and a["selectedExperienceQuality"] is None, "Conflict claims persisted outcome")
    metrics = outcome_metrics(attempts)
    require(entry["outcomes"] == dict(Counter(a["outcome"] for a in attempts)), "Index outcome mismatch")
    require(entry["integrity"]["invariantViolationCount"] == len(entry["integrity"]["violations"]), "Invariant audit mismatch")
    require(entry["integrity"]["persistedRequestCount"] == metrics["normallyProcessedRequests"], "Persisted request reconciliation mismatch")
    paths, fair, recovery = data["attendee-paths"], data["metrics"]["fairness"], data["metrics"]["recovery"]
    require({uid(p["attendeeId"]) for p in paths} == evaluated, "Fairness evaluated population mismatch")
    requesting = {uid(member) for a in attempts if a["outcome"] in MEASURED for member in a["attendeeIds"]}
    participating = {uid(p["attendeeId"]) for p in paths if p["pathLength"] > 0}
    require(sum(p["pathLength"] for p in paths) == fair["persistedOutcomeCount"] == entry["integrity"]["persistedAssignmentCount"], "Persisted path count mismatch")
    for name, predicate in (("noPersistedPathCount", lambda p: p["pathLength"] == 0),
                            ("insufficientEvidenceCount", lambda p: p["pathLength"] == 1),
                            ("classifiablePathCount", lambda p: p["pathLength"] >= 2)):
        require(sum(predicate(p) for p in paths) == fair[name], f"Population mismatch: {name}")
    require(sum(a["groupSize"] for a in attempts if a["outcome"] == "Successful") == fair["persistedOutcomeCount"], "Successful membership differs from persisted outcomes")
    persisted_entries = {(uid(p["attendeeId"]), uid(e["festivalDayId"])): e for p in paths for e in p["entries"]}
    for a in attempts:
        entries = [persisted_entries.get((uid(member), uid(a["festivalDayId"]))) for member in a["attendeeIds"]]
        require(all(e is not None and e["experienceQuality"] == a["selectedExperienceQuality"] for e in entries)
                if a["outcome"] == "Successful" else all(e is None for e in entries), "Synthetic/missing persisted path evidence")
    for quality in ("Good", "Medium", "Bad"):
        require(sum(e["experienceQuality"] == quality for e in persisted_entries.values()) == fair[quality.lower() + "OutcomeCount"], "Stored quality count mismatch")
    for classification in ("Balanced", "Acceptable", "NotBalanced"):
        require(sum(p["classification"] == classification for p in paths) == fair[classification[0].lower() + classification[1:] + "PathCount"], "Stored classification count mismatch")
    episodes = [e for record in data["recovery-observations"] for e in record["episodes"]]
    require(len(episodes) == recovery["recoveryEpisodeCount"]
            and sum(e["status"] == "Recovered" for e in episodes) == recovery["recoveredEpisodeCount"]
            and sum(e["recoveryEligible"] for e in episodes) == recovery["recoveryEligibleEpisodeCount"], "Stored recovery evidence mismatch")
    conflicts = [a for a in attempts if a["outcome"] == "KnownConcurrencyConflict"]
    diagnostics = Counter((a.get("databaseSqlState"), a.get("diagnosticCode")) for a in conflicts)
    def segments(categories, key):
        return [dict(category=value, **outcome_metrics([a for a in attempts if key(a) == value])) for value in categories]
    window_rows = []
    for day in days:
        daily = [a for a in attempts if a["festivalDayId"] == day["festivalDayId"]]
        last_end = -1
        for window in sorted({a["windowNumber"] for a in daily}):
            group = [a for a in daily if a["windowNumber"] == window]
            start = min(ticks(a["processingStartedOffset"]) for a in group)
            end = max(ticks(a["processingStartedOffset"]) + ticks(a["duration"]) for a in group)
            require(start >= last_end, "Concurrent windows overlap")
            last_end = end
            events = [(ticks(a["processingStartedOffset"]), 1) for a in group] + [(ticks(a["processingStartedOffset"]) + ticks(a["duration"]), -1) for a in group]
            active = peak = 0
            for _, delta in sorted(events):
                active += delta
                peak = max(peak, active)
            window_rows.append(dict(day=day["dayNumber"], windowNumber=window, windowSize=len(group), observedPeak=peak,
                                    durationMs=(end - start) / 10_000, **outcome_metrics(group)))
    return dict(seed=entry["seed"], concurrencyLevel=level, runId=entry["runId"], evidencePath=entry["evidencePath"],
        metadata=meta, integrity=entry["integrity"], outcomes=metrics, performance=performance(attempts),
        population=dict(registeredAttendees=5500, requestingAttendees=len(requesting), participatingAttendees=len(participating),
            attendeesWithOnlyRejectedOrFailedAttempts=len(requesting - participating), noPersistedPath=fair["noPersistedPathCount"],
            insufficientEvidence=fair["insufficientEvidenceCount"], classifiable=fair["classifiablePathCount"]),
        fairness=fair, recovery=recovery,
        conflictByDay=segments(range(1, 6), lambda a: positions[a["simulationRequestId"]][0]),
        conflictByBaselineThird=segments(THIRDS, lambda a: positions[a["simulationRequestId"]][1]),
        conflictByGroupSize=segments(BUCKETS, lambda a: group_bucket(a["groupSize"])),
        conflictDiagnostics=[dict(databaseSqlState=state, diagnosticCode=code, count=count, shareOfConflicts=rate(count, len(conflicts)))
            for (state, code), count in sorted(diagnostics.items(), key=lambda item: str(item[0]))],
        windowSummary=dict(windowCount=len(window_rows), partialWindows=sum(w["windowSize"] < level for w in window_rows),
            maxObservedOverlap=max(w["observedPeak"] for w in window_rows), windowsWithoutOverlap=sum(w["observedPeak"] <= 1 for w in window_rows),
            minDurationMs=min(w["durationMs"] for w in window_rows), meanDurationMs=mean(w["durationMs"] for w in window_rows),
            maxDurationMs=max(w["durationMs"] for w in window_rows)),
        # Existing raw attempts reconstruct every window; retain only aggregate
        # window facts in the selected report to avoid duplicating telemetry.
        failureDiagnostics=[dict(outcome=a["outcome"], simulationRequestId=a["simulationRequestId"], databaseSqlState=a.get("databaseSqlState"),
            diagnosticCode=a["diagnosticCode"], diagnosticType=a["diagnosticType"], diagnostic=a["diagnostic"])
            for a in attempts if a["outcome"] == "UnexpectedTechnicalFailure"])


def analyze(index_path):
    raw = Path(index_path).read_bytes()
    index = json.loads(raw)
    require(index["role"] == "official" and index["status"] == "Completed", "Official matrix not complete")
    entries = index["runs"]
    require(len(entries) == 6 and {(e["concurrencyLevel"], e["seed"]) for e in entries} == {(c, s) for c in (4, 16) for s in SEEDS}, "Wrong six-run initial matrix")
    require(index["retryCount"] == 0, "Retries changed")
    smoke_raw = Path(index["smokeIndex"]).read_bytes()
    require(hashlib.sha256(smoke_raw).hexdigest() == index["smokeIndexSha256"], "Smoke index changed")
    smoke = json.loads(smoke_raw)
    require(smoke["role"] == "smoke" and smoke["status"] == "Passed" and smoke["sourceSha256"] == index["sourceSha256"], "Smoke source/status mismatch")
    for entry in smoke["runs"]:
        read_package(entry)
    data = [read_package(entry) for entry in entries]
    pair_proofs = []
    retained_proofs = []
    for seed in SEEDS:
        pair = [(e, d) for e, d in zip(entries, data) if e["seed"] == seed]
        (left, a), (right, b) = sorted(pair, key=lambda x: x[0]["concurrencyLevel"])
        require(a["definition"] == b["definition"], "Paired source definition changed")
        require(all(a["manifest"][key] == b["manifest"][key] for key in ("catalog", "setup", "dailyPlans")), "Exact S2 workload pair changed")
        require(left["sourceManifestSha256"] == right["sourceManifestSha256"], "Source manifest proof mismatch")
        pair_proofs.append(dict(seed=seed, exactDefinitionCatalogSetupDailyPlans=True, sourceManifestSha256=left["sourceManifestSha256"]))
        retained_proofs.append(retained_s2_proof(seed, a))
    runs = [summarize_run(e, d) for e, d in zip(entries, data)]
    aggregate = {}
    for level in (4, 16):
        selected = [r for r in runs if r["concurrencyLevel"] == level]
        fields = {**{k: [r["outcomes"][k] for r in selected] for k in ("knownConcurrencyConflictRate", "technicalErrorRate", "successRate", "rejectionRate")},
                  **{k: [r["performance"][k] for r in selected] for k in ("p50Ms", "p95Ms", "p99Ms", "throughput", "executionSeconds")}}
        aggregate[str(level)] = {k: dict(min=min(values), mean=mean(values), max=max(values), seeds=len(values))
            for k, samples in fields.items() if (values := [v for v in samples if v is not None])}
    # Re-read hashes after analysis to prove the raw package boundary was read-only.
    for entry in entries:
        read_package(entry)
    return dict(indexPath=str(index_path), indexSha256=hashlib.sha256(raw).hexdigest(), smoke=smoke,
        analysisSourceSha256={name: hashlib.sha256((Path(__file__).parent / name).read_bytes()).hexdigest()
            for name in ("analyze_s8_concurrency.py", "analyze_s2_paths.py", "analyze_s6_groups.py", "analyze_s5_order.py", "summarize.py")},
        repositoryCommitSha=index["repositoryCommitSha"], sourceSha256=index["sourceSha256"], pairIntegrity=pair_proofs,
        retainedS2InputIntegrity=retained_proofs,
        timingBoundary="max(ProcessingStartedOffset + Duration) - min(ProcessingStartedOffset), over all terminal measured Attempts across Days 1–5; monotonic 100 ns ticks",
        percentileRule="Nearest-rank ceil(p*N); all AttemptedRequests; exclusions omitted",
        warmup=index["warmup"], buildConfiguration=index["buildConfiguration"], runs=runs, crossSeed=aggregate,
        variabilityBoundary="One run per Seed/level; cross-Seed min/mean/max is not repetition-level scheduler variability")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("index")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    output = Path(args.output)
    require(not output.exists(), "Refusing to overwrite existing analysis")
    require("simulations" not in output.parts, "Analysis must stay outside raw simulation evidence")
    result = analyze(args.index)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, indent=2, ensure_ascii=False, allow_nan=False) + "\n")
    print(output)


if __name__ == "__main__":
    main()
