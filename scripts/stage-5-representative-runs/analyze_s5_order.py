"""Paired retained S2 / reversed S5 analysis; no simulation or rubric replay.

Usage: python3 -B scripts/stage-5-representative-runs/analyze_s5_order.py INDEX --output SUMMARY
Run from the repository root. Raw packages are read-only and checked before/after.
"""

import argparse
import copy
import hashlib
import json
import math
from collections import Counter
from datetime import datetime
from pathlib import Path
from statistics import mean

from analyze_s2_paths import (RESULTS, SEEDS, THIRDS, arrival_band, association,
                              group_band, rate, require, sequence_third, uid)
from summarize import FILES, check_metadata, duration_ms, normalized


QUALITY = ("Good", "Medium", "Bad")
CLASSIFICATIONS = ("Balanced", "Acceptable", "NotBalanced", "InsufficientEvidence", "NoPath")
FIELDS = {
    "requestOutcomes": ("attemptedRequests", "successfulRequests", "domainRejectedRequests",
                        "knownConcurrencyConflictRequests", "unexpectedTechnicalFailureRequests",
                        "successRate", "rejectionRate", "concurrencyConflictRate", "technicalErrorRate"),
    "fairness": ("goodOutcomeRate", "mediumOutcomeRate", "badOutcomeRate", "balancedPathRate",
                 "acceptablePathRate", "notBalancedPathRate", "classifiablePathRate"),
    "recovery": ("recoveryEpisodeCount", "recoveredEpisodeCount", "unresolvedEpisodeCount",
                 "recoveryEligibleEpisodeCount", "recoveryRate", "averageOpportunitiesToRecovery",
                 "medianOpportunitiesToRecovery"),
}


def read_package(entry):
    directory = Path(entry["evidencePath"])
    require(sorted(p.name for p in directory.iterdir()) == sorted(FILES), f"Incomplete evidence: {directory}")
    data = {}
    require(set(entry["integrity"]["sha256"]) == set(FILES), "Incomplete checksum inventory")
    for name in FILES:
        raw = (directory / name).read_bytes()
        require(hashlib.sha256(raw).hexdigest() == entry["integrity"]["sha256"][name], f"Checksum mismatch: {directory}/{name}")
        data[name[:-5]] = json.loads(raw)
    check_metadata(data["run-metadata"], entry["metadata"])
    require(data["run-metadata"]["runId"] == entry["runId"], "RunId mismatch")
    for key in ("scenarioId", "seed", "generatorVersion", "executionMode", "concurrencyLevel",
                "workloadSourceScenarioId", "orderingTransformation"):
        require(data["run-metadata"].get(key) == data["manifest"]["metadata"].get(key), f"Manifest/run metadata mismatch: {key}")
    for key, value in (("generatorVersion", "v2"), ("engineVersion", "v0.3.0"),
                       ("executionMode", "Sequential"), ("concurrencyLevel", 1), ("rubricVersion", "v1")):
        require(data["run-metadata"][key] == value, f"Wrong run setting: {key}")
    require(data["run-metadata"]["seed"] == entry["seed"], "Seed mismatch")
    return data


def pair_integrity(s2, s5):
    """Compare every input field; allow only declared S5 metadata/order additions."""
    require(s2["definition"] == s5["definition"], "Source definition mismatch")
    left, right = copy.deepcopy(s2["manifest"]), copy.deepcopy(s5["manifest"])
    require(left["metadata"]["scenarioId"] == "s2-expected-baseline", "Wrong control scenario")
    require(right["metadata"]["scenarioId"] == "s5-adverse-arrival-order", "Wrong experimental scenario")
    require(right["metadata"].pop("workloadSourceScenarioId") == "s2-expected-baseline", "Wrong source")
    require(right["metadata"].pop("orderingTransformation") == "reverse-daily-execution-order", "Wrong transformation")
    right["metadata"]["scenarioId"] = left["metadata"]["scenarioId"]
    for key in ("workloadSourceScenarioId", "orderingTransformation"):
        require(left["metadata"].pop(key, None) is None, "Transformed control")
    require(len(left["dailyPlans"]) == len(right["dailyPlans"]) == 5, "Expected five days")
    for baseline, reversed_day in zip(left["dailyPlans"], right["dailyPlans"]):
        b, r = baseline["requests"], reversed_day["requests"]
        sequences = [request["sequence"] for request in b]
        require(sequences == sorted(set(sequences)), "Ambiguous baseline order")
        require(len(b) == len(r), "Daily request count mismatch")
        require([q["simulationRequestId"] for q in sorted(r, key=lambda q: q["executionSequence"])]
                == [q["simulationRequestId"] for q in b][::-1], "Not exact daily reverse")
        for original, changed, slot in zip(b, r, reversed(sequences)):
            require(original.pop("executionSequence", original["sequence"]) == original["sequence"], "Changed S2 order")
            require(changed.pop("executionSequence") == slot, "Wrong S5 execution slot")
        members = [uid(m["id"]) for q in b for m in q["members"]]
        require(len(members) == len(set(members)) == 4400, "Wrong daily requester cohort")
    require(left == right, "Workload mismatch (identity, membership, eligibility, timestamps, topology or baseline order)")
    return {name: True for name in ("sameWorkload", "sameRequests", "sameFestivalDays", "sameDailyCohorts",
            "sameGroupMembershipAndSizes", "sameTimestamps", "sameEligibility", "sameTopologyAndSpots",
            "preservedBaselineSequence", "exactDailyReversedExecution")}


