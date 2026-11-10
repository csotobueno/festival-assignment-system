import unittest

from analyze_arrival_concurrency import offset_ms, max_active, timing_diagnostics, validate_schedule


class ArrivalAnalysisTests(unittest.TestCase):
    def test_schedules_and_cluster_boundary(self):
        self.assertEqual([offset_ms("Moderate", i) for i in range(5)], [0,25,50,75,100])
        self.assertEqual([offset_ms("BurstLike", i) for i in range(9)], [0,5,10,15,65,70,75,80,130])

    def test_invalid_pattern_or_ordinal(self):
        for pattern, ordinal in (("unknown",0),("Moderate",-1)):
            with self.assertRaises(ValueError): offset_ms(pattern,ordinal)

    def test_half_open_boundary(self):
        self.assertEqual(max_active([(0,10),(10,10)]), 1)

    def test_overlap_and_simultaneous_entries(self):
        self.assertEqual(max_active([(0,10),(5,15),(15,10)]), 2)
        self.assertEqual(max_active([(0,10)]*3), 3)

    def test_empty_intervals_do_not_create_activity(self):
        self.assertEqual(max_active([(0,0),(5,0)]), 0)
        self.assertEqual(max_active([]), 0)

    def test_lag_is_not_use_case_duration(self):
        attempts = [dict(outcome="Successful", processingStartedOffset="00:00:00.0100000",
                         scheduledArrivalOffset="00:00:00.0050000", duration="00:00:00.0200000")]
        result = timing_diagnostics(attempts)
        self.assertEqual(result["meanSchedulingLagMs"], 5)
        self.assertEqual(result["p95SchedulingLagMs"], 5)
        self.assertEqual(result["maxActiveRequests"], 1)
        self.assertEqual(result["meanActiveConcurrency"], 1)

    def test_missing_timing_or_early_start_rejected(self):
        with self.assertRaises(ValueError): timing_diagnostics([dict(outcome="Successful")])
        with self.assertRaises(ValueError):
            timing_diagnostics([dict(outcome="Successful", processingStartedOffset="00:00:00",
                                    scheduledArrivalOffset="00:00:00.0010000", duration="00:00:00.0100000")])

    def test_schedule_validation_detects_drift_and_window_gate(self):
        request = dict(simulationRequestId="r", sequence=1, executionSequence=1, members=[dict(id="a")])
        attempt = dict(simulationRequestId="r", attemptSequence=1, baselineSequence=1, attendeeIds=["a"],
                       scheduledArrivalOffset="00:00:00")
        validate_schedule("Moderate", dict(requests=[request]), [attempt])
        with self.assertRaises(ValueError):
            validate_schedule("Moderate", dict(requests=[request]), [dict(attempt, windowNumber=1)])
        second = dict(request, simulationRequestId="s", sequence=2, executionSequence=2)
        bad = dict(attempt, simulationRequestId="s", attemptSequence=2, baselineSequence=2,
                   scheduledArrivalOffset="00:00:00.0260000")
        with self.assertRaises(ValueError): validate_schedule("Moderate", dict(requests=[request,second]), [attempt,bad])


if __name__ == "__main__": unittest.main()
