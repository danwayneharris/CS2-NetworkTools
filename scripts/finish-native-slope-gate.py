"""Apply a prepared toy Slope candidate with a separately captured native selected-edge witness.
--selected-edges must come from the live tool's m_CurrentPathEdges, not bridge BFS.
Stops on token/city/input mismatch; never retries Apply. Captures support audit-slope-capture.py.
"""
import argparse,json,runpy,math
from pathlib import Path
H=Path(__file__).resolve().parent;m=runpy.run_path(str(H/'live-regression.py'))
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--prepared',required=True);p.add_argument('--selected-edges',required=True);p.add_argument('--bridge',required=True);p.add_argument('--save-root',required=True);p.add_argument('--output',required=True);p.add_argument('--run',action='store_true');a=p.parse_args()
if not a.run:p.error('Explicit --run required')
d=json.loads(Path(a.prepared).read_text());f=d['fixture'];r=m['Runner'](a.bridge,a.output);city=r.call('get_city_state');assert r.city_session==d['citySession'] and city['selectedSpeed']==0 and city['population']==0 and city['controlEnabled']
runpy.run_path(str(H/'reload-toy-baseline.py'))['verified_package'](a.save_root,f['baseline'],f['baselineSha256'])
ns,es=r.network(f['region']);assert m['fingerprint'](ns,es)==m['fingerprint'](d['nodes'],d['edges'])==f['fingerprint']
chosen=[tuple(map(int,x.split(':'))) for x in a.selected_edges.split(',')];assert len(set(chosen))==len(chosen)==d['state']['pathEdges'];old={m['identity'](e):e for e in es};assert all(x in old for x in chosen)
# Validate a contiguous ordered path independently against the live graph.
current=m['identity'](d['state']['start']);selected={current}
for key in chosen:
 edge=old[key];ends=[m['identity'](edge[k]) for k in ('startNode','endNode')];assert current in ends;current=ends[1] if current==ends[0] else ends[0];assert current not in selected;selected.add(current)
assert current==m['identity'](d['state']['end'])
side=m['identity'](f['incidentEdge']);assert side not in chosen and all(m['identity'](old[side][k]) in selected for k in ('startNode','endNode'))
def invoke(cmd,args=None):return r.call('invoke_provider',{'provider':'networktools','revision':d['providerRevision'],'command':'slope_'+cmd,'args':args or {}})
def same(s):return s['previewReady'] and all(s[k]==d['state'][k] for k in ('session','revision','submission'))
assert same(invoke('state'));watched=[{k:n[k] for k in ('index','version')} for n in ns if m['identity'](n) in selected and len(n['edges'])>1]
before=[r.call('get_junction_snapshot',n) for n in watched];previews={}
for n in watched:
 snap=r.call('get_junction_preview',n).get('connectedSnapshot');assert snap and snap['complete'] and not snap['errors']
 for o in snap['owners']:
  if o.get('curve'):
   key=m['identity'](o['temp']['original']);assert key not in previews or previews[key]==o['curve'];previews[key]=o['curve']
assert set(chosen+[side]).issubset(previews);assert same(invoke('state'))
invoke('apply',{k:d['state'][k] for k in ('session','revision','submission')});r.poll('invoke_provider',lambda s:s['phase']=='Idle',{'provider':'networktools','revision':d['providerRevision'],'command':'slope_state','args':{}})
after,an,ae=r.settled_permanent(watched,f['region']);new={m['identity'](e):e for e in ae};on={m['identity'](n):n for n in ns};nn={m['identity'](n):n for n in an}
error=max(math.dist(m['position'](p),m['position'](q)) for k,c in previews.items() for p,q in zip(c,new[k]['curve']))
report={'case':'two-ended-incident-slope','geometryToleranceMeters':.05,'parameters':{'smoothStart':False,'smoothEnd':False,'slopeMode':'linear'},'sameTopology':old.keys()==new.keys() and on.keys()==nn.keys() and all(all(e[k]==new[key][k] for k in ('startNode','endNode','prefab')) for key,e in old.items()),'previewApplyMaxError':error,'nodeHorizontalMaxError':max(math.hypot(n['position']['x']-nn[k]['position']['x'],n['position']['z']-nn[k]['position']['z']) for k,n in on.items()),'directedConnectionsPreserved':all(m['lane_transitions'](b)==m['lane_transitions'](c) for b,c in zip(before,after)),'physicalLaneMappingPreserved':all(m['_lane_module'].composition_signature(b)==m['_lane_module'].composition_signature(c) for b,c in zip(before,after)),'incidentEdge':side}
deltas=[tuple(nn[m['identity'](old[side][end])]['position'][axis]-on[m['identity'](old[side][end])]['position'][axis] for axis in ('x','y','z')) for end in ('startNode','endNode')]
report['changedUnselectedEdges']=[k for k,e in old.items() if k not in chosen and e['curve']!=new[k]['curve']]
report['bothEndpointDeltas']=deltas;report['bothEndpointsMoved']=all(math.dist(x,(0,0,0))>.05 for x in deltas)
report['incidentTranslationMaxError']=max(math.dist(tuple(p[j]+deltas[0 if i<2 else 1][j] for j in range(3)),m['position'](new[side]['curve'][i])) for i,c in enumerate(old[side]['curve']) for p in [m['position'](c)])
report['passed']=all(report[k] for k in ('sameTopology','directedConnectionsPreserved','physicalLaneMappingPreserved','bothEndpointsMoved')) and max(error,report['nodeHorizontalMaxError'],report['incidentTranslationMaxError'])<=.05
(r.output/'report.json').write_text(json.dumps(report,indent=2));(r.output/'comparison.json').write_text(json.dumps({'beforeNodes':ns,'beforeEdges':es,'afterNodes':an,'afterEdges':ae,'path':{'edges':[dict(index=k[0],version=k[1]) for k in chosen]},'previews':[{'edge':k,'curve':c} for k,c in previews.items()]},indent=2));print(json.dumps(report,indent=2))
if not report['passed']:raise AssertionError('Native two-ended gate failed; inspect captures')
