"""No-game orchestration tests for the non-deploying offline aggregate."""
import importlib.util
from pathlib import Path
import subprocess
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('offline', Path(__file__).with_name('run-offline-tests.py'))
offline = importlib.util.module_from_spec(spec)
spec.loader.exec_module(offline)


class OfflineRunnerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / 'suite.py').write_text('fixture', encoding='utf-8')

    def invoke(self, run):
        return offline.run_suite('fixture', ['fake'], 'suite.py', r'3 tests passed',
                                 self.root, self.root, 1, run=run)

    def test_success_requires_coverage_marker(self):
        result = self.invoke(lambda *a, **k: subprocess.CompletedProcess(a, 0, '3 tests passed', ''))
        self.assertEqual(result['status'], 'passed')
        result = self.invoke(lambda *a, **k: subprocess.CompletedProcess(a, 0, '', ''))
        self.assertEqual(result['status'], 'failed')

    def test_nonzero_fails_even_with_marker(self):
        result = self.invoke(lambda *a, **k: subprocess.CompletedProcess(a, 1, '3 tests passed', 'failure'))
        self.assertEqual(result['status'], 'failed')
        self.assertIn('failure', Path(result['log']).read_text(encoding='utf-8'))

    def test_missing_suite_blocks_without_launch(self):
        (self.root / 'suite.py').unlink()
        result = self.invoke(lambda *a, **k: self.fail('Missing suite must not launch'))
        self.assertEqual(result['status'], 'blocked')

    def test_launch_blocks_but_timeout_fails(self):
        for error, expected in ((FileNotFoundError('tool missing'), 'blocked'), (subprocess.TimeoutExpired('fake', 1), 'failed')):
            def fail(*args, **kwargs):
                raise error
            self.assertEqual(self.invoke(fail)['status'], expected)

    def test_aggregate_never_passes_empty_failed_or_blocked(self):
        self.assertEqual(offline.aggregate_status([]), 'failed')
        self.assertEqual(offline.aggregate_status([{'status': 'passed'}]), 'passed')
        for status in ('failed', 'blocked', 'skipped'):
            self.assertNotEqual(offline.aggregate_status([{'status': 'passed'}, {'status': status}]), 'passed')

    def test_prerequisite_block_does_not_launch(self):
        row = offline.run_suite('fixture', ['fake'], 'suite.py', 'passed', self.root, self.root, 1,
                                run=lambda *a, **k: self.fail('must not launch'), prerequisites=['Missing SDK'])
        self.assertEqual(row['status'], 'blocked')
        self.assertEqual(row['prerequisites'], ['Missing SDK'])

    def test_sdk_prerequisites_are_distinct(self):
        self.assertTrue(offline.prerequisite_issues('geometry', self.root, which=lambda _: None))
        for output, blocked in [('9.0.100 [sdk]', True), ('8.0.425 [sdk]', False)]:
            issues = offline.prerequisite_issues('geometry', self.root, which=lambda _: 'dotnet',
                run=lambda *a, **k: subprocess.CompletedProcess(a, 0, output, ''))
            self.assertEqual(bool(issues), blocked)

    def test_slope_missing_installed_inputs_block(self):
        def fake(command, **kwargs):
            output = '8.0.425 [sdk]' if '--list-sdks' in command else '{"managed": null, "tool": null}'
            return subprocess.CompletedProcess(command, 0, output, '')
        issues = offline.prerequisite_issues('slope-production', self.root, run=fake, which=lambda n: n)
        self.assertTrue(any('installed input' in x for x in issues))
        self.assertTrue(any('Common' in x for x in issues))

    def test_commands_are_explicit_non_deploying_stages(self):
        specs = offline.suite_specs(self.root, self.root)
        self.assertEqual({row[0] for row in specs}, {'geometry', 'path-selection', 'parameters', 'connect-profile', 'connect-coverage','lane-direction','lane-connection', 'connect-candidate', 'codegen', 'slope-production', 'original-input', 'python'})
        for _, command, _, _ in specs:
            self.assertNotIn('build', command)
            self.assertNotIn('bootstrap.ps1', command)
            self.assertNotIn('test', command)  # No empty legacy dotnet-test project.


if __name__ == '__main__':
    unittest.main()
