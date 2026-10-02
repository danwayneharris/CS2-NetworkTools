"""Checkpoint and compare the current paused toy Slope preview with one Apply.
No activation, parameter changes or reselection. Requires explicit --apply.
Raw output remains in the requested capture directory.
"""
import argparse,json,runpy,time
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--bridge',required=True);p.add_argument('--output',required=True)
p.add_argument('--save-root',required=True);p.add_argument('--apply',action='store_true')
a=p.parse_args()
if not a.apply:p.error('Explicit --apply required')
r=m['Runner'](a.bridge,a.output);root=Path(a.output)
city=r.call('get_city_state')
if city['population']!=0 or city['selectedSpeed']!=0 or not city['controlEnabled']:raise RuntimeError('Paused controlled toy required')
provider=next(x for x in r.call('list_providers')['providers'] if x['id']=='networktools')
def invoke(command,args=None):return r.call('invoke_provider',{'provider':'networktools','revision':provider['revision'],'command':command,'args':args or {}})
s=invoke('slope_state')
if not s['active'] or not s['previewReady'] or not s['mode'].startswith('Slope'):raise RuntimeError('Expected ready slope selection')
path=r.call('trace_network',dict(fromIndex=s['start']['index'],fromVersion=s['start']['version'],toIndex=s['end']['index'],toVersion=s['end']['version']))
if not path['connected'] or len(path['edges'])>16:raise RuntimeError('Bounded connected selection required')
operation=r.call('save_checkpoint',{'label':'surface-preview-before-apply'})
saved=r.poll('get_operation',lambda x:x['status'] in ('complete','failed','interrupted'),{'id':operation['id']})
if saved['status']!='complete':raise RuntimeError('Checkpoint failed')
runpy.run_path(str(Path(__file__).with_name('reload-toy-baseline.py')))['verified_package'](a.save_root,saved['saveName']+'.cok')
watched=[s['start'],s['end']]
before=[r.call('get_junction_snapshot',n) for n in watched]
previews=[r.call('get_junction_preview',n) for n in watched]
if any(not x.get('connectedSnapshot') or not x['connectedSnapshot']['complete'] for x in previews):raise RuntimeError('Missing preview')
owners={m['identity'](o):o for snap in before for o in snap['owners'] if o.get('curve')}
points=[]
for o in owners.values():
 for i in range(9):
  t=i/8;w=((1-t)**3,3*(1-t)**2*t,3*(1-t)*t*t,t**3)
  pos={k:sum(q*v[k] for q,v in zip(w,o['curve'])) for k in ('x','z')}
  for dx,dz in ((0,0),(5,0),(-5,0),(0,5),(0,-5)):points.append({'x':pos['x']+dx,'z':pos['z']+dz})
terrain_before=r.call('sample_terrain',{'points':points})
final=invoke('slope_state')
if not final['previewReady'] or any(final[k]!=s[k] for k in ('session','revision','submission')):raise RuntimeError('Preview token changed')
root.joinpath('before.json').write_text(json.dumps({'city':city,'state':s,'path':path,'checkpoint':saved,'snapshots':before,'previews':previews,'terrain':terrain_before,'points':points},indent=2))
invoke('slope_apply',{k:s[k] for k in ('session','revision','submission')})
for i in range(40):
 if invoke('slope_state')['phase']=='Idle':break
 time.sleep(.25)
else:raise RuntimeError('Apply incomplete; no retry')
# Observe road surfaces over time, not just authored Curve stability.
for label,delay in (('immediate',0),('settled',3),('later',5)):
 time.sleep(delay)
 snapshots=[r.call('get_junction_snapshot',n) for n in watched]
 terrain=r.call('sample_terrain',{'points':points})
 root.joinpath('after-'+label+'.json').write_text(json.dumps({'snapshots':snapshots,'terrain':terrain},indent=2))
print(json.dumps({'output':a.output,'checkpoint':saved['saveName'],'appliedSubmission':s['submission'],'paused':r.call('get_city_state')['selectedSpeed']==0}))
