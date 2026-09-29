import sys
sys.dont_write_bytecode = True
import runpy
import unittest
import json
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
analyze = runpy.run_path(str(ROOT / 'scripts/analyze-rail-composition.py'))['analyze']
CAPTURES = ROOT / 'NetworkTools.docs/session-notes/captures/rail-pair-20260928'


class CapturedRailTests(unittest.TestCase):
    def test_connected_preview_and_detached_original_proxy(self):
        folder = ROOT / 'NetworkTools.docs/session-notes/captures/sprint-20260929-interior-composition'
        packet = json.loads((folder / '004-get_junction_preview.json').read_text())
        rows = analyze(folder / '004-get_junction_preview.json')['connections']
        self.assertEqual(len(rows), 6)
        self.assertEqual(sum(r['observedConnector'] for r in rows), 3)
        self.assertTrue(all(r['passesCurvatureGate'] == r['observedConnector'] for r in rows))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'detached.json'
            path.write_text(json.dumps({'ok': True, 'result': packet['result']['snapshot']}))
            with self.assertRaises(ValueError): analyze(path)
            packet['result']['topologyResolution']['status'] = 'ambiguous'
            path.write_text(json.dumps(packet))
            with self.assertRaises(ValueError): analyze(path)

    def test_working_merge(self):
        rows = analyze(CAPTURES / '350919.json')['connections']
        self.assertEqual(len(rows), 6)
        self.assertEqual(sum(r['observedConnector'] for r in rows), 4)
        for row in rows:
            self.assertEqual(row['passesCurvatureGate'], row['observedConnector'])
            self.assertAlmostEqual(row['limit'], 0.0314159244)

    def test_broken_merge(self):
        rows = analyze(CAPTURES / '358242.json')['connections']
        self.assertEqual(len(rows), 6)
        self.assertEqual(sum(r['observedConnector'] for r in rows), 2)
        for row in rows:
            self.assertEqual(row['passesCurvatureGate'], row['observedConnector'])
        branch = [r for r in rows if {r['sourceEdge']['index'], r['targetEdge']['index']} == {360575, 361395}]
        self.assertEqual(len(branch), 2)
        self.assertTrue(all(r['curviness'] > r['limit'] for r in branch))


if __name__ == '__main__':
    unittest.main()
