"""Re-run current assertions against an existing command capture. No bridge calls.

Strict command/argument ordering prevents accidentally replaying a different test.
Local checkpoint packages are still read and verified by the underlying runner.
"""
import argparse
from collections import deque
import importlib.util
import json
from pathlib import Path

spec=importlib.util.spec_from_file_location('regression',Path(__file__).with_name('live-regression.py'))
regression=importlib.util.module_from_spec(spec);spec.loader.exec_module(regression)


class Replay(regression.Runner):
    def __init__(self,capture,output):
        super().__init__('.',output)
        self.responses=deque(sorted(p for p in Path(capture).glob('[0-9]*-*.json')
                                   if not p.name.endswith('.request.json')))

    def call(self,command,args=None):
        if command.startswith('nt_') and not self.responses[0].stem.split('-',1)[1].startswith('nt_'):
            command,args=self.route_provider(command,args)
        path=self.responses.popleft()
        expected=path.stem.split('-',1)[1]
        recorded=json.loads(path.with_suffix('.request.json').read_text())
        if command!=expected or (args or {})!=recorded:
            raise ValueError(f'Capture does not match requested operation: {path.name}, requested {command}')
        response=json.loads(path.read_text())
        if self.city_session is None:self.city_session=response['citySession']
        if response['citySession']!=self.city_session or not response['ok']:
            raise ValueError('Failed or changed-session capture')
        return response['result']


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('capture','output','fixture','case','save-root'):
        parser.add_argument('--'+name,required=True)
    args=parser.parse_args();fixture=json.loads(Path(args.fixture).read_text())
    runner=Replay(args.capture,args.output)
    case=next(c for c in fixture['cases'] if c['name']==args.case)
    print(json.dumps(runner.execute(fixture,case,args.save_root),indent=2))


if __name__=='__main__':main()
