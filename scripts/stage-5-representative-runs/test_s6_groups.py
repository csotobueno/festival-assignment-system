"""S6 analysis regressions: controlled inputs, weighting, rejection and exposure joins."""

import copy
import unittest
from datetime import datetime, timedelta
from unittest.mock import patch

from analyze_s6_groups import (BUCKETS, S2_SHARES, S6_SHARES, by_category,
                               control_integrity, episode_groups, exposure,
                               normal_workload, rejected, segment, render_markdown,
                               reproduce, percentage, metric_delta)
from analyze_s5_order import FIELDS


def path(identity, classification="NotBalanced", length=3):
    return dict(attendeeId={"value": identity}, classification=classification, pathLength=length)


def row(members, quality="Bad", outcome="Successful", group="6–7", day="d1"):
    return dict(attendeeIds=members, quality=quality, outcome=outcome, groupSize=len(members),
                groupBand=group, festivalDayId=day, dayNumber=1, executionThird="last",
                simulationRequestId="request/" + members[0], dailyExecutionPosition=3,
                attemptSequence=3, diagnosticCode="NO_CONTIGUOUS_SPOTS_AVAILABLE",
                eligibleAvailableSpotCount=100, feasibleContiguousBlockFound=False,
                remainingTotalBeforeRequest=598)


def workload(scenario, shares):
    profile = [dict(minSize=lo, maxSize=hi, percentage=share) for (lo, hi), share in
               zip(((1, 1), (2, 2), (3, 3), (4, 4), (5, 5), (6, 7), (8, 10)), shares)]
    definition = dict(scenarioId=scenario, seed=1001, generatorVersion="v2", executionMode="Sequential",
                      concurrencyLevel=1, attendeeCount=5500, firstDate="2026-08-15", days=list(range(1, 6)),
                      workload=dict(dailyRequesterCount=4400, dailyParticipationContinuity=.9,
                                    allowsFrontStandingRequestShare=.5, arrivalProfile=["fixed-profile"], groupSizeProfile=profile))
    manifest = dict(metadata={key: definition[key] for key in ("scenarioId", "seed", "generatorVersion", "executionMode", "concurrencyLevel")},
                    setup=dict(assignments=[], completedRequests=[], supportingAttendees=[]), dailyPlans=[],
                    catalog=dict(attendees=[{"id": {"value": str(i)}} for i in range(5500)],
                                 spots=[dict(code=str(i)) for i in range(4998)], zones=["shared-zone"],
                                 festivalDays=[dict(date=f"2026-08-{15+i}", assignmentWindow="07-23") for i in range(5)]))
    for day in range(5):
        members = [(day * 440 + i) % 5500 for i in range(4400)]
        requests = []
        for i in range(440):
            timestamp = (datetime(2026, 8, 15 + day, 7) + timedelta(minutes=i)).isoformat() + "Z"
            requests.append(dict(sequence=day * 440 + i + 1, simulationRequestId=f"{scenario}/{day}/{i}",
                                 members=[dict(id={"value": str(m)}) for m in members[i * 10:(i+1) * 10]],
                                 requestedAt=timestamp, assignedAt=timestamp,
                                 eligibility=dict(allowsFrontStanding=i % 2 == 0)))
        manifest["dailyPlans"].append(dict(dayNumber=day+1, requests=requests))
    return dict(definition=definition, manifest=manifest)


