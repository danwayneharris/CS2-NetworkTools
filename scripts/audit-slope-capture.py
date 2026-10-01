"""Independently audit captured Slope Apply: directed lanes and side-edge movement."""
import argparse,json,math,runpy
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))
p=argparse.ArgumentParser();p.add_argument('capture');a=p.parse_args();root=Path(a.capture)
def read(path):return json.loads(path.read_text())['result']
def ends(pattern,key):
 files=sorted(root.glob(pattern));return read(files[0])[key],read(files[-1])[key]
old_nodes,new_nodes=ends('*-get_network.json','nodes');old_edges,new_edges=ends('*-get_network_edges.json','edges')
old={m['identity'](e):e for e in old_edges};new={m['identity'](e):e for e in new_edges}
nodes={m['identity'](n):n for n in new_nodes};moves={m['identity'](n):[nodes[m['identity'](n)]['position'][k]-n['position'][k] for k in ('x','y','z')] for n in old_nodes}
assert all(abs(v[0])<.001 and abs(v[2])<.001 for v in moves.values()),'Slope changed node XZ'
groups={}
for path in sorted(root.glob('*-get_junction_snapshot.json')):
 s=read(path);groups.setdefault(m['identity'](s['junction']),[]).append(s)
lanes=[]
for node,ss in groups.items():
 before,after=m['lane_transitions'](ss[0]),m['lane_transitions'](ss[-1])
 lanes.append({'node':node,'missing':sorted(before-after),'added':sorted(after-before),'preserved':before==after,'count':len(after)})
 assert before==after,f'Directed lane connections changed at {node}'
report=json.loads((root/'report.json').read_text());side=[]
for key in report['changedUnselectedEdges']:
 key=tuple(key);b,c=old[key],new[key];ds=moves[m['identity'](b['startNode'])];de=moves[m['identity'](b['endNode'])]
 errors=[math.dist([q[k]-p[k] for k in ('x','y','z')],delta) for p,q,delta in zip(b['curve'],c['curve'],[ds,ds,de,de])]
 assert max(errors)<.001,'Side edge change exceeded endpoint/node translation'
 side.append({'edge':key,'startDelta':ds,'endDelta':de,'maxTranslationError':max(errors)})
result={'directedConnections':lanes,'unselectedEdgeTranslations':side,'nodeXZPreserved':True,'limits':'Slope changes node heights by design; native junction mesh and vehicle traversal need visual/live traffic checks.'}
(root/'independent-audit.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
