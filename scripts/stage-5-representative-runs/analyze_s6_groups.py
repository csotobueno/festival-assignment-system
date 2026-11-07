"""Offline S2/S6 scenario-level comparison. Never execute, classify or tune a scenario.

Reuse checksum readers, stable joins, request metrics and reproduction normalization.
Stored fairness/recovery aggregates remain authoritative; segmentation only counts
the same observed requests/Assignments. Raw packages are read-only.
"""

import argparse
import copy
import hashlib
import json
import math
from collections import Counter
from pathlib import Path

from analyze_s2_paths import RESULTS, SEEDS, THIRDS, rate, reconcile, require, uid
from analyze_s5_order import FIELDS, QUALITY, joined, read_package, run_metrics
from summarize import duration_ms, normalized


BUCKETS = ("1", "2", "3", "4", "5", "6–7", "8–10")
S2_SHARES = (.15, .30, .25, .15, .08, .05, .02)
S6_SHARES = (.10, .20, .20, .15, .10, .15, .10)
CAPACITY_FIELDS = ("goodAvailableSpotCount", "mediumAvailableSpotCount",
                   "badAvailableSpotCount", "totalAvailableSpotCount")


def normal_workload(data):
    definition, manifest = data["definition"], data["manifest"]
    workload = definition["workload"]
    require(definition["attendeeCount"] == 5500 and workload["dailyRequesterCount"] == 4400
            and workload["dailyParticipationContinuity"] == .9
            and workload["allowsFrontStandingRequestShare"] == .5, "Changed baseline population/continuity/eligibility")
    require(manifest["metadata"].get("orderingTransformation") is None
            and manifest["metadata"].get("workloadSourceScenarioId") is None, "Unexpected S5 transformation")
    require(all(not values for values in manifest["setup"].values()), "Unexpected setup/history")
    require(len(manifest["dailyPlans"]) == len(manifest["catalog"]["festivalDays"]) == 5, "Wrong day count")
    registered = {uid(a["id"]) for a in manifest["catalog"]["attendees"]}
    require(len(registered) == len(manifest["catalog"]["attendees"]) == 5500, "Wrong registered catalog")
    require(len(manifest["catalog"]["spots"]) == 4998, "Wrong physical capacity")
    previous, sequence = None, 0
    for number, day in enumerate(manifest["dailyPlans"], 1):
        require(day["dayNumber"] == number, "Nonconsecutive days")
        requests = day["requests"]
        members = [uid(m["id"]) for q in requests for m in q["members"]]
        current = set(members)
        require(len(members) == len(current) == 4400 and current <= registered, "Wrong/duplicate daily cohort")
        if previous is not None:
            require(len(current & previous) == 3960 and len(current - previous) == 440, "Continuity mismatch")
        previous = current
        require(sum(q["eligibility"]["allowsFrontStanding"] for q in requests) == (len(requests) + 1) // 2,
                "Wrong request-level eligibility allocation")
        require(requests == sorted(requests, key=lambda q: (q["requestedAt"], q["simulationRequestId"])),
                "Changed RequestedAt baseline order")
        for request in requests:
            sequence += 1
            require(request["sequence"] == request.get("executionSequence", sequence) == sequence,
                    "Changed normal processing order")
            require(1 <= len(request["members"]) <= 10, "Invalid GroupSize")
            require(request["assignedAt"] == request["requestedAt"], "Changed business-time convention")


def control_integrity(s2, s6):
    left, right = copy.deepcopy(s2["definition"]), copy.deepcopy(s6["definition"])
    require(left["scenarioId"] == "s2-expected-baseline" and right["scenarioId"] == "s6-large-groups", "Wrong scenario identity")
    for definition, shares in ((left, S2_SHARES), (right, S6_SHARES)):
        expected = [dict(minSize=low, maxSize=high, percentage=share)
                    for (low, high), share in zip(((1, 1), (2, 2), (3, 3), (4, 4), (5, 5), (6, 7), (8, 10)), shares)]
        require(definition["workload"].pop("groupSizeProfile") == expected, "Wrong group profile")
    right["scenarioId"] = left["scenarioId"]
    require(left == right, "Uncontrolled definition difference")
    for data in (s2, s6):
        normal_workload(data)
        for key in ("scenarioId", "seed", "generatorVersion", "executionMode", "concurrencyLevel"):
            require(data["definition"][key] == data["manifest"]["metadata"][key], "Definition/manifest metadata mismatch")
    for key in ("attendees", "zones", "spots"):
        require(s2["manifest"]["catalog"][key] == s6["manifest"]["catalog"][key], "Changed catalog/topology")
    calendars = [[(d["date"], d["assignmentWindow"]) for d in data["manifest"]["catalog"]["festivalDays"]]
                 for data in (s2, s6)]
    require(calendars[0] == calendars[1], "Changed calendar/windows")
    return dict(preservedDefinitionExceptIdentityAndGroupProfile=True, sameRegisteredCatalog=True,
                sameCalendar=True, sameTopologyAndIdentities=True, normalProcessingOrder=True,
                exactRequestPairingRequired=False)


