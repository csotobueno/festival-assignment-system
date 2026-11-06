"""Regression checks for paired identity joins and evidence-integrity failures."""

import copy
import json
import tempfile
import unittest
from pathlib import Path

from analyze_s5_order import (CLASSIFICATIONS, FILES, QUALITY, identity_sensitivity,
                              pair_integrity, quality_comparison, read_package, transitions)
from summarize import normalized


def workloads():
    manifest = dict(metadata=dict(scenarioId="s2-expected-baseline", seed=1001,
                    generatorVersion="v2", executionMode="Sequential", concurrencyLevel=1),
                    catalog=dict(festivalDays=[1, 2, 3, 4, 5], spots=[dict(id="spot", number=1)]),
                    setup={}, dailyPlans=[])
    for day in range(5):
        manifest["dailyPlans"].append(dict(dayNumber=day + 1, festivalDayId={"value": str(day)}, requests=[
            dict(simulationRequestId=f"{day}/{i}", sequence=day * 440 + i + 1,
                 members=[dict(id={"value": str(i * 10 + member)}) for member in range(10)],
                 eligibility=dict(allowsFrontStanding=i % 2 == 0), requestedAt="2026-08-15T09:00:00Z",
                 assignedAt="2026-08-15T09:00:00Z") for i in range(440)]))
    baseline = dict(definition=dict(seed=1001), manifest=manifest)
    experimental = copy.deepcopy(baseline)
    experimental["manifest"]["metadata"].update(scenarioId="s5-adverse-arrival-order",
        workloadSourceScenarioId="s2-expected-baseline", orderingTransformation="reverse-daily-execution-order")
    for day in experimental["manifest"]["dailyPlans"]:
        for request, slot in zip(day["requests"], reversed([q["sequence"] for q in day["requests"]])):
            request["executionSequence"] = slot
    return baseline, experimental


class PairIntegrityTests(unittest.TestCase):
    def test_exact_reversal_accepts_retained_controls_without_additive_fields(self):
        baseline, experimental = workloads()
        before = copy.deepcopy((baseline, experimental))
        self.assertTrue(all(pair_integrity(baseline, experimental).values()))
        self.assertEqual((baseline, experimental), before)

    def test_workload_changes_block_paired_interpretation(self):
        baseline, experimental = workloads()
        for field, value in (("requestedAt", "2026-08-15T10:00:00Z"),
                             ("assignedAt", "2026-08-15T10:00:00Z"),
                             ("eligibility", {"allowsFrontStanding": False}),
                             ("members", [{"id": {"value": "different"}}]),
                             ("sequence", 99999)):
            with self.subTest(field=field):
                changed = copy.deepcopy(experimental)
                changed["manifest"]["dailyPlans"][0]["requests"][0][field] = value
                with self.assertRaises(ValueError):
                    pair_integrity(baseline, changed)

    def test_changed_topology_is_rejected(self):
        baseline, experimental = workloads()
        experimental["manifest"]["catalog"]["spots"][0]["number"] = 2
        with self.assertRaisesRegex(ValueError, "Workload mismatch"):
            pair_integrity(baseline, experimental)

    def test_unchanged_execution_order_is_rejected(self):
        baseline, experimental = workloads()
        for day in experimental["manifest"]["dailyPlans"]:
            for request in day["requests"]:
                request["executionSequence"] = request["sequence"]
        with self.assertRaisesRegex(ValueError, "Not exact daily reverse"):
            pair_integrity(baseline, experimental)

    def test_equal_sets_with_different_member_order_are_rejected(self):
        baseline, experimental = workloads()
        experimental["manifest"]["dailyPlans"][0]["requests"][0]["members"].reverse()
        with self.assertRaisesRegex(ValueError, "Workload mismatch"):
            pair_integrity(baseline, experimental)


