import copy
import json
from pathlib import Path
import runpy
import sys
import unittest

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
MODULE = runpy.run_path(str(HERE / 'preview-freshness.py'))
fingerprint = MODULE['fingerprint']
may_accept = MODULE['may_accept']
DATA = HERE.parent / 'NetworkTools.docs/session-notes/captures/preview-resolver-20260928'


def capture(name):
    return json.loads((DATA / name).read_text(encoding='utf-8-sig'))


class FreshnessTests(unittest.TestCase):
    def test_same_entity_different_geometry(self):
        a = capture('79436-preview.json')
        b = capture('79436-preview-080.json')
        self.assertEqual(a['citySession'], b['citySession'])
        self.assertEqual(a['result']['topologyResolution'], b['result']['topologyResolution'])
        self.assertNotEqual(fingerprint(a), fingerprint(b))
        self.assertFalse(a['result']['validationReady'])
        self.assertFalse(b['result']['validationReady'])

    def test_order_and_response_metadata_do_not_change_evidence(self):
        a = capture('79436-preview.json')
        b = copy.deepcopy(a)
        b['id'] = 'another-read'
        b['utc'] = 'another-time'
        b['result']['connectedSnapshot']['owners'].reverse()
        b['result']['connectedSnapshot']['lanes'].reverse()
        self.assertEqual(fingerprint(a), fingerprint(b))

    def test_missing_incomplete_and_nonfinite_fail(self):
        with self.assertRaises(ValueError):
            fingerprint(capture('79440-preview.json'))
        a = capture('79436-preview.json')
        a['result']['connectedSnapshot']['complete'] = False
        with self.assertRaises(ValueError): fingerprint(a)
        a['result']['connectedSnapshot']['complete'] = True
        a['result']['connectedSnapshot']['owners'][0]['position']['x'] = float('nan')
        with self.assertRaises(ValueError): fingerprint(a)

    def test_revision_city_tool_and_completion_are_all_required(self):
        token = ('city-a', 'tool-a', 3)
        self.assertTrue(may_accept(token, token, True))
        self.assertFalse(may_accept(token, token, False))
        for stale in [('city-b', 'tool-a', 3), ('city-a', 'tool-b', 3),
                      ('city-a', 'tool-a', 2), ('city-a', 'tool-a', 4), None]:
            self.assertFalse(may_accept(token, stale, True))

    def test_returning_to_same_slider_value_does_not_reuse_old_revision(self):
        # 0.5 -> 0.8 -> 0.5: shape may be identical, but the earlier verdict is stale.
        self.assertFalse(may_accept(('city', 'tool', 3), ('city', 'tool', 1), True))


if __name__ == '__main__':
    unittest.main()
