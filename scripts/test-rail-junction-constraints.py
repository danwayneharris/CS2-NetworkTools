import sys
sys.dont_write_bytecode = True
import copy
import runpy
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
m = runpy.run_path(str(HERE / 'rail-junction-constraints.py'))
analyze = runpy.run_path(str(HERE / 'analyze-rail-composition.py'))['analyze']
DATA = HERE.parent / 'NetworkTools.docs/session-notes/captures/rail-pair-20260928'


class ConstraintTests(unittest.TestCase):
    def test_preserve_working(self):
        working = analyze(DATA / '350919.json')
        required = m['obligations'](working)
        self.assertEqual(len(required), 4)
        self.assertTrue(m['evaluate'](working, required)['passesCurvatureConstraints'])

    def test_preserve_is_not_repair(self):
        broken = analyze(DATA / '358242.json')
        required = m['obligations'](broken)
        self.assertEqual(len(required), 2)
        self.assertTrue(m['evaluate'](broken, required)['passesCurvatureConstraints'])
        with self.assertRaises(ValueError):
            m['obligations'](broken, 'repair')
        branch = {((360575, 1), (361395, 1)), ((361395, 1), (360575, 1))}
        repair = m['obligations'](broken, 'repair', branch)
        result = m['evaluate'](broken, repair)
        self.assertFalse(result['passesCurvatureConstraints'])
        failed = [r for r in result['constraints'] if not r['passes']]
        self.assertEqual(len(failed), 2)
        for row in failed:
            self.assertGreater(row['minimumSpanMetres'], row['availableSpanMetres'])
            self.assertLess(row['maximumAngleDegrees'], row['angleDegrees'])

    def test_bad_identity_and_nonfinite_fail(self):
        working = analyze(DATA / '350919.json')
        required = m['obligations'](working)
        bad = copy.deepcopy(working)
        bad['connections'][0]['limit'] = float('nan')
        bad_required = {m['pair'](bad['connections'][0])}
        with self.assertRaises(ValueError): m['evaluate'](bad, bad_required)
        with self.assertRaises(ValueError): m['evaluate'](working, {((0, 1), (1, 1))})
        with self.assertRaises(ValueError): m['evaluate'](working, required, 1)

    def test_reserve_cannot_make_a_failure_pass(self):
        working = analyze(DATA / '350919.json')
        required = m['obligations'](working)
        self.assertFalse(m['evaluate'](working, required, .5)['passesCurvatureConstraints'])


if __name__ == '__main__': unittest.main()
