import unittest

from summarize import check_metadata


class MetadataComparisonTests(unittest.TestCase):
    def test_index_and_exporter_serialization_can_differ_without_changing_metadata(self):
        # Regression: the unmodified exporter writes named enums and seven-digit
        # UTC Z timestamps; the harness index uses default System.Text.Json forms.
        check_metadata(
            {"seed": 1001, "executionMode": "Sequential", "startedAt": "2026-11-03T23:00:18.8523480Z"},
            {"seed": 1001, "executionMode": 0, "startedAt": "2026-11-03T23:00:18.852348+00:00"})

    def test_different_seed_is_not_accepted(self):
        with self.assertRaises(AssertionError):
            check_metadata({"seed": 1001}, {"seed": 2001})

    def test_different_execution_mode_is_not_accepted(self):
        with self.assertRaises(AssertionError):
            check_metadata({"executionMode": "Sequential"}, {"executionMode": 1})


if __name__ == "__main__":
    unittest.main()
