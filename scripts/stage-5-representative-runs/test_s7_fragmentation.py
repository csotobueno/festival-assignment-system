"""S7 analysis regressions: paired inputs, setup isolation and diagnostic denominators."""

import copy
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import analyze_s7_fragmentation as s7
from test_s6_groups import S2_SHARES, path, row, workload


def setup_pair():
    """Synthetic input fixture, never executed. Exact physical quotas from the contract."""
    source = workload("s2-expected-baseline", S2_SHARES)
    catalog = source["manifest"]["catalog"]
    catalog["zones"], catalog["spots"] = [], []
    topology = [
        ("FrontStanding", [75] * 10, [15] * 10),
        ("MiddleCenter", list(range(78, 90)), [16,16,16,16,16,17,17,17,17,17,17,18]),
        *[(name, list(range(57,69)), [11,12,12,12,12,12,13,13,13,13,13,14]) for name in ("MiddleLeft","MiddleRight","UpperCenter")],
        *[(name, list(range(36,48)), [7,8,8,8,8,8,8,9,9,9,9,9]) for name in ("UpperLeft","UpperRight")]
    ]
    occupied = {v: [] for v in s7.VARIANTS}
    for name, lengths, quotas in topology:
        zone = {"value": name}
        catalog["zones"].append(dict(id=zone, type=name))
        for r, (length, quota) in enumerate(zip(lengths, quotas), 1):
            spots = [dict(code={"value": f"{name}/{r}/{i}"}, zoneId=zone,
                          rowCode={"value": f"R{r:03}"}, number={"value": i}) for i in range(1, length+1)]
            catalog["spots"].extend(spots)
            occupied[s7.VARIANTS[0]].extend(spots[:quota])
            gap, extra = divmod(length-quota, quota+1)
            position = 0
            for i in range(quota):
                position += gap + (i < extra)
                occupied[s7.VARIANTS[1]].append(spots[position])
                position += 1
    result = []
    for variant in s7.VARIANTS:
        data = copy.deepcopy(source)
        manifest = data["manifest"]
        manifest["metadata"].update(scenarioId=variant, workloadSourceScenarioId="s2-expected-baseline",
            fragmentationPatternVersion="s7-preoccupation-v1", openingOccupiedSpotCount=1000,
            workloadSourceManifestSha256="a"*64)
        setup = manifest["setup"]
        for day, festival in zip(manifest["dailyPlans"], manifest["catalog"]["festivalDays"]):
            day["festivalDayId"] = festival["id"] = {"value": str(day["dayNumber"])}
            for i, spot in enumerate(occupied[variant]):
                identity = {"value": f"support/{day['dayNumber']}/{i}"}
                request = {"value": f"request/{day['dayNumber']}/{i}"}
                setup["supportingAttendees"].append(dict(id=identity))
                setup["completedRequests"].append(dict(id=request, festivalDayId=day["festivalDayId"], attendeeCodes=[identity]))
                setup["assignments"].append(dict(id=identity, assignmentRequestId=request,
                    festivalDayId=day["festivalDayId"], attendeeId=identity, spotCode=spot["code"],
                    zoneId=spot["zoneId"], rowCode=spot["rowCode"], spotNumber=spot["number"],
                    experienceQuality=s7.ZONE_QUALITY[spot["zoneId"]["value"]]))
        result.append(data)
    return result


class OpeningAndPairTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.pair = setup_pair()

    def test_equal_capacity_different_geometry_and_no_mutation(self):
        before = json.dumps(self.pair)
        proof = s7.pair_integrity(*self.pair)
        self.assertTrue(proof["equalDailyRowZoneQualityOpeningCapacity"])
        self.assertEqual(proof["controlOpening"][0]["largestRunSum"], 3998)
        self.assertEqual(proof["fragmentedOpening"][0]["largestRunSum"], 328)
        self.assertEqual(json.dumps(self.pair), before)

    def test_catalog_and_every_request_field_are_exactly_paired(self):
        for field, value in (("members", []), ("eligibility", {}), ("requestedAt", "changed"),
                             ("assignedAt", "changed"), ("sequence", -1), ("simulationRequestId", "changed")):
            with self.subTest(field=field):
                changed = copy.deepcopy(self.pair[1])
                changed["manifest"]["dailyPlans"][0]["requests"][0][field] = value
                with self.assertRaisesRegex(ValueError, "workload/catalog"):
                    s7.pair_integrity(self.pair[0], changed)

    def test_source_and_pattern_provenance_mismatch_stop_pairing(self):
        changed = copy.deepcopy(self.pair[1])
        changed["manifest"]["metadata"]["fragmentationPatternVersion"] = "v2"
        with self.assertRaisesRegex(ValueError, "provenance"):
            s7.pair_integrity(self.pair[0], changed)

    def test_duplicate_occupied_spot_fails_opening_validation(self):
        changed = copy.deepcopy(self.pair[0])
        changed["manifest"]["setup"]["assignments"][1]["spotCode"] = changed["manifest"]["setup"]["assignments"][0]["spotCode"]
        with self.assertRaisesRegex(ValueError, "occupied set"):
            s7.opening(changed)

    def test_support_quality_and_day_scope_are_validated(self):
        for field, value in (("experienceQuality", "Bad"), ("festivalDayId", {"value":"2"})):
            changed = copy.deepcopy(self.pair[0])
            changed["manifest"]["setup"]["assignments"][0][field] = value
            with self.assertRaises(ValueError):
                s7.opening(changed)

    def test_support_population_cannot_leak_into_workload_catalog(self):
        changed = copy.deepcopy(self.pair[0])
        changed["manifest"]["setup"]["supportingAttendees"][0]["id"] = changed["manifest"]["catalog"]["attendees"][0]["id"]
        with self.assertRaisesRegex(ValueError, "population"):
            s7.opening(changed)

    def test_s6_profile_and_reversed_order_are_rejected(self):
        changed = copy.deepcopy(self.pair[0])
        changed["definition"]["workload"]["groupSizeProfile"][0]["percentage"] = .10
        with self.assertRaisesRegex(ValueError, "S2 group profile"):
            s7.opening(changed)
        changed = copy.deepcopy(self.pair[0])
        changed["manifest"]["dailyPlans"][0]["requests"][0]["executionSequence"] = 9999
        with self.assertRaisesRegex(ValueError, "processing order"):
            s7.opening(changed)


class RejectionTests(unittest.TestCase):
    def test_capacity_boundary_and_missing_observations(self):
        base = row(["a","b","c"], quality=None, outcome="DomainRejected", group="3")
        for capacity, found, expected in ((0,False,"CapacityShortage"), (2,False,"CapacityShortage"),
                (3,False,"ContiguousBlockUnavailable"), (9,False,"ContiguousBlockUnavailable"),
                (None,False,"N/A"), (9,None,"N/A"), (9,True,"N/A")):
            with self.subTest(capacity=capacity, found=found):
                self.assertEqual(s7.diagnosis(dict(base, eligibleAvailableSpotCount=capacity,
                                                   feasibleContiguousBlockFound=found)), expected)
        self.assertEqual(s7.diagnosis(dict(base, diagnosticCode="OTHER")), "N/A")

    def test_request_denominators_and_no_synthetic_quality(self):
        rows = [row(["a"], quality="Good"), row(["b","c","d"], quality=None, outcome="DomainRejected"),
                row(["e"], quality=None, outcome="DomainRejected")]
        rows[1]["eligibleAvailableSpotCount"] = 3
        rows[2]["eligibleAvailableSpotCount"] = 0
        summary = s7.summarize_segment(rows, [path(a) for a in "abcde"])
        self.assertEqual(summary["rejectionRate"], 2/3)
        self.assertEqual(summary["persistedParticipationCount"], 1)
        self.assertEqual(summary["goodOutcomeRate"], 1)
        self.assertEqual(summary["causes"]["ContiguousBlockUnavailable"]["rateAmongDomainRejected"], .5)
        self.assertEqual(summary["causes"]["ContiguousBlockUnavailable"]["rateAmongAttemptedRequests"], 1/3)

    def test_empty_denominators_are_na(self):
        result = s7.summarize_segment([], [])
        self.assertIsNone(result["rejectionRate"])
        for cause in s7.CAUSES:
            self.assertIsNone(result["causes"][cause]["rateAmongDomainRejected"])

    def test_remaining_capacity_summary_retains_missing_and_zero(self):
        rows = [dict(remainingTotalBeforeRequest=a, eligibleAvailableSpotCount=b) for a,b in ((0,0),(10,5),(20,None))]
        result = s7.capacity_summary(rows)
        self.assertEqual(result["remainingTotalBeforeRequest"], dict(observed=3, missing=0, min=0, median=10, max=20))
        self.assertEqual(result["eligibleAvailableSpotCount"]["median"], 2.5)
        self.assertEqual(result["eligibleAvailableSpotCount"]["missing"], 1)

    def test_first_rejection_preserves_identity_rank_and_capacity(self):
        value = dict(row(["a"], outcome="DomainRejected"), cause="CapacityShortage")
        value["remainingTotalBeforeRequest"] = 100
        value["eligibleAvailableSpotCount"] = 0
        self.assertEqual(s7.event(value)["eligibleAvailableSpotCount"], 0)
        self.assertEqual(s7.event(value)["remainingTotalBeforeRequest"], 100)
        self.assertIsNone(s7.event(None))


