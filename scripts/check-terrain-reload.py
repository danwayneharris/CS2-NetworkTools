"""Read-only identity-independent geometry/lane snapshot for save/reload comparison."""
import argparse,json,runpy,hashlib
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))
p=argparse.ArgumentParser();p.add_argument('--bridge',default='../cities2-agent-bridge-ndc');p.add_argument('--fixture',required=True);p.add_argument('--output',required=True);p.add_argument('--compare');a=p.parse_args()
f=json.loads(Path(a.fixture).read_text());r=m['Runner'](a.bridge,a.output);city=r.call('get_city_state')
if city['selectedSpeed']!=0 or city['population']!=0:raise RuntimeError('Paused toy required')
ns,es=r.network(f['region']);points={m['identity'](n):n['position'] for n in ns}
labels={e['index']:hashlib.sha256(json.dumps([e['prefab'],e['curve'],points[m['identity'](e['startNode'])],points[m['identity'](e['endNode'])]],sort_keys=True).encode()).hexdigest() for e in es}
if len(set(labels.values()))!=len(labels):raise RuntimeError('Ambiguous geometry labels for persistence comparison')
connections={};physical={}
for node in ns:
 if len(node['edges'])<2:continue
 s=r.call('get_junction_snapshot',{k:node[k] for k in ('index','version')})
 if not s['complete'] or s['errors']:raise RuntimeError('Incomplete snapshot')
 pairs=sorted((kind,(labels[start[0]],*start[1:]),(labels[end[0]],*end[1:])) for kind,start,end in m['lane_transitions'](s))
 key=json.dumps(node['position'],sort_keys=True)
 connections[key]=pairs
 physical[key]={}
 for index,signature in m['_lane_module'].composition_signature(s).items():
  semantic=json.loads(signature)
  for lane in semantic['lanes']:
   # Prefab entity identities are world-local; retain native flags/limits plus
   # lane index, lateral position, direction, carriageway and owner prefab name.
   lane['prefab']={k:v for k,v in lane['prefab'].items() if k not in ('index','version')}
  physical[key][labels[index]]=semantic
result={'fingerprint':m['fingerprint'](ns,es),'junctionConnections':connections,'physicalLaneMapping':physical}
# Normalize tuples before comparing with previously serialized lists.
result=json.loads(json.dumps(result));(r.output/'comparison.json').write_text(json.dumps(result,indent=2))
if a.compare:
 old=json.loads(Path(a.compare).read_text())
 if old!=result:raise AssertionError('Geometry or directed junction connections changed across reload')
print(json.dumps({'citySession':r.city_session,'fingerprint':result['fingerprint'],'junctions':len(connections),'compared':bool(a.compare),'matches':True if a.compare else None}))
(r.output/'state.json').write_text(json.dumps({'citySession':r.city_session,'city':city},indent=2))