class ControlIntegrityTests(unittest.TestCase):
    def setUp(self):
        self.s2 = workload("s2-expected-baseline", S2_SHARES)
        self.s6 = workload("s6-large-groups", S6_SHARES)

    def test_scenario_comparison_accepts_different_request_ids_membership_order_and_counts(self):
        # Split one ten-person request into two valid requests; restore normal slots/eligibility.
        day = self.s6["manifest"]["dailyPlans"][0]
        first = day["requests"][0]
        extra = copy.deepcopy(first)
        extra["members"] = first["members"][5:]
        first["members"] = first["members"][:5]
        extra["simulationRequestId"] += "/extra"
        day["requests"].insert(1, extra)
        sequence = 0
        for d in self.s6["manifest"]["dailyPlans"]:
            d["requests"].sort(key=lambda q: (q["requestedAt"], q["simulationRequestId"]))
            for i, request in enumerate(d["requests"]):
                sequence += 1
                request["sequence"] = sequence
                request["eligibility"]["allowsFrontStanding"] = i % 2 == 0
        self.assertFalse(control_integrity(self.s2, self.s6)["exactRequestPairingRequired"])

    def test_control_changes_are_rejected(self):
        for key, value in (("arrivalProfile", ["changed"]), ("allowsFrontStandingRequestShare", .8),
                           ("dailyRequesterCount", 4401), ("dailyParticipationContinuity", .8)):
            with self.subTest(key=key):
                changed = copy.deepcopy(self.s6)
                changed["definition"]["workload"][key] = value
                with self.assertRaises(ValueError):
                    control_integrity(self.s2, changed)

    def test_topology_changes_are_rejected(self):
        self.s6["manifest"]["catalog"]["spots"][0]["code"] = "changed"
        with self.assertRaisesRegex(ValueError, "catalog/topology"):
            control_integrity(self.s2, self.s6)

    def test_s5_reversal_and_provenance_are_rejected(self):
        self.s6["manifest"]["dailyPlans"][0]["requests"][0]["executionSequence"] = 440
        with self.assertRaisesRegex(ValueError, "normal processing order"):
            normal_workload(self.s6)
        self.s6["manifest"]["metadata"]["orderingTransformation"] = "reverse-daily-execution-order"
        with self.assertRaisesRegex(ValueError, "S5 transformation"):
            normal_workload(self.s6)

    def test_duplicate_daily_attendee_and_wrong_continuity_are_rejected(self):
        first = self.s6["manifest"]["dailyPlans"][0]["requests"][0]
        first["members"][1] = first["members"][0]
        with self.assertRaisesRegex(ValueError, "daily cohort"):
            normal_workload(self.s6)
        changed = workload("s6-large-groups", S6_SHARES)
        # Preserve unique membership but change the retained intersection by one.
        changed["manifest"]["dailyPlans"][1]["requests"][0]["members"][0]["id"]["value"] = "5000"
        with self.assertRaisesRegex(ValueError, "Continuity"):
            normal_workload(changed)


class SegmentTests(unittest.TestCase):
    def test_quality_is_attendee_weighted_and_request_rates_use_normal_outcomes(self):
        rows = [row(["a"], "Good"), row(["b", "c"], "Bad"), row(["d"], None, "DomainRejected"),
                row(["e"], None, "UnexpectedTechnicalFailure")]
        result = segment(rows, [path(a) for a in "abcde"])
        self.assertEqual(result["plannedAttendeeCount"], 5)
        self.assertEqual(result["persistedParticipationCount"], 3)
        self.assertEqual(result["successRate"], 2/3)
        self.assertEqual(result["rejectionRate"], 1/3)
        self.assertEqual(result["badOutcomeRate"], 2/3)
        self.assertEqual(result["unexpectedTechnicalFailureRequests"], 1)

    def test_empty_segments_and_no_persisted_outcomes_are_na(self):
        self.assertIsNone(segment([], [])["successRate"])
        result = segment([row(["a"], None, "DomainRejected")], [path("a", None, 0)])
        self.assertEqual(result["rejectionRate"], 1)
        self.assertIsNone(result["badOutcomeRate"])
        self.assertIsNone(result["notBalancedParticipationRate"])

    def test_notbalanced_uses_stored_classifiable_participations(self):
        result = segment([row(["a", "b", "c"])], [path("a"), path("b", "Balanced"), path("c", "InsufficientEvidence", 1)])
        self.assertEqual(result["classifiableParticipationCount"], 2)
        self.assertEqual(result["notBalancedParticipationRate"], .5)

    def test_cross_tab_preserves_empty_cells(self):
        result = by_category([row(["a"], group="1")], [path("a")], BUCKETS, lambda r: r["groupBand"])
        self.assertEqual(len(result), 7)
        self.assertEqual(sum(r["requestCount"] for r in result), 1)
        self.assertIsNone(result[-1]["rejectionRate"])

    def test_exposure_counts_large_assignments_without_permanent_attendee_group(self):
        rows = [row(["a", "b", "c", "d", "e", "f"]), row(["a"], group="1", day="d2")]
        result = exposure(rows, [path(a) for a in "abcdefg"])
        self.assertEqual(result[0]["largeGroupAssignmentCount"], 0)
        self.assertEqual(result[0]["evaluatedAttendees"], 1)
        self.assertEqual(result[1]["largeGroupAssignmentCount"], 1)
        self.assertEqual(result[1]["evaluatedAttendees"], 6)

    def test_recovery_attribution_uses_opening_group_not_later_group(self):
        rows = [row(["a"], group="1"), row(["a", "b", "c", "d", "e", "f"], day="d2")]
        data = {"recovery-observations": [dict(episodes=[dict(attendeeId={"value": "a"},
                startFestivalDayId={"value": "d1"}, recoveryEligible=True, status="Unresolved")])]}
        result = episode_groups(data, rows)
        self.assertEqual(result[0]["eligibleUnresolved"], 1)
        self.assertEqual(result[5]["eligibleUnresolved"], 0)


