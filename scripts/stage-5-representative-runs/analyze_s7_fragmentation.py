"""Offline exact-pair S7 analysis. Read-only raw evidence; no Engine/rubric replay."""

import argparse
import hashlib
import json
import math
from collections import Counter
from pathlib import Path
from statistics import mean, median

from analyze_s2_paths import SEEDS, THIRDS, rate, reconcile, require, uid
from analyze_s5_order import FIELDS, QUALITY, joined, read_package, run_metrics
from analyze_s6_groups import (BUCKETS, CAPACITY_FIELDS, S2_SHARES, metric_delta,
                               normal_workload, number, percentage, segment, table)
from summarize import duration_ms, normalized

VARIANTS = ("s7-control", "s7-fragmented")
CAUSES = ("CapacityShortage", "ContiguousBlockUnavailable", "N/A")
OPENING = dict(zip(CAPACITY_FIELDS, (1402, 1800, 796, 3998)))
ZONE_QUALITY = {"FrontStanding": "Good", "MiddleCenter": "Good", "MiddleLeft": "Medium",
                "MiddleRight": "Medium", "UpperCenter": "Medium", "UpperLeft": "Bad", "UpperRight": "Bad"}
ZONE_OCCUPIED = dict(zip(ZONE_QUALITY, (150, 200, 150, 150, 150, 100, 100)))
PROVENANCE = ("workloadSourceScenarioId", "fragmentationPatternVersion",
              "openingOccupiedSpotCount", "workloadSourceManifestSha256")


def opening(data):
    manifest = data["manifest"]
    metadata = manifest["metadata"]
    require(metadata["scenarioId"] in VARIANTS, "Wrong S7 variant")
    require(metadata["workloadSourceScenarioId"] == "s2-expected-baseline"
            and metadata["fragmentationPatternVersion"] == "s7-preoccupation-v1"
            and metadata["openingOccupiedSpotCount"] == 1000
            and metadata.get("orderingTransformation") is None, "Wrong S7 provenance")
    source_hash = metadata["workloadSourceManifestSha256"]
    require(len(source_hash) == 64 and all(c in "0123456789abcdef" for c in source_hash), "Invalid source checksum")
    if "run-metadata" in data:
        require(all(data["run-metadata"][key] == metadata[key] for key in PROVENANCE), "S7 run/manifest provenance mismatch")
    # Reuse unchanged S2 workload validation without treating supporting setup as
    # evaluated history or requiring a second materialization/execution of S2.
    source = dict(definition=data["definition"], manifest=dict(manifest,
        setup=dict(supportingAttendees=[], completedRequests=[], assignments=[]),
        metadata=dict(metadata, workloadSourceScenarioId=None)))
    normal_workload(source)
    profile = data["definition"]["workload"]["groupSizeProfile"]
    require([(b["minSize"], b["maxSize"], b["percentage"]) for b in profile] ==
            [(lo, hi, share) for (lo, hi), share in zip(((1, 1), (2, 2), (3, 3), (4, 4), (5, 5), (6, 7), (8, 10)), S2_SHARES)],
            "S7 must retain S2 group profile")
    require(data["definition"]["scenarioId"] == "s2-expected-baseline", "Wrong source definition")
    require(all(data["definition"][key] == metadata[key] for key in
                ("seed", "generatorVersion", "executionMode", "concurrencyLevel")), "Source definition/manifest metadata mismatch")
    catalog, setup = manifest["catalog"], manifest["setup"]
    registered = {uid(a["id"]) for a in catalog["attendees"]}
    supporters = {uid(a["id"]) for a in setup["supportingAttendees"]}
    require(len(supporters) == len(setup["supportingAttendees"]) == 5000 and not supporters & registered,
            "Supporting population leaks into evaluated catalog")
    require(len(setup["assignments"]) == len(setup["completedRequests"]) == 5000, "Incomplete supporting setup")
    require({uid(a["attendeeId"]) for a in setup["assignments"]} == supporters, "Support identities not day-scoped")
    spots = {uid(s["code"]): s for s in catalog["spots"]}
    zones = {uid(z["id"]): z for z in catalog["zones"]}
    rows = {}
    for spot in spots.values():
        key = (uid(spot["zoneId"]), uid(spot["rowCode"]))
        rows.setdefault(key, []).append(spot)
    requests = {uid(r["id"]): r for r in setup["completedRequests"]}
    require(len(requests) == 5000, "Duplicate setup request")
    result, first_codes = [], None
    for day in manifest["dailyPlans"]:
        assignments = [a for a in setup["assignments"] if a["festivalDayId"] == day["festivalDayId"]]
        codes = {uid(a["spotCode"]) for a in assignments}
        require(len(codes) == len(assignments) == 1000 and codes <= spots.keys(), "Invalid daily occupied set")
        require(first_codes is None or codes == first_codes, "Daily pattern accumulates/changes")
        first_codes = codes
        occupied_rows, occupied_zones = Counter(), Counter()
        for a in assignments:
            spot = spots[uid(a["spotCode"])]
            require(a["zoneId"] == spot["zoneId"] and a["rowCode"] == spot["rowCode"]
                    and a["spotNumber"] == spot["number"], "Setup Spot identity mismatch")
            require(a["experienceQuality"] == ZONE_QUALITY[zones[uid(a["zoneId"])]["type"]], "Setup quality mismatch")
            request = requests[uid(a["assignmentRequestId"])]
            require(request["festivalDayId"] == a["festivalDayId"] and len(request["attendeeCodes"]) == 1,
                    "Setup request/day mismatch")
            occupied_rows[(uid(a["zoneId"]), uid(a["rowCode"]))] += 1
            occupied_zones[zones[uid(a["zoneId"])]["type"]] += 1
        require(dict(occupied_zones) == ZONE_OCCUPIED, "Opening Zone quotas differ from contract")
        remaining_quality = Counter(ZONE_QUALITY[zones[uid(s["zoneId"])]["type"]]
                                    for code, s in spots.items() if code not in codes)
        require([remaining_quality[q] for q in QUALITY] == [1402, 1800, 796], "Wrong opening quality capacity")
        largest = {}
        for key, row in rows.items():
            run = longest = 0
            for spot in sorted(row, key=lambda s: uid(s["number"])):
                run = 0 if uid(spot["code"]) in codes else run + 1
                longest = max(longest, run)
            largest["/".join(key)] = longest
        result.append(dict(dayNumber=day["dayNumber"], occupied=1000, available=3998,
                           remainingQuality=dict(remaining_quality), zoneQuotas=dict(sorted(occupied_zones.items())),
                           rowQuotas={"/".join(k): v for k, v in sorted(occupied_rows.items())},
                           largestAvailableRuns=largest, largestRunSum=sum(largest.values()),
                           minLargestRun=min(largest.values()), maxLargestRun=max(largest.values())))
    return result