def joined(data):
    """Validate dispatch and join persisted entries, retaining missing Assignments."""
    attempts = data["attempts"]
    by_id = {a["simulationRequestId"]: a for a in attempts}
    require(len(by_id) == len(attempts), "Duplicate attempts")
    paths = data["attendee-paths"]
    require(len(paths) == len({uid(p["attendeeId"]) for p in paths}), "Duplicate paths")
    entries = {}
    for path in paths:
        require(path["pathLength"] == len(path["entries"]), "Path length mismatch")
        for entry in path["entries"]:
            key = (uid(path["attendeeId"]), uid(entry["festivalDayId"]))
            require(key not in entries, "Duplicate attendee/day Assignment")
            entries[key] = entry
    rows, participation, expected_order = [], {}, []
    for day in data["manifest"]["dailyPlans"]:
        baseline = sorted(day["requests"], key=lambda q: q["sequence"])
        dispatched = sorted(baseline, key=lambda q: q.get("executionSequence", q["sequence"]))
        execution_rank = {q["simulationRequestId"]: i for i, q in enumerate(dispatched, 1)}
        expected_order.extend(q["simulationRequestId"] for q in dispatched)
        for rank, request in enumerate(baseline, 1):
            attempt = by_id[request["simulationRequestId"]]
            members = [uid(m["id"]) for m in request["members"]]
            require(len(members) == len(set(members)) == attempt["groupSize"], "Group size mismatch")
            require(set(members) == {uid(a) for a in attempt["attendeeIds"]}, "Attempt membership mismatch")
            require(attempt["festivalDayId"] == day["festivalDayId"], "Attempt day mismatch")
            require(attempt["attemptSequence"] == request.get("executionSequence", request["sequence"]), "Wrong dispatch sequence")
            require(attempt.get("baselineSequence", attempt["attemptSequence"]) == request["sequence"], "Wrong baseline sequence")
            row = dict(simulationRequestId=request["simulationRequestId"], attendeeIds=members,
                dayNumber=day["dayNumber"], festivalDayId=uid(day["festivalDayId"]), groupSize=len(members),
                baselineThird=sequence_third(rank, len(baseline)),
                executionThird=sequence_third(execution_rank[request["simulationRequestId"]], len(baseline)),
                arrivalBand=arrival_band(request["requestedAt"], data["definition"]["workload"]["arrivalProfile"]),
                groupBand=group_band(len(members), data["definition"]["workload"]["groupSizeProfile"]),
                allowsFrontStanding=request["eligibility"]["allowsFrontStanding"], quality=attempt["selectedExperienceQuality"])
            rows.append(row)
            for member in members:
                key = member, row["festivalDayId"]
                require(key not in participation, "Duplicate attendee/day request")
                entry = entries.get(key)
                require((entry is not None) == (attempt["outcome"] == "Successful"), "Outcome/persisted Assignment mismatch")
                if entry:
                    require(entry["experienceQuality"] == row["quality"], "Path/attempt quality mismatch")
                    require(datetime.fromisoformat(entry["assignedAt"].replace("Z", "+00:00")) ==
                            datetime.fromisoformat(request["assignedAt"].replace("Z", "+00:00")), "Persisted timestamp mismatch")
                participation[key] = {"quality": entry["experienceQuality"] if entry else None, "requestOutcome": attempt["outcome"]}
    require(expected_order == [a["simulationRequestId"] for a in attempts], "Actual dispatch is not planned order")
    require(set(entries) <= set(participation), "Unplanned persisted entries")
    require({uid(p["attendeeId"]) for p in paths} == {a for a, d in participation}, "Evaluated cohort mismatch")
    return rows, participation


