"""Acceptance policy and malformed-capture tests for repeat-Apply auditing."""
import copy
import runpy
import unittest
from pathlib import Path

audit = runpy.run_path(str(Path(__file__).with_name("audit-profile-repeat.py")))["audit"]

class RepeatAuditTests(unittest.TestCase):
    def fixture(self, error=0):
        points = [{"x": x, "y": 0, "z": 0} for x in range(4)]
        edge = {"index": 1, "version": 1, "startNode": 2, "endNode": 3, "prefab": "road", "curve": points}
        nodes = [{"index": i, "version": 1, "position": points[i-2]} for i in (2, 3)]
        result = {"beforeEdges": [edge], "beforeNodes": nodes, "afterEdges": copy.deepcopy([edge]), "afterNodes": copy.deepcopy(list(reversed(nodes)))}
        result["afterEdges"][0]["curve"][1]["y"] = error
        return result

    def test_tolerance_and_reordered_nodes(self):
        for e in (0, .004, .05): self.assertTrue(audit(self.fixture(e))["idempotentWithinTolerance"])
        self.assertFalse(audit(self.fixture(.0501))["idempotentWithinTolerance"])

    def test_topology_is_not_a_distance_tolerance(self):
        d = self.fixture(); d["afterEdges"][0]["endNode"] = 4
        self.assertFalse(audit(d)["idempotentWithinTolerance"])

    def test_missing_controls_and_nonfinite_are_not_success(self):
        for e in (float("nan"), float("inf")):
            with self.assertRaises(ValueError): audit(self.fixture(e))
        d = self.fixture(); d["afterEdges"][0]["curve"].pop()
        with self.assertRaises(ValueError): audit(d)

if __name__ == "__main__": unittest.main()
