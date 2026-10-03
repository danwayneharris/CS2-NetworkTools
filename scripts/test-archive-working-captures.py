"""Working-tree diagnostic archive boundaries and manifest verification."""
import importlib.util
import json
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('archiver', Path(__file__).with_name('archive-diagnostic-captures.py'))
archiver = importlib.util.module_from_spec(spec)
spec.loader.exec_module(archiver)


class CaptureArchiveTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)
        self.repo = self.base / 'repo'
        self.root = self.repo / 'artifacts' / 'capture'
        self.root.mkdir(parents=True)
        (self.root / 'state.json').write_text('{"a":1}', encoding='utf-8')
        self.output = self.base / 'evidence.zip'

    def collect(self, roots=None, output=None):
        return archiver.collect_working_captures(self.repo, roots or [self.root], output or self.output)

    def test_roundtrip_filters_and_multiple_roots(self):
        other = self.repo / 'artifacts' / 'other'
        other.mkdir()
        for name in ['native.log', 'notes.md', 'result.txt', 'nested.JSON']:
            (other / name).write_text('evidence', encoding='utf-8')
        for name in ['mod.dll', 'city.cok', 'image.png', 'archive.zip', 'program.exe']:
            (other / name).write_bytes(b'not evidence')
        result = archiver.archive_working_captures(self.repo, [self.root, other], self.output, 'abc123')
        manifest = archiver.verify(self.output)
        self.assertEqual(5, result['files'])
        self.assertEqual('working-tree', manifest['scope'])
        self.assertEqual('abc123', manifest['sourceCommit'])
        self.assertEqual(['artifacts/capture', 'artifacts/other'], manifest['captureRoots'])
        self.assertTrue(self.output.with_suffix('.zip.sha256').is_file())
        self.assertEqual({'path', 'bytes', 'sha256'}, set(manifest['files'][0]))
        self.assertEqual(sorted(row['path'] for row in manifest['files']), [row['path'] for row in manifest['files']])

    def test_outside_root_rejected(self):
        with self.assertRaises(ValueError): self.collect([self.base])
        with self.assertRaises(ValueError): self.collect([self.repo])

    def test_artifacts_prefix_sibling_rejected(self):
        sibling = self.repo / 'artifacts-elsewhere'
        sibling.mkdir()
        with self.assertRaises(ValueError): self.collect([sibling])

    def test_relative_escape_rejected(self):
        with self.assertRaises(ValueError): self.collect([Path('artifacts/..')])

    def test_duplicate_and_overlapping_roots_rejected(self):
        for roots in [[self.root, self.root], [self.root, self.root.parent], [self.root.parent, self.root]]:
            with self.subTest(roots=roots), self.assertRaises(ValueError): self.collect(roots)

    def test_output_source_or_capture_root_rejected(self):
        for output in [self.root / 'out.zip', self.repo / 'source.zip', self.repo / 'scripts' / 'out.zip']:
            with self.subTest(output=output), self.assertRaises(ValueError): self.collect(output=output)
        self.collect(output=self.repo / 'artifacts' / 'archives' / 'evidence.zip')

    def test_file_root_and_empty_evidence_rejected(self):
        with self.assertRaises(ValueError): self.collect([self.root / 'state.json'])
        (self.root / 'state.json').unlink()
        (self.root / 'mod.dll').write_bytes(b'x')
        with self.assertRaises(ValueError): self.collect()

    def test_link_escape_rejected_without_following(self):
        # Inject the filesystem classification so Windows does not require symlink privilege.
        link = self.root / 'linked.json'
        link.write_text('placeholder')
        original = Path.is_symlink
        with patch.object(Path, 'is_symlink', lambda path: path == link or original(path)):
            with self.assertRaisesRegex(ValueError, 'Symlink'): self.collect()

    def test_capture_root_alias_rejected(self):
        original = Path.is_symlink
        with patch.object(Path, 'is_symlink', lambda path: path == self.root or original(path)):
            with self.assertRaisesRegex(ValueError, 'Symlink'): self.collect()

    def test_corrupt_entry_rejected(self):
        archiver.archive_working_captures(self.repo, [self.root], self.output)
        with zipfile.ZipFile(self.output) as archive:
            members = {name: archive.read(name) for name in archive.namelist()}
        members['artifacts/capture/state.json'] = b'corrupt'
        damaged = self.base / 'damaged.zip'
        with zipfile.ZipFile(damaged, 'w') as archive:
            for name, data in members.items(): archive.writestr(name, data)
        with self.assertRaisesRegex(ValueError, 'content mismatch'): archiver.verify(damaged)

    def test_unlisted_entry_rejected(self):
        archiver.archive_working_captures(self.repo, [self.root], self.output)
        with zipfile.ZipFile(self.output, 'a') as archive: archive.writestr('unexpected.txt', b'bad')
        with self.assertRaisesRegex(ValueError, 'member list'): archiver.verify(self.output)

    def test_existing_archive_not_overwritten(self):
        self.output.write_bytes(b'keep')
        with self.assertRaises(FileExistsError): archiver.archive_working_captures(self.repo, [self.root], self.output)
        self.assertEqual(b'keep', self.output.read_bytes())


if __name__ == '__main__':
    unittest.main()
