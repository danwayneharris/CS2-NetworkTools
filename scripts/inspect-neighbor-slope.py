"""Capture the current paused toy selection and explicitly apply once after checkpointing."""
import argparse,json,runpy
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('--apply',action='store_true');p.add_argument('--bridge',default='../bridge-terrain-profile');p.add_argument('--expected-city-session');p.add_argument('--expected-fingerprint');p.add_argument('--save-root');a=p.parse_args()
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')));r=m['Runner'](a.bridge,a.output)
f=json.loads(Path('scripts/fixtures/toy-terrain-v11.json').read_text());city=r.call('get_city_state');s=r.state()
if city['selectedSpeed']!=0 or city['population']!=0 or not city['controlEnabled']:raise RuntimeError('Paused controlled toy required')
if s['mode']!='SlopeLinear' or not s['previewReady']:raise RuntimeError('Ready Constant Slope required')
n,e=r.network(f['region']);path=r.call('trace_network',{'fromIndex':s['start']['index'],'fromVersion':s['start']['version'],'toIndex':s['end']['index'],'toVersion':s['end']['version']})
watch=[{k:x[k] for k in ('index','version')} for x in n if len(x['edges'])>1 and x['position']['x']>-2900]
before=[r.call('get_junction_snapshot',x) for x in watch]
preview=[r.call('get_junction_preview',x) for x in watch]
report={'city':city,'state':s,'path':path,'beforeNodes':n,'beforeEdges':e}
if a.apply:
 if not a.save_root or r.city_session!=a.expected_city_session or m['fingerprint'](n,e)!=a.expected_fingerprint:raise RuntimeError('Explicit known city session, geometry fingerprint and save root required')
 op=r.call('save_checkpoint',{'label':'before-highway-neighbor-slope'});done=r.poll('get_operation',lambda x:x['status'] in ('complete','failed','interrupted'),{'id':op['id']})
 if done['status']!='complete':raise RuntimeError('Checkpoint failed')
 verify=runpy.run_path(str(Path(__file__).with_name('reload-toy-baseline.py')))['verified_package']
 root=Path(a.save_root)
 verify(root,done['saveName']+'.cok');report['checkpoint']=done['saveName']
 current=r.state()
 if any(current[k]!=s[k] for k in ('session','revision','submission')) or not current['previewReady']:raise RuntimeError('Selection changed')
 r.call('invoke_provider',{'provider':'networktools','revision':r.provider_revision,'command':'slope_apply','args':{k:s[k] for k in ('session','revision','submission')}})
 r.poll('nt_get_state',lambda x:x['phase']=='Idle')
 after,an,ae=r.settled_permanent(watch,f['region']);report.update(afterNodes=an,afterEdges=ae,directedConnectionsPreserved=all(m['lane_transitions'](b)==m['lane_transitions'](c) for b,c in zip(before,after)))
 report['paused']=r.call('get_city_state')['selectedSpeed']==0
(r.output/'comparison.json').write_text(json.dumps(report,indent=2));print(json.dumps({k:report[k] for k in ('checkpoint','directedConnectionsPreserved','paused') if k in report}))
