"""Checkpoint and compare the current paused toy Slope or combined Curve preview with one Apply.
No activation, parameter changes or reselection. Requires explicit --apply.
Raw output remains in the requested capture directory.
"""
import argparse,json,runpy,time
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--bridge',required=True);p.add_argument('--output',required=True)
p.add_argument('--save-root',required=True)
p.add_argument('--managed-finish',action='store_true',help='Change captured finishing execution; requires --trace-native --apply')
p.add_argument('--trace-native',action='store_true',help='Capture one native Apply geometry pass, explicitly allowing Burst')
g=p.add_mutually_exclusive_group(required=True);g.add_argument('--apply',action='store_true');g.add_argument('--capture-only',action='store_true')
a=p.parse_args()
if a.managed_finish and not a.trace_native:raise ValueError("Managed finishing requires --trace-native")
r=m['Runner'](a.bridge,a.output);root=Path(a.output)
if a.trace_native:
 if not a.apply:raise ValueError('--trace-native requires --apply')
 r.command_timeout=45
city=r.call('get_city_state')
if city['population']!=0 or city['selectedSpeed']!=0 or not city['controlEnabled']:raise RuntimeError('Paused controlled toy required')
provider=next(x for x in r.call('list_providers')['providers'] if x['id']=='networktools')
def invoke(command,args=None):return r.call('invoke_provider',{'provider':'networktools','revision':provider['revision'],'command':command,'args':args or {}})
s=invoke('slope_state')
is_slope=s['mode'].startswith('Slope')
if not s['active'] or not s['previewReady'] or not (is_slope or (s['mode']=='CurveSmooth' and s['combinedSlope'])):raise RuntimeError('Expected ready slope or combined selection')
path=r.call('trace_network',dict(fromIndex=s['start']['index'],fromVersion=s['start']['version'],toIndex=s['end']['index'],toVersion=s['end']['version']))
if not path['connected'] or len(path['edges'])>16:raise RuntimeError('Bounded connected selection required')
operation=r.call('save_checkpoint',{'label':'surface-preview-before-apply'})
saved=r.poll('get_operation',lambda x:x['status'] in ('complete','failed','interrupted'),{'id':operation['id']})
if saved['status']!='complete':raise RuntimeError('Checkpoint failed')
runpy.run_path(str(Path(__file__).with_name('reload-toy-baseline.py')))['verified_package'](a.save_root,saved['saveName']+'.cok')
watched=[s['start'],s['end']]
before=[r.call('get_junction_snapshot',n) for n in watched]
# Include interior nodes when the endpoint snapshots cover the selected path.
# Fail closed rather than silently inspecting only boundaries of a longer path.
selected={m['identity'](e) for e in path['edges']}
selected_owners={m['identity'](o):o for snap in before for o in snap['owners'] if m['identity'](o) in selected}
if selected_owners.keys()!=selected:raise RuntimeError('Selected edge coverage incomplete; use full regression runner')
seen={m['identity'](n) for n in watched}
for o in selected_owners.values():
 for field in ('startNode','endNode'):
  n=o[field]
  if m['identity'](n) not in seen:
   watched.append(n);seen.add(m['identity'](n));before.append(r.call('get_junction_snapshot',n))
previews=[r.call('get_junction_preview',n) for n in watched]
for x in previews:
 if x.get('connectedSnapshot'):
  if not x['connectedSnapshot']['complete']:raise RuntimeError('Incomplete preview')
 else:
  # Degree-one has no unique shared-endpoint junction. Retain unsupported status;
  # capture its one uniquely mapped edge, without claiming node/lane coverage.
  rel=x.get('relatedPreviewEdges',{})
  if x.get('topologyResolution',{}).get('status')!='unsupported' or not rel.get('complete') or len(rel.get('expectedOriginalEdges',[]))!=1 or len(rel.get('edges',[]))!=1:
   raise RuntimeError('Missing or ambiguous preview')
  if m['identity'](rel['edges'][0]['temp']['original'])!=m['identity'](rel['expectedOriginalEdges'][0]):raise RuntimeError('Preview edge mismatch')
owners={m['identity'](o):o for snap in before for o in snap['owners'] if o.get('curve')}
points=[]
for o in owners.values():
 for i in range(9):
  t=i/8;w=((1-t)**3,3*(1-t)**2*t,3*(1-t)*t*t,t**3)
  pos={k:sum(q*v[k] for q,v in zip(w,o['curve'])) for k in ('x','z')}
  for dx,dz in ((0,0),(5,0),(-5,0),(0,5),(0,-5)):points.append({'x':pos['x']+dx,'z':pos['z']+dz})
for snap in before:
 center=next(o['position'] for o in snap['owners'] if m['identity'](o)==m['identity'](snap['junction']))
 for dx in range(-30,31,3):
  for dz in range(-30,31,3):points.append({'x':center['x']+dx,'z':center['z']+dz})
def terrain_capture():
 samples=[]
 for i in range(0,len(points),256):
  response=r.call('sample_terrain',{'points':points[i:i+256]})
  if len(response['samples'])!=len(points[i:i+256]):raise RuntimeError('Terrain coverage incomplete')
  samples.extend(response['samples'])
 return {'samples':samples}
terrain_before=terrain_capture()
final=invoke('slope_state')
if not final['previewReady'] or any(final[k]!=s[k] for k in ('session','revision','submission')):raise RuntimeError('Preview token changed')
root.joinpath('before.json').write_text(json.dumps({'city':city,'state':s,'path':path,'checkpoint':saved,'snapshots':before,'previews':previews,'terrain':terrain_before,'points':points},indent=2))
if a.capture_only:
 print(json.dumps({'output':a.output,'submission':s['submission'],'captureOnly':True}));raise SystemExit(0)
if a.trace_native:r.call('begin_geometry_schedule_trace',dict(citySession=r.city_session,operationId=root.name,maxPasses=1,allowBurst=True,finishExecution="managed" if a.managed_finish else "native"))
try:
 invoke('slope_apply' if is_slope else 'apply',{k:s[k] for k in ('session','revision','submission')})
 for i in range(40):
  if invoke('slope_state')['phase']=='Idle':break
  time.sleep(.25)
 else:raise RuntimeError('Apply incomplete; no retry')
finally:
 if a.trace_native:
  trace=r.call('end_geometry_schedule_trace')
  root.joinpath('trace-completed.json').write_text(json.dumps(dict(result=trace),indent=2))
# Observe road surfaces over time, not just authored Curve stability.
for label,delay in (('immediate',0),('settled',3),('later',5)):
 time.sleep(delay)
 snapshots=[r.call('get_junction_snapshot',n) for n in watched]
 terrain=terrain_capture()
 root.joinpath('after-'+label+'.json').write_text(json.dumps({'snapshots':snapshots,'terrain':terrain},indent=2))
print(json.dumps({'output':a.output,'checkpoint':saved['saveName'],'appliedSubmission':s['submission'],'paused':r.call('get_city_state')['selectedSpeed']==0}))
