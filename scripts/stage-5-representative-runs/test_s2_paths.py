import copy
import json
import tempfile
import unittest
from pathlib import Path

from analyze_s2_paths import (
    RESULTS, analyze, arrival_band, association, episode_detail, join_requests,
    reconcile, sequence_third,
)


def identity(value):
    return {"value": value}


def path(attendee, classification, entries):
    return {"attendeeId": identity(attendee), "classification": classification,
            "pathLength": len(entries), "entries": entries}


def entry(assignment, day, quality, timestamp):
    return {"assignmentId": identity(assignment), "festivalDayId": identity(day),
            "experienceQuality": quality, "assignedAt": timestamp}


class S2ExposureTests(unittest.TestCase):
    def test_daily_thirds_use_rank_with_explicit_boundaries(self):
        self.assertEqual([sequence_third(n, 6) for n in range(1, 7)],
                         ["first", "first", "middle", "middle", "last", "last"])
        self.assertEqual(sequence_third(1464, 1464), "last")
        with self.assertRaisesRegex(ValueError, "sequence"):
            sequence_third(0, 6)

    def test_arrival_bands_are_half_open(self):
        bands = [{"start": "07:00:00", "end": "09:00:00"},
                 {"start": "09:00:00", "end": "12:00:00"}]
        self.assertEqual(arrival_band("2026-08-15T08:59:59.9999999Z", bands), "07:00–09:00")
        self.assertEqual(arrival_band("2026-08-15T09:00:00Z", bands), "09:00–12:00")
        with self.assertRaisesRegex(ValueError, "outside"):
            arrival_band("2026-08-15T12:00:00Z", bands)

    def test_group_exposure_preserves_changes_and_counts_participations(self):
        paths = [path("a", "NotBalanced", [{}, {}, {}]), path("b", "Balanced", [{}, {}])]
        requests = [
            {"groupBand": "1", "quality": "Bad", "attendeeIds": ["a"]},
            {"groupBand": "2", "quality": "Medium", "attendeeIds": ["a", "b"]},
            {"groupBand": "2", "quality": "Good", "attendeeIds": ["a", "b"]},
        ]
        rows = {r["category"]: r for r in association(requests, paths, lambda r: r["groupBand"])}
        self.assertEqual(rows["1"]["notBalancedParticipationRate"], 1)
        self.assertEqual(rows["2"]["participationCount"], 4)
        self.assertEqual(rows["2"]["notBalancedParticipationCount"], 2)
        self.assertEqual(rows["2"]["notBalancedParticipationRate"], .5)
        self.assertEqual(rows["2"]["notBalancedDistinctAttendeeCount"], 1)
        self.assertEqual(rows["2"]["distinctAttendeeCount"], 2)
        self.assertEqual(rows["1"]["badParticipationRate"], 1)
        self.assertEqual(rows["2"]["badParticipationRate"], 0)


class S2JoinTests(unittest.TestCase):
    def fixture(self):
        days, attempts, entries = [], [], []
        for number, quality in [(1, "Medium"), (2, "Bad")]:
            day, request = f"d{number}", f"r{number}"
            stamp = f"2026-08-{14 + number}T09:00:00.1234568Z"
            days.append({"festivalDayId": identity(day), "dayNumber": number, "requests": [{
                "simulationRequestId": request, "sequence": number,
                "members": [{"id": identity("a")}], "requestedAt": stamp, "assignedAt": stamp,
                "eligibility": {"allowsFrontStanding": number == 1},
            }]})
            attempts.append({"simulationRequestId": request, "outcome": "Successful",
                             "festivalDayId": identity(day), "groupSize": 1,
                             "attendeeIds": [identity("a")], "attemptSequence": number,
                             "persistedRequestId": identity("durable-" + request),
                             "selectedExperienceQuality": quality, "eligibleAvailableSpotCount": 10,
                             "feasibleContiguousBlockFound": True})
            entries.append(entry("a" + str(number), day, quality, stamp.replace("1234568", "1234560")))
        definition = {"workload": {"arrivalProfile": [{"start": "07:00:00", "end": "23:00:00"}],
                                   "groupSizeProfile": [{"minSize": 1, "maxSize": 1}]}}
        return {"dailyPlans": days}, attempts, [path("a", "NotBalanced", entries)], definition

    def test_global_sequence_resets_daily_rank_and_accepts_persisted_precision(self):
        requests, membership = join_requests(*self.fixture())
        self.assertEqual([r["sequence"] for r in requests], [1, 2])
        self.assertEqual([r["dailyPosition"] for r in requests], [1, 1])
        self.assertEqual([r["sequenceThird"] for r in requests], ["first", "first"])
        self.assertEqual(len(membership), 2)
        self.assertEqual([r["allowsFrontStanding"] for r in requests], [True, False])

    def test_wrong_persisted_quality_fails_instead_of_being_reclassified(self):
        manifest, attempts, paths, definition = self.fixture()
        paths[0]["entries"][1]["experienceQuality"] = "Good"
        with self.assertRaisesRegex(ValueError, "quality"):
            join_requests(manifest, attempts, paths, definition)

    def test_missing_group_member_is_not_silently_dropped(self):
        manifest, attempts, paths, definition = self.fixture()
        attempts[0]["attendeeIds"] = [identity("other")]
        with self.assertRaisesRegex(ValueError, "membership"):
            join_requests(manifest, attempts, paths, definition)


