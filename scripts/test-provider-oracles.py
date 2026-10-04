"""Offline adversarial tests of suite aggregation and Connect acceptance."""
import copy
import json
from pathlib import Path
import runpy
import tempfile
import unittest

suite = runpy.run_path(str(Path(__file__).with_name('run-provider-suite.py')))
tool = runpy.run_path(str(Path(__file__).with_name('exercise-tool-provider.py')))

class SuiteTests(unittest.TestCase):
    def run_cases(self, outcomes):
        calls=[]
        def execute(i, fixture, case):
            calls.append(i)
            if isinstance(outcomes[i], Exception): raise outcomes[i]
            return outcomes[i]
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/'summary.json'
            code=suite['run_cases']([('fixture',str(i)) for i in range(len(outcomes))],execute,path)
            return code,json.loads(path.read_text()),calls

    def test_failure_is_nonzero_even_when_followed_by_pass(self):
        code,rows,calls=self.run_cases([(1,{'passed':False}),(0,{'passed':True})])
        self.assertEqual(1,code)
        self.assertEqual(['assertion-failed','passed'],[r['status'] for r in rows])
        self.assertEqual([0,1],calls)

    def test_incomplete_stops_and_records_remaining_not_run(self):
        for outcome in [(1,None),(0,None),(0,{}),(1,{'passed':True}),RuntimeError('uncertain Apply')]:
            with self.subTest(outcome=outcome):
                code,rows,calls=self.run_cases([outcome,(0,{'passed':True})])
                self.assertEqual(2,code)
                self.assertEqual(['execution-incomplete','not-run'],[r['status'] for r in rows])
                self.assertEqual([0],calls)

    def test_success_requires_nonempty_explicit_reports(self):
        self.assertEqual(0,self.run_cases([(0,{'passed':True})])[0])
        self.assertEqual(2,self.run_cases([])[0])
        self.assertEqual(1,self.run_cases([(0,{'passed':False})])[0])

class ConnectTests(unittest.TestCase):
    def edge(self,i,x):
        return dict(index=i,version=1,prefab='road',startNode={'index':i,'version':1},endNode={'index':i+1,'version':1},curve=[dict(x=x+k,y=0,z=0) for k in range(4)])
    def preview(self,edges):
        return {'previewObservation':[row for e in edges for row in ({'m_Original':{'Index':0,'Version':0}},dict(zip('abcd',[[p['x'],p['y'],p['z']] for p in e['curve']])))]}
    def test_reversal_and_permutation_are_covered(self):
        edges=[self.edge(1,0),self.edge(2,10)]
        actual=copy.deepcopy(edges[::-1]);actual[0]['curve'].reverse()
        self.assertEqual(0,tool['connect_preview_error'](self.preview(edges),actual))
    def test_nearest_neighbor_reuse_cannot_hide_missing_curve(self):
        edges=[self.edge(1,0),self.edge(2,10)]
        actual=[self.edge(3,0),self.edge(4,0)]
        self.assertEqual(10,tool['connect_preview_error'](self.preview(edges),actual))
    def test_extra_missing_duplicate_and_nonfinite_rejected(self):
        edge=self.edge(1,0)
        for actual in [[],[edge,edge]]:
            with self.assertRaises(ValueError):tool['connect_preview_error'](self.preview([edge]),actual)
        with self.assertRaises(ValueError):tool['connect_preview_error'](self.preview([edge,edge]),[edge,edge])
        broken=copy.deepcopy(edge);broken['curve'][1]['x']=float('nan')
        with self.assertRaises(ValueError):tool['connect_preview_error'](self.preview([edge]),[broken])
        with self.assertRaises(ValueError):tool['connect_preview_error']({'previewObservation':[{'a':[0,0,0],'b':[0,0,0],'c':[0,0,0],'d':[0,0]}]},[edge])
    def test_existing_preview_curve_is_not_a_new_edge(self):
        edge=self.edge(1,0);state=self.preview([edge])
        state['previewObservation'] += [{'m_Original':{'Index':99,'Version':1}},dict(zip('abcd',[[100,0,0]]*4))]
        self.assertEqual(0,tool['connect_preview_error'](state,[edge]))
        with self.assertRaises(ValueError):tool['connect_preview_error']({'previewObservation':[dict(zip('abcd',[[0,0,0]]*4))]},[edge])

    def test_matching_reassigns_ambiguous_nearest_choice(self):
        # Actual A can use either preview within .04, B can use only preview 0.
        previews=[self.edge(1,0),self.edge(2,.08)]
        actual=[self.edge(3,.04),self.edge(4,0)]
        self.assertAlmostEqual(.04,tool['connect_preview_error'](self.preview(previews),actual))
    def test_preservation_is_enforced_with_five_cm_geometry_tolerance(self):
        edge=self.edge(1,0);node=dict(index=1,version=1,position=dict(x=0,y=0,z=0))
        changed=copy.deepcopy(edge);changed['curve'][0]['y']=.049
        self.assertTrue(tool['connect_preservation']([node],[edge],[node],[changed])['unchangedExistingEdges'])
        changed['curve'][0]['y']=.051
        self.assertFalse(tool['connect_preservation']([node],[edge],[node],[changed])['unchangedExistingEdges'])
        self.assertFalse(tool['connect_preservation']([node],[edge],[],[edge])['unchangedExistingNodes'])
        changed=copy.deepcopy(edge);changed['prefab']='rail'
        self.assertFalse(tool['connect_preservation']([node],[edge],[node],[changed])['unchangedExistingEdges'])
        good=dict(connected=True,prefabInherited=True,unchangedExistingEdges=True,unchangedExistingNodes=True,newEdges=['3:1'],previewApplyMaxError=.049)
        tool['assert_connect_report'](good)
        for flag in ('connected','prefabInherited','unchangedExistingEdges','unchangedExistingNodes'):
            with self.assertRaises(AssertionError):tool['assert_connect_report'](dict(good,**{flag:False}))
        for error in (.051,float('nan'),float('inf')):
            with self.assertRaises(AssertionError):tool['assert_connect_report'](dict(good,previewApplyMaxError=error))