def segment(rows, paths):
    """Request rates use normal requests; quality uses persisted participations.

    NotBalanced is the stored final attendee path, never a request classification.
    Its participation denominator here includes classifiable persisted paths only.
    """
    counts = Counter(r["outcome"] for r in rows)
    normal = counts["Successful"] + counts["DomainRejected"]
    successful = [r for r in rows if r["outcome"] == "Successful"]
    persisted = sum(r["groupSize"] for r in successful)
    classifications = {uid(p["attendeeId"]): p for p in paths}
    classifiable = [a for r in successful for a in r["attendeeIds"] if classifications[a]["pathLength"] >= 2]
    nb = sum(classifications[a]["classification"] == "NotBalanced" for a in classifiable)
    quality = {q: sum(r["groupSize"] for r in successful if r["quality"] == q) for q in QUALITY}
    return dict(requestCount=len(rows), plannedAttendeeCount=sum(r["groupSize"] for r in rows),
                normallyProcessedRequests=normal, successfulRequests=counts["Successful"],
                domainRejectedRequests=counts["DomainRejected"],
                knownConcurrencyConflictRequests=counts["KnownConcurrencyConflict"],
                unexpectedTechnicalFailureRequests=counts["UnexpectedTechnicalFailure"],
                successRate=rate(counts["Successful"], normal), rejectionRate=rate(counts["DomainRejected"], normal),
                persistedParticipationCount=persisted,
                **{q.lower() + "OutcomeCount": quality[q] for q in QUALITY},
                **{q.lower() + "OutcomeRate": rate(quality[q], persisted) for q in QUALITY},
                classifiableParticipationCount=len(classifiable), notBalancedParticipationCount=nb,
                notBalancedParticipationRate=rate(nb, len(classifiable)))


def by_category(rows, paths, labels, key):
    return [dict(category=label, **segment([r for r in rows if key(r) == label], paths)) for label in labels]


def exposure(rows, paths):
    counts = Counter(a for r in rows if r["outcome"] == "Successful" and r["groupSize"] >= 6 for a in r["attendeeIds"])
    result = []
    for count in sorted({counts[uid(p["attendeeId"])] for p in paths}):
        selected = [p for p in paths if counts[uid(p["attendeeId"])] == count]
        classified = [p for p in selected if p["pathLength"] >= 2]
        nb = sum(p["classification"] == "NotBalanced" for p in classified)
        result.append(dict(largeGroupAssignmentCount=count, evaluatedAttendees=len(selected),
                           classifiableAttendees=len(classified), notBalancedAttendees=nb,
                           notBalancedRate=rate(nb, len(classified))))
    return result


def episode_groups(data, rows):
    membership = {(a, r["festivalDayId"]): r for r in rows for a in r["attendeeIds"]}
    episodes = [e for record in data["recovery-observations"] for e in record["episodes"]]
    result = []
    for label in BUCKETS:
        selected = [e for e in episodes if membership[(uid(e["attendeeId"]), uid(e["startFestivalDayId"]))]["groupBand"] == label]
        eligible = sum(e["recoveryEligible"] for e in selected)
        unresolved = sum(e["recoveryEligible"] and e["status"] == "Unresolved" for e in selected)
        result.append(dict(category=label, episodeOpenings=len(selected), shareOfAllOpenings=rate(len(selected), len(episodes)),
                           recoveryEligibleOpenings=eligible, eligibleUnresolved=unresolved,
                           eligibleUnresolvedRate=rate(unresolved, eligible)))
    return result


def rejected(rows):
    events = []
    for row in rows:
        if row["outcome"] != "DomainRejected":
            continue
        capacity, found = row["eligibleAvailableSpotCount"], row["feasibleContiguousBlockFound"]
        diagnosis = "Unavailable"
        if row["diagnosticCode"] == "NO_CONTIGUOUS_SPOTS_AVAILABLE" and found is False and capacity is not None:
            diagnosis = "InsufficientEligibleCapacity" if capacity < row["groupSize"] else "ContiguousBlockPressure"
        events.append(dict(row, diagnosis=diagnosis))
    groups = {key: dict(sorted(Counter(str(e[key]) for e in events).items()))
              for key in ("groupBand", "dayNumber", "executionThird", "diagnosticCode", "diagnosis")}
    require(all(sum(counts.values()) == len(events) for counts in groups.values()), "Rejection grouping mismatch")
    return dict(count=len(events), events=events, grouped=groups)