def pair_integrity(control, fragmented):
    require(control["definition"] == fragmented["definition"], "Source definition mismatch")
    left, right = control["manifest"], fragmented["manifest"]
    require(left["metadata"]["scenarioId"] == VARIANTS[0] and right["metadata"]["scenarioId"] == VARIANTS[1], "Wrong pair direction")
    require({k: v for k, v in left["metadata"].items() if k != "scenarioId"} ==
            {k: v for k, v in right["metadata"].items() if k != "scenarioId"}, "Pair provenance mismatch")
    require(left["catalog"] == right["catalog"] and left["dailyPlans"] == right["dailyPlans"], "Exact workload/catalog pair mismatch")
    a, b = left["setup"], right["setup"]
    require(a["supportingAttendees"] == b["supportingAttendees"] and a["completedRequests"] == b["completedRequests"], "Setup support mismatch")
    require(len(a["assignments"]) == len(b["assignments"]), "Setup Assignment count mismatch")
    for x, y in zip(a["assignments"], b["assignments"]):
        require({k: v for k, v in x.items() if k not in ("spotCode", "spotNumber")} ==
                {k: v for k, v in y.items() if k not in ("spotCode", "spotNumber")},
                "Nonspatial supporting Assignment difference")
    openings = [opening(data) for data in (control, fragmented)]
    for c, f in zip(*openings):
        require(all(c[k] == f[k] for k in ("dayNumber", "occupied", "available", "remainingQuality", "zoneQuotas", "rowQuotas")),
                "Opening capacity mismatch")
        require(c["largestAvailableRuns"].keys() == f["largestAvailableRuns"].keys()
                and all(f["largestAvailableRuns"][k] < value for k, value in c["largestAvailableRuns"].items()),
                "No shortening of available runs")
    return dict(exactWorkloadCatalogAndSequences=True, supportingIdentitiesIsolatedAndPaired=True,
                equalDailyRowZoneQualityOpeningCapacity=True, onlyOccupiedArrangementDiffers=True,
                controlOpening=openings[0], fragmentedOpening=openings[1])


def diagnosis(row):
    capacity = row.get("eligibleAvailableSpotCount")
    if row.get("diagnosticCode") != "NO_CONTIGUOUS_SPOTS_AVAILABLE" or row.get("feasibleContiguousBlockFound") is not False or capacity is None:
        return "N/A"
    return "CapacityShortage" if capacity < row["groupSize"] else "ContiguousBlockUnavailable"


def summarize_segment(rows, paths):
    summary = segment(rows, paths)
    counts = Counter(diagnosis(row) for row in rows if row["outcome"] == "DomainRejected")
    rejected_count = summary["domainRejectedRequests"]
    attempted = len([r for r in rows if r["outcome"] != "ExcludedInvalidInput"])
    require(sum(counts.values()) == rejected_count, "Rejection decomposition mismatch")
    return dict(**summary, attemptedRequests=attempted, causes={cause: dict(count=counts[cause],
        rateAmongDomainRejected=rate(counts[cause], rejected_count), rateAmongAttemptedRequests=rate(counts[cause], attempted),
        rateAmongNormallyProcessedRequests=rate(counts[cause], summary["normallyProcessedRequests"])) for cause in CAUSES})


def capacity_summary(rows):
    result = {}
    for field in ("remainingTotalBeforeRequest", "eligibleAvailableSpotCount"):
        values = [row[field] for row in rows if row[field] is not None]
        result[field] = dict(observed=len(values), missing=len(rows) - len(values),
                             min=min(values) if values else None, median=median(values) if values else None,
                             max=max(values) if values else None)
    return result


def event(row):
    if row is None:
        return None
    return {k: row[k] for k in ("simulationRequestId", "dayNumber", "attemptSequence", "executionThird",
        "dailyExecutionPosition", "groupSize", "remainingTotalBeforeRequest", "eligibleAvailableSpotCount",
        "feasibleContiguousBlockFound", "cause")}


