"""Summarize only this six-run protocol and its separately labelled reproduction.

Usage: python3 scripts/stage-5-representative-runs/summarize.py <set>/index.json
No engine, rubric, recovery or exported aggregate formulas are reimplemented.
"""

import hashlib
import json
import math
import sys
from collections import Counter
from datetime import datetime
from pathlib import Path
from statistics import mean


FILES = [name + ".json" for name in (
    "run-metadata", "definition", "manifest", "attempts", "checkpoints",
    "attendee-paths", "recovery-observations", "metrics")]


def uid(value):
    return value["value"]


def ratio(numerator, denominator):
    return numerator / denominator if denominator else None


def duration_ms(value):
    # System.Text.Json serializes TimeSpan as [d.]hh:mm:ss.fffffff.
    hours, minutes, seconds = value.split(":")
    days = 0
    if "." in hours:
        days, hours = hours.split(".")
    return ((int(days) * 24 + int(hours)) * 3600 + int(minutes) * 60 + float(seconds)) * 1000


def check_metadata(metadata, index_metadata):
    for key, value in metadata.items():
        if key in ("startedAt", "completedAt"):
            assert datetime.fromisoformat(value.replace("Z", "+00:00")) == datetime.fromisoformat(index_metadata[key])
        elif key == "executionMode":
            assert value == "Sequential" and index_metadata[key] in ("Sequential", 0)
        else:
            assert value == index_metadata[key], key


