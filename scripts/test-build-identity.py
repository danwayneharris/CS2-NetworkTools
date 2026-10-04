"""Build identity fixtures use temporary repositories; no deployment or game access."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / 'scripts' / 'build-identity.ps1'
GIT = shutil.which('git')
POWERSHELL = shutil.which('powershell') or shutil.which('pwsh')


class BuildIdentityTests(unittest.TestCase):
    def setUp(self):
        if not GIT or not POWERSHELL:
            self.fail('Build identity tests require Git and PowerShell on PATH')
        self.temp = tempfile.TemporaryDirectory(prefix='nt-build-identity-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.repo = self.root / 'repo'
        self.repo.mkdir()
        self.identity = self.root / 'build-identity.json'

    def git(self, *args):
        return subprocess.check_output([GIT, '-C', str(self.repo), *args], text=True).strip()

    def capture(self, configuration='Debug', git=GIT):
        subprocess.run([POWERSHELL, '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', str(SCRIPT),
                        '-RepositoryRoot', str(self.repo), '-ReleaseVersion', '1.5.7',
                        '-Configuration', configuration, '-IdentityPath', str(self.identity),
                        '-GitExecutable', git], check=True, capture_output=True, text=True)
        return json.loads(self.identity.read_text(encoding='utf-8-sig'))

    def init_repo(self):
        self.git('init', '-q')
        self.git('config', 'user.name', 'Identity Fixture')
        self.git('config', 'user.email', 'fixture@example.invalid')
        (self.repo / 'source.txt').write_text('one', encoding='utf-8')
        self.git('add', 'source.txt')
        self.git('commit', '-qm', 'fixture')

    def test_clean_dirty_and_revision(self):
        self.init_repo()
        first = self.capture()
        self.assertEqual(first['workingTree'], 'clean')
        self.assertEqual(first['sourceRevision'], self.git('rev-parse', 'HEAD'))
        self.assertEqual(first['informationalVersion'], f"1.5.7+g{first['sourceRevision'][:12]}.clean.Debug")
        (self.repo / 'source.txt').write_text('two', encoding='utf-8')
        self.assertEqual(self.capture()['workingTree'], 'dirty')
        self.git('commit', '-qam', 'second')
        second = self.capture('Release')
        self.assertNotEqual(first['sourceRevision'], second['sourceRevision'])
        self.assertTrue(second['informationalVersion'].endswith('.clean.Release'))
        (self.repo / 'untracked.txt').write_text('new', encoding='utf-8')
        self.assertEqual(self.capture()['workingTree'], 'dirty')

    def test_archive_and_missing_git_are_unknown(self):
        for identity in [self.capture(), self.capture(git='nonexistent-nt-git')]:
            self.assertEqual(identity['sourceRevision'], 'unknown')
            self.assertEqual(identity['workingTree'], 'unknown')
            self.assertEqual(identity['informationalVersion'], '1.5.7+unknown.unknown.Debug')
        self.init_repo()
        self.assertEqual(self.capture(git='nonexistent-nt-git')['workingTree'], 'unknown')

    def test_hashes_actual_artifacts_and_rejects_incomplete_package(self):
        identity = self.capture()
        package = self.root / 'package'
        package.mkdir()
        command = [POWERSHELL, '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', str(SCRIPT),
                   '-Mode', 'Manifest', '-IdentityPath', str(self.identity), '-ArtifactDirectory', str(package)]
        self.assertNotEqual(subprocess.run(command, capture_output=True).returncode, 0)
        for name in ['NetworkTools.dll', 'NetworkTools.mjs', 'NetworkTools.css']:
            (package / name).write_bytes(('postprocessed ' + name).encode())
        # A similarly named legacy .js file must not satisfy the module contract.
        (package / 'NetworkTools.mjs').rename(package / 'NetworkTools.js')
        self.assertNotEqual(subprocess.run(command, capture_output=True).returncode, 0)
        (package / 'NetworkTools.js').rename(package / 'NetworkTools.mjs')
        subprocess.run(command, check=True, capture_output=True)
        manifest = json.loads((package / 'build-manifest.json').read_text())
        self.assertEqual(manifest['build'], identity)
        self.assertEqual(len(manifest['artifacts']), 3)
        for artifact in manifest['artifacts']:
            self.assertEqual(artifact['sha256'], hashlib.sha256((package / artifact['path']).read_bytes()).hexdigest())
        subprocess.run(command, check=True, capture_output=True)
        self.assertEqual(len(json.loads((package / 'build-manifest.json').read_text())['artifacts']), 3)

    def test_ui_reads_same_capture_and_rejects_absent_identity(self):
        node = shutil.which('node')
        self.assertIsNotNone(node, 'UI identity verification requires Node')
        identity = self.capture()
        helper = ROOT / 'NetworkTools.Mod' / 'UI' / 'tools' / 'build-identity.js'
        command = [node, '-e', 'console.log(JSON.stringify(require(process.argv[1]).loadBuildIdentity()))', str(helper)]
        env = dict(os.environ, NT_BUILD_IDENTITY=str(self.identity))
        result = subprocess.check_output(command, env=env, text=True)
        self.assertEqual(json.loads(result), identity)
        env.pop('NT_BUILD_IDENTITY')
        self.assertNotEqual(subprocess.run(command, env=env, capture_output=True).returncode, 0)


if __name__ == '__main__':
    unittest.main()