def analyze_run(entry, data):
    require(entry["status"] == "Completed" and entry["integrity"]["invariantViolationCount"] == 0
            and not entry["integrity"]["violations"], "Run has integrity findings")
    metrics = run_metrics(entry, data)
    outcomes = metrics["requestOutcomes"]
    require(outcomes["knownConcurrencyConflictRequests"] == outcomes["unexpectedTechnicalFailureRequests"] == outcomes["excludedInvalidInput"] == 0,
            "Run has technical/conflict/exclusion outcomes; causal analysis stopped")
    recovery = metrics["recovery"]
    reconcile(data["attendee-paths"], data["recovery-observations"], data["metrics"],
              dict(**data["metrics"], eligibleUnresolvedEpisodeCount=recovery["recoveryEligibleEpisodeCount"] - recovery["recoveredEpisodeCount"]))
    rows, _ = joined(data)
    attempts = {a["simulationRequestId"]: a for a in data["attempts"]}
    remaining, ranks = {}, Counter()
    for row in rows:
        attempt = attempts[row["simulationRequestId"]]
        row.update({key: attempt[key] for key in ("outcome", "diagnosticCode", "eligibleAvailableSpotCount", "feasibleContiguousBlockFound", "attemptSequence")})
        day = row["dayNumber"]
        ranks[day] += 1
        row["dailyExecutionPosition"] = ranks[day]
        row["remainingTotalBeforeRequest"] = remaining.get(day, 3998)
        row["cause"] = diagnosis(row) if row["outcome"] == "DomainRejected" else None
        if row["eligibleAvailableSpotCount"] is not None:
            require(0 <= row["eligibleAvailableSpotCount"] <= row["remainingTotalBeforeRequest"], "Inconsistent evaluation capacity")
        remaining[day] = row["remainingTotalBeforeRequest"] - (row["groupSize"] if row["outcome"] == "Successful" else 0)
    paths = data["attendee-paths"]
    daily = []
    for day in data["manifest"]["dailyPlans"]:
        number = day["dayNumber"]
        selected = [r for r in rows if r["dayNumber"] == number]
        checkpoint = next(c for c in data["checkpoints"] if c["festivalDayId"] == day["festivalDayId"])
        require(remaining[number] == checkpoint["totalAvailableSpotCount"] >= 0, "Closing capacity mismatch")
        require(checkpoint["evaluatedRequestCount"] == len(selected)
                and checkpoint["successfulAttemptCount"] == sum(r["outcome"] == "Successful" for r in selected)
                and checkpoint["rejectedAttemptCount"] == sum(r["outcome"] == "DomainRejected" for r in selected), "Checkpoint outcomes mismatch")
        for quality, field in zip(QUALITY, CAPACITY_FIELDS):
            used = sum(r["groupSize"] for r in selected if r["outcome"] == "Successful" and r["quality"] == quality)
            require(checkpoint[field] == OPENING[field] - used, "Quality consumption mismatch")
        for band in data["definition"]["workload"]["groupSizeProfile"]:
            require(abs(sum(band["minSize"] <= r["groupSize"] <= band["maxSize"] for r in selected) - len(selected) * band["percentage"]) < 1,
                    "Group profile quota mismatch")
        for band in data["definition"]["workload"]["arrivalProfile"]:
            label = band["start"][:5] + "–" + band["end"][:5]
            require(abs(sum(r["arrivalBand"] == label for r in selected) - len(selected) * band["percentage"]) < 1, "Arrival profile quota mismatch")
        rejected = [r for r in selected if r["outcome"] == "DomainRejected"]
        daily.append(dict(dayNumber=number, successfulAttendeeAssignments=3998 - remaining[number],
            **{key: checkpoint[key] for key in CAPACITY_FIELDS}, unusedCapacityRate=rate(remaining[number], 3998),
            consumedBeforeFirstRejection=3998 - rejected[0]["remainingTotalBeforeRequest"] if rejected else None,
            firstRejection=event(next(iter(rejected), None)),
            firstContiguousBlockUnavailable=event(next((r for r in rejected if r["cause"] == CAUSES[1]), None))))
    require(entry["integrity"]["persistedAssignmentCount"] == 5000 + metrics["fairness"]["persistedOutcomeCount"], "Supporting/evaluated Assignment count mismatch")
    require(entry["integrity"]["persistedRequestCount"] == 5000 + len(rows), "Supporting/evaluated request count mismatch")
    rejected = [r for r in rows if r["outcome"] == "DomainRejected"]
    population = dict(registeredAttendees=len(data["manifest"]["catalog"]["attendees"]), requestingAttendees=len(paths),
        participatingAttendees=sum(p["pathLength"] > 0 for p in paths),
        attendeesWithOnlyRejectedOrFailedAttempts=sum(p["pathLength"] == 0 for p in paths),
        noPersistedPath=metrics["fairness"]["noPersistedPathCount"], insufficientEvidence=metrics["fairness"]["insufficientEvidenceCount"],
        classifiable=metrics["fairness"]["classifiablePathCount"])
    rejected_people = {a for r in rejected for a in r["attendeeIds"]}
    last_success = {}
    for row in rows:
        if row["outcome"] == "Successful":
            for attendee in row["attendeeIds"]:
                last_success[attendee] = max(last_success.get(attendee, 0), row["dayNumber"])
    later_success = {a for r in rejected for a in r["attendeeIds"] if last_success.get(a, 0) > r["dayNumber"]}
    durations = sorted(duration_ms(a["duration"]) for a in data["attempts"])
    def categories(labels, key):
        return [dict(category=label, **summarize_segment([r for r in rows if key(r) == label], paths)) for label in labels]
    summary = dict(**metrics, population=population, eligibleUnresolved=recovery["recoveryEligibleEpisodeCount"] - recovery["recoveredEpisodeCount"],
        rejections=summarize_segment(rows, paths)["causes"],
        firstRejection=event(next(iter(rejected), None)),
        rejectionCapacity={cause: capacity_summary([r for r in rejected if r["cause"] == cause]) for cause in CAUSES},
        groupSize=categories(BUCKETS, lambda r: r["groupBand"]),
        supplementarySize=categories(("1–2", "3–4", "5+"), lambda r: "1–2" if r["groupSize"] <= 2 else "3–4" if r["groupSize"] <= 4 else "5+"),
        executionThird=categories(THIRDS, lambda r: r["executionThird"]),
        groupSizeByThird=categories([b + "/" + t for b in BUCKETS for t in THIRDS], lambda r: r["groupBand"] + "/" + r["executionThird"]),
        dailyCapacity=daily, rejectedAttendeeExposure=dict(everRejected=len(rejected_people), laterSuccessfullyAssigned=len(later_success),
            laterSuccessfullyAssignedRate=rate(len(later_success), len(rejected_people))),
        exploratoryTiming=dict(sampleCount=len(durations), method="nearest-rank", **{
            f"p{p}Ms": durations[math.ceil(p / 100 * len(durations)) - 1] if durations else None for p in (50, 95, 99)}))
    return summary, rows