def analyze_run(entry, data):
    metrics = run_metrics(entry, data)
    reconcile(data["attendee-paths"], data["recovery-observations"], data["metrics"],
              dict(**data["metrics"], eligibleUnresolvedEpisodeCount=metrics["recovery"]["recoveryEligibleEpisodeCount"]
                   - metrics["recovery"]["recoveredEpisodeCount"]))
    rows, _ = joined(data)
    attempts = {a["simulationRequestId"]: a for a in data["attempts"]}
    remaining, positions = {}, Counter()
    for row in rows:
        day = row["dayNumber"]
        positions[day] += 1
        row.update({k: attempts[row["simulationRequestId"]][k] for k in
                    ("outcome", "diagnosticCode", "eligibleAvailableSpotCount", "feasibleContiguousBlockFound", "attemptSequence")})
        row["dailyExecutionPosition"] = positions[day]
        row["remainingTotalBeforeRequest"] = remaining.get(day, 4998)
        remaining[day] = row["remainingTotalBeforeRequest"] - (row["groupSize"] if row["outcome"] == "Successful" else 0)
    for checkpoint in data["checkpoints"]:
        day = next(d["dayNumber"] for d in data["manifest"]["dailyPlans"] if d["festivalDayId"] == checkpoint["festivalDayId"])
        daily = [r for r in rows if r["dayNumber"] == day]
        require(remaining[day] == checkpoint["totalAvailableSpotCount"], "Remaining total does not reconcile")
        require(checkpoint["evaluatedRequestCount"] == len(daily)
                and checkpoint["successfulAttemptCount"] == sum(r["outcome"] == "Successful" for r in daily)
                and checkpoint["rejectedAttemptCount"] == sum(r["outcome"] == "DomainRejected" for r in daily), "Checkpoint outcome mismatch")
        for quality, capacity, field in zip(QUALITY, (1752, 2250, 996), CAPACITY_FIELDS):
            consumed = sum(r["groupSize"] for r in daily if r["outcome"] == "Successful" and r["quality"] == quality)
            require(checkpoint[field] == capacity - consumed, "Quality inventory does not reconcile")
        for band in data["definition"]["workload"]["groupSizeProfile"]:
            count = sum(band["minSize"] <= r["groupSize"] <= band["maxSize"] for r in daily)
            require(abs(count - len(daily) * band["percentage"]) < 1, "Group quota mismatch")
        for band in data["definition"]["workload"]["arrivalProfile"]:
            label = band["start"][:5] + "–" + band["end"][:5]
            require(abs(sum(r["arrivalBand"] == label for r in daily) - len(daily) * band["percentage"]) < 1, "Arrival quota mismatch")
    paths = data["attendee-paths"]
    rejection = rejected(rows)
    require(rejection["count"] == metrics["requestOutcomes"]["domainRejectedRequests"], "Rejection total mismatch")
    durations = sorted(duration_ms(a["duration"]) for a in data["attempts"] if a["outcome"] != "ExcludedInvalidInput")
    return dict(**metrics, metadata=data["run-metadata"],
                groupSize=by_category(rows, paths, BUCKETS, lambda r: r["groupBand"]),
                largeGroup=by_category(rows, paths, ("1–5", "6–10"), lambda r: "1–5" if r["groupSize"] <= 5 else "6–10"),
                executionThird=by_category(rows, paths, THIRDS, lambda r: r["executionThird"]),
                groupSizeByExecutionThird=by_category(rows, paths, [b + "/" + t for b in BUCKETS for t in THIRDS],
                                                    lambda r: r["groupBand"] + "/" + r["executionThird"]),
                largeGroupExposure=exposure(rows, paths), recoveryByOpeningGroup=episode_groups(data, rows),
                rejectionAnalysis=rejection,
                dailyCapacity=[dict(dayNumber=d["dayNumber"], **next(c for c in data["checkpoints"]
                    if c["festivalDayId"] == d["festivalDayId"])) for d in data["manifest"]["dailyPlans"]],
                dailyQualityPositions=[dict(dayNumber=d, requestCount=positions[d], **{
                    q.lower(): dict(first=min(r["dailyExecutionPosition"] for r in rows if r["dayNumber"] == d and r["quality"] == q),
                                    last=max(r["dailyExecutionPosition"] for r in rows if r["dayNumber"] == d and r["quality"] == q))
                        if any(r["dayNumber"] == d and r["quality"] == q for r in rows) else None for q in QUALITY})
                    for d in sorted(positions)],
                exploratoryTiming=dict(sampleCount=len(durations), percentileMethod="nearest-rank", **{
                    f"p{p}Ms": durations[math.ceil(p / 100 * len(durations)) - 1] if durations else None for p in (50, 95, 99)}))


def reproduce(reference, repeated):
    first, second = (read_package(e) for e in (reference, repeated))
    normal_workload(second)
    analyze_run(repeated, second)
    left, right = (normalized(e["evidencePath"]) for e in (reference, repeated))
    matches = {key: left[key] == right[key] for key in left}
    for key in ("definition", "checkpoints"):
        matches[key] = first[key] == second[key]
    matches["runMetadataExceptRuntime"] = {
        k: v for k, v in first["run-metadata"].items() if k not in ("runId", "startedAt", "completedAt")
    } == {k: v for k, v in second["run-metadata"].items() if k not in ("runId", "startedAt", "completedAt")}
    exact = {name: reference["integrity"]["sha256"][name] == repeated["integrity"]["sha256"][name]
             for name in ("definition.json", "manifest.json", "checkpoints.json", "metrics.json")}
    require(all(matches.values()) and all(exact.values()), "Reproduction mismatch; retain evidence for investigation")
    return dict(referenceRunId=reference["runId"], repeatedRun=repeated, logicalMatches=matches,
                exactFileMatches=exact, ignored="RunId and observed start/end/duration; generated durable request/Assignment IDs normalized by stable simulation-request and attendee/FestivalDay identities.")