class SelectedLaneTests(unittest.TestCase):
    def fixture(self):
        before=[];after=[];selected=[]
        for ordinal in range(2):
            owner=10+ordinal;new=30+ordinal;junction=100+ordinal
            def port(eq,edge):return dict(equalityId=eq,ownerIndex=edge,laneIndex=1,secondary=False)
            approach=dict(index=50+ordinal,version=1,owner=dict(index=owner,version=1),car={},start=port(1 if ordinal==0 else 4,owner),middle=port(8,owner),end=port(3 if ordinal==0 else 7,owner))
            added=dict(index=60+ordinal,version=1,owner=dict(index=new,version=1),car={},start=port(4 if ordinal==0 else 1,new),middle=port(9,new),end=port(7 if ordinal==0 else 3,new))
            link=dict(index=70+ordinal,version=1,owner=dict(index=junction,version=1),car={},start=port(3,owner if ordinal==0 else new),middle=port(10,junction),end=port(4,new if ordinal==0 else owner))
            def comp(i):return dict(index=i,composition={'edge':{'width':16,'lanes':[]}})
            old=dict(complete=True,errors=[],junction={'index':junction,'version':1},incidentEdges=[{'index':owner,'version':1}],owners=[comp(owner)],lanes=[approach])
            current=copy.deepcopy(old);current['incidentEdges'].append({'index':new,'version':1});current['owners'].append(comp(new));current['lanes'] += [added,link]
            before.append(old);after.append(current);selected.append({'index':50+ordinal,'version':1})
        return before,after,selected,[(30,1),(31,1)]
    def test_actual_graph_reachability_both_endpoints(self):
        self.assertEqual(2,len(tool['connect_selected_lane_proof'](*self.fixture())))
    def test_missing_reversed_wrong_new_edge_and_stale_lane_fail(self):
        for kind in ('missing','reversed','wrong-edge','stale','composition'):
            before,after,selected,new=self.fixture()
            if kind=='missing':after[0]['lanes'].pop()
            if kind=='reversed':
                link=after[0]['lanes'][-1];link['start'],link['end']=link['end'],link['start']
            if kind=='wrong-edge':new=[(999,1)]
            if kind=='stale':selected[0]['version']=2
            if kind=='composition':after[0]['owners'][0]['composition']['edge']['width']=20
            with self.subTest(kind=kind),self.assertRaises((AssertionError,ValueError)):
                tool['connect_selected_lane_proof'](before,after,selected,new)

if __name__=='__main__':unittest.main()