def paired_requests(control, fragmented):
    left, right = ({r["simulationRequestId"]: r for r in rows} for rows in (control, fragmented))
    require(left.keys() == right.keys(), "Unpaired request identities")
    labels = ("Successful", "DomainRejected")
    matrix = {a: {b: 0 for b in labels} for a in labels}
    harm, reverse = Counter(), Counter()
    for key, row in left.items():
        other = right[key]
        matrix[row["outcome"]][other["outcome"]] += 1
        if row["outcome"] == "Successful" and other["outcome"] == "DomainRejected":
            harm[other["cause"]] += 1
        if row["outcome"] == "DomainRejected" and other["outcome"] == "Successful":
            reverse[row["cause"]] += 1
    return dict(requestCount=len(left), matrix=matrix,
        controlSuccessFragmentedRejected=dict(total=sum(harm.values()), causes={k: harm[k] for k in CAUSES}),
        controlRejectedFragmentedSuccess=dict(total=sum(reverse.values()), causes={k: reverse[k] for k in CAUSES}))


def reproduction(reference, repeated):
    first, second = (read_package(e) for e in (reference, repeated))
    opening(second)
    summary, rows = analyze_run(repeated, second)
    reference_summary, _ = analyze_run(reference, first)
    left, right = (normalized(e["evidencePath"]) for e in (reference, repeated))
    matches = {key: left[key] == right[key] for key in left}
    runtime_fields = {"runId", "evidencePath", "integrity", "exploratoryP95Ms", "exploratoryTiming"}
    matches["derivedAnalysisExceptRuntime"] = {k:v for k,v in reference_summary.items() if k not in runtime_fields} == {
        k:v for k,v in summary.items() if k not in runtime_fields}
    matches["metadataExceptRuntime"] = {k: v for k, v in first["run-metadata"].items() if k not in ("runId", "startedAt", "completedAt")} == {
        k: v for k, v in second["run-metadata"].items() if k not in ("runId", "startedAt", "completedAt")}
    exact = {name: reference["integrity"]["sha256"][name] == repeated["integrity"]["sha256"][name]
             for name in ("definition.json", "manifest.json", "checkpoints.json", "metrics.json")}
    require(all(matches.values()) and all(exact.values()), "S7 reproduction mismatch")
    return dict(referenceRunId=reference["runId"], repeatedRun=repeated, logicalMatches=matches, byteIdentical=exact), rows


def analyze(index_path):
    index = json.loads(index_path.read_text())
    require([(e["role"], e["scenario"], e["seed"]) for e in index] ==
            [("official", v, seed) for seed in SEEDS for v in VARIANTS] + [("reproduction", v, 1001) for v in VARIANTS], "Wrong official/reproduction matrix")
    require(len({e["runId"] for e in index}) == 8, "RunIds must be distinct")
    harness_hashes = {e["executionHarnessSha256"] for e in index}
    require(len(harness_hashes) == 1, "Execution harness changed within the run set")
    source_revisions = {e["metadata"]["repositoryCommitSha"] for e in index}
    require(len(source_revisions) == 1 and None not in source_revisions, "Missing/mixed protected source revision")
    pairs = []
    all_data = [read_package(e) for e in index]
    for i, seed in enumerate(SEEDS):
        c, f = index[i * 2:i * 2 + 2]
        left_data, right_data = all_data[i * 2:i * 2 + 2]
        require(f["pairedControl"] == c, "Wrong paired control provenance")
        proof = pair_integrity(left_data, right_data)  # Always before interpreting outcomes.
        left, left_rows = analyze_run(c, left_data)
        right, right_rows = analyze_run(f, right_data)
        deltas = {family: {key: right[family][key] - left[family][key]
            if right[family][key] is not None and left[family][key] is not None else None for key in keys} for family, keys in FIELDS.items()}
        delta = deltas["requestOutcomes"]["rejectionRate"]
        pairs.append(dict(seed=seed, integrity=proof, control=left, fragmented=right, deltas=deltas,
            fragmentationImpactPercentagePoints=delta * 100,
            relativeRejectionIncrease=rate(delta, left["requestOutcomes"]["rejectionRate"]),
            strongEvidenceDelta={key: right["rejections"][CAUSES[1]][key] - left["rejections"][CAUSES[1]][key]
                if right["rejections"][CAUSES[1]][key] is not None and left["rejections"][CAUSES[1]][key] is not None else None
                for key in ("count", "rateAmongDomainRejected", "rateAmongAttemptedRequests")},
            pairedRequests=paired_requests(left_rows, right_rows)))
    pair_integrity(all_data[6], all_data[7])
    require(index[7]["pairedControl"] == index[6], "Wrong reproduction control provenance")
    repro, repeated_rows = [], []
    for first, repeated in zip(index[:2], index[6:]):
        proof, rows = reproduction(first, repeated)
        repro.append(proof)
        repeated_rows.append(rows)
    repeated_pair = paired_requests(*repeated_rows)
    require(repeated_pair == pairs[0]["pairedRequests"], "Paired analysis reproduction mismatch")
    impacts = [p["fragmentationImpactPercentagePoints"] for p in pairs]
    for entry in index:
        read_package(entry)
    return dict(executionDate="2026-11-07", timezone="America/Lima", simulatedFirstDate="2026-08-15",
        sourceIndex=str(index_path), sourceIndexSha256=hashlib.sha256(index_path.read_bytes()).hexdigest(),
        executionHarnessSha256=next(iter(harness_hashes)), protectedSourceRevision=next(iter(source_revisions)),
        analysisToolSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
        runs=index, pairs=pairs, fragmentationImpactSummary=dict(min=min(impacts), mean=mean(impacts), max=max(impacts)),
        reproducibility=dict(variants=repro, pairedRequestAnalysisEqual=True,
            normalization="Only RunId, observed start/end/duration and generated durable IDs are excluded from logical comparison. Definition/manifest (including setup), checkpoints and metrics require byte equality."),
        checksumsUnchangedAfterAnalysis=True)


