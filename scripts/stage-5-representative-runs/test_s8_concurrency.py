"""Analytical regression tests; no simulation or PostgreSQL execution."""

import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from analyze_s8_concurrency import (aggregate_runs, analyze, compare_configurations, group_bucket,
    outcome_metrics, performance, read_package, ticks, useful_throughput, verify_sequential)
from summarize import FILES


def attempt(outcome="Successful", start="00:00:00", duration="00:00:01"):
    return dict(outcome=outcome, processingStartedOffset=start, duration=duration)


def comparison_fixture():
    runs = []
    for seed in (1001, 2001, 3001):
        for level in (1, 4, 16):
            outcomes = outcome_metrics([attempt()] * 2 + [attempt("KnownConcurrencyConflict")] * (level - 1))
            timing = performance([attempt()] * (level + 1))
            timing.update(useful_throughput(2, 5, timing["executionSeconds"]))
            runs.append(dict(seed=seed, concurrencyLevel=level, runId=f"{seed}-c{level}", outcomes=outcomes, performance=timing,
                population=dict(registeredAttendees=10, requestingAttendees=8, participatingAttendees=5 if level == 1 else 2,
                    attendeesWithOnlyRejectedOrFailedAttempts=3 if level == 1 else 6,
                    noPersistedPath=3 if level == 1 else 6, insufficientEvidence=1, classifiable=4 if level == 1 else 1),
                integrity=dict(invariantViolationCount=0, persistedAssignmentCount=5)))
    return runs