class RejectionTests(unittest.TestCase):
    def test_positive_eligible_capacity_below_group_size_is_insufficient(self):
        request = row(["a", "b", "c"], outcome="DomainRejected")
        request["eligibleAvailableSpotCount"] = 2
        self.assertEqual(rejected([request])["events"][0]["diagnosis"], "InsufficientEligibleCapacity")

    def test_sufficient_eligible_capacity_and_no_block_support_pressure(self):
        result = rejected([row(["a"], outcome="DomainRejected")])
        self.assertEqual(result["events"][0]["diagnosis"], "ContiguousBlockPressure")
        for counts in result["grouped"].values():
            self.assertEqual(sum(counts.values()), result["count"])

    def test_missing_snapshot_and_nonphysical_reason_remain_unavailable(self):
        request = row(["a"], outcome="DomainRejected")
        request["eligibleAvailableSpotCount"] = None
        self.assertEqual(rejected([request])["events"][0]["diagnosis"], "Unavailable")
        request["eligibleAvailableSpotCount"] = 100
        request["diagnosticCode"] = "OTHER_REASON"
        self.assertEqual(rejected([request])["events"][0]["diagnosis"], "Unavailable")

    def test_successful_requests_are_not_classified_as_pressure(self):
        self.assertEqual(rejected([row(["a"])])["count"], 0)


class ReportTests(unittest.TestCase):
    def test_rates_and_deltas_preserve_percentage_point_units_and_na(self):
        self.assertEqual(percentage(.125), "12.500%")
        self.assertEqual(metric_delta(.0125, True), "+1.250 pp")
        self.assertEqual(metric_delta(.0125, False), "+0.012500")
        self.assertEqual(percentage(None), "N/A")
        self.assertEqual(metric_delta(None, True), "N/A")

    def test_markdown_is_deterministic_and_renders_empty_population_as_na(self):
        families = {family: {key: None if key.endswith("Rate") or "Opportunities" in key else 0 for key in keys}
                    for family, keys in FIELDS.items()}
        families["fairness"].update({key: 0 for key in ("evaluatedAttendeeCount", "noPersistedPathCount", "insufficientEvidenceCount",
                                      "classifiablePathCount", "balancedPathCount", "acceptablePathCount", "notBalancedPathCount", "persistedOutcomeCount")})
        families["recovery"]["recoveryIneligibleEpisodeCount"] = 0
        run = dict(**families, runId="test-run", status="Completed", evidencePath="test-evidence", integrity=dict(invariantViolationCount=0),
                   groupSize=by_category([], [], BUCKETS, lambda r: ""),
                   largeGroup=by_category([], [], ("1–5", "6–10"), lambda r: ""),
                   executionThird=by_category([], [], ("first", "middle", "last"), lambda r: ""),
                   groupSizeByExecutionThird=by_category([], [], [b+"/"+t for b in BUCKETS for t in ("first", "middle", "last")], lambda r: ""),
                   largeGroupExposure=[], recoveryByOpeningGroup=[], rejectionAnalysis=dict(count=0, grouped=dict(diagnosis={})),
                   dailyCapacity=[], dailyQualityPositions=[], exploratoryTiming=dict(sampleCount=0, p50Ms=None, p95Ms=None, p99Ms=None))
        pair = dict(seed=1001, s2=run, s6=run, deltas=families, dailyCapacity=[])
        result = dict(pairs=[pair], sourceIndex="test-index", reproducibility=dict(repeatedRun=run,
                      logicalMatches=dict(manifest=True), exactFileMatches={}, ignored="test normalization"))
        first = render_markdown(result)
        self.assertEqual(first, render_markdown(result))
        self.assertIn("N/A", first)
        self.assertIn("no attendee/day transition matrices", first)
        self.assertIn("not a dedicated performance benchmark", first)
        self.assertTrue(first.endswith("\n"))

    def test_reproduction_includes_checkpoints_and_definition_in_logical_boundary(self):
        for changed_field in ("checkpoints", "definition", "run-metadata"):
            with self.subTest(field=changed_field):
                original = {"definition": {}, "checkpoints": [], "run-metadata": {"repositoryCommitSha": "same"}}
                changed = copy.deepcopy(original)
                changed[changed_field] = {"repositoryCommitSha": "changed"} if changed_field == "run-metadata" else ["changed"]
                entry = dict(runId="run", evidencePath="test", integrity=dict(sha256={
                    name: "same" for name in ("definition.json", "manifest.json", "checkpoints.json", "metrics.json")}))
                with patch("analyze_s6_groups.read_package", side_effect=[original, changed]), \
                     patch("analyze_s6_groups.normal_workload"), patch("analyze_s6_groups.analyze_run"), \
                     patch("analyze_s6_groups.normalized", return_value=dict(manifest={}, attempts=[], metrics={})):
                    with self.assertRaisesRegex(ValueError, "Reproduction mismatch"):
                        reproduce(entry, entry)


if __name__ == "__main__":
    unittest.main()
