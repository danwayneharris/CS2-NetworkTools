"""Ensure ordinary aggregate tests cannot silently skip unknown failures or pass empty coverage."""
import json,subprocess,sys,tempfile,unittest
from pathlib import Path
RUNNER=Path(__file__).with_name('run-offline-python-tests.py')
class DiscoveryTests(unittest.TestCase):
    def run_fixture(self,files):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);(root/'scripts').mkdir()
            for name,text in files.items():(root/'scripts'/name).write_text(text)
            output=root/'report.json'
            r=subprocess.run([sys.executable,str(RUNNER),'--root',str(root),'--output',str(output)],capture_output=True,text=True)
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
        r,j=self.run_fixture({'test-normal.py':'print("one check")','test-native-pipeline.py':'raise RuntimeError()'})
        self.assertEqual(r.returncode,0);self.assertEqual(len(j['executed']),1);self.assertEqual(len(j['notRunResearch']),1)
if __name__=='__main__':unittest.main()