class OutcomeComparisonTests(unittest.TestCase):
    def test_all_quality_cells_have_correct_direction(self):
        left = {(a, b): {"quality": a} for a in QUALITY for b in QUALITY}
        right = {(a, b): {"quality": b} for a in QUALITY for b in QUALITY}
        result = quality_comparison(left, right)
        self.assertEqual(result["matrix"], {a: {b: 1 for b in QUALITY} for a in QUALITY})
        for label in ("improved", "worsened", "unchanged"):
            self.assertEqual(result["changes"][label], dict(count=3, rate=1 / 3))

    def test_missing_assignments_remain_separate_with_status(self):
        left = {(str(i), "day"): dict(quality=q, requestOutcome="Successful" if q else "DomainRejected")
                for i, q in enumerate(("Good", None, "Bad", None))}
        right = {(str(i), "day"): dict(quality=q, requestOutcome="Successful" if q else "DomainRejected")
                 for i, q in enumerate((None, "Good", "Medium", None))}
        result = quality_comparison(left, right)
        self.assertEqual(result["assignmentCoverage"], dict(assignedBoth=1, assignedOnlyS2=1, assignedOnlyS5=1, assignedNeither=1))
        self.assertEqual(result["matrix"]["Bad"]["Medium"], 1)
        self.assertEqual(result["changes"]["improved"], dict(count=1, rate=1))
        self.assertEqual(result["missingCases"]["assignedOnlyS2"][0]["s5"]["requestOutcome"], "DomainRejected")

    def test_mismatched_participation_cohorts_are_rejected(self):
        with self.assertRaisesRegex(ValueError, "cohort mismatch"):
            quality_comparison({("a", "d"): {"quality": "Good"}}, {})

    def test_insufficient_and_empty_paths_are_not_ranked(self):
        left = dict(a="Balanced", b="InsufficientEvidence", c="NoPath", d="NotBalanced")
        right = dict(a="Acceptable", b="Balanced", c="NoPath", d="NotBalanced")
        result = transitions(left, right, CLASSIFICATIONS, CLASSIFICATIONS[:3])
        self.assertEqual(result["unrankedCount"], 2)
        self.assertEqual(result["changes"]["worsened"], dict(count=1, rate=.5))
        self.assertEqual(result["changes"]["improved"]["count"], 0)

    def test_no_assigned_both_yields_na_rates(self):
        result = quality_comparison({("a", "d"): {"quality": None}}, {("a", "d"): {"quality": None}})
        self.assertIsNone(result["changes"]["unchanged"]["rate"])

    def test_identity_overlap_uses_union_and_reports_turnover_both_ways(self):
        result = identity_sensitivity({"a", "b", "c"}, {"b", "c", "d", "e"})
        self.assertEqual(result["intersectionCount"], 2)
        self.assertEqual(result["unionCount"], 5)
        self.assertEqual(result["overlapRate"], 2 / 5)
        self.assertEqual(result["churnRate"], 3 / 5)
        self.assertEqual(result["s2RetentionRate"], 2 / 3)

    def test_empty_identity_sets_have_na_overlap(self):
        self.assertIsNone(identity_sensitivity(set(), set())["overlapRate"])


class EvidenceTests(unittest.TestCase):
    def test_missing_package_file_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaisesRegex(ValueError, "Incomplete evidence"):
                read_package(dict(evidencePath=folder))

    def test_checksum_corruption_is_rejected_before_outcome_analysis(self):
        with tempfile.TemporaryDirectory() as folder:
            for name in FILES:
                (Path(folder) / name).write_text(json.dumps({}))
            entry = dict(evidencePath=folder, integrity=dict(sha256={n: "wrong" for n in FILES}))
            with self.assertRaisesRegex(ValueError, "Checksum mismatch"):
                read_package(entry)

    def test_reproduction_ignores_timing_and_generated_ids_but_keeps_order_and_outcomes(self):
        def package(folder, suffix, sequence=1, quality="Good"):
            data = {name[:-5]: {} for name in FILES}
            data["manifest"] = {"executionSequence": 1}
            data["attempts"] = [dict(simulationRequestId="stable-request", persistedRequestId={"value": "request-" + suffix},
                                     attemptSequence=sequence, selectedExperienceQuality=quality,
                                     startedAt=suffix, completedAt=suffix, duration=suffix)]
            data["attendee-paths"] = [dict(attendeeId={"value": "attendee"}, entries=[
                dict(assignmentId={"value": "assignment-" + suffix}, festivalDayId={"value": "day"}, experienceQuality=quality)])]
            data["recovery-observations"] = [{"startAssignmentId": {"value": "assignment-" + suffix}}]
            for name in FILES:
                (Path(folder) / name).write_text(json.dumps(data[name[:-5]]))
        with tempfile.TemporaryDirectory() as first, tempfile.TemporaryDirectory() as second:
            package(first, "first")
            package(second, "second")
            self.assertEqual(normalized(first), normalized(second))
            package(second, "second", sequence=2)
            self.assertNotEqual(normalized(first), normalized(second))
            package(second, "second", quality="Bad")
            self.assertNotEqual(normalized(first), normalized(second))


if __name__ == "__main__":
    unittest.main()