def transitions(left, right, labels, ranked):
    require(set(left) == set(right), "Paired identity cohort mismatch")
    matrix = {a: {b: 0 for b in labels} for a in labels}
    counts = Counter()
    for key in left:
        a, b = left[key], right[key]
        matrix[a][b] += 1
        if a not in ranked or b not in ranked:
            counts["unranked"] += 1
        else:
            counts["unchanged" if a == b else "improved" if ranked.index(b) < ranked.index(a) else "worsened"] += 1
    denominator = sum(counts[k] for k in ("unchanged", "improved", "worsened"))
    return dict(matrix=matrix, pairedCount=len(left), rankedCount=denominator, unrankedCount=counts["unranked"],
                changes={k: dict(count=counts[k], rate=rate(counts[k], denominator)) for k in ("unchanged", "improved", "worsened")})


def quality_comparison(left, right):
    require(set(left) == set(right), "Participation cohort mismatch")
    both = {k for k in left if left[k]["quality"] is not None and right[k]["quality"] is not None}
    missing = {label: [] for label in ("assignedOnlyS2", "assignedOnlyS5", "assignedNeither")}
    for key in sorted(set(left) - both):
        label = "assignedOnlyS2" if left[key]["quality"] else "assignedOnlyS5" if right[key]["quality"] else "assignedNeither"
        missing[label].append(dict(attendeeId=key[0], festivalDayId=key[1], s2=left[key], s5=right[key]))
    return dict(transitions({k: left[k]["quality"] for k in both}, {k: right[k]["quality"] for k in both}, QUALITY, QUALITY),
                assignmentCoverage=dict(assignedBoth=len(both), **{k: len(v) for k, v in missing.items()}), missingCases=missing)


def identity_sensitivity(left, right):
    intersection, union = left & right, left | right
    return dict(s2Count=len(left), s5Count=len(right), intersectionCount=len(intersection),
                s2OnlyCount=len(left - right), s5OnlyCount=len(right - left), unionCount=len(union),
                overlapRate=rate(len(intersection), len(union)), churnRate=rate(len(left ^ right), len(union)),
                s2RetentionRate=rate(len(intersection), len(left)), s5NewRecipientRate=rate(len(right - left), len(right)))


def run_metrics(entry, data):
    counts = Counter(a["outcome"] for a in data["attempts"])
    known = ("Successful", "DomainRejected", "KnownConcurrencyConflict", "UnexpectedTechnicalFailure")
    require(set(counts) <= set(known) | {"ExcludedInvalidInput"}, "Unknown request outcome")
    attempted = sum(counts[k] for k in known)
    normal = counts["Successful"] + counts["DomainRejected"]
    outcomes = dict(attemptedRequests=attempted, normallyProcessedRequests=normal,
        successfulRequests=counts["Successful"], domainRejectedRequests=counts["DomainRejected"],
        knownConcurrencyConflictRequests=counts["KnownConcurrencyConflict"], unexpectedTechnicalFailureRequests=counts["UnexpectedTechnicalFailure"],
        excludedInvalidInput=counts["ExcludedInvalidInput"], successRate=rate(counts["Successful"], normal),
        rejectionRate=rate(counts["DomainRejected"], normal), concurrencyConflictRate=rate(counts["KnownConcurrencyConflict"], attempted),
        technicalErrorRate=rate(counts["UnexpectedTechnicalFailure"], attempted))
    integrity = entry["integrity"]
    require(integrity["unexpectedTechnicalFailureCount"] == counts["UnexpectedTechnicalFailure"]
            and integrity["knownConcurrencyConflictCount"] == counts["KnownConcurrencyConflict"], "Audit/request count mismatch")
    require(integrity["invariantViolationCount"] == len(integrity["violations"]), "Audit findings mismatch")
    require(integrity["completedFestivalDays"] == len(data["checkpoints"]) == 5, "Incomplete days")
    require({uid(d["festivalDayId"]) for d in data["manifest"]["dailyPlans"]} ==
            {uid(d["festivalDayId"]) for d in data["checkpoints"]}, "Checkpoint/day mismatch")
    fairness = data["metrics"]["fairness"]
    paths = data["attendee-paths"]
    require(len(paths) == fairness["evaluatedAttendeeCount"], "Evaluated denominator mismatch")
    require(sum(p["pathLength"] >= 2 for p in paths) == fairness["classifiablePathCount"], "Classifiable denominator mismatch")
    for classification in CLASSIFICATIONS[:3]:
        field = classification[0].lower() + classification[1:] + "PathCount"
        require(sum(p["classification"] == classification for p in paths) == fairness[field], "Stored classification count mismatch")
    entries = [e for p in data["attendee-paths"] for e in p["entries"]]
    require(len(entries) == fairness["persistedOutcomeCount"], "Fairness outcome denominator mismatch")
    require(all(sum(e["experienceQuality"] == q for e in entries) == fairness[q.lower() + "OutcomeCount"] for q in QUALITY), "Fairness counts mismatch")
    durations = sorted(duration_ms(a["duration"]) for a in data["attempts"] if a["outcome"] != "ExcludedInvalidInput")
    return dict(runId=entry["runId"], evidencePath=entry["evidencePath"], status=entry["status"], integrity=integrity,
                requestOutcomes=outcomes, **data["metrics"], exploratoryP95Ms=durations[math.ceil(.95 * len(durations)) - 1] if durations else None)


