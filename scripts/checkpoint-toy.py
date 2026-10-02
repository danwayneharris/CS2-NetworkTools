"""Verify a known paused toy session and save a uniquely named checkpoint.
Requires --save for mutation; baseline packages are checked but never overwritten.
"""
import argparse
import json
import runpy
from pathlib import Path
HERE=Path(__file__).resolve().parent
Runner=runpy.run_path(str(HERE/'live-regression.py'))['Runner']
verified_package=runpy.run_path(str(HERE/'reload-toy-baseline.py'))['verified_package']


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--bridge',default='../cities2-agent-bridge-ndc')
    parser.add_argument('--output',required=True)
    parser.add_argument('--save-root',required=True)
    parser.add_argument('--expected-city-session',required=True)
    parser.add_argument('--fixture',action='append',required=True)
    parser.add_argument('--label',default='toy-checkpoint')
    parser.add_argument('--save',action='store_true')
    args=parser.parse_args()
    runner=Runner(args.bridge,args.output)
    city=runner.call('get_city_state')
    if runner.city_session!=args.expected_city_session or city['selectedSpeed']!=0 or city['population']!=0 or not city['controlEnabled']:
        raise ValueError('Known paused control-enabled toy session required')
    for filename in args.fixture:
        fixture=json.loads(Path(filename).read_text())
        verified_package(args.save_root,fixture['baseline'],fixture['baselineSha256'])
    if not args.save:
        print('Session and baseline packages verified; no save requested');return
    operation=runner.call('save_checkpoint',{'label':args.label})
    done=runner.poll('get_operation',lambda s:s['status'] in ('complete','failed','interrupted'),{'id':operation['id']})
    if done['status']!='complete':raise RuntimeError('Checkpoint did not complete; do not replay blindly')
    package=verified_package(args.save_root,done['saveName']+'.cok')
    if runner.call('get_city_state')['selectedSpeed']!=0:raise RuntimeError('Pause changed')
    report={'citySession':runner.city_session,'checkpoint':package.name,'paused':True,'baselineHashesVerified':True}
    (runner.output/'checkpoint.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2))


if __name__=='__main__':main()
