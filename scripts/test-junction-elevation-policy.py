"""Independent bounds oracle negative tests; does not establish native behavior."""
import copy,runpy,unittest
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))
class Bounds(unittest.TestCase):
    def test_defaults_and_invalid(self):
        self.assertTrue(m['junction_elevation_policy']({})['unlimitedJunctionElevation'])
        for value in (True,-1,21,float('nan'),float('inf'),'1'):
            with self.subTest(value=value),self.assertRaises(ValueError):
                m['junction_elevation_policy']({'junctionElevationLimit':value})
    def test_finite_disabled_unlimited_and_nonfinite(self):
        before={(1,1):{'edges':[1,2,3],'position':{'y':10}}}
        after=copy.deepcopy(before)
        def check(policy,y):
            after[(1,1)]['position']['y']=y
            return m['check_junction_elevation_bounds'](before,after,{(1,1)},m['junction_elevation_policy'](policy))
        self.assertEqual(check({'unlimitedJunctionElevation':False,'junctionElevationLimit':1},11)[0]['absoluteHeightChange'],1)
        check({'allowJunctionElevation':False},10.03)
        check({},100)
        for policy,y in (({'allowJunctionElevation':False},10.1),({'unlimitedJunctionElevation':False,'junctionElevationLimit':1},11.1),({},float('nan'))):
            with self.subTest(policy=policy,y=y),self.assertRaises(AssertionError):check(policy,y)
if __name__=='__main__':unittest.main()