def recovery_sets(data):
    records = data["recovery-observations"]
    require(len(records) == len({uid(r["attendeeId"]) for r in records}), "Duplicate recovery attendee")
    require({uid(r["attendeeId"]) for r in records} == {uid(p["attendeeId"]) for p in data["attendee-paths"]}, "Recovery cohort mismatch")
    episodes = [e for r in records for e in r["episodes"]]
    rec = data["metrics"]["recovery"]
    require(len(episodes) == rec["recoveryEpisodeCount"], "Recovery episode mismatch")
    require(sum(e["status"] == "Recovered" for e in episodes) == rec["recoveredEpisodeCount"], "Recovered count mismatch")
    require(sum(e["status"] == "Unresolved" for e in episodes) == rec["unresolvedEpisodeCount"], "Unresolved count mismatch")
    eligible_unresolved = [e for e in episodes if e["status"] == "Unresolved" and e["recoveryEligible"]]
    require(len(eligible_unresolved) == rec["recoveryEligibleEpisodeCount"] - rec["recoveredEpisodeCount"], "Eligible unresolved mismatch")
    return dict(episodeRecipients={uid(e["attendeeId"]) for e in episodes},
                episodeOpeningAttendeeDays={(uid(e["attendeeId"]), uid(e["startFestivalDayId"])) for e in episodes},
                eligibleUnresolvedRecipients={uid(e["attendeeId"]) for e in eligible_unresolved},
                eligibleUnresolvedOpeningAttendeeDays={(uid(e["attendeeId"]), uid(e["startFestivalDayId"])) for e in eligible_unresolved})


def analyze_pair(control, experimental):
    data2, data5 = read_package(control), read_package(experimental)
    proof = pair_integrity(data2, data5)  # Must precede any paired interpretation.
    rows2, quality2 = joined(data2)
    rows5, quality5 = joined(data5)
    m2, m5 = run_metrics(control, data2), run_metrics(experimental, data5)
    require(m2["fairness"] == control["fairness"] and m2["recovery"] == control["recovery"], "Control summary metric mismatch")
    require(m2["requestOutcomes"] == control["requestOutcomes"], "Control request formula mismatch")
    classifications = [{uid(p["attendeeId"]): p["classification"] or "NoPath" for p in d["attendee-paths"]} for d in (data2, data5)]
    lengths5 = {uid(p["attendeeId"]): p["pathLength"] for p in data5["attendee-paths"]}
    recovery2, recovery5 = recovery_sets(data2), recovery_sets(data5)
    associations = {}
    for name, field in (("baselineThird", "baselineThird"), ("executionThird", "executionThird"),
                        ("originalArrivalBand", "arrivalBand"), ("groupSize", "groupBand"), ("allowsFrontStanding", "allowsFrontStanding")):
        associations[name] = {scenario: association([r for r in rows if r["quality"] is not None], data["attendee-paths"], lambda r: str(r[field]).lower())
                              for scenario, rows, data in (("s2", rows2, data2), ("s5", rows5, data5))}
    checkpoints = []
    for d2, d5 in zip(data2["checkpoints"], data5["checkpoints"]):
        require(d2["festivalDayId"] == d5["festivalDayId"], "Checkpoint day mismatch")
        fields = ("goodAvailableSpotCount", "mediumAvailableSpotCount", "badAvailableSpotCount", "totalAvailableSpotCount")
        checkpoints.append(dict(dayNumber=len(checkpoints) + 1, s2={k: d2[k] for k in fields}, s5={k: d5[k] for k in fields}, delta={k: d5[k] - d2[k] for k in fields}))
    deltas = {family: {k: m5[family][k] - m2[family][k] if m5[family][k] is not None and m2[family][k] is not None else None
                      for k in fields} for family, fields in FIELDS.items()}
    result = dict(seed=control["seed"], pairIntegrity=proof, s2=m2, s5=m5, deltas=deltas,
        quality=quality_comparison(quality2, quality5),
        badRecipients=identity_sensitivity({a for (a, d), q in quality2.items() if q["quality"] == "Bad"},
                                          {a for (a, d), q in quality5.items() if q["quality"] == "Bad"}),
        badAttendeeDays=identity_sensitivity({k for k, q in quality2.items() if q["quality"] == "Bad"},
                                            {k for k, q in quality5.items() if q["quality"] == "Bad"}),
        pathTransitions=transitions(*classifications, CLASSIFICATIONS, CLASSIFICATIONS[:3]),
        pathCoverageTransitions=[dict(s2Length=a, s5Length=b, count=n) for (a, b), n in sorted(Counter(
            (p["pathLength"], lengths5[uid(p["attendeeId"])])
            for p in data2["attendee-paths"]).items())],
        notBalancedRecipients=identity_sensitivity(*({a for a, c in cs.items() if c == "NotBalanced"} for cs in classifications)),
        recoverySensitivity={k: identity_sensitivity(recovery2[k], recovery5[k]) for k in recovery2},
        associations=associations, dailyCapacity=checkpoints)
    # Re-read every source after analysis, checking the original recorded hashes.
    read_package(control)
    read_package(experimental)
    return result


