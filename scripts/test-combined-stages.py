"""Offline negative tests for diagnostic stage evidence, not a native verifier."""
import copy,json,runpy,unittest
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('summarize-combined-stages.py')))
class Stages(unittest.TestCase):
    def rows(self):
        curve=[[i,0,0] for i in range(4)]
        base=dict(session='s',id=3,mode='Preview',valid=True,nodeCount=2,edgeCount=1,edges=[dict(entity='1:1',input=curve,output=curve)])
        return [{**copy.deepcopy(base),'stage':s} for s in ('horizontal','vertical','final')]
    def run_rows(self,rows):return m['summarize']('\n'.join('[NetworkTools.CombinedStage] '+json.dumps(r) for r in rows))
    def test_valid_and_vertical_change(self):
        r=self.rows();r[1]['edges'][0]['output']=copy.deepcopy(r[1]['edges'][0]['output']);r[1]['edges'][0]['output'][1][1]=2
        self.assertEqual(self.run_rows(r)['completeCaptures'][0]['horizontalToVertical']['maxYChange'],2)
    def test_missing_and_corrupt(self):
        for kind in ('missing','revision','short','xz','nan','original'):
            r=self.rows()
            if kind=='missing':r.pop()
            elif kind=='revision':r[2]['id']=4
            elif kind=='short':r[2]['edges'][0]['output']=r[2]['edges'][0]['output'][:3]
            else:
                r[2]['edges'][0]['output']=copy.deepcopy(r[2]['edges'][0]['output'])
                if kind=='xz':r[2]['edges'][0]['output'][1][0]=100
                elif kind=='nan':r[2]['edges'][0]['output'][1][1]=float('nan')
                else:r[2]['edges'][0]['input']=[[9,9,9]]*4
            with self.subTest(kind=kind),self.assertRaises(ValueError):self.run_rows(r)
if __name__=='__main__':unittest.main()
