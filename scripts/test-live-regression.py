"""Offline checks of fixture discovery/identity guards, not game verification."""
import copy
import importlib.util
from pathlib import Path
import unittest

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

if __name__=='__main__': unittest.main()