def analyze(index_path):
    controls = json.loads(RESULTS.read_text())
    index = json.loads(index_path.read_text())
    official = [e for e in index if e["role"] == "official"]
    require([(e["scenario"], e["seed"]) for e in official] == [("s5", s) for s in SEEDS], "Wrong official S5 matrix")
    repeated = [e for e in index if e["role"] == "reproduction"]
    require(len(index) == 4 and len(repeated) == 1 and repeated[0]["scenario"] == "s5" and repeated[0]["seed"] == 1001, "Wrong reproduction protocol")
    pairs = []
    for entry in official:
        control = next(e for e in controls["officialRuns"] if e["scenario"] == "s2" and e["seed"] == entry["seed"])
        require(entry["pairedControl"] == control, "Wrong retained control provenance")
        pairs.append(analyze_pair(control, entry))
    read_package(repeated[0])
    first, repeat = (normalized(e["evidencePath"]) for e in (official[0], repeated[0]))
    matches = {k: first[k] == repeat[k] for k in first}
    require(all(matches.values()), "Logical reproduction mismatch; preserve evidence for investigation")
    exact = {k: official[0]["integrity"]["sha256"][k] == repeated[0]["integrity"]["sha256"][k]
             for k in ("definition.json", "manifest.json", "metrics.json")}
    require(all(exact.values()), "Reproduction definition/manifest/metrics byte mismatch")
    read_package(official[0])
    read_package(repeated[0])
    cross = {}
    for family, fields in FIELDS.items():
        for field in fields:
            values = [p["s5"][family][field] for p in pairs if p["s5"][family][field] is not None]
            cross[field] = dict(min=min(values) if values else None, mean=mean(values) if values else None,
                                max=max(values) if values else None, naRunCount=len(pairs) - len(values))
    pooled = {a: {b: sum(p["quality"]["matrix"][a][b] for p in pairs) for b in QUALITY} for a in QUALITY}
    return dict(sourceIndex=str(index_path), sourceIndexSha256=hashlib.sha256(index_path.read_bytes()).hexdigest(),
                sourceS2Results=str(RESULTS), sourceS2ResultsSha256=hashlib.sha256(RESULTS.read_bytes()).hexdigest(),
                pairs=pairs, pooledQualityMatrix=pooled, crossSeedS5=cross,
                reproducibility=dict(referenceRunId=official[0]["runId"], repeatedRun=repeated[0], logicalMatches=matches,
                    exactFileMatches=exact, ignored="Observed start/end/duration and generated durable IDs normalized by stable identities.",
                    logicalSha256={k: hashlib.sha256(json.dumps(v, sort_keys=True).encode()).hexdigest() for k, v in first.items()}),
                checksumsUnchangedAfterAnalysis=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("index", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.resolve().is_relative_to(Path("artifacts/simulations").resolve()), "Derived output must be outside raw evidence")
    require(not args.output.exists(), "Output exists; choose a fresh destination")
    result = analyze(args.index)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("x", encoding="utf-8") as output:
        output.write(json.dumps(result, indent=2, ensure_ascii=False, allow_nan=False) + "\n")
    print(args.output)


if __name__ == "__main__":
    main()