class PairedOutcomeTests(unittest.TestCase):
    def test_all_transition_directions_and_harm_causes_reconcile(self):
        control, fragmented = [], []
        for i, (a,b,cause) in enumerate((("Successful","Successful",None),
                ("DomainRejected","DomainRejected","CapacityShortage"),
                ("Successful","DomainRejected","ContiguousBlockUnavailable"),
                ("Successful","DomainRejected","N/A"),
                ("DomainRejected","Successful","CapacityShortage"))):
            control.append(dict(simulationRequestId=str(i), outcome=a, cause=cause))
            fragmented.append(dict(simulationRequestId=str(i), outcome=b, cause=cause))
        result = s7.paired_requests(control, fragmented)
        self.assertEqual(sum(sum(r.values()) for r in result["matrix"].values()), 5)
        self.assertEqual(result["controlSuccessFragmentedRejected"]["total"], 2)
        self.assertEqual(result["controlSuccessFragmentedRejected"]["causes"]["ContiguousBlockUnavailable"], 1)
        self.assertEqual(result["controlRejectedFragmentedSuccess"]["total"], 1)

    def test_unpaired_request_identity_blocks_interpretation(self):
        with self.assertRaisesRegex(ValueError, "Unpaired"):
            s7.paired_requests([dict(simulationRequestId="a")], [dict(simulationRequestId="b")])

    def test_wrong_official_matrix_stops_before_reading_packages(self):
        with tempfile.TemporaryDirectory() as directory:
            index = Path(directory)/"index.json"
            index.write_text("[]")
            with self.assertRaisesRegex(ValueError, "matrix"):
                s7.analyze(index)

    def test_render_is_deterministic_and_keeps_scope_boundaries(self):
        result = dict(executionDate="2026-11-07", timezone="America/Lima", runs=[], pairs=[],
            sourceIndex="test/index.json", sourceIndexSha256="a", executionHarnessSha256="b", analysisToolSha256="c",
            reproducibility=dict(variants=[], normalization="Only runtime fields excluded."))
        with patch.object(s7, "experimental_answers", return_value=[("Question", "Observed answer")] * 11):
            first = s7.render_markdown(result)
            self.assertEqual(first, s7.render_markdown(result))
        for expected in ("Main finding", "Paired request outcomes", "Rejection-time capacity", "requires stakeholder validation",
                         "not a dedicated performance benchmark", "No new materiality threshold"):
            self.assertIn(expected, first)


class ReproductionTests(unittest.TestCase):
    def check(self, change=None):
        fields = ("definition.json", "manifest.json", "checkpoints.json", "metrics.json")
        entry = dict(runId="original", evidencePath="original", integrity=dict(sha256={k:"same" for k in fields}))
        repeated = copy.deepcopy(entry)
        repeated.update(runId="repeat", evidencePath="repeat")
        data = dict(**{"run-metadata": dict(runId="original", startedAt="a", completedAt="b", fragmentationPatternVersion="s7-preoccupation-v1")})
        other = copy.deepcopy(data)
        other["run-metadata"].update(runId="repeat", startedAt="c", completedAt="d")
        normalized = {k:{} for k in ("manifest", "attempts", "attendee-paths", "recovery-observations", "metrics")}
        normalized_other = copy.deepcopy(normalized)
        if change == "checkpoint": repeated["integrity"]["sha256"]["checkpoints.json"] = "changed"
        if change == "metrics": normalized_other["metrics"] = dict(changed=True)
        if change == "pattern": other["run-metadata"]["fragmentationPatternVersion"] = "changed"
        with patch.object(s7, "read_package", side_effect=[data,other]), patch.object(s7, "opening"), \
                patch.object(s7, "analyze_run", return_value=({},[])), \
                patch.object(s7, "normalized", side_effect=[normalized,normalized_other]):
            return s7.reproduction(entry, repeated)

    def test_only_runtime_metadata_differences_are_accepted(self):
        result, _ = self.check()
        self.assertTrue(all(result["logicalMatches"].values()))
        self.assertTrue(all(result["byteIdentical"].values()))

    def test_changed_checkpoint_or_metric_or_pattern_is_not_normalized_away(self):
        for change in ("checkpoint", "metrics", "pattern"):
            with self.subTest(change=change), self.assertRaisesRegex(ValueError, "reproduction mismatch"):
                self.check(change)


if __name__ == "__main__":
    unittest.main()
