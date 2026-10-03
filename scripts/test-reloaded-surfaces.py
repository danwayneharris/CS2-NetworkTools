"""Offline negative contracts for native surface persistence observations."""
import runpy,unittest
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('compare-reloaded-surfaces.py')))
class SurfaceComparisonTests(unittest.TestCase):
    def test_equal(self):
        self.assertEqual(m['difference']([dict(x=1,y=2,z=3)],[dict(x=1,y=2,z=3)]),0)
    def test_meter_change(self):
        self.assertAlmostEqual(m['difference']([dict(x=1,y=2,z=3)],[dict(x=1,y=3.84,z=3)]),1.84)
    def test_missing_extra_and_empty(self):
        for a,b in [([],[]),([dict(x=0,y=0,z=0)],[]),([dict(x=0,y=0,z=0)],[dict(x=0,y=0,z=0)]*2)]:
            with self.assertRaises(ValueError):m['difference'](a,b)
    def test_nonfinite(self):
        with self.assertRaises(ValueError):m['difference']([dict(x=0,y=float('nan'),z=0)],[dict(x=0,y=0,z=0)])
if __name__=='__main__':unittest.main()
