"""Ensure ordinary aggregate tests cannot silently skip unknown failures or pass empty coverage."""
import json,subprocess,sys,tempfile,unittest
from pathlib import Path
RUNNER=Path(__file__).with_name('run-offline-python-tests.py')
class DiscoveryTests(unittest.TestCase):
    def run_fixture(self,files,timeout=60):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);(root/'scripts').mkdir()
            for name,text in files.items():(root/'scripts'/name).write_text(text)
            output=root/'report.json'
            r=subprocess.run([sys.executable,str(RUNNER),'--root',str(root),'--output',str(output),'--timeout',str(timeout)],capture_output=True,text=True)
            return r,json.loads(output.read_text())
    def test_empty_fails(self):
        r,j=self.run_fixture({});self.assertNotEqual(r.returncode,0)
    def test_known_research_is_not_reported_as_pass(self):
        r,j=self.run_fixture({'test-native-pipeline.py':'raise RuntimeError()'})
        self.assertNotEqual(r.returncode,0);self.assertEqual(j['executed'],[]);self.assertEqual(j['notRunResearch'][0]['status'],'not-run')
    def test_unknown_failure_still_fails(self):
        r,j=self.run_fixture({'test-unregistered.py':'raise RuntimeError()'})
        self.assertNotEqual(r.returncode,0);self.assertEqual(len(j['executed']),1)
    def test_normal_success_and_explicit_research(self):
        r,j=self.run_fixture({'test-normal.py':'import unittest\nclass Tests(unittest.TestCase):\n def test_one(self): self.assertEqual(1,1)\nunittest.main()','test-native-pipeline.py':'raise RuntimeError()'})
        self.assertEqual(r.returncode,0);self.assertEqual(len(j['executed']),1);self.assertEqual(len(j['notRunResearch']),1)
    def test_empty_script_fails(self):
        r,j=self.run_fixture({'test-empty.py':''})
        self.assertNotEqual(r.returncode,0)
        self.assertEqual(j['executed'][0]['executedChecks'],0)
    def test_zero_unittest_fails(self):
        r,j=self.run_fixture({'test-zero.py':'import unittest; unittest.main()'})
        self.assertNotEqual(r.returncode,0)
    def test_all_skipped_fails(self):
        r,j=self.run_fixture({'test-skipped.py':'import unittest\nclass Tests(unittest.TestCase):\n @unittest.skip("fixture")\n def test_one(self): pass\nunittest.main()'})
        self.assertNotEqual(r.returncode,0)
        self.assertEqual(j['executed'][0]['executedChecks'],0)
    def test_arbitrary_success_message_fails(self):
        r,j=self.run_fixture({'test-print.py':'print("100 checks passed")'})
        self.assertNotEqual(r.returncode,0)
    def test_timeout_writes_json_report(self):
        r,j=self.run_fixture({'test-slow.py':'import time; time.sleep(5)'},timeout=0.1)
        self.assertNotEqual(r.returncode,0)
        self.assertEqual(j['status'],'failed')
        self.assertIn('timed out',j['executed'][0]['reason'])
    def test_retained_analytic_assertions_reject_optimization(self):
        path=RUNNER.with_name('test-native-middle-height.py')
        r=subprocess.run([sys.executable,'-O',str(path)],capture_output=True,text=True)
        self.assertNotEqual(r.returncode,0)
        self.assertIn('optimization disabled',r.stderr)
    def test_timeout_preserves_report_and_other_results(self):
        import importlib.util
        spec=importlib.util.spec_from_file_location('python_runner',RUNNER)
        runner=importlib.util.module_from_spec(spec);spec.loader.exec_module(runner)
        self.assertEqual(runner.script_prerequisites('test-build-identity.py', which=lambda _: None), ['git', 'node', 'PowerShell'])
        self.assertEqual(runner.script_prerequisites('test-build-identity.py', which=lambda n: n), [])
        def timed_out(*a,**k): raise subprocess.TimeoutExpired('fixture',1)
        row=runner.run_script(Path('test-slow.py'),Path('.'),1,run=timed_out)
        self.assertEqual(row['status'],'failed');self.assertIn('timed out',row['reason'])
        def missing(*a,**k): raise FileNotFoundError('missing executable')
        row=runner.run_script(Path('test-missing.py'),Path('.'),1,run=missing)
        self.assertEqual(row['status'],'blocked')
if __name__=='__main__':unittest.main()