class S8AnalysisTests(unittest.TestCase):
    def test_useful_delivery_uses_same_elapsed_interval_as_terminal_throughput(self):
        attempts = [attempt(), attempt("KnownConcurrencyConflict"), attempt("DomainRejected", start="00:00:03")]
        elapsed = performance(attempts)["executionSeconds"]
        values = useful_throughput(outcome_metrics(attempts)["successful"], 3, elapsed)
        self.assertEqual(values["successfulRequestThroughput"], .25)
        self.assertEqual(values["successfulAssignmentThroughput"], .75)
        self.assertEqual(performance(attempts)["throughput"], .75)

    def test_assignment_throughput_counts_only_durable_assignments(self):
        # Two successful groups deliver five rows; failed/rejected membership
        # must not inflate assignment delivery or the successful request count.
        self.assertEqual(useful_throughput(2, 5, 10),
            dict(successfulRequestThroughput=.2, successfulAssignmentThroughput=.5))

    def test_useful_throughput_zero_delivery_and_missing_time(self):
        self.assertEqual(useful_throughput(0, 0, 2),
            dict(successfulRequestThroughput=0, successfulAssignmentThroughput=0))
        for elapsed in (None, 0):
            self.assertTrue(all(v is None for v in useful_throughput(0, 0, elapsed).values()))

    def test_comparison_keeps_all_three_configurations_paired_by_seed(self):
        rows = compare_configurations(list(reversed(comparison_fixture())))
        self.assertEqual([r["seed"] for r in rows], [1001, 2001, 3001])
        for row in rows:
            configs = row["configurations"]
            self.assertEqual(list(configs), ["1", "4", "16"])
            self.assertEqual(configs["1"]["outcomes"]["knownConcurrencyConflictRate"], 0)
            self.assertEqual(configs["16"]["outcomes"]["knownConcurrencyConflictRate"], 15 / 17)
            self.assertEqual(configs["4"]["persistedInvariantViolationCount"], 0)

    def test_comparison_rejects_missing_or_duplicate_configurations(self):
        runs = comparison_fixture()
        for invalid in (runs[:-1], runs[:-1] + [runs[0]], runs + [runs[0]]):
            with self.assertRaisesRegex(ValueError, "nine-run"):
                compare_configurations(invalid)

    def test_population_comparison_exposes_survivor_selection(self):
        configs = compare_configurations(comparison_fixture())[0]["configurations"]
        self.assertEqual(configs["1"]["population"]["requestingAttendees"], configs["16"]["population"]["requestingAttendees"])
        self.assertEqual([configs[c]["population"]["participatingAttendees"] for c in ("1", "4", "16")], [5, 2, 2])
        self.assertEqual([configs[c]["population"]["classifiable"] for c in ("1", "4", "16")], [4, 1, 1])
        self.assertEqual([configs[c]["population"]["noPersistedPath"] for c in ("1", "4", "16")], [3, 6, 6])

    def test_cross_seed_aggregate_min_mean_max_includes_useful_throughput(self):
        runs = comparison_fixture()
        for run in runs:
            multiplier = run["seed"] // 1000
            run["performance"]["successfulRequestThroughput"] = multiplier / 10
            run["performance"]["successfulAssignmentThroughput"] = multiplier / 2
        aggregate = aggregate_runs(runs)
        self.assertEqual(set(aggregate), {"1", "4", "16"})
        for level in aggregate.values():
            self.assertEqual(level["successfulRequestThroughput"], dict(min=.1, mean=.2, max=.3, seeds=3))
            self.assertEqual(level["successfulAssignmentThroughput"], dict(min=.5, mean=1, max=1.5, seeds=3))
            self.assertEqual(level["technicalErrorRate"], dict(min=0, mean=0, max=0, seeds=3))

    def test_serialized_comparison_and_aggregates_replay_identically(self):
        runs = comparison_fixture()
        def summary(values):
            return dict(runs=values, perSeedComparison=compare_configurations(values), crossSeed=aggregate_runs(values))
        encoded = json.dumps(summary(runs), sort_keys=True, allow_nan=False)
        restored = json.loads(encoded)
        self.assertEqual(encoded, json.dumps(summary(restored["runs"]), sort_keys=True, allow_nan=False))

    def test_sequential_evidence_has_no_overlap_or_window_gate(self):
        summary = verify_sequential([attempt(), attempt(start="00:00:02")])
        self.assertEqual(summary["requestCount"], 2)
        self.assertTrue(summary["oneRequestCompletesBeforeNextStarts"])
        with self.assertRaisesRegex(ValueError, "overlap"):
            verify_sequential([attempt(), attempt(start="00:00:00.5")])
        with self.assertRaisesRegex(ValueError, "window provenance"):
            verify_sequential([dict(attempt(), windowNumber=1, windowPosition=1)])

    def test_terminal_throughput_uses_overlap_and_elapsed_gaps(self):
        metrics = performance([attempt(), attempt("KnownConcurrencyConflict")])
        self.assertEqual(metrics["executionSeconds"], 1)
        self.assertEqual(metrics["throughput"], 2)
        metrics = performance([attempt(), attempt(start="00:00:03")])
        self.assertEqual(metrics["executionSeconds"], 4)
        self.assertEqual(metrics["throughput"], .5)

    def test_nearest_rank_percentiles_include_conflicts_and_failures(self):
        attempts = [attempt("UnexpectedTechnicalFailure", duration=f"00:00:{i:02}") for i in range(1, 21)]
        attempts.append(dict(outcome="ExcludedInvalidInput"))
        metrics = performance(attempts)
        self.assertEqual(metrics["sampleCount"], 20)
        self.assertEqual((metrics["p50Ms"], metrics["p95Ms"], metrics["p99Ms"]), (10000, 19000, 20000))

    def test_rates_keep_normal_and_attempted_populations_separate(self):
        values = ["Successful"] * 2 + ["DomainRejected"] + ["KnownConcurrencyConflict"] * 2 + ["UnexpectedTechnicalFailure", "ExcludedInvalidInput"]
        metrics = outcome_metrics([attempt(outcome) for outcome in values])
        self.assertEqual(metrics["attemptedRequests"], 6)
        self.assertEqual(metrics["normallyProcessedRequests"], 3)
        self.assertEqual(metrics["successRate"], 2 / 3)
        self.assertEqual(metrics["rejectionRate"], 1 / 3)
        self.assertEqual(metrics["knownConcurrencyConflictRate"], 2 / 6)
        self.assertEqual(metrics["technicalErrorRate"], 1 / 6)
        self.assertEqual(metrics["excludedInvalidInput"], 1)

    def test_zero_evidence_remains_na(self):
        self.assertIsNone(outcome_metrics([])["successRate"])
        self.assertIsNone(outcome_metrics([attempt("KnownConcurrencyConflict")])["rejectionRate"])
        self.assertIsNone(performance([])["throughput"])
        self.assertIsNone(performance([attempt(duration="00:00:00")])["throughput"])

    def test_monotonic_ticks_preserve_days_and_100_ns_precision(self):
        self.assertEqual(ticks("1.00:00:00.0000001"), 86400 * 10_000_000 + 1)
        self.assertEqual(ticks("00:00:01.1234567"), 11234567)
        self.assertEqual(performance([attempt(start="00:00:00.0000001", duration="00:00:00.0000002")])["executionSeconds"], .0000002)

    def test_group_buckets_preserve_combined_ranges(self):
        self.assertEqual([group_bucket(i) for i in range(1, 11)], ["1", "2", "3", "4", "5", "6–7", "6–7", "8–10", "8–10", "8–10"])

    def test_smoke_and_expanded_matrices_cannot_enter_official_analysis(self):
        with tempfile.TemporaryDirectory() as root:
            index = Path(root) / "index.json"
            index.write_text(json.dumps(dict(role="smoke", status="Passed", runs=[])))
            with self.assertRaisesRegex(ValueError, "Official matrix"):
                analyze(index)
            index.write_text(json.dumps(dict(role="official", status="Completed", runs=[dict(concurrencyLevel=4, seed=1001)] * 7)))
            with self.assertRaisesRegex(ValueError, "six-run"):
                analyze(index)

    def test_checksum_validation_rejects_mutated_evidence(self):
        with tempfile.TemporaryDirectory() as root:
            directory = Path(root)
            metadata = dict(runId="test")
            for name in FILES:
                (directory / name).write_text(json.dumps(metadata if name == "run-metadata.json" else {}))
            entry = dict(evidencePath=root, metadata=metadata, integrity=dict(sha256={
                name: hashlib.sha256((directory / name).read_bytes()).hexdigest() for name in FILES}))
            self.assertEqual(read_package(entry)["run-metadata"], metadata)
            (directory / "attempts.json").write_text("[]")
            with self.assertRaisesRegex(ValueError, "Corrupt evidence"):
                read_package(entry)


if __name__ == "__main__":
    unittest.main()
