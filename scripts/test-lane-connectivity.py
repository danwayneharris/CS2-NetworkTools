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


if __name__=='__main__':unittest.main()