class S2RecoveryTests(unittest.TestCase):
    def test_episode_depth_uses_persisted_entries_across_absent_days(self):
        entries = [entry("a1", "d1", "Bad", "2026-08-15T10:00:00Z"),
                   entry("a4", "d4", "Bad", "2026-08-18T10:00:00Z"),
                   entry("a5", "d5", "Bad", "2026-08-19T10:00:00Z")]
        episode = {"startAssignmentId": identity("a1"), "startFestivalDayId": identity("d1"),
                   "startAssignedAt": entries[0]["assignedAt"], "observedOpportunities": 2}
        detail = episode_detail(episode, path("a", "NotBalanced", entries), {("a", "d1"): {"dayNumber": 1}})
        self.assertEqual(detail["openingPathPosition"], 1)
        self.assertEqual(detail["observedOpportunities"], 2)
        self.assertEqual(detail["laterPersistedQualities"], ["Bad", "Bad"])
        self.assertEqual(detail["laterFestivalDayIds"], ["d4", "d5"])

    def test_inconsistent_stored_unresolved_episode_fails(self):
        entries = [entry("a1", "d1", "Bad", "2026-08-15T10:00:00Z"),
                   entry("a2", "d2", "Good", "2026-08-16T10:00:00Z")]
        episode = {"startAssignmentId": identity("a1"), "startFestivalDayId": identity("d1"),
                   "startAssignedAt": entries[0]["assignedAt"], "observedOpportunities": 1}
        with self.assertRaisesRegex(ValueError, "later persisted"):
            episode_detail(episode, path("a", "NotBalanced", entries), {})

    def test_population_mismatch_stops_analysis(self):
        paths = [path("a", "NotBalanced", [{}, {}])]
        episode = {"attendeeId": identity("a"), "status": "Unresolved",
                   "recoveryEligible": True, "observedOpportunities": 1}
        records = [{"attendeeId": identity("a"), "episodes": [episode]}]
        metrics = {"fairness": {"notBalancedPathCount": 1, "evaluatedAttendeeCount": 1,
                                "classifiablePathCount": 1},
                   "recovery": {"recoveryEligibleEpisodeCount": 1, "recoveredEpisodeCount": 0,
                                "recoveryEpisodeCount": 1, "unresolvedEpisodeCount": 1}}
        official = dict(copy.deepcopy(metrics), eligibleUnresolvedEpisodeCount=1)
        self.assertEqual(reconcile(paths, records, metrics, official)[2]["notBalanced"]["analyzed"], 1)
        metrics["fairness"]["notBalancedPathCount"] = 2
        official["fairness"]["notBalancedPathCount"] = 2
        with self.assertRaisesRegex(ValueError, "NotBalanced reconciliation"):
            reconcile(paths, records, metrics, official)


class S2EvidenceTests(unittest.TestCase):
    def test_tampered_evidence_fails_with_run_and_file_context(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            results = root / RESULTS
            results.parent.mkdir(parents=True)
            runs = []
            for seed in (1001, 2001, 3001):
                directory = root / f"artifacts/simulations/s2-expected-baseline/run-{seed}"
                directory.mkdir(parents=True)
                (directory / "run-metadata.json").write_text("{}")
                runs.append({"scenario": "s2", "seed": seed, "runId": f"official-{seed}",
                             "evidencePath": str(directory.relative_to(root)),
                             "integrity": {"sha256": {"run-metadata.json": "invalid-checksum"}}})
            results.write_text(json.dumps({"officialRuns": runs}))
            with self.assertRaisesRegex(ValueError, r"S2/1001 .*checksum mismatch: run-metadata.json"):
                analyze(root)


if __name__ == "__main__":
    unittest.main()