def summarize(entry):
    directory = Path(entry["evidencePath"])
    assert sorted(p.name for p in directory.iterdir()) == sorted(FILES), directory
    data = {}
    for name in FILES:
        raw = (directory / name).read_bytes()
        assert hashlib.sha256(raw).hexdigest() == entry["integrity"]["sha256"][name], name
        data[name[:-5]] = json.loads(raw)
    metadata, manifest, definition = (data[name] for name in ("run-metadata", "manifest", "definition"))
    check_metadata(metadata, entry["metadata"])
    for field in ("scenarioId", "seed", "generatorVersion", "executionMode", "concurrencyLevel"):
        assert metadata[field] == manifest["metadata"][field] == definition[field], field
    assert metadata["seed"] == entry["seed"]
    assert metadata["generatorVersion"] == "v2" and metadata["engineVersion"] == "v0.3.0"
    assert metadata["rubricVersion"] == "v1"
    assert metadata["executionMode"] == "Sequential" and metadata["concurrencyLevel"] == 1
    assert definition["workload"]["allowsFrontStandingRequestShare"] == 0.5
    expected_requesters = 3300 if entry["scenario"] == "s1" else 4400
    days = {uid(day["festivalDayId"]): day for day in manifest["dailyPlans"]}
    assert len(days) == 5
    assert set(days) == {uid(checkpoint["festivalDayId"]) for checkpoint in data["checkpoints"]}
    evaluated = set()
    for day in days.values():
        members = [uid(m["id"]) for r in day["requests"] for m in r["members"]]
        assert len(members) == len(set(members)) == expected_requesters
        assert sum(r["eligibility"]["allowsFrontStanding"] for r in day["requests"]) == math.ceil(len(day["requests"]) / 2)
        evaluated.update(members)
    assert evaluated == {uid(path["attendeeId"]) for path in data["attendee-paths"]}
    attempts = data["attempts"]
    planned = [r["simulationRequestId"] for day in days.values() for r in day["requests"]]
    assert planned == [a["simulationRequestId"] for a in attempts]
    counts = Counter(a["outcome"] for a in attempts)
    assert entry["integrity"]["unexpectedTechnicalFailureCount"] == counts["UnexpectedTechnicalFailure"]
    assert entry["integrity"]["knownConcurrencyConflictCount"] == counts["KnownConcurrencyConflict"]
    assert entry["integrity"]["invariantViolationCount"] == len(entry["integrity"]["violations"])
    attempted = sum(counts[name] for name in ("Successful", "DomainRejected", "KnownConcurrencyConflict", "UnexpectedTechnicalFailure"))
    normally_processed = counts["Successful"] + counts["DomainRejected"]
    outcomes = dict(
        attemptedRequests=attempted, normallyProcessedRequests=normally_processed,
        successfulRequests=counts["Successful"], domainRejectedRequests=counts["DomainRejected"],
        knownConcurrencyConflictRequests=counts["KnownConcurrencyConflict"],
        unexpectedTechnicalFailureRequests=counts["UnexpectedTechnicalFailure"],
        excludedInvalidInput=counts["ExcludedInvalidInput"],
        successRate=ratio(counts["Successful"], normally_processed),
        rejectionRate=ratio(counts["DomainRejected"], normally_processed),
        concurrencyConflictRate=ratio(counts["KnownConcurrencyConflict"], attempted),
        technicalErrorRate=ratio(counts["UnexpectedTechnicalFailure"], attempted))
    rejections = [a for a in attempts if a["outcome"] == "DomainRejected"]
    no_block = [a for a in rejections if a["diagnosticCode"] == "NO_CONTIGUOUS_SPOTS_AVAILABLE"]
    sufficient = [a for a in no_block if a["eligibleAvailableSpotCount"] is not None
                  and a["eligibleAvailableSpotCount"] >= a["groupSize"] and a["feasibleContiguousBlockFound"] is False]
    insufficient = [a for a in no_block if a["eligibleAvailableSpotCount"] is not None
                    and a["eligibleAvailableSpotCount"] < a["groupSize"] and a["feasibleContiguousBlockFound"] is False]
    daily = []
    for checkpoint in data["checkpoints"]:
        day = days[uid(checkpoint["festivalDayId"])]
        day_rejections = [a for a in rejections if a["festivalDayId"] == checkpoint["festivalDayId"]]
        daily.append(dict(dayNumber=day["dayNumber"], dailyRequesterCount=expected_requesters,
                          **checkpoint, rejectionReasons=dict(Counter(a["diagnosticCode"] for a in day_rejections))))
    durations = sorted(duration_ms(a["duration"]) for a in attempts if a["outcome"] != "ExcludedInvalidInput")
    recovery = data["metrics"]["recovery"]
    assert recovery["unresolvedEpisodeCount"] == recovery["recoveryIneligibleEpisodeCount"] + (
        recovery["recoveryEligibleEpisodeCount"] - recovery["recoveredEpisodeCount"])
    return dict(entry, metadata=metadata, requestOutcomes=outcomes, fairness=data["metrics"]["fairness"], recovery=recovery,
                eligibleUnresolvedEpisodeCount=recovery["recoveryEligibleEpisodeCount"] - recovery["recoveredEpisodeCount"],
                dailyCheckpoints=daily,
                rejectionAnalysis=dict(reasons=dict(Counter(a["diagnosticCode"] for a in rejections)),
                    noBlockWithSufficientEligibleCapacity=len(sufficient),
                    noBlockWithInsufficientEligibleCapacity=len(insufficient),
                    noBlockWithUnavailableDiagnosis=len(no_block) - len(sufficient) - len(insufficient)),
                exploratoryTiming=dict(sampleCount=len(durations), percentileMethod="nearest-rank",
                    p50Ms=durations[math.ceil(.5 * len(durations)) - 1] if durations else None,
                    p95Ms=durations[math.ceil(.95 * len(durations)) - 1] if durations else None,
                    meanMs=mean(durations) if durations else None, maxMs=max(durations) if durations else None))


