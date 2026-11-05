"""Read the three official S2 packages; never classify paths or derive episodes.

Run from the repository root. Output is deterministic JSON, including per-path
and per-episode traceability. Only the explicitly named output file is written.
"""

import argparse
import hashlib
import json
import sys
from collections import Counter
from datetime import datetime, time
from pathlib import Path


RESULTS = Path("docs/stage-5/s1-s2-representative-results.json")
SEEDS = (1001, 2001, 3001)
FILES = tuple(name + ".json" for name in (
    "run-metadata", "definition", "manifest", "attempts", "checkpoints",
    "attendee-paths", "recovery-observations", "metrics"))
QUALITY = {"Good": "G", "Medium": "M", "Bad": "B"}
THIRDS = ("first", "middle", "last")


def require(condition, message):
    if not condition:
        raise ValueError(message)


def uid(value):
    return value["value"]


def rate(count, total):
    return count / total if total else None


def distribution(values, denominator=None):
    counts = Counter(values)
    total = sum(counts.values()) if denominator is None else denominator
    return [{"value": value, "count": count, "rate": rate(count, total)}
            for value, count in sorted(counts.items())]


def sequence_third(sequence, count):
    require(count > 0 and 1 <= sequence <= count, "Invalid daily execution sequence")
    # Floor((sequence - 1) * 3 / count): half-open intervals in zero-based rank.
    return THIRDS[(sequence - 1) * 3 // count]


def arrival_band(timestamp, profile):
    clock = datetime.fromisoformat(timestamp.replace("Z", "+00:00")).time()
    for band in profile:
        if time.fromisoformat(band["start"]) <= clock < time.fromisoformat(band["end"]):
            return band["start"][:5] + "–" + band["end"][:5]
    raise ValueError(f"RequestedAt outside documented arrival bands: {timestamp}")


def group_band(size, profile):
    for band in profile:
        if band["minSize"] <= size <= band["maxSize"]:
            low, high = band["minSize"], band["maxSize"]
            return str(low) if low == high else f"{low}–{high}"
    raise ValueError(f"Group size outside documented profile: {size}")


def reconcile(paths, recovery, metrics, official):
    require(len({uid(p["attendeeId"]) for p in paths}) == len(paths), "Duplicate attendee path")
    require({uid(r["attendeeId"]) for r in recovery} == {uid(p["attendeeId"]) for p in paths}
            and len(recovery) == len(paths), "Recovery/path population mismatch")
    nb = [p for p in paths if p["classification"] == "NotBalanced"]
    episodes = [e for record in recovery for e in record["episodes"]]
    for record in recovery:
        for episode in record["episodes"]:
            require(episode["attendeeId"] == record["attendeeId"], "Recovery attendee identity mismatch")
            require(episode["recoveryEligible"] == (episode["observedOpportunities"] > 0),
                    "Stored recovery eligibility/opportunity mismatch")
    unresolved = [e for e in episodes if e["status"] == "Unresolved" and e["recoveryEligible"]]
    fairness, rec = metrics["fairness"], metrics["recovery"]
    require(metrics["fairness"] == official["fairness"] and rec == official["recovery"],
            "Exported metrics differ from official results")
    expected_nb = fairness["notBalancedPathCount"]
    expected_unresolved = rec["recoveryEligibleEpisodeCount"] - rec["recoveredEpisodeCount"]
    require(len(paths) == fairness["evaluatedAttendeeCount"], "Evaluated attendee count mismatch")
    require(sum(p["pathLength"] >= 2 for p in paths) == fairness["classifiablePathCount"],
            "Classifiable population mismatch")
    require(len(episodes) == rec["recoveryEpisodeCount"], "Recovery episode count mismatch")
    require(sum(e["status"] == "Recovered" for e in episodes) == rec["recoveredEpisodeCount"],
            "Recovered episode count mismatch")
    require(sum(e["status"] == "Unresolved" for e in episodes) == rec["unresolvedEpisodeCount"],
            "Unresolved episode count mismatch")
    require(len(nb) == expected_nb == official["fairness"]["notBalancedPathCount"],
            f"NotBalanced reconciliation failed: analyzed={len(nb)}, expected={expected_nb}")
    require(len(unresolved) == expected_unresolved == official["eligibleUnresolvedEpisodeCount"],
            f"Eligible unresolved reconciliation failed: analyzed={len(unresolved)}, expected={expected_unresolved}")
    return nb, unresolved, {
        "notBalanced": {"expected": expected_nb, "analyzed": len(nb)},
        "eligibleUnresolvedEpisodes": {"expected": expected_unresolved, "analyzed": len(unresolved)},
    }


def join_requests(manifest, attempts, paths, definition):
    """Join by stable attendee/day, checking the observed request membership.

    A participation is one attendee in one daily request, not a permanent group.
    All official S2 attempts succeeded; reject different input instead of silently
    treating rejected/failed attempts as persisted outcomes.
    """
    by_id = {a["simulationRequestId"]: a for a in attempts}
    require(len(by_id) == len(attempts), "Duplicate attempt identity")
    require(all(a["outcome"] == "Successful" for a in attempts), "Expected official all-successful S2 attempts")
    requests, membership, seen = [], {}, set()
    for day in manifest["dailyPlans"]:
        day_id = uid(day["festivalDayId"])
        planned = day["requests"]
        sequences = [r["sequence"] for r in planned]
        require(sequences == sorted(set(sequences)), "Daily request sequences must be unique and ordered")
        observed = [a["simulationRequestId"] for a in attempts if uid(a["festivalDayId"]) == day_id]
        require(observed == [r["simulationRequestId"] for r in planned], "Daily attempt order mismatch")
        for daily_position, request in enumerate(planned, 1):
            identity = request["simulationRequestId"]
            require(identity in by_id and identity not in seen, f"Missing/duplicate request: {identity}")
            seen.add(identity)
            attempt = by_id[identity]
            members = [uid(m["id"]) for m in request["members"]]
            require(len(members) == len(set(members)) == attempt["groupSize"], "Group size/member mismatch")
            require(set(members) == {uid(a) for a in attempt["attendeeIds"]}, "Attempt membership mismatch")
            require(day_id == uid(attempt["festivalDayId"])
                    and request["sequence"] == attempt["attemptSequence"], "Attempt day/sequence mismatch")
            require(attempt["persistedRequestId"] is not None, "Successful attempt missing persisted request")
            row = {
                "simulationRequestId": identity, "persistedRequestId": uid(attempt["persistedRequestId"]),
                "festivalDayId": day_id, "dayNumber": day["dayNumber"],
                "sequence": request["sequence"], "dailyPosition": daily_position, "dailyRequestCount": len(planned),
                "sequenceThird": sequence_third(daily_position, len(planned)),
                "requestedAt": request["requestedAt"], "assignedAt": request["assignedAt"],
                "arrivalBand": arrival_band(request["requestedAt"], definition["workload"]["arrivalProfile"]),
                "groupSize": len(members), "groupBand": group_band(len(members), definition["workload"]["groupSizeProfile"]),
                "allowsFrontStanding": request["eligibility"]["allowsFrontStanding"],
                "quality": attempt["selectedExperienceQuality"],
                "eligibleAvailableSpotCount": attempt["eligibleAvailableSpotCount"],
                "feasibleContiguousBlockFound": attempt["feasibleContiguousBlockFound"],
                "attendeeIds": members,
            }
            requests.append(row)
            for member in members:
                key = (member, day_id)
                require(key not in membership, "Duplicate attendee/day request membership")
                membership[key] = row
    require(seen == set(by_id), "Unplanned attempts")
    path_membership = set()
    assignment_ids = set()
    for path in paths:
        require(path["pathLength"] == len(path["entries"]), "Stored path length mismatch")
        require(path["entries"] == sorted(path["entries"], key=lambda e: (e["assignedAt"], uid(e["assignmentId"]))),
                "Stored path is not chronological")
        for entry in path["entries"]:
            key = (uid(path["attendeeId"]), uid(entry["festivalDayId"]))
            require(key in membership and key not in path_membership, "Missing/duplicate path/request join")
            request = membership[key]
            # PostgreSQL persists microseconds; manifest timestamps retain .NET's
            # seventh fractional digit. Compare at persisted precision.
            require(entry["experienceQuality"] == request["quality"]
                    and datetime.fromisoformat(entry["assignedAt"].replace("Z", "+00:00"))
                    == datetime.fromisoformat(request["assignedAt"].replace("Z", "+00:00")),
                    "Persisted path/attempt quality or business timestamp mismatch")
            require(uid(entry["assignmentId"]) not in assignment_ids, "Duplicate Assignment identity")
            assignment_ids.add(uid(entry["assignmentId"]))
            path_membership.add(key)
    require(path_membership == set(membership), "Persisted paths do not cover all successful participations")
    return requests, membership


def association(requests, paths, key):
    """Return both repeated participation and overlapping distinct-attendee rates."""
    by_attendee = {uid(p["attendeeId"]): p for p in paths}
    buckets = {}
    for request in requests:
        label = key(request)
        bucket = buckets.setdefault(label, {"requests": [], "attendees": set(), "participations": []})
        bucket["requests"].append(request)
        bucket["attendees"].update(request["attendeeIds"])
        bucket["participations"].extend((member, request["quality"]) for member in request["attendeeIds"])
    rows = []
    for label, bucket in sorted(buckets.items(), key=lambda item: str(item[0])):
        members = bucket["attendees"]
        nb = {a for a in members if by_attendee[a]["classification"] == "NotBalanced"}
        classifiable = {a for a in members if by_attendee[a]["pathLength"] >= 2}
        participations = bucket["participations"]
        nb_count = sum(a in nb for a, quality in participations)
        bad_count = sum(quality == "Bad" for a, quality in participations)
        nb_bad = sum(a in nb and quality == "Bad" for a, quality in participations)
        rows.append({
            "category": label, "requestCount": len(bucket["requests"]),
            "participationCount": len(participations), "notBalancedParticipationCount": nb_count,
            "notBalancedParticipationRate": rate(nb_count, len(participations)),
            "badParticipationCount": bad_count, "badParticipationRate": rate(bad_count, len(participations)),
            "notBalancedBadParticipationCount": nb_bad,
            "notBalancedBadParticipationRate": rate(nb_bad, len(participations)),
            "distinctAttendeeCount": len(members), "classifiableDistinctAttendeeCount": len(classifiable),
            "notBalancedDistinctAttendeeCount": len(nb),
            "notBalancedDistinctAttendeeRate": rate(len(nb), len(members)),
            "notBalancedClassifiableDistinctAttendeeRate": rate(len(nb), len(classifiable)),
        })
    return rows


def episode_detail(episode, path, membership):
    entries = path["entries"]
    starts = [i for i, e in enumerate(entries) if e["assignmentId"] == episode["startAssignmentId"]]
    require(len(starts) == 1, "Recovery opening Assignment missing from path")
    position = starts[0]
    opening, later = entries[position], entries[position + 1:]
    require(opening["experienceQuality"] == "Bad" and opening["festivalDayId"] == episode["startFestivalDayId"]
            and opening["assignedAt"] == episode["startAssignedAt"], "Stored episode opening mismatch")
    require(len(later) == episode["observedOpportunities"] and all(e["experienceQuality"] == "Bad" for e in later),
            "Stored eligible unresolved episode does not match later persisted entries")
    return dict(episode, openingPathPosition=position + 1, pathLength=path["pathLength"],
                openingDayNumber=membership[(uid(path["attendeeId"]), uid(opening["festivalDayId"]))]["dayNumber"],
                laterPersistedQualities=[e["experienceQuality"] for e in later],
                laterFestivalDayIds=[uid(e["festivalDayId"]) for e in later],
                finalQuality=entries[-1]["experienceQuality"],
                classification=path["classification"])


def analyze_run(official, data):
    paths, recovery = data["attendee-paths"], data["recovery-observations"]
    nb, unresolved, reconciliation = reconcile(paths, recovery, data["metrics"], official)
    require(data["manifest"]["metadata"]["seed"] == official["seed"], "Manifest seed mismatch")
    require(data["run-metadata"]["runId"] == official["runId"], "Run identity mismatch")
    requests, membership = join_requests(data["manifest"], data["attempts"], paths, data["definition"])
    nb_ids = {uid(p["attendeeId"]) for p in nb}
    by_attendee = {uid(p["attendeeId"]): p for p in paths}
    details = []
    for path in nb:
        joined = [dict(entry, request=membership[(uid(path["attendeeId"]), uid(entry["festivalDayId"]))])
                  for entry in path["entries"]]
        details.append(dict(path, qualitySequence="".join(QUALITY[e["experienceQuality"]] for e in path["entries"]),
                            assignments=joined))
    episodes = [episode_detail(e, by_attendee[uid(e["attendeeId"])], membership) for e in unresolved]
    episode_ids = {uid(e["attendeeId"]) for e in unresolved}
    overlap = len(nb_ids & episode_ids)
    shapes = Counter(p["qualitySequence"] for p in details)
    positions = Counter()
    for path in nb:
        qualities = [e["experienceQuality"] for e in path["entries"]]
        positions["firstAssignmentBad"] += qualities[0] == "Bad"
        positions["anyMiddleAssignmentBad"] += "Bad" in qualities[1:-1]
        positions["lastAssignmentBad"] += qualities[-1] == "Bad"
        positions["multipleBadOutcomes"] += qualities.count("Bad") >= 2
    nb_bad_requests = [r for r in requests if r["quality"] == "Bad" and nb_ids.intersection(r["attendeeIds"])]
    capacities = [r["eligibleAvailableSpotCount"] for r in nb_bad_requests]
    path_recovery = {uid(r["attendeeId"]): r["episodes"] for r in recovery}
    eligible_opening_days = Counter(
        membership[(uid(record["attendeeId"]), uid(episode["startFestivalDayId"]))]["dayNumber"]
        for record in recovery for episode in record["episodes"] if episode["recoveryEligible"])
    unresolved_opening_days = Counter(e["openingDayNumber"] for e in episodes)
    return {
        "seed": official["seed"], "runId": official["runId"], "evidencePath": official["evidencePath"],
        "reconciliation": reconciliation, "evaluatedAttendees": len(paths),
        "classifiableAttendees": data["metrics"]["fairness"]["classifiablePathCount"],
        "requestCount": len(requests), "participationCount": sum(len(r["attendeeIds"]) for r in requests),
        "pathLengths": [{"pathLength": length, "notBalancedCount": count,
                         "rateAmongNotBalanced": rate(count, len(nb)),
                         "allPathCount": sum(p["pathLength"] == length for p in paths),
                         "notBalancedRateWithinLength": rate(count, sum(p["pathLength"] == length for p in paths))}
                        for length, count in sorted(Counter(p["pathLength"] for p in nb).items())],
        "pathShapes": [{"sequence": shape, "count": count, "rateAmongNotBalanced": rate(count, len(nb))}
                       for shape, count in sorted(shapes.items(), key=lambda pair: (-pair[1], pair[0]))],
        "storedRubricCells": distribution((p["overallQualityBand"], p["direction"]) for p in nb),
        "badPosition": {name: {"count": count, "rateAmongNotBalanced": rate(count, len(nb))}
                        for name, count in sorted(positions.items())},
        "notBalancedFinalDay": distribution(membership[(uid(p["attendeeId"]), uid(p["entries"][-1]["festivalDayId"]))]["dayNumber"] for p in nb),
        "notBalancedFinalBadDay": distribution(membership[(uid(p["attendeeId"]), uid(p["entries"][-1]["festivalDayId"]))]["dayNumber"] for p in nb if p["entries"][-1]["experienceQuality"] == "Bad"),
        "notBalancedWithChangingGroupSize": sum(len({a["request"]["groupSize"] for a in p["assignments"]}) > 1 for p in details),
        "notBalancedWithChangingEligibility": sum(len({a["request"]["allowsFrontStanding"] for a in p["assignments"]}) > 1 for p in details),
        "associations": {
            "groupSize": association(requests, paths, lambda r: r["groupBand"]),
            "eligibility": association(requests, paths, lambda r: str(r["allowsFrontStanding"]).lower()),
            "day": association(requests, paths, lambda r: r["dayNumber"]),
            "sequenceThird": association(requests, paths, lambda r: r["sequenceThird"]),
            "arrivalBand": association(requests, paths, lambda r: r["arrivalBand"]),
            "eligibilityAndSequenceThird": association(requests, paths, lambda r: (str(r["allowsFrontStanding"]).lower(), r["sequenceThird"])),
            "groupSizeAndSequenceThird": association(requests, paths, lambda r: (r["groupBand"], r["sequenceThird"])),
        },
        "capacity": {"notBalancedBadRequestCount": len(nb_bad_requests),
                     "capturedEligibleCapacityCount": sum(c is not None for c in capacities),
                     "minEligibleAvailableSpots": min((c for c in capacities if c is not None), default=None),
                     "maxEligibleAvailableSpots": max((c for c in capacities if c is not None), default=None),
                     "feasibleBlockFoundCount": sum(r["feasibleContiguousBlockFound"] is True for r in nb_bad_requests)},
        "unresolved": {
            "episodeCount": len(episodes), "attendeeCount": len(episode_ids),
            "opportunityDepth": distribution(e["observedOpportunities"] for e in episodes),
            "openingDay": distribution(e["openingDayNumber"] for e in episodes),
            "openingDayExposure": [
                {"dayNumber": day, "eligibleEpisodeCount": eligible,
                 "eligibleUnresolvedCount": unresolved_opening_days[day],
                 "eligibleUnresolvedRate": rate(unresolved_opening_days[day], eligible)}
                for day, eligible in sorted(eligible_opening_days.items())],
            "openingPathPosition": distribution(e["openingPathPosition"] for e in episodes),
            "laterQualitySequences": distribution("".join(QUALITY[q] for q in e["laterPersistedQualities"]) for e in episodes),
            "finalQuality": distribution(e["finalQuality"] for e in episodes),
            "classification": distribution(e["classification"] for e in episodes),
        },
        "overlap": {"attendeeCount": overlap, "rateAmongNotBalanced": rate(overlap, len(nb)),
                    "rateAmongEligibleUnresolvedAttendees": rate(overlap, len(episode_ids)),
                    "notBalancedWithoutEligibleUnresolved": len(nb_ids - episode_ids),
                    "notBalancedWithRecoveredEpisode": sum(any(e["status"] == "Recovered" for e in path_recovery[a]) for a in nb_ids)},
        "notBalancedPaths": sorted(details, key=lambda p: uid(p["attendeeId"])),
        "eligibleUnresolvedEpisodes": sorted(episodes, key=lambda e: (uid(e["attendeeId"]), uid(e["startAssignmentId"]))),
    }


def analyze(root):
    summary_path = root / RESULTS
    summary_bytes = summary_path.read_bytes()
    official = json.loads(summary_bytes)
    runs = [r for r in official["officialRuns"] if r["scenario"] == "s2"]
    require(sorted(r["seed"] for r in runs) == list(SEEDS), "Expected exactly the three official S2 seeds")
    output = []
    for run in sorted(runs, key=lambda r: r["seed"]):
        try:
            directory = (root / run["evidencePath"]).resolve()
            require(directory.is_relative_to((root / "artifacts/simulations/s2-expected-baseline").resolve()),
                    "Expected retained S2 package location")
            data = {}
            for name in FILES:
                raw = (directory / name).read_bytes()
                require(hashlib.sha256(raw).hexdigest() == run["integrity"]["sha256"][name],
                        f"Evidence checksum mismatch: {name}")
                data[name[:-5]] = json.loads(raw)
            require(data["manifest"]["metadata"]["scenarioId"] == "s2-expected-baseline", "Wrong scenario")
            output.append(analyze_run(run, data))
        except (KeyError, TypeError, ValueError, OSError) as error:
            raise ValueError(f"S2/{run['seed']} ({run['runId']}): {error}") from error
    return {"sourceResults": str(RESULTS), "sourceResultsSha256": hashlib.sha256(summary_bytes).hexdigest(),
            "method": "Stored classifications/episodes; joined attendee-day participations; no simulation replay.",
            "runs": output}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True, help="Derived JSON destination outside evidence packages")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    try:
        output = args.output.resolve()
        require(not output.is_relative_to((root / "artifacts/simulations/s2-expected-baseline").resolve()),
                "Output must not modify a raw S2 evidence package")
        require(not output.exists(), "Output already exists; choose a new derived destination")
        result = analyze(root)
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(json.dumps(result, indent=2, ensure_ascii=False, allow_nan=False) + "\n", encoding="utf-8")
        print(args.output)
        for run in result["runs"]:
            print(f"S2/{run['seed']}: reconciled {run['reconciliation']}")
    except (KeyError, TypeError, ValueError, OSError) as error:
        print(f"S2 analysis failed: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
