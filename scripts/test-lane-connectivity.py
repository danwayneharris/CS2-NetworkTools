import copy
import importlib.util
import json
from pathlib import Path
import unittest

spec=importlib.util.spec_from_file_location('lanes',Path(__file__).with_name('lane-connectivity.py'))
lanes=importlib.util.module_from_spec(spec);spec.loader.exec_module(lanes)


class ConnectivityTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.groups={}
        root=Path(__file__).resolve().parents[1]/'NetworkTools.docs/session-notes/captures/sprint-20260929-one-split'
        for p in sorted(root.glob('*get_junction_snapshot.json')):
            s=json.loads(p.read_text())['result']
            cls.groups.setdefault(s['junction']['index'],[]).append(s)
        assert len(cls.groups)==3

    def test_captured_connector_and_direct_join_have_same_lane_transitions(self):
        for group in self.groups.values():
            before,after=lanes.transitions(group[0],'track'),lanes.transitions(group[-1],'track')
            self.assertTrue(before)
            self.assertEqual(before,after)

    def test_broken_direct_join_is_detected(self):
        s=copy.deepcopy(self.groups[350911][-1])
        before=lanes.transitions(s,'track')
        lane=next(l for l in s['lanes'] if 'track' in l and l['start']['ownerIndex']==350911)
        lane['start']['equalityId']=999999
        self.assertNotEqual(before,lanes.transitions(s,'track'))

    def test_composition_mapping_preserved_but_lateral_swap_detected(self):
        for group in self.groups.values():
            self.assertEqual(lanes.composition_signature(group[0]),lanes.composition_signature(group[-1]))
        s=copy.deepcopy(self.groups[350911][-1])
        before=lanes.composition_signature(s)
        owner=next(o for o in s['owners'] if 'composition' in o)
        rows=owner['composition']['edge']['lanes']
        rows[0]['position'],rows[1]['position']=rows[1]['position'],rows[0]['position']
        self.assertNotEqual(before,lanes.composition_signature(s))

    def test_target_lane_swap_is_not_hidden_by_equal_counts(self):
        s=copy.deepcopy(self.groups[350911][-1])
        before=lanes.transitions(s,'track')
        for lane in s['lanes']:
            if 'track' in lane and lane['owner']['index']==354792:
                for p in ('start','middle','end'):
                    node=lane[p]
                    if node['ownerIndex']==354792:
                        node['laneIndex']=(node['laneIndex'] & ~255) | (3-(node['laneIndex'] & 255))
        after=lanes.transitions(s,'track')
        self.assertEqual(len(before),len(after))
        self.assertNotEqual(before,after)

    def test_same_edge_uturn_is_preserved_and_loss_detected(self):
        root=Path(__file__).resolve().parents[1]/'NetworkTools.docs/session-notes/captures/sprint-20260929-road-splits'
        snapshots=[json.loads(p.read_text())['result'] for p in sorted(root.glob('*get_junction_snapshot.json'))]
        s=next(s for s in snapshots if len(s['incidentEdges'])==4)
        before=lanes.transitions(s,'car')
        self.assertEqual(len(before),16)
        self.assertTrue(any(a[0]==b[0] for a,b in before))
        changed=copy.deepcopy(s)
        turn=next(l for l in changed['lanes'] if 'car' in l and l['owner']==changed['junction']
                  and l['start']['ownerIndex']==l['end']['ownerIndex'])
        changed['lanes'].remove(turn)
        self.assertNotEqual(before,lanes.transitions(changed,'car'))


if __name__=='__main__':unittest.main()