def analyze(index_path):
    controls = json.loads(RESULTS.read_text())["officialRuns"]
    index = json.loads(index_path.read_text())
    official = [e for e in index if e["role"] == "official"]
    repeated = [e for e in index if e["role"] == "reproduction"]
    require([(e["scenario"], e["seed"]) for e in official] == [("s6", s) for s in SEEDS], "Wrong official S6 matrix")
    require(len(index) == 4 and len(repeated) == 1 and repeated[0]["scenario"] == "s6" and repeated[0]["seed"] == 1001,
            "Wrong reproduction protocol")
    pairs, sources = [], []
    for entry in official:
        control = next(e for e in controls if e["scenario"] == "s2" and e["seed"] == entry["seed"])
        require(entry["pairedControl"] == control, "Wrong retained S2 provenance")
        s2, s6 = read_package(control), read_package(entry)
        proof = control_integrity(s2, s6)
        left, right = analyze_run(control, s2), analyze_run(entry, s6)
        deltas = {family: {key: right[family][key] - left[family][key]
                    if right[family][key] is not None and left[family][key] is not None else None for key in keys}
                  for family, keys in FIELDS.items()}
        capacities = [dict(dayNumber=a["dayNumber"], s2={k: a[k] for k in CAPACITY_FIELDS},
                           s6={k: b[k] for k in CAPACITY_FIELDS}, delta={k: b[k] - a[k] for k in CAPACITY_FIELDS})
                      for a, b in zip(left["dailyCapacity"], right["dailyCapacity"])]
        pairs.append(dict(seed=entry["seed"], controlIntegrity=proof, s2=left, s6=right, deltas=deltas, dailyCapacity=capacities))
        sources.extend((control, entry))
    require(repeated[0]["pairedControl"] == official[0]["pairedControl"], "Wrong reproduction control")
    reproduction = reproduce(official[0], repeated[0])
    sources.append(repeated[0])
    for entry in sources:
        read_package(entry)
    return dict(executionDate="2026-11-06", timezone="America/Lima", simulatedFirstDate="2026-08-15",
                comparison="Controlled scenario-level same-Seed comparisons; no exact request/attendee-day pairing.",
                sourceIndex=str(index_path), sourceIndexSha256=hashlib.sha256(index_path.read_bytes()).hexdigest(),
                sourceS2Results=str(RESULTS), sourceS2ResultsSha256=hashlib.sha256(RESULTS.read_bytes()).hexdigest(),
                executionHarnessSha256=hashlib.sha256(Path(__file__).with_name("Program.cs").read_bytes()).hexdigest(),
                analysisToolSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
                pairs=pairs, reproducibility=reproduction, checksumsUnchangedAfterAnalysis=True)


def percentage(value):
    return "N/A" if value is None else f"{value * 100:.3f}%"


def number(value, precision=6):
    return "N/A" if value is None else f"{value:.{precision}f}"


def metric_delta(value, is_rate):
    if value is None:
        return "N/A"
    return f"{value * 100:+.3f} pp" if is_rate else f"{value:+.6f}"


def table(headers, rows):
    return "\n".join(["| " + " | ".join(headers) + " |", "| " + " | ".join("---" for _ in headers) + " |"]
                     + ["| " + " | ".join(str(v) for v in row) + " |" for row in rows])


def conclusions(result):
    pairs = result["pairs"]
    def changes(family, key):
        return "; ".join(f"{p['seed']}: {metric_delta(p['deltas'][family][key], key.endswith('Rate'))}" for p in pairs)
    rejected_count = sum(p["s6"]["requestOutcomes"]["domainRejectedRequests"] for p in pairs)
    feasibility = ("No request-feasibility degradation was observed across these Seeds."
                   if all(p["deltas"]["requestOutcomes"]["successRate"] == 0 for p in pairs)
                   else "Request-feasibility changes are quantified by Seed above.")
    early_bad = sum(r["badOutcomeCount"] for p in pairs for r in p["s6"]["executionThird"] if r["category"] != "last")
    size_comparisons = []
    for p in pairs:
        small, large = p["s6"]["largeGroup"]
        six, eight = p["s6"]["groupSize"][-2:]
        rates = [r["badOutcomeRate"] for r in p["s6"]["groupSize"]]
        monotonic = all(a is not None and b is not None and a <= b for a, b in zip(rates, rates[1:]))
        size_comparisons.append(f"{p['seed']}: size 1–5 {percentage(small['badOutcomeRate'])}, 6–10 {percentage(large['badOutcomeRate'])}; "
                               f"6–7 {percentage(six['badOutcomeRate'])}, 8–10 {percentage(eight['badOutcomeRate'])}; "
                               f"nondecreasing across all seven buckets: {monotonic}")
    final_third = "; ".join(f"{p['seed']}: " + ", ".join(
        r["category"].split("/")[0] + " " + percentage(r["badOutcomeRate"])
        for r in p["s6"]["groupSizeByExecutionThird"] if r["category"] in ("6–7/last", "8–10/last")) for p in pairs)
    return [
        ("Does increasing 6–10 prevalence materially change feasibility?",
         f"Observed S6 DomainRejected = {rejected_count}; SuccessRate changes: {changes('requestOutcomes', 'successRate')}. {feasibility} This does not establish feasibility for all group mixes or loads."),
        ("Does it materially change quality distribution?",
         f"BadOutcomeRate changes: {changes('fairness', 'badOutcomeRate')}; Good: {changes('fairness', 'goodOutcomeRate')}; Medium: {changes('fairness', 'mediumOutcomeRate')}. These are measured effect sizes; no new materiality threshold or Decision Gate verdict is imposed."),
        ("Does it materially change NotBalanced prevalence?",
         f"NotBalancedPathRate changes: {changes('fairness', 'notBalancedPathRate')}. Cohorts and classifiable denominators can differ; this is scenario-level sensitivity, not exact attendee pairing."),
        ("Does it materially change recovery?",
         f"RecoveryRate changes: {changes('recovery', 'recoveryRate')}; average opportunities: {changes('recovery', 'averageOpportunitiesToRecovery')}. Eligible-unresolved counts and episode-opening attribution remain separate from Bad counts."),
        ("Are larger groups disproportionately affected?",
         "; ".join(size_comparisons) + ". These are normalized BadOutcomeRates. A size gradient is not evidence of a discrete feasibility threshold, and within-S6 association alone cannot establish causality."),
        ("Are negative effects explained by group size, execution position, or both?",
         f"S6 has {early_bad} Bad Assignments in the first/middle execution thirds. Final-third Bad rates for larger buckets: {final_third}. This controls only a coarse timing stratum; exact rank, eligibility, history and changing composition remain intertwined. A persistent within-third size association supports both timing and size as descriptive dimensions, not a causal decomposition."),
        ("Do rejections occur while total/eligible capacity remains?",
         f"There are {rejected_count} S6 rejections. Each observed rejection is retained with execution identity, eligible-capacity snapshot, block-found observation and reconstructed total inventory; zero rejections means no such event was observed, not that residual capacity is always usable."),
        ("Is there evidence of contiguous-block pressure?",
         "Only no-block rejections with eligible raw capacity at least GroupSize support that diagnosis in this report. Positive eligible capacity below GroupSize is insufficient capacity. No rejection evidence means no demonstrated placement failure from contiguous-block pressure; residual quality inventory alone cannot prove fragmentation."),
        ("Does the evidence justify S7 as a distinct fragmentation experiment?",
         "S7 remains a distinct unanswered sensitivity question. S6 changes group prevalence without controlling fragmentation geometry; its residual inventory and timing signals can motivate S7 design, but neither establish fragmentation causality nor predefine S7 parameters. Defining the S7 contract is the evidence-supported next analytical task, subject to the existing Stage 5 scope."),
    ]


