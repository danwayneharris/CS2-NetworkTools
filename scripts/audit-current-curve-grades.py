"""Read-only comparison of current Smooth Curve trace and native preview grades."""
import json,math,runpy,argparse,os
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))
p=argparse.ArgumentParser();p.add_argument('--output',required=True);a=p.parse_args();r=m['Runner']('../cities2-agent-bridge-ndc',a.output)
r.call('get_city_state');s=r.state()
log=Path(os.environ['USERPROFILE'])/'AppData/LocalLow/Colossal Order/Cities Skylines II/Player.log'
traces=[json.loads(line.split('[NetworkTools.SmoothTrace] ',1)[1]) for line in log.read_text(errors='replace').splitlines() if '[NetworkTools.SmoothTrace] ' in line]
t=next(t for t in reversed(traces) if t['id']==s['submission'] and t['mode']=='Preview')
if t['selectedNodes']!=[f"{s[k]['index']}:{s[k]['version']}" for k in ('start','end')]:raise ValueError('Trace selection mismatch')
(r.output/'trace.json').write_text(json.dumps(t,indent=2))
def stats(q):
 gs=[]
 for j in range(1001):
  u=j/1000;d=[3*sum(w*(q[i+1][k]-q[i][k]) for i,w in enumerate(((1-u)**2,2*(1-u)*u,u*u))) for k in range(3)]
  gs.append(100*d[1]/math.hypot(d[0],d[2]))
 return dict(start=gs[0],end=gs[-1],minimum=min(gs),maximum=max(gs))
rows=[];native={}
for n in t['nodes']:
 idx,ver=map(int,n['entity'].split(':'));p=r.call('get_junction_preview',dict(index=idx,version=ver));snap=p.get('connectedSnapshot')
 if snap:
  for o in snap['owners']:
   if o.get('curve') and o.get('temp'):
    orig=o['temp']['original'];native[f"{orig['index']}:{orig['version']}"]=[m['position'](p) for p in o['curve']]
for e in t['edges']:
 before,after=e['input'],e['output'];b,c=stats(before),stats(after)
 rows.append(dict(edge=e['entity'],before=b,preview=c,maxControlYChange=max(abs(x[1]-y[1]) for x,y in zip(before,after)),nativeMaxError=max(math.dist(x,y) for x,y in zip(after,native[e['entity']])) if e['entity'] in native else None))
end=r.state()
if any(end[k]!=s[k] for k in ('session','revision','submission')):raise ValueError('Preview changed during capture')
report=dict(state=s,edges=rows,maxNodeYChange=max(abs(n['input'][1]-n['output'][1]) for n in t['nodes']),limits='No terrain-height or rendered-mesh samples; grade is dy/d horizontal arc length of edge centerlines.')
(r.output/'comparison.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
