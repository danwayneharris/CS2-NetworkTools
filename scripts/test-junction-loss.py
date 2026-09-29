import copy
from pathlib import Path
import runpy
import unittest

M = runpy.run_path(str(Path(__file__).with_name('investigate-junction-loss.py')))


class CapturedLossTests(unittest.TestCase):
    def test_all_observed_directions_agree_with_gate(self):
        result = M['investigate']()
        self.assertEqual(result['requiredConnections'], 4)
        self.assertTrue(result['baseline']['passesCurvatureConstraints'])
        self.assertFalse(result['candidate']['passesCurvatureConstraints'])
        lost = [r for r in result['connections'] if not r['after']['observedConnector']]
        self.assertEqual(len(lost), 1)
        self.assertEqual(lost[0]['source'][0], 55025)
        self.assertEqual(lost[0]['target'][0], 80305)
        for r in result['connections']:
            self.assertEqual(r['after']['passesCurvatureGate'], r['after']['observedConnector'])
        self.assertGreater(lost[0]['requiredAngleReductionDegrees'], 0)
        self.assertGreater(lost[0]['requiredSpanIncreaseMetres'], 0)

    def test_obligations_do_not_depend_on_arm_order(self):
        before = M['analyze'](M['DATA'] / '54998-get_junction_snapshot-before.json')
        reversed_order = copy.deepcopy(before)
        reversed_order['connections'].reverse()
        self.assertEqual(M['rules']['obligations'](before), M['rules']['obligations'](reversed_order))


if __name__ == '__main__': unittest.main()
