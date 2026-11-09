"""Analytical regression tests; no simulation or PostgreSQL execution."""

import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from analyze_s8_concurrency import analyze, group_bucket, outcome_metrics, performance, read_package, ticks
from summarize import FILES


def attempt(outcome="Successful", start="00:00:00", duration="00:00:01"):
    return dict(outcome=outcome, processingStartedOffset=start, duration=duration)


class S8AnalysisTests(unittest.TestCase):
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