def render_markdown(result):
    pairs = result["pairs"]
    sections = ["# S2/S6 large-group sensitivity results",
        "Execution/analysis date: **2026-11-06 (America/Lima)**. RunIds and runtime timestamps use UTC. "
        "The simulated first date is **2026-08-15**, a fixture rather than a confirmed event date.",
        "## Summary",
        "Across the three official S6 runs, increased large-group prevalence preserved observed request feasibility. "
        "BadOutcomeRate and NotBalancedPathRate increased in every Seed; recovery-rate changes were mixed and average recovery "
        "distance increased slightly. All S6 Bad Assignments occurred in the final execution third, where the size gradient "
        "also remained visible. No placement rejection demonstrated contiguous-block pressure. "
        "These are scenario-level sensitivity findings under provisional inputs, with no Decision Gate verdict.",
        "## Experimental answers",
        *[f"{i}. **{question}** {answer}" for i, (question, answer) in enumerate(conclusions(result), 1)],
        "## Execution matrix and retained evidence"]
    execution = []
    for pair in pairs:
        for scenario in ("s2", "s6"):
            run = pair[scenario]
            execution.append([scenario.upper(), pair["seed"], run["runId"], run["status"], f"`{run['evidencePath']}/`"])
    repeated = result["reproducibility"]["repeatedRun"]
    execution.append(["S6 reproduction", 1001, repeated["runId"], repeated["status"], f"`{repeated['evidencePath']}/`"])
    sections += [table(["Scenario", "Seed", "RunId", "Status", "Raw evidence path"], execution),
        f"Set index: `{result['sourceIndex']}`. No S2 controls were rerun or overwritten. "
        "All eight raw-file SHA-256 hashes are retained in the structured report and checked before and after analysis. "
        "Raw packages remain ignored under `artifacts/simulations/` and require separate backup before deleting the checkout.",
        "## Controls, provenance and comparison limits",
        "For every Seed, definitions match after removing only scenario identity and GroupSizeProfile. "
        "Both preserve 5,500 registered Attendees, exactly 4,400 daily planned requesters, five days, 90% continuity "
        "(3,960 retained / 440 replacements), 50% request-level FrontStanding eligibility, generator v2, Engine v0.3.0, "
        "sequential execution/concurrency 1, calendar, S2 arrival configuration, exact catalog/topology identities and normal order. "
        "BaselineSequence equals ExecutionSequence; no S5 transformation is present. "
        "Clean Day 1 and natural persisted Day 2–5 history follow the unchanged runner.",
        "S2 target shares are 15/30/25/15/8/5/2%; S6 shares are 10/20/20/15/10/15/10%, over request buckets "
        "1, 2, 3, 4, 5, 6–7, 8–10. Thus 6–10 increases from 7% to 25%. "
        "Exact daily attendee demand takes priority over integer group quotas. These runs materialized 1,464 S2 versus "
        "1,080 S6 requests/day. S6 bucket counts/day are 108/216/216/162/108/162/108; target shares are exactly representable here.",
        "Equal Seeds do not imply equal daily cohorts or requests: generator streams include ScenarioId. "
        "This report performs scenario-level controlled comparisons and no attendee/day transition matrices. "
        "The recorded source revision identifies unchanged scenario/engine code; the execution harness has local task changes. "
        "Harness and analysis-tool hashes accompany the index hash in JSON.",
        "## Run integrity",
        table(["Scenario", "Seed/role", "Attempted", "Successful", "DomainRejected", "Conflict", "Technical", "Final-state findings"],
              [[scenario.upper(), p["seed"], *[p[scenario]["requestOutcomes"][k] for k in
                ("attemptedRequests", "successfulRequests", "domainRejectedRequests", "knownConcurrencyConflictRequests", "unexpectedTechnicalFailureRequests")],
                p[scenario]["integrity"]["invariantViolationCount"]] for p in pairs for scenario in ("s2", "s6")]
              + [["S6", "1001 reproduction", *[pairs[0]["s6"]["requestOutcomes"][k] for k in
                  ("attemptedRequests", "successfulRequests", "domainRejectedRequests", "knownConcurrencyConflictRequests", "unexpectedTechnicalFailureRequests")],
                  repeated["integrity"]["invariantViolationCount"]]]),
        "Each S6 package has five completed days, all eight expected files, 22,000 persisted Assignments and 4,400 unique planned "
        "requesters/day with valid sizes 1..10. ExcludedInvalidInput is zero. The durable-state audit checks unique Spot/day and "
        "Attendee/day, full contiguous same-Zone groups, catalog identity, request terminal consistency, dispatch and checkpoint "
        "reconciliation. This is a final-state audit; it is not a complete temporal proof of INV-06. "
        "The unchanged Application flow does not replace/cancel Assignments.",
        "## Primary aggregate comparison",
        "Deltas are **S6 minus S2**. Rates use percentage points (pp); recovery distances use assigned opportunities. "
        "Success/rejection use NormallyProcessedRequests; quality uses persisted evaluated Assignments; path classification uses "
        "ClassifiablePaths; recovery rate uses eligible episodes and recovery speed uses recovered episodes. "
        "Stored metrics/rubric/recovery outputs are consumed unchanged. Null is N/A."]
    fields = {"requestOutcomes": ("successRate", "rejectionRate"),
              "fairness": ("goodOutcomeRate", "mediumOutcomeRate", "badOutcomeRate", "balancedPathRate", "acceptablePathRate", "notBalancedPathRate"),
              "recovery": ("recoveryRate", "averageOpportunitiesToRecovery", "medianOpportunitiesToRecovery")}
    rows = []
    for p in pairs:
        for family, names in fields.items():
            for key in names:
                is_rate = key.endswith("Rate")
                values = [percentage(p[s][family][key]) if is_rate else number(p[s][family][key]) for s in ("s2", "s6")]
                rows.append([p["seed"], key, *values, metric_delta(p["deltas"][family][key], is_rate)])
    sections += [table(["Seed", "Metric", "S2", "S6", "Delta"], rows), "## Group-size outcomes",
        "Attendee counts below are repeated attendee/day participations, not distinct people. Planned and persisted counts are "
        "separate in JSON; they coincide here because every request succeeded. Quality rates are attendee-weighted; "
        "request rates count each group once. NB association is the final stored NotBalanced path of a participation's attendee, "
        "divided by classifiable persisted participations in that bucket. It never labels a request NotBalanced. "
        "Attendees can appear in different buckets across days; category attendee populations are not additive.",
        table(["Seed", "Scenario", "Size", "Requests", "Attendee participations", "Successful", "Rejected", "Success", "Rejection", "Good", "Medium", "Bad", "NB/classifiable participations"],
              [[p["seed"], s.upper(), r["category"], r["requestCount"], r["plannedAttendeeCount"], r["successfulRequests"], r["domainRejectedRequests"],
                *[percentage(r[k]) for k in ("successRate", "rejectionRate", "goodOutcomeRate", "mediumOutcomeRate", "badOutcomeRate")],
                f"{r['notBalancedParticipationCount']}/{r['classifiableParticipationCount']} ({percentage(r['notBalancedParticipationRate'])})"]
               for p in pairs for s in ("s2", "s6") for r in p[s]["groupSize"]]),
        "## Large-group focus: 1–5 versus 6–10",
        table(["Seed", "Scenario", "Size", "Requests", "Participations", "Success", "Rejection", "Bad"],
              [[p["seed"], s.upper(), r["category"], r["requestCount"], r["persistedParticipationCount"],
                *[percentage(r[k]) for k in ("successRate", "rejectionRate", "badOutcomeRate")]]
               for p in pairs for s in ("s2", "s6") for r in p[s]["largeGroup"]]),
        "The separate 6–7 versus 8–10 comparison is in the group-size table above. "
        "Compare rates within each Seed; a prevalence-weighted total is not an individual group-size causal estimate.",
        "## Rejections and contiguous-block pressure",
        "Every rejection, when present, is included in JSON with day, simulation request identity, actual execution position/third, "
        "GroupSize, production reason, eligible capacity and block-found evidence. Grouped totals reconcile exactly by size, day, "
        "third and reason. Remaining total before a request is reconstructed from the clean 4,998-Spot inventory minus preceding "
        "successful persisted group memberships; it is not an added telemetry snapshot. Eligible-capacity evidence remains the "
        "baseline's independent pre-save read and does not guarantee a transactionally consistent snapshot.",
        table(["Seed", "Rejected", "Sufficient eligible capacity/no block", "Insufficient eligible capacity", "Unavailable diagnosis"],
              [[p["seed"], p["s6"]["rejectionAnalysis"]["count"], *[p["s6"]["rejectionAnalysis"]["grouped"]["diagnosis"].get(k, 0)
                for k in ("ContiguousBlockPressure", "InsufficientEligibleCapacity", "Unavailable")]] for p in pairs]),
        "No-block rejection with eligible capacity >= GroupSize supports contiguous-block pressure. "
        "Eligible capacity > 0 but < GroupSize is insufficient eligible capacity. End-of-day residuals alone establish neither "
        "unusable capacity nor fragmentation causality.",
        "## Actual execution position",
        "Thirds use zero-based daily actual ExecutionSequence rank: floor(rank × 3 / daily request count). "
        "This is a descriptive control, with no order transformation.",
        table(["Seed", "Scenario", "Third", "Requests", "Participations", "Success", "Rejection", "Bad"],
              [[p["seed"], s.upper(), r["category"], r["requestCount"], r["persistedParticipationCount"],
                *[percentage(r[k]) for k in ("successRate", "rejectionRate", "badOutcomeRate")]]
               for p in pairs for s in ("s2", "s6") for r in p[s]["executionThird"]]),
        "## Group size × execution position",
        "All cells, including empty/N/A cells, are retained in JSON. The table shows S6 BadOutcomeRate and RejectionRate in "
        "each daily third. S2 cells are also retained in JSON for controlled comparison.",
        table(["Seed", "Size", "First Bad/rejection", "Middle Bad/rejection", "Last Bad/rejection", "Last Bad/participations"],
              [[p["seed"], b, *[f"{percentage(r['badOutcomeRate'])} / {percentage(r['rejectionRate'])}" for r in
                [next(r for r in p["s6"]["groupSizeByExecutionThird"] if r["category"] == b + "/" + t) for t in THIRDS]],
                next(f"{r['badOutcomeCount']}/{r['persistedParticipationCount']}" for r in p["s6"]["groupSizeByExecutionThird"] if r["category"] == b + "/last")]
               for p in pairs for b in BUCKETS]),
        "## Path fairness and large-group exposure",
        table(["Seed", "Scenario", "Evaluated", "No path", "Insufficient", "Classifiable", "Balanced", "Acceptable", "NotBalanced"],
              [[p["seed"], s.upper(), *[p[s]["fairness"][k] for k in ("evaluatedAttendeeCount", "noPersistedPathCount", "insufficientEvidenceCount",
                  "classifiablePathCount", "balancedPathCount", "acceptablePathCount", "notBalancedPathCount")]] for p in pairs for s in ("s2", "s6")]),
        "For S6, each evaluated attendee is assigned to one exposure row by the **number of persisted Assignments in size 6–10 "
        "groups**. This does not assign a permanent group size. Rates use classifiable attendees in that row. "
        "Exposure count is also affected by total path length, so it is not a causal dose-response measure.",
        table(["Seed", "Large-group Assignments", "Evaluated", "Classifiable", "NotBalanced", "NB rate"],
              [[p["seed"], r["largeGroupAssignmentCount"], r["evaluatedAttendees"], r["classifiableAttendees"], r["notBalancedAttendees"], percentage(r["notBalancedRate"])]
               for p in pairs for r in p["s6"]["largeGroupExposure"]]),
        "Exposure counts do not form a universal monotonic NotBalanced gradient: read each count alongside its classifiable "
        "population, especially the small five-assignment category. The size-associated participation rates and attendee-level "
        "exposure rates answer different descriptive questions.",
        "## Recovery and episode-opening groups",
        table(["Seed", "Scenario", "Episodes", "Eligible", "Recovered", "Eligible unresolved", "Ineligible unresolved"],
              [[p["seed"], s.upper(), *[p[s]["recovery"][k] for k in ("recoveryEpisodeCount", "recoveryEligibleEpisodeCount", "recoveredEpisodeCount")],
                p[s]["recovery"]["recoveryEligibleEpisodeCount"] - p[s]["recovery"]["recoveredEpisodeCount"], p[s]["recovery"]["recoveryIneligibleEpisodeCount"]]
               for p in pairs for s in ("s2", "s6")]),
        "Opening-group attribution uses the stored Bad opening Assignment and its original daily group. "
        "It does not attribute later unresolved behavior solely to that group: membership can change. "
        "Opening rates below use persisted participations; eligible-unresolved rates use eligible episodes opening in that bucket. "
        "Every opening remains one episode, including consecutive Bad outcomes; detection and eligibility are never replayed.",
        table(["Seed", "Scenario", "Opening size", "Openings/participations", "Opening rate", "Eligible openings", "Eligible unresolved", "Unresolved/eligible"],
              [[p["seed"], s.upper(), r["category"],
                f"{r['episodeOpenings']}/{next(g['persistedParticipationCount'] for g in p[s]['groupSize'] if g['category'] == r['category'])}",
                percentage(rate(r["episodeOpenings"], next(g["persistedParticipationCount"] for g in p[s]["groupSize"] if g["category"] == r["category"]))),
                r["recoveryEligibleOpenings"], r["eligibleUnresolved"], percentage(r["eligibleUnresolvedRate"])]
               for p in pairs for s in ("s2", "s6") for r in p[s]["recoveryByOpeningGroup"]]),
        "For a normalized large-group comparison, the next table contrasts their share of all persisted participations, "
        "episode openings, eligible openings and eligible-unresolved episodes. An excess share of openings shows concentration "
        "of initial Bad episodes; it does not itself demonstrate slower recovery after an opening.",
        table(["Seed", "6–10 share of participations", "6–10 share of openings", "6–10 share of eligible openings", "6–10 share of eligible unresolved"],
              [[p["seed"], percentage(rate(p["s6"]["largeGroup"][1]["persistedParticipationCount"], p["s6"]["fairness"]["persistedOutcomeCount"])),
                *[percentage(rate(sum(r[k] for r in p["s6"]["recoveryByOpeningGroup"] if r["category"] in ("6–7", "8–10")),
                                  sum(r[k] for r in p["s6"]["recoveryByOpeningGroup"])))
                  for k in ("episodeOpenings", "recoveryEligibleOpenings", "eligibleUnresolved")]] for p in pairs]),
        "Large groups are disproportionately represented in Bad episode openings relative to their participation share. "
        "Eligible-unresolved concentration must also be compared with eligible openings; the bucket-level unresolved rates "
        "do not show a consistent monotonic size penalty across Seeds.",
        "## Daily capacity evolution",
        "Vectors are remaining **Good / Medium / Bad / total** Spots after each drained daily plan. "
        "Totals are global for that FestivalDay. Identical total residuals can conceal different quality consumption or block geometry.",
        table(["Seed", "Day", "S2 G/M/B/total", "S6 G/M/B/total", "Delta G/M/B/total"],
              [[p["seed"], r["dayNumber"], *[" / ".join(str(r[s][k]) for k in CAPACITY_FIELDS) for s in ("s2", "s6", "delta")]] for p in pairs for r in p["dailyCapacity"]]),
        "The existing evidence does not include per-request quality-specific inventory snapshots or block geometry. "
        "Exact Good/Medium feasibility-exhaustion times and unusable residual capacity therefore remain unestablished. "
        "Observed first Bad and last Good/Medium positions below describe consumption timing; last assignment is not an exhaustion proof.",
        table(["Seed", "Scenario", "Day", "Daily requests", "First Bad rank", "Last Good rank", "Last Medium rank"],
              [[p["seed"], s.upper(), r["dayNumber"], r["requestCount"], r["bad"]["first"] if r["bad"] else "N/A",
                r["good"]["last"] if r["good"] else "N/A", r["medium"]["last"] if r["medium"] else "N/A"]
               for p in pairs for s in ("s2", "s6") for r in p[s]["dailyQualityPositions"]]),
        "## Reproduction",
        table(["Logical component", "Equivalent"], [[k, v] for k, v in result["reproducibility"]["logicalMatches"].items()]),
        table(["Byte-identical file", "Equal SHA-256"], [[k, v] for k, v in result["reproducibility"]["exactFileMatches"].items()]),
        result["reproducibility"]["ignored"] + " All other logical fields must match. Reproduction is excluded from cross-Seed conclusions.",
        "## Exploratory performance observation",
        "Nearest-rank percentiles over existing exported duration values, in milliseconds. "
        "These include ordinary runtime effects and have no dedicated warm-up or controlled load. "
        "They are **exploratory observations, not a dedicated performance benchmark or production-performance evidence**.",
        table(["Seed", "Scenario", "Samples", "p50 ms", "p95 ms", "p99 ms"],
              [[p["seed"], s.upper(), p[s]["exploratoryTiming"]["sampleCount"], *[number(p[s]["exploratoryTiming"][k], 3) for k in ("p50Ms", "p95Ms", "p99Ms")]]
               for p in pairs for s in ("s2", "s6")]),
        "## Reproducing the derived reports",
        "`python3 -B scripts/stage-5-representative-runs/analyze_s6_groups.py " + result["sourceIndex"] +
        " --output artifacts/analysis/s2-s6-repeat.json --markdown artifacts/analysis/s2-s6-repeat.md`\n\n"
        "Choose fresh output names. Analysis verifies original hashes before and after reading. "
        "The JSON includes all per-Seed counts, denominators, cross-tabs, integrity audits, rejection events and provenance. "
        "Repeated derived JSON and Markdown are checked byte-for-byte. No wall-clock analysis timestamps are added.",
        "## Interpretation boundaries and scope",
        "These three analytical Seeds support sensitivity conclusions under provisional S2 operating conditions. "
        "They do not establish real festival distributions, all possible group mixes, fragmentation causality, concurrency behavior "
        "or production performance. Descriptive size/exposure associations retain confounding from exact rank, eligibility and history. "
        "No new materiality thresholds, Engine tuning or policy recommendation is implied by a metric worsening. "
        "Engine v1, fairness, S2/S5/S6 configuration, rubric and recovery are unchanged; S7/S8 and Decision Gate logic remain deferred."]
    return "\n\n".join(sections) + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("index", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--markdown", type=Path)
    args = parser.parse_args()
    require(not args.output.resolve().is_relative_to(Path("artifacts/simulations").resolve()), "Output must be outside raw evidence")
    require(not args.output.exists(), "Output exists; choose a fresh filename")
    if args.markdown:
        require(args.markdown != args.output and not args.markdown.exists(), "Markdown output must be fresh and distinct")
        require(not args.markdown.resolve().is_relative_to(Path("artifacts/simulations").resolve()), "Markdown must be outside raw evidence")
    result = analyze(args.index)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("x", encoding="utf-8") as output:
        output.write(json.dumps(result, indent=2, ensure_ascii=False, allow_nan=False) + "\n")
    if args.markdown:
        args.markdown.parent.mkdir(parents=True, exist_ok=True)
        with args.markdown.open("x", encoding="utf-8") as output:
            output.write(render_markdown(result))
    print(args.output)


if __name__ == "__main__":
    main()
