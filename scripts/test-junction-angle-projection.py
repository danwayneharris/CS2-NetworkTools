import json
import math
from pathlib import Path
import runpy
import unittest

HERE = Path(__file__).resolve().parent
M = runpy.run_path(str(HERE / 'junction-angle-projection.py'))
LOSS = runpy.run_path(str(HERE / 'investigate-junction-loss.py'))
POINTS = runpy.run_path(str(HERE / 'rail-composition-inputs.py'))['connect_positions']


class ProjectionTests(unittest.TestCase):
    def row(self, angle, coefficient=1, bound=30):
        return dict(signedAngle=angle, coefficient=coefficient, span=10,
                    limit=2*math.sin(math.radians(bound)/2)/10)

    def test_satisfies_both_directions(self):
        result = M['project']([self.row(34), self.row(-32, -1)], reserve=0)
        self.assertAlmostEqual(result['rotationDegrees'], -4)

    def test_conflicting_connections_are_infeasible(self):
        self.assertIsNone(M['project']([self.row(40), self.row(-40)], reserve=0)['rotationDegrees'])

    def test_unselected_failure_cannot_be_fixed(self):
        self.assertIsNone(M['project']([self.row(40, 0)], reserve=0)['rotationDegrees'])

    def test_wraparound(self):
        self.assertAlmostEqual(M['project']([self.row(359, bound=.5)], reserve=0)['rotationDegrees'], .5)

    def test_valid_candidate_does_not_move(self):
        self.assertEqual(M['project']([self.row(10)])['rotationDegrees'], 0)

    def test_capture_joint_correction(self):
        baseline = LOSS['analyze'](LOSS['DATA'] / '54998-get_junction_snapshot-before.json')
        required = LOSS['rules']['obligations'](baseline)
        packet = json.loads((LOSS['DATA'] / '54998-applied.json').read_text(encoding='utf-8-sig'))
        rows = M['from_points'](POINTS(packet['result']), required, (80305, 23))
        result = M['project'](rows)
        rotation = result['rotationDegrees']
        self.assertIsNotNone(rotation)
        self.assertGreater(abs(rotation), 2.79)
        for row in rows:
            angle = row['signedAngle'] + row['coefficient'] * rotation
            value = abs(2*math.sin(math.radians(angle)/2))/row['span']
            self.assertLessEqual(value, row['limit']*.98 + 1e-12)
        self.assertEqual(result, M['project'](list(reversed(rows))))
        self.assertFalse(result['validationReady'])
        print('Captured connection-space correction:', result)

    def test_bad_data_rejected(self):
        with self.assertRaises(ValueError): M['project']([])
        with self.assertRaises(ValueError): M['project']([self.row(float('nan'))])


if __name__ == '__main__': unittest.main()
