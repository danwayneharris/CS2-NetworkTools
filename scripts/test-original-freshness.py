import copy
import json
from pathlib import Path
import runpy
import unittest

HERE = Path(__file__).resolve().parent
M = runpy.run_path(str(HERE / 'preview-freshness.py'))
DATA = HERE.parent / 'NetworkTools.docs/session-notes/captures/preview-resolver-20260928'


class OriginalFreshnessTests(unittest.TestCase):
    def setUp(self):
        self.packet = json.loads((DATA / '79436-original.json').read_text(encoding='utf-8-sig'))
        self.token = (self.packet['citySession'], 'tool', 10)

    def reject(self, other):
        return M['observation_rejection'](self.token, self.token, self.packet, other)

    def test_unchanged_and_read_metadata(self):
        other = copy.deepcopy(self.packet)
        other['id'] = 'different-query'
        other['result']['capturedUtc'] = 'later'
        other['result']['owners'].reverse()
        other['result']['owners'][0]['updated'] = True
        self.assertIsNone(self.reject(other))

    def test_external_changes_with_unchanged_revision(self):
        mutations = [
            lambda s: s['owners'][0]['position'].__setitem__('x', 999),
            lambda s: s['owners'][1]['curve'][1].__setitem__('x', 999),
            lambda s: s['owners'][1]['startNode'].__setitem__('version', 999),
            lambda s: s['owners'][1]['prefab'].__setitem__('version', 999),
        ]
        for mutate in mutations:
            with self.subTest(mutation=mutate):
                other = copy.deepcopy(self.packet)
                mutate(other['result'])
                self.assertEqual(self.reject(other), 'original_network_changed')

    def test_actual_apply_changes_original_fingerprint(self):
        other = json.loads((DATA / '79436-applied-080.json').read_text(encoding='utf-8-sig'))
        self.assertEqual(self.reject(other), 'original_network_changed')

    def test_incomplete_and_duplicate_owners_fail_closed(self):
        for kind in ('incomplete', 'duplicate'):
            other = copy.deepcopy(self.packet)
            if kind == 'incomplete': other['result']['complete'] = False
            else: other['result']['owners'].append(other['result']['owners'][0])
            self.assertEqual(self.reject(other), 'original_snapshot_unavailable')

    def test_delayed_observation_and_new_city(self):
        old = (self.token[0], 'tool', 9)
        self.assertEqual(M['observation_rejection'](self.token, old, self.packet, self.packet),
                         'revision_or_session_changed')
        other = copy.deepcopy(self.packet)
        other['citySession'] = 'other-city'
        self.assertEqual(self.reject(other), 'revision_or_session_changed')


if __name__ == '__main__': unittest.main()
