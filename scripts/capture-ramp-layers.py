"""Read-only fixed-XZ ramp stations, generated boundaries/lanes and adjusted terrain.
Reference captures supply identity-independent horizontal curves; never treats
EdgeGeometry or CPU terrain as the final rendered mesh or pristine ground.
"""
import argparse,json,runpy,math,time
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('--bridge',default='../bridge-terrain-profile');p.add_argument('--reference',required=True);a=p.parse_args()
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')));r=m['Runner'](a.bridge,a.output)
c=r.call('get_city_state')
if c['selectedSpeed']!=0 or c['population']!=0:raise RuntimeError('Paused toy required')
f=json.loads(Path('scripts/fixtures/toy-terrain-v11.json').read_text());ns,es=r.network(f['region'])
ref=json.loads(Path(a.reference).read_text());ids={m['identity'](e) for e in ref['path']['edges']};old=[e for e in ref['afterEdges'] if m['identity'](e) in ids]
def key(e):return tuple(round(p[k],2) for p in e['curve'] for k in ('x','z'))
matched=[]
for e in old:
 q=[x for x in es if key(x)==key(e)]
 if len(q)!=1:raise RuntimeError('Missing/ambiguous horizontal curve')
 matched.append(q[0])
def point(c,t):return {k:sum(w*v[k] for w,v in zip(((1-t)**3,3*t*(1-t)**2,3*t*t*(1-t),t**3),c)) for k in ('x','y','z')}
rows=[]
for ordinal,(original,e) in enumerate(zip(old,matched)):
 for i in range(21):
  t=i/20;v=point(original['curve'],t);cur=point(e['curve'],t);lo=point(original['curve'],max(0,t-.001));hi=point(original['curve'],min(1,t+.001));dx=hi['x']-lo['x'];dz=hi['z']-lo['z'];length=math.hypot(dx,dz)
  for offset in (-12,-6,0,6,12):rows.append({'edgeOrdinal':ordinal,'t':t,'offset':offset,'x':v['x']-offset*dz/length,'z':v['z']+offset*dx/length,'authoredY':cur['y']})
selected={m['identity'](x) for e in matched for x in (e['startNode'],e['endNode'])}
snaps=[r.call('get_junction_snapshot',{k:n[k] for k in ('index','version')}) for n in ns if m['identity'](n) in selected]
if any(not s['complete'] or s['errors'] for s in snaps):raise RuntimeError('Incomplete native snapshot')
terrain=[]
for _ in range(3):
 terrain.append([r.call('sample_terrain',{'points':[{k:x[k] for k in ('x','z')} for x in rows[start:start+100]]}) for start in range(0,len(rows),100)]);time.sleep(1)
(r.output/'layers.json').write_text(json.dumps({'city':c,'fingerprint':m['fingerprint'](ns,es),'edges':matched,'stations':rows,'snapshots':snaps,'terrain':terrain},indent=2));print(json.dumps({'session':r.city_session,'stations':len(rows),'edges':len(matched),'terrainType':type(terrain[0]).__name__}))