def experimental_answers(result):
    pairs = result["pairs"]
    def seeds(fn):
        return "; ".join(f"{p['seed']}: {fn(p)}" for p in pairs)
    def causes(p, cause):
        return " / ".join(f"{p[v]['rejections'][cause]['count']} ({percentage(p[v]['rejections'][cause]['rateAmongDomainRejected'])})"
                          for v in ("control", "fragmented"))
    stepped = all([r['rejectionRate'] for r in p['fragmented']['groupSize']] == [0,0,0,0,1,1,1] for p in pairs)
    return [
        ("How much does Fragmented increase RejectionRate?",
         seeds(lambda p: f"{percentage(p['control']['requestOutcomes']['rejectionRate'])} → {percentage(p['fragmented']['requestOutcomes']['rejectionRate'])}, {p['fragmentationImpactPercentagePoints']:+.3f} pp")
         + ". Cross-Seed min/mean/max: " + " / ".join(f"{result['fragmentationImpactSummary'][k]:.3f}" for k in ("min", "mean", "max"))
         + " pp. Three deterministic Seeds describe variation; they do not estimate production occurrence probability."),
        ("What proportion is ordinary capacity shortage?", "Control / Fragmented: " + seeds(lambda p: causes(p, CAUSES[0]))
         + ". Control has the same opening capacity pressure as Fragmented, but its rejections are not all capacity shortage either."),
        ("What proportion is no contiguous block despite sufficient eligible capacity?", "Control / Fragmented: " + seeds(lambda p: causes(p, CAUSES[1]))
         + ". These diagnoses require captured eligible capacity and a false block-found observation, not just the rejection code."),
        ("Which group sizes are most affected?", seeds(lambda p: ", ".join(
            f"{c['category']}: {metric_delta(f['rejectionRate'] - c['rejectionRate'], True)}" for c, f in zip(p['control']['groupSize'], p['fragmented']['groupSize'])))
         + (". The observed shape is a step: sizes 1–4 have zero Fragmented rejection, sizes 5+ have 100%. Every opening largest available run is four; immutable occupancy cannot create a longer run later. This is specific to the tested pattern, not a universal group-size threshold."
            if stepped else ". Read the observed seven-bucket curve; do not impose monotonicity or infer a universal size threshold.")),
        ("Does fragmentation cause rejection earlier?", seeds(lambda p: "Control first rank " + str(p['control']['firstRejection']['dailyExecutionPosition'])
            + " vs Fragmented " + str(p['fragmented']['firstRejection']['dailyExecutionPosition']))
         + ". Daily thirds and all five daily first events are reported below. Early effects coexist with later natural inventory consumption."),
        ("How much capacity remains at fragmentation-related rejection?", seeds(lambda p: "Control / Fragmented median total " + " / ".join(
            str(p[v]['rejectionCapacity'][CAUSES[1]]['remainingTotalBeforeRequest']['median']) for v in ('control', 'fragmented'))
            + ", median eligible " + " / ".join(str(p[v]['rejectionCapacity'][CAUSES[1]]['eligibleAvailableSpotCount']['median']) for v in ('control', 'fragmented')))
         + ". Min/median/max and missing counts are retained by cause. Total is reconstructed from 3,998 minus confirmed prior successes."),
        ("Does Fragmented leave more unused capacity?", seeds(lambda p: "mean end-of-day remaining Control / Fragmented " + " / ".join(
            f"{mean(d['totalAvailableSpotCount'] for d in p[v]['dailyCapacity']):.1f}" for v in ('control', 'fragmented')))
         + ". The daily table separates residual total and quality inventory; more unused capacity with more rejection indicates lower spatial usability under this pattern."),
        ("Does quality change among successfully assigned attendees?", seeds(lambda p: "Good/Medium/Bad deltas " + " / ".join(
            metric_delta(p['deltas']['fairness'][q.lower() + 'OutcomeRate'], True) for q in QUALITY))
         + ". These are measured changes among persisted Assignments, with selection from rejection and changing history; no new materiality threshold is imposed."),
        ("Does path fairness change or mainly participation coverage?", seeds(lambda p: "classifiable Control / Fragmented " + " / ".join(
            str(p[v]['population']['classifiable']) for v in ('control', 'fragmented'))
            + ", NotBalanced delta " + metric_delta(p['deltas']['fairness']['notBalancedPathRate'], True))
         + ". Both the observed path-rate distributions and coverage change. Rates reflect altered successful-assignment composition and natural histories, not an independently established overall fairness improvement. Rejected attempts add no synthetic Bad or recovery opportunity."),
        ("Does exact request pairing support fragmentation-induced infeasibility?", seeds(lambda p: str(
            p['pairedRequests']['controlSuccessFragmentedRejected']['causes'][CAUSES[1]]) + " Control-success/Fragmented-rejected requests with sufficient eligible capacity/no block")
         + ". These are paired evidence of fragmentation-induced infeasibility under the tested setup. Reverse transitions remain visible; five-day effects include naturally diverging history/inventory."),
        ("Should the later Decision Gate consider fragmentation mitigation?",
         ("Yes: the paired sufficient-capacity/no-block cases support considering fragmentation risk at the later Decision Gate. "
          if any(p['pairedRequests']['controlSuccessFragmentedRejected']['causes'][CAUSES[1]] for p in pairs)
          else "No added-harm sufficient-capacity/no-block cases were observed; these runs alone do not demonstrate a mitigation need. ")
         + "This is evidence for a later evaluation, not a final gate verdict, selected mitigation or authorization to change Engine v1.")]


