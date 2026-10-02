"""Offline checks of fixture discovery/identity guards, not game verification."""
import copy
import importlib.util
from pathlib import Path
import unittest
import tempfile

spec = importlib.util.spec_from_file_location('runner', Path(__file__).with_name('live-regression.py'))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)

class FixtureTests(unittest.TestCase):
    def setUp(self):
        self.nodes = [dict(index=i, version=1, position=dict(x=i,y=0,z=0)) for i in (1,2)]
        self.edges = [dict(prefab='rail', startNode=dict(index=1,version=1),
            endNode=dict(index=2,version=1), curve=[dict(x=i,y=0,z=0) for i in (1,1.3,1.7,2)])]

    def test_reload_identity_does_not_change_fingerprint(self):
        nodes,edges=copy.deepcopy((self.nodes,self.edges))
        for n in nodes: n['index']+=50; n['version']=3
        for end in ('startNode','endNode'):
            edges[0][end]['index']+=50; edges[0][end]['version']=3
        self.assertEqual(runner.fingerprint(self.nodes,self.edges),runner.fingerprint(nodes,edges))

    def test_modified_geometry_invalidates_baseline(self):
        changed=copy.deepcopy(self.edges)
        changed[0]['curve'][1]['z']=0.01
        self.assertNotEqual(runner.fingerprint(self.nodes,self.edges),runner.fingerprint(self.nodes,changed))

    def test_missing_or_duplicate_node_rejected(self):
        with self.assertRaises(ValueError): runner.resolve(self.nodes,(9,0,0))
        with self.assertRaises(ValueError): runner.resolve(self.nodes+[self.nodes[0]],(1,0,0))
        self.assertEqual(runner.resolve(self.nodes,(1,0,0)),dict(index=1,version=1))

    def test_partial_snapshot_rejected(self):
        with self.assertRaises(ValueError): runner.connections(dict(complete=False,errors=[]))

    def test_equal_counts_do_not_hide_changed_connections(self):
        node=dict(index=10,version=1)
        def endpoint(owner): return dict(ownerIndex=owner,laneIndex=1,secondary=False)
        a=dict(complete=True,errors=[],junction=node,lanes=[dict(owner=node,track={},
            start=endpoint(1),end=endpoint(2))])
        b=copy.deepcopy(a); b['lanes'][0]['end']=endpoint(3)
        self.assertEqual(len(runner.connections(a)),len(runner.connections(b)))
        self.assertNotEqual(runner.connections(a),runner.connections(b))

    def test_stability_ignores_capture_time_but_not_pending_or_geometry(self):
        edges=copy.deepcopy(self.edges)
        edges[0].update(index=3,version=1)
        snapshot=dict(complete=True,errors=[],junction=dict(index=1,version=1),
                      lanes=[],incidentEdges=[],owners=[dict(index=1,version=1,updated=False)])
        original=runner.permanent_signature([snapshot],self.nodes,edges)
        snapshot['capturedUtc']='later'
        self.assertEqual(original,runner.permanent_signature([snapshot],self.nodes,edges))
        snapshot['owners'][0]['updated']=True
        self.assertIsNone(runner.permanent_signature([snapshot],self.nodes,edges))
        snapshot['owners'][0]['updated']=False
        edges[0]['curve'][1]['z']=0.01
        self.assertNotEqual(original,runner.permanent_signature([snapshot],self.nodes,edges))

class CombinedConstraintTests(unittest.TestCase):
    def test_only_free_interiors_can_change_height(self):
        original={(i,1):dict(index=i,version=1,position=dict(x=i,y=0,z=0),edges=[{},{}]) for i in range(5)}
        original[(3,1)]['edges'].append({})
        selected={(0,1),(1,1),(2,1),(3,1)};anchors={(0,1),(2,1)}
        for moved in range(5):
            changed=copy.deepcopy(original);changed[(moved,1)]['position']['y']=1
            for combined in (False,True):
                if combined and moved in (1,3):
                    self.assertEqual([],runner.check_node_constraints(original,changed,selected,anchors,combined))
                else:
                    with self.assertRaises(AssertionError): runner.check_node_constraints(original,changed,selected,anchors,combined)
    def test_tolerance_and_nonfinite_are_distinct(self):
        original={(1,1):dict(position=dict(x=0,y=0,z=0),edges=[])}
        changed=copy.deepcopy(original);changed[(1,1)]['position']['y']=.03
        runner.check_node_constraints(original,changed,set(),set(),True)
        changed[(1,1)]['position']['y']=float('nan')
        with self.assertRaises(AssertionError): runner.check_node_constraints(original,changed,{(1,1)},set(),True)

class IncidentCurveTests(unittest.TestCase):
    def test_both_ends_and_forbidden_edits(self):
        nodes={(i,1):dict(position=dict(x=i,y=0,z=0)) for i in (1,2)}
        moved=copy.deepcopy(nodes);moved[(1,1)]['position']['y']=2;moved[(2,1)]['position']['y']=-3
        edges={(9,1):dict(startNode=dict(index=1,version=1),endNode=dict(index=2,version=1),curve=[dict(x=i,y=0,z=0) for i in range(4)])}
        result=copy.deepcopy(edges)
        for i,p in enumerate(result[(9,1)]['curve']): p['y']=2 if i<2 else -3
        self.assertEqual(0,runner.check_incident_curves(nodes,moved,edges,result,set(),True))
        with self.assertRaises(AssertionError): runner.check_incident_curves(nodes,moved,edges,result,set(),False)
        for axis,value in [('x',1),('y',1),('z',1),('y',float('nan'))]:
            broken=copy.deepcopy(result);broken[(9,1)]['curve'][1][axis]+=value
            with self.assertRaises(AssertionError): runner.check_incident_curves(nodes,moved,edges,broken,set(),True)
        with self.assertRaises(AssertionError): runner.check_incident_curves(nodes,moved,edges,edges,set(),True)

class TransportTests(unittest.TestCase):
    def test_session_is_pinned_and_error_retains_request_id(self):
        class FakeClient:
            last_request = 'original-intent'
            def call(self, command, args, *, expected):
                self.expected = expected
                raise RuntimeError('stale_session')
        with tempfile.TemporaryDirectory() as folder:
            r = runner.Runner.__new__(runner.Runner)
            r.output = Path(folder)
            r.sequence = 0
            r.city_session = 'city-a'
            r.transport_session = dict(session='process-a', citySession='city-a')
            r.client = FakeClient()
            with self.assertRaisesRegex(RuntimeError, 'stale_session'):
                r.call('save_checkpoint', {'label': 'test'})
            self.assertEqual(r.client.expected, r.transport_session)
            import json
            evidence = json.loads((r.output / '001-save_checkpoint.error.json').read_text())
            self.assertEqual(evidence['requestId'], 'original-intent')

    def test_requested_city_mismatch_never_calls_transport(self):
        with tempfile.TemporaryDirectory() as folder:
            r = runner.Runner.__new__(runner.Runner)
            r.output = Path(folder)
            r.sequence = 0
            r.city_session = 'expected-city'
            r.transport_session = dict(session='process-a', citySession='other-city')
            r.client = object()  # No call method: reaching transport would fail.
            with self.assertRaisesRegex(RuntimeError, 'City changed before'):
                r.call('save_checkpoint')

if __name__=='__main__': unittest.main()
