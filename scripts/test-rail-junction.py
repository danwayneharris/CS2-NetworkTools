import sys
sys.dont_write_bytecode = True
import importlib.util
import math
from pathlib import Path
import unittest

spec=importlib.util.spec_from_file_location('rail',Path(__file__).with_name('analyze-rail-junction.py'))
rail=importlib.util.module_from_spec(spec);spec.loader.exec_module(rail)
ROOT=Path(__file__).resolve().parents[1]

class RailAnalysisTests(unittest.TestCase):
    def test_analytic_circle_and_clamp(self):
        r=rail.metric((0,0,0),(1,0,0),(10,0,10),(0,0,1))
        self.assertAlmostEqual(r['curviness'],.1)
        self.assertAlmostEqual(r['angleDegrees'],90)
        self.assertEqual(rail.metric((0,0,0),(1,0,0),(0,0,0),(1,0,0))['curviness'],0)
        self.assertAlmostEqual(rail.metric((0,0,0),(1,0,0),(.1,0,0),(-1,0,0))['curviness'],2)
    def test_grade_uses_3d_distance_but_planar_direction(self):
        a=rail.metric((0,0,0),(1,20,0),(10,0,0),(0,-20,1))
        b=rail.metric((0,0,0),(1,20,0),(10,10,0),(0,-20,1))
        self.assertAlmostEqual(a['curviness']/b['curviness'],math.sqrt(2))
    def test_degenerate_tangent_rejected(self):
        with self.assertRaises(ValueError):rail.metric((0,0,0),(0,1,0),(1,0,0),(1,0,0))
    def test_captured_pair_and_default_threshold_hypothesis(self):
        folder=ROOT/'NetworkTools.docs/session-notes/captures/rail-merge-20260928'
        before=rail.read(folder/'before.json');after=rail.read(folder/'after.json')
        rows=rail.compare(before,after)
        self.assertEqual(len(rows),4)
        self.assertEqual(sum(r['after']['connectorPresent'] for r in rows),2)
        # Class default, not a measured instance value; tests evidence consistency only.
        candidate_limit=math.radians(1.8)
        for row in rows:
            for state in ('before','after'):
                r=row[state]
                self.assertEqual(r['curviness']<=candidate_limit,r['connectorPresent'])
        after['citySession']='different'
        with self.assertRaises(ValueError):rail.compare(before,after)

if __name__=='__main__':unittest.main()