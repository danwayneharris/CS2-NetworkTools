"""Re-evaluate a captured Runner.execute call sequence offline; never contacts the game."""
import argparse,json,runpy
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--capture',required=True);p.add_argument('--start',type=int,required=True)
p.add_argument('--fixture',required=True);p.add_argument('--case',required=True)
p.add_argument('--save-root',required=True);p.add_argument('--output',required=True)
a=p.parse_args()
class Replay(m['Runner']):
    def __init__(self):
        self.output=Path(a.output);self.output.mkdir(parents=True,exist_ok=False)
        self.sequence=a.start-1;self.city_session=None
    def call(self,command,args=None):
        command,args=self.route_provider(command,args);self.sequence+=1
        prefix=Path(a.capture)/f'{self.sequence:03d}-{command}'
        expected=json.loads(prefix.with_suffix('.request.json').read_text())
        if expected!=(args or {}):raise AssertionError('Replay arguments differ: '+str(prefix))
        response=json.loads(prefix.with_suffix('.json').read_text())
        if not response['ok']:raise AssertionError('Captured command failed')
        if self.city_session is None:self.city_session=response['citySession']
        if self.city_session!=response['citySession']:raise AssertionError('Captured session changed')
        return response['result']
f=json.loads(Path(a.fixture).read_text());case=next(c for c in f['cases'] if c['name']==a.case)
print(json.dumps(Replay().execute(f,case,a.save_root),indent=2))