def normalized(directory):
    data = {name[:-5]: json.loads((Path(directory) / name).read_text()) for name in FILES}
    # Generated durable IDs are outputs. Match request IDs to simulation IDs and
    # Assignment IDs to stable attendee/FestivalDay identities before comparison.
    request_ids = {uid(a["persistedRequestId"]): a["simulationRequestId"]
                   for a in data["attempts"] if a["persistedRequestId"] is not None}
    assignment_ids = {uid(e["assignmentId"]): uid(p["attendeeId"]) + "/" + uid(e["festivalDayId"])
                      for p in data["attendee-paths"] for e in p["entries"]}
    def normalize(value):
        if isinstance(value, dict):
            return {key: normalize(item) for key, item in value.items()
                    if key not in ("startedAt", "completedAt", "duration")}
        if isinstance(value, list):
            return [normalize(item) for item in value]
        if isinstance(value, str):
            return request_ids.get(value, assignment_ids.get(value, value))
        return value
    return {name: normalize(data[name]) for name in
            ("manifest", "attempts", "attendee-paths", "recovery-observations", "metrics")}


def main():
    index_path = Path(sys.argv[1])
    entries = json.loads(index_path.read_text())
    official = [summarize(entry) for entry in entries if entry["role"] == "official"]
    assert [(r["scenario"], r["seed"]) for r in official] == [
        (scenario, seed) for scenario in ("s1", "s2") for seed in (1001, 2001, 3001)]
    reproduction = [summarize(entry) for entry in entries if entry["role"] == "reproduction"]
    assert len(reproduction) == 1
    reference = next(r for r in official if (r["scenario"], r["seed"]) ==
                     (reproduction[0]["scenario"], reproduction[0]["seed"]))
    first, repeated = (normalized(r["evidencePath"]) for r in (reference, reproduction[0]))
    matches = {name: first[name] == repeated[name] for name in first}
    exact_file_matches = {name: reference["integrity"]["sha256"][name] == reproduction[0]["integrity"]["sha256"][name]
                          for name in ("definition.json", "manifest.json", "metrics.json")}
    matches["manifest"] &= exact_file_matches["manifest.json"]
    matches["metrics"] &= exact_file_matches["metrics.json"]
    stats = {}
    for scenario in ("s1", "s2"):
        runs = [r for r in official if r["scenario"] == scenario]
        stats[scenario] = {}
        fields = {"requestOutcomes": ("successRate", "rejectionRate"),
                  "fairness": ("goodOutcomeRate", "mediumOutcomeRate", "badOutcomeRate", "balancedPathRate", "acceptablePathRate", "notBalancedPathRate"),
                  "recovery": ("recoveryRate", "averageOpportunitiesToRecovery", "medianOpportunitiesToRecovery")}
        for family, names in fields.items():
            for name in names:
                values = [r[family][name] for r in runs if r[family][name] is not None]
                stats[scenario][name] = dict(min=min(values) if values else None,
                    mean=mean(values) if values else None, max=max(values) if values else None,
                    naRunCount=len(runs) - len(values))
    output = dict(executionDate="2026-11-03", simulatedFirstDate="2026-08-15", sourceIndex=str(index_path),
                  officialRuns=official, crossSeedStatistics=stats,
                  reproducibility=dict(referenceRunId=reference["runId"], repeatedRun=reproduction[0],
                      matches=matches, ignoredFields=["runId", "startedAt", "completedAt", "duration"],
                      exactFileMatches=exact_file_matches,
                      generatedIds="Matched by simulation request and attendee/FestivalDay identity.",
                      logicalSha256={name: hashlib.sha256(json.dumps(first[name], sort_keys=True).encode()).hexdigest()
                                     for name in first}))
    destination = index_path.parent / "summary.json"
    destination.write_text(json.dumps(output, indent=2) + "\n")
    print(destination)
    print(json.dumps(dict(crossSeedStatistics=stats, reproducibilityMatches=matches), indent=2))
    assert all(matches.values()), "Logical reproduction mismatch; evidence preserved for investigation."
    assert all(exact_file_matches.values()), "Definition/manifest/metrics bytes differ; inspect retained evidence."


if __name__ == "__main__":
    main()
