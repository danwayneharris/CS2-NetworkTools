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
# Verify enabled boundary smoothing against the actual unselected neighbor's
# tangent, in path-forward orientation. Compare vertical handle displacement in
# meters, not a permissive grade tolerance on short handles.
if report.get('parameters',{}).get('slopeMode')=='linear':
 data=json.loads((root/'comparison.json').read_text())
 util=runpy.run_path(str(Path(__file__).with_name('summarize-profile-experiment.py')))
 before=util['oriented_edges'](data['beforeEdges'],data['path']);after=util['oriented_edges'](data['afterEdges'],data['path'])
 chosen={m['identity'](e) for e in before};boundary=[]
 for start,enabled in ((True,report['parameters']['smoothStart']),(False,report['parameters']['smoothEnd'])):
  e=before[0 if start else -1];forward=e['pathForward'];node=m['identity'](e['startNode' if forward==start else 'endNode'])
  others=[e for e in data['beforeEdges'] if m['identity'](e) not in chosen and node in (m['identity'](e['startNode']),m['identity'](e['endNode']))]
  entry={'end':'start' if start else 'end','enabled':enabled,'eligible':len(others)==1}
  if enabled and len(others)==1:
   other=others[0];atstart=m['identity'](other['startNode'])==node;ep=other['curve'][0 if atstart else 3];handle=other['curve'][1 if atstart else 2]
   horizontal=math.hypot(handle['x']-ep['x'],handle['z']-ep['z'])
   if horizontal<.001:entry['eligible']=False
   else:
    grade=(handle['y']-ep['y'])/horizontal*(-1 if start else 1)
    c=after[0 if start else -1]['curve'];ep=c[0 if start else 3];handle=c[1 if start else 2]
    distance=math.hypot(handle['x']-ep['x'],handle['z']-ep['z'])
    error=abs(handle['y']-ep['y']-grade*distance*(1 if start else -1))
    entry.update(anchorGrade=grade,verticalHandleError=error)
    assert error<=.001,'Enabled boundary smoothing does not match its neighbor'
  boundary.append(entry)
 result['boundarySmoothing']=boundary
(root/'independent-audit.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