def render_markdown(result):
    pairs = result["pairs"]
    run_rows = [[e['scenario'], e['seed'], e['role'], f"`{e['runId']}`", f"`{e['evidencePath']}/`"] for e in result['runs']]
    sections = ["# S7 fragmentation impact results",
        f"Execution/analysis date: **{result['executionDate']} ({result['timezone']})**. Runtime timestamps and directory dates use UTC. Simulated first date: **2026-08-15**, a fixture, not a confirmed festival date.",
        "## Main finding", experimental_answers(result)[0][1],
        "Exact paired requests include " + str(sum(p['pairedRequests']['controlSuccessFragmentedRejected']['causes'][CAUSES[1]] for p in pairs))
        + " Control successes that reject in Fragmented despite sufficient eligible capacity and no feasible block. Fragmented's larger residual inventory and changed quality/fairness populations must be read alongside that feasibility loss; these are results for the tested synthetic spatial pattern.",
        "## Experimental answers", "\n\n".join(f"{i}. **{question}** {answer}" for i, (question, answer) in enumerate(experimental_answers(result), 1)),
        "## Execution matrix", table(["Variant", "Seed", "Role", "RunId", "Evidence path"], run_rows),
        f"Set index: `{result['sourceIndex']}`. SHA-256: `{result['sourceIndexSha256']}`. All eight package hashes per run are in the adjacent JSON. Raw packages remain ignored and local; back them up before deleting the checkout.",
        "## Pair and opening integrity",
        "Every same-Seed pair passes exact definition, attendee/day catalog, daily cohort, request identity, ordered membership, size, eligibility, RequestedAt/AssignedAt, baseline/execution sequence, topology and Spot-catalog equality. Supporting identities/requests are also paired; only assigned Spot positions differ. All pairs use normal sequential S2 order, generator v2, Engine v0.3.0, rubric v1 and concurrency 1. No S2 control was executed.",
        "Every daily opening has 4,998 total / 1,000 occupied / 3,998 available Spots, remaining Good 1,402 / Medium 1,800 / Bad 796, identical Row/Zone quotas and `s7-preoccupation-v1`. The 20% stress hypothesis **requires stakeholder validation**. Setup identities are disjoint and day-scoped; evaluated history accumulates naturally.",
        table(["Seed", "Rows", "Control sum largest runs", "Fragmented sum", "Control min/max", "Fragmented min/max"],
            [[p['seed'], len(p['integrity']['controlOpening'][0]['rowQuotas']), *[p['integrity'][v+'Opening'][0]['largestRunSum'] for v in ('control','fragmented')],
              *[f"{p['integrity'][v+'Opening'][0]['minLargestRun']}/{p['integrity'][v+'Opening'][0]['maxLargestRun']}" for v in ('control','fragmented')]] for p in pairs]),
        "## Run integrity and primary comparison",
        "Success/RejectionRate denominator: NormallyProcessedRequests = Successful + DomainRejected. FragmentationImpact = RejectionRate(Fragmented) − RejectionRate(Control), in percentage points. Relative increase divides the rate delta by Control's rate (N/A when zero). Conflicts/technical failures use AttemptedRequests. All category counts, exclusions and final-state findings remain visible. DomainRejected is expected experimental evidence.",
        table(["Seed", "Variant", "Attempted", "Successful", "Rejected", "Conflict", "Technical", "Excluded", "Findings", "Success", "Rejection"],
            [[p['seed'], v, *[p[v]['requestOutcomes'][k] for k in ('attemptedRequests','successfulRequests','domainRejectedRequests','knownConcurrencyConflictRequests','unexpectedTechnicalFailureRequests','excludedInvalidInput')],
              p[v]['integrity']['invariantViolationCount'], *[percentage(p[v]['requestOutcomes'][k]) for k in ('successRate','rejectionRate')]] for p in pairs for v in ('control','fragmented')]),
        table(["Seed", "Control rejection", "Fragmented rejection", "Delta pp", "Relative increase"],
            [[p['seed'], *[percentage(p[v]['requestOutcomes']['rejectionRate']) for v in ('control','fragmented')], f"{p['fragmentationImpactPercentagePoints']:+.3f}", percentage(p['relativeRejectionIncrease'])] for p in pairs]),
        "Final-state audits cover unique Spot/day and Attendee/day, valid setup and complete contiguous same-Zone evaluated results, immutable catalog identity, request terminal consistency, dispatch and checkpoints. They are not a complete temporal proof of INV-06. Supporting rows are counted separately; five opening states are checked against persisted day-specific inventory. Reproduction runs also have zero conflicts, technical failures and findings; their package counts are retained in JSON.",
        "## Rejection decomposition and strong evidence",
        "CapacityShortage requires eligible capacity below GroupSize with a no-block observation. ContiguousBlockUnavailable requires eligible capacity at least GroupSize and no block. Unobserved/other cases remain N/A. The existing rejection code alone does not distinguish these causes.",
        table(["Seed", "Variant", "Cause", "Count", "% rejected", "% attempted"],
            [[p['seed'], v, cause, p[v]['rejections'][cause]['count'], percentage(p[v]['rejections'][cause]['rateAmongDomainRejected']), percentage(p[v]['rejections'][cause]['rateAmongAttemptedRequests'])]
             for p in pairs for v in ('control','fragmented') for cause in CAUSES]),
        table(["Seed", "Strong-evidence count delta", "% rejected delta", "% attempted delta"],
            [[p['seed'], p['strongEvidenceDelta']['count'], metric_delta(p['strongEvidenceDelta']['rateAmongDomainRejected'], True), metric_delta(p['strongEvidenceDelta']['rateAmongAttemptedRequests'], True)] for p in pairs]),
        "## Paired request outcomes and incremental harm",
        table(["Seed", "Both success", "Both rejected", "Control success/F rejected", "Control rejected/F success", "Added harm: shortage/block/N/A", "Reverse: shortage/block/N/A"],
            [[p['seed'], p['pairedRequests']['matrix']['Successful']['Successful'], p['pairedRequests']['matrix']['DomainRejected']['DomainRejected'],
              p['pairedRequests']['controlSuccessFragmentedRejected']['total'], p['pairedRequests']['controlRejectedFragmentedSuccess']['total'],
              *[' / '.join(str(p['pairedRequests'][k]['causes'][c]) for c in CAUSES) for k in ('controlSuccessFragmentedRejected','controlRejectedFragmentedSuccess')]] for p in pairs]),
        "Additional net rejection and individual added harm are distinct. The net equals forward minus reverse transitions. Only the forward sufficient-eligible-capacity/no-block subset is labelled paired fragmentation-induced infeasibility; total delta is an effect of the spatial intervention with natural subsequent history/inventory divergence.",
        "## Group-size degradation and cause cross-tab",
        "Each group counts once. Rates use the normally processed requests in that bucket; quality outcomes count persisted attendee Assignments. Empty denominators are N/A.",
        table(["Seed", "Variant", "Size", "Requests", "Successful", "Rejected", "Success", "Rejection", "Shortage", "Block unavailable", "N/A"],
            [[p['seed'], v, row['category'], row['requestCount'], row['successfulRequests'], row['domainRejectedRequests'], percentage(row['successRate']), percentage(row['rejectionRate']), *[row['causes'][c]['count'] for c in CAUSES]]
             for p in pairs for v in ('control','fragmented') for row in p[v]['groupSize']]),
        "## Supplementary sizes", table(["Seed", "Variant", "Size", "Requests", "Success", "Rejection"],
            [[p['seed'], v, r['category'], r['requestCount'], percentage(r['successRate']), percentage(r['rejectionRate'])] for p in pairs for v in ('control','fragmented') for r in p[v]['supplementarySize']]),
        "## Daily execution thirds",
        "Thirds use floor(3 × zero-based daily execution rank / daily request count), not global sequence cutoffs. Block-unavailable rate below uses normally processed requests; attempted and rejection denominators are also retained in JSON.",
        table(["Seed", "Variant", "Third", "Requests", "Success", "Rejection", "Block unavailable"],
            [[p['seed'], v, r['category'], r['requestCount'], percentage(r['successRate']), percentage(r['rejectionRate']), percentage(r['causes'][CAUSES[1]]['rateAmongNormallyProcessedRequests'])]
             for p in pairs for v in ('control','fragmented') for r in p[v]['executionThird']]),
        "## Size × execution third",
        table(["Seed", "Variant", "Size/third", "Requests", "Rejection", "Block unavailable"],
            [[p['seed'], v, r['category'], r['requestCount'], percentage(r['rejectionRate']), percentage(r['causes'][CAUSES[1]]['rateAmongNormallyProcessedRequests'])]
             for p in pairs for v in ('control','fragmented') for r in p[v]['groupSizeByThird']]),
        "## First rejection in each run",
        table(["Seed", "Variant", "Day", "ExecutionSequence", "Daily rank", "Third", "Size", "Total left", "Eligible left", "Cause"],
            [[p['seed'], v, *[p[v]['firstRejection'][k] for k in ('dayNumber','attemptSequence','dailyExecutionPosition','executionThird','groupSize','remainingTotalBeforeRequest','eligibleAvailableSpotCount','cause')]] for p in pairs for v in ('control','fragmented')]),
        "## Rejection-time capacity",
        "Total capacity is reconstructed from the verified 3,998 opening minus preceding confirmed successful group sizes. Eligible capacity and block-found are captured from the real evaluation. Reconstruction requires complete reconciled sequential evidence; no uncertain commits or external inventory writes occurred. Independent pre-save reads retain the baseline's snapshot-consistency limitation.",
        table(["Seed", "Variant", "Cause", "Total min/median/max", "Eligible min/median/max", "Missing total/eligible"],
            [[p['seed'], v, cause, *[' / '.join(str(p[v]['rejectionCapacity'][cause][field][k]) if p[v]['rejectionCapacity'][cause][field][k] is not None else 'N/A' for k in ('min','median','max')) for field in ('remainingTotalBeforeRequest','eligibleAvailableSpotCount')],
              ' / '.join(str(p[v]['rejectionCapacity'][cause][field]['missing']) for field in ('remainingTotalBeforeRequest','eligibleAvailableSpotCount'))]
             for p in pairs for v in ('control','fragmented') for cause in CAUSES[:2]]),
        "## Daily capacity utilization and quality evolution",
        "Remaining vectors are Good / Medium / Bad / total. Each day starts with equal quality capacity; closing differences are observed consequences. Assignments below exclude the 1,000 setup occupants. Unused capacity divides closing total by 3,998. First-rejection consumption is 3,998 minus its total-left value.",
        table(["Seed", "Variant", "Day", "Successful Assignments", "Remaining G/M/B/total", "Unused", "Used before first rejection", "First rejection rank / total / eligible"],
            [[p['seed'], v, d['dayNumber'], d['successfulAttendeeAssignments'], ' / '.join(str(d[k]) for k in CAPACITY_FIELDS), percentage(d['unusedCapacityRate']),
              d['consumedBeforeFirstRejection'],
              ' / '.join(str(d['firstRejection'][k]) for k in ('dailyExecutionPosition','remainingTotalBeforeRequest','eligibleAvailableSpotCount')) if d['firstRejection'] else 'N/A']
             for p in pairs for v in ('control','fragmented') for d in p[v]['dailyCapacity']]),
        "## Fairness population and coverage",
        table(["Seed", "Variant", "Registered", "Requesting", "Participating", "Only rejected/failed", "No path", "Insufficient", "Classifiable"],
            [[p['seed'], v, *p[v]['population'].values()] for p in pairs for v in ('control','fragmented')]),
        "Supporting attendees are excluded. Requesting includes everyone with an evaluated attempt; Participating requires at least one persisted Assignment. NoPath remains visible and unclassified. Rejected attempts generate no synthetic quality or recovery opportunity. Changes among survivors must be read alongside participation and classifiability coverage.",
        "## Quality, path fairness and recovery deltas",
        "Quality denominators are persisted evaluated Assignments; path denominators are classifiable paths; RecoveryRate uses eligible episodes, and speed uses recovered episodes. Stored rubric/recovery outputs are consumed unchanged. Deltas are Fragmented minus Control.",
        table(["Seed", "Metric", "Control", "Fragmented", "Delta"],
            [[p['seed'], key, *[(percentage(p[v][family][key]) if key.endswith('Rate') else number(p[v][family][key])) for v in ('control','fragmented')], metric_delta(p['deltas'][family][key], key.endswith('Rate'))]
             for p in pairs for family, keys in (("fairness", FIELDS['fairness']), ("recovery", FIELDS['recovery'])) for key in keys]),
        table(["Seed", "Variant", "Persisted outcomes", "Classifiable", "Episodes", "Eligible", "Recovered", "Eligible unresolved"],
            [[p['seed'], v, p[v]['fairness']['persistedOutcomeCount'], p[v]['fairness']['classifiablePathCount'],
              *[p[v]['recovery'][k] for k in ('recoveryEpisodeCount','recoveryEligibleEpisodeCount','recoveredEpisodeCount')], p[v]['eligibleUnresolved']] for p in pairs for v in ('control','fragmented')]),
        "## Rejected-attendee later exposure",
        table(["Seed", "Variant", "Ever rejected", "Successfully assigned on a later day", "Share"],
            [[p['seed'], v, p[v]['rejectedAttendeeExposure']['everRejected'], p[v]['rejectedAttendeeExposure']['laterSuccessfullyAssigned'], percentage(p[v]['rejectedAttendeeExposure']['laterSuccessfullyAssignedRate'])] for p in pairs for v in ('control','fragmented')]),
        "Distinct people are counted once per run. Later success means any successful request on a strictly later day than a rejection. A person can be rejected again afterward; no later success may also reflect no later request. This is descriptive exposure, not a new fairness classification.",
        "## Reproduction and deterministic report replay",
        table(["Variant", "Logical components equal", "Byte-identical definition/manifest/checkpoints/metrics"],
            [[r['repeatedRun']['scenario'], all(r['logicalMatches'].values()), all(r['byteIdentical'].values())] for r in result['reproducibility']['variants']]),
        result['reproducibility']['normalization'] + " The repeated 1001 pair produces the same paired request matrix and harm diagnosis. Reproduction is excluded from cross-Seed statistics. Derived analysis has no wall-clock analysis timestamps and is replayed twice for byte-identical JSON and Markdown.",
        "## Exploratory performance observation",
        "Existing attempt durations, nearest-rank percentiles in milliseconds. These are **exploratory observations, not a dedicated performance benchmark**; serial run order, runtime warm-up and local Docker conditions are uncontrolled. They do not establish production performance.",
        table(["Seed", "Variant", "Samples", "p50 ms", "p95 ms", "p99 ms"],
            [[p['seed'], v, p[v]['exploratoryTiming']['sampleCount'], *[number(p[v]['exploratoryTiming'][k], 3) for k in ('p50Ms','p95Ms','p99Ms')]] for p in pairs for v in ('control','fragmented')]),
        "## Evidence provenance and replay",
        f"Harness SHA-256: `{result['executionHarnessSha256']}`. Analyzer SHA-256: `{result['analysisToolSha256']}`. Metadata identifies the unchanged protected source revision; harness/analyzer changes are local task work. Every package checksum is verified before and after analysis.",
        "`python3 -B scripts/stage-5-representative-runs/analyze_s7_fragmentation.py " + result['sourceIndex'] + " --output artifacts/analysis/s7-repeat.json --markdown artifacts/analysis/s7-repeat.md`",
        "Use fresh output paths outside raw evidence. The adjacent JSON retains per-Seed denominators, cause counts/rates, all cross-tabs, daily first events and capacity summaries, population coverage, reproduction and raw checksums.",
        "## Interpretation boundaries and scope",
        "These results concern the tested synthetic 20% pre-occupation pattern under S2 workload and topology. They do not establish real festival pre-occupation behavior, production fragmentation probability, every spatial arrangement, concurrency behavior or production performance. The pattern is a deliberate analytical stress input requiring stakeholder validation. No new materiality threshold or final Decision Gate is imposed. Engine v1, fairness, scoring, selection, rubric, recovery, S2/S5/S6 and S7 configuration/pattern are unchanged; no mitigation or S8 is implemented.",
        "Evidence-supported next task: define the separate S8 concurrency/operational-sensitivity contract before its later implementation/execution, then integrate the remaining independent scenario evidence at the Decision Gate. No such execution or policy change begins here."]
    return "\n\n".join(sections) + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("index", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--markdown", type=Path, required=True)
    args = parser.parse_args()
    require(args.output.resolve() != args.markdown.resolve(), "Report paths must differ")
    for path in (args.output, args.markdown):
        require(not path.exists() and not path.resolve().is_relative_to(Path("artifacts/simulations").resolve()), "Output must be fresh and outside raw evidence")
    result = analyze(args.index)
    markdown = render_markdown(result)
    for path, content in ((args.output, json.dumps(result, indent=2, ensure_ascii=False, allow_nan=False) + "\n"), (args.markdown, markdown)):
        path.parent.mkdir(parents=True, exist_ok=True)
        with path.open("x", encoding="utf-8") as stream:
            stream.write(content)
    print(args.output)


if __name__ == "__main__":
    main()
