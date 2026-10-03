"""Discriminating offline checks for repeat-Apply evidence; no game access."""
import copy,runpy,unittest
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('run-profile-sequence.py')))
class RepeatTests(unittest.TestCase):
    def setUp(self):
        self.nodes=[dict(index=1,version=1,position=dict(x=0,y=0,z=0))]
        self.edges=[dict(index=2,version=1,startNode=dict(index=1,version=1),endNode=dict(index=1,version=1),prefab='road',curve=[dict(x=i,y=0,z=0) for i in range(4)])]
    def test_measures_cumulative_movement(self):
        changed=copy.deepcopy(self.edges);changed[0]['curve'][2]['y']=.07
        result=m['geometry_displacement'](self.nodes,self.edges,self.nodes,changed)
        self.assertAlmostEqual(result['maxControlDisplacement'],.07)
        self.assertGreater(max(result.values()),.05)
    def test_rejects_missing_duplicate_topology_and_nonfinite(self):
        for kind in ('missing','duplicate','topology','nan','short'):
            changed=copy.deepcopy(self.edges)
            if kind=='missing':changed=[]
            elif kind=='duplicate':changed*=2
            elif kind=='topology':changed[0]['prefab']='rail'
            elif kind=='nan':changed[0]['curve'][0]['y']=float('nan')
            else:changed[0]['curve'].pop()
            with self.subTest(kind=kind),self.assertRaises(AssertionError):
                m['geometry_displacement'](self.nodes,self.edges,self.nodes,changed)
    def test_identical_and_small_positions(self):
        changed=copy.deepcopy(self.nodes);changed[0]['position']['y']=.03
        result=m['geometry_displacement'](self.nodes,self.edges,changed,self.edges)
        self.assertLessEqual(max(result.values()),.05)
        self.assertEqual(m['geometry_displacement'](self.nodes,self.edges,self.nodes,self.edges)['maxControlDisplacement'],0)
if __name__=='__main__':unittest.main()
