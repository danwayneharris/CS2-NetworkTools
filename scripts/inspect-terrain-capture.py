"""Summarize saved read-only network/terrain captures; no game requests."""
import argparse,json,math
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('captures',nargs='+');a=p.parse_args()
nodes={};edges={};terrain={}
for directory in a.captures:
 for path in sorted(Path(directory).glob('*.json')):
  d=json.loads(path.read_text()).get('result',{})
  if 'nodes' in d:
   nodes.update({n['index']:n for n in d['nodes']})
  if 'edges' in d and isinstance(d['edges'],list):
   if d.get('possiblyTruncated'):raise RuntimeError('Truncated edges')
   edges.update({e['index']:e for e in d['edges']})
  if 'samples' in d:
   terrain.update({(s['position']['x'],s['position']['z']):s['position']['y'] for s in d['samples']})
remaining=dict(edges);groups=[]
while remaining:
 _,seed=remaining.popitem();group=[seed];ids={seed[k]['index'] for k in ('startNode','endNode')}
 changed=True
 while changed:
  changed=False
  for idx,e in list(remaining.items()):
   if any(e[k]['index'] in ids for k in ('startNode','endNode')):
    group.append(remaining.pop(idx));ids.update(e[k]['index'] for k in ('startNode','endNode'));changed=True
 groups.append((group,ids))
groups.sort(key=lambda g:sum(e['curve'][0]['z'] for e in g[0])/len(g[0]))
reports=[]
for i,(group,ids) in enumerate(groups,1):
 ns=[nodes[n] for n in ids if n in nodes];ys=[n['position']['y'] for n in ns];ts=[terrain[(n['position']['x'],n['position']['z'])] for n in ns if (n['position']['x'],n['position']['z']) in terrain]
 maxgrade=0
 for e in group:
  c=e['curve']
  for j in range(101):
   t=j/100;v={k:3*((1-t)**2*(c[1][k]-c[0][k])+2*(1-t)*t*(c[2][k]-c[1][k])+t*t*(c[3][k]-c[2][k])) for k in ('x','y','z')}
   horizontal=math.hypot(v['x'],v['z'])
   if horizontal>1e-6:maxgrade=max(maxgrade,100*abs(v['y'])/horizontal)
 points=[p for e in group for p in e['curve']]
 reports.append({'case':i,'edges':len(group),'nodes':len(ids),'capturedNodes':len(ns),'prefabs':sorted({e['prefab'] for e in group}),'bounds':{k:[min(p[k] for p in points),max(p[k] for p in points)] for k in ('x','z')},'nodeElevationRange':[min(ys),max(ys)],'terrainAtNodesRange':[min(ts),max(ts)] if ts else None,'sampledMaxAbsGradePercent':maxgrade,'junctions':[{'index':n['index'],'version':n['version'],'degree':len(n['edges']),'position':n['position']} for n in ns if len(n['edges'])>2]})
out=Path(a.output);out.mkdir(parents=True,exist_ok=True);(out/'network-summary.json').write_text(json.dumps(reports,indent=2));print(json.dumps(reports,indent=2))
try:
 import matplotlib.pyplot as plt
except ImportError:raise SystemExit()
fig,ax=plt.subplots(figsize=(10,10))
for report,(group,ids) in zip(reports,groups):
 for e in group:
  c=e['curve'];pts=[{k:sum(w*p[k] for w,p in zip(((1-t)**3,3*(1-t)**2*t,3*(1-t)*t*t,t**3),c)) for k in ('x','z')} for t in [j/40 for j in range(41)]]
  ax.plot([p['x'] for p in pts],[p['z'] for p in pts])
 x=sum(e['curve'][0]['x'] for e in group)/len(group);z=sum(e['curve'][0]['z'] for e in group)/len(group)
 ax.annotate(str(report['case']),(x,z),fontsize=15,bbox=dict(facecolor='white',alpha=.85))
ax.set_aspect('equal');ax.set_xlabel('World X (m)');ax.set_ylabel('World Z (m)');ax.set_title('Toy networks: component numbers match inspection summary');ax.grid(alpha=.2);fig.tight_layout();fig.savefig(out/'network-map.png',dpi=120)
