"""Read-only before/after oracle for debugger-driven Release shape operations.
Uses a checksummed toy fixture and generic bridge snapshots, not Debug-only NT APIs.
"""
import argparse,json,runpy,math
from pathlib import Path
H=Path(__file__).resolve().parent;m=runpy.run_path(str(H/'live-regression.py'))
p=argparse.ArgumentParser();p.add_argument('--phase',choices=['before','after'],required=True);p.add_argument('--mode',choices=['curve','slope'],default='curve');p.add_argument('--fixture',required=True);p.add_argument('--case',required=True);p.add_argument('--bridge',required=True);p.add_argument('--save-root',required=True);p.add_argument('--output',required=True);a=p.parse_args()
f=json.loads(Path(a.fixture).read_text());root=Path(a.output);root.mkdir(exist_ok=True);r=m['Runner'](a.bridge,root/a.phase);city=r.call('get_city_state');assert city['selectedSpeed']==0 and city['population']==0 and city['controlEnabled']
runpy.run_path(str(H/'reload-toy-baseline.py'))['verified_package'](a.save_root,f['baseline'],f['baselineSha256'])
if a.phase=='before':
 ns,es=r.network(f['region']);assert m['fingerprint'](ns,es)==f['fingerprint'];case=next(c for c in f['cases'] if c['name']==a.case);start,end=(m['resolve'](ns,case[k]) for k in ('start','end'));path=r.call('trace_network',dict(fromIndex=start['index'],fromVersion=start['version'],toIndex=end['index'],toVersion=end['version']));assert path['connected'];chosen={m['identity'](e) for e in path['edges']};selected={m['identity'](n) for e in es if m['identity'](e) in chosen for n in (e['startNode'],e['endNode'])};assert len(selected)==len(chosen)+1
 watched=[{k:n[k] for k in ('index','version')} for n in ns if m['identity'](n) in selected and len(n['edges'])>1];assert watched
 before=[r.call('get_junction_snapshot',n) for n in watched];previews={}
 for n in watched:
  snap=r.call('get_junction_preview',n).get('connectedSnapshot');assert snap and snap['complete'] and not snap['errors']
  for o in snap['owners']:
   if o.get('curve'):
    k=m['identity'](o['temp']['original']);assert k not in previews or previews[k]==o['curve'];previews[k]=o['curve']
 assert chosen.issubset(previews)
 d={'citySession':r.city_session,'case':a.case,'mode':a.mode,'nodes':ns,'edges':es,'path':path,'watched':watched,'snapshots':before,'previews':[{'edge':k,'curve':c} for k,c in previews.items()],'start':start,'end':end};(root/'prepared.json').write_text(json.dumps(d,indent=2));print('Native preview captured; no Apply requested')
else:
 d=json.loads((root/'prepared.json').read_text());assert r.city_session==d['citySession'] and a.mode==d['mode'];sn,ns,es=r.settled_permanent(d['watched'],f['region']);old={m['identity'](e):e for e in d['edges']};new={m['identity'](e):e for e in es};on={m['identity'](n):n for n in d['nodes']};nn={m['identity'](n):n for n in ns};chosen={m['identity'](e) for e in d['path']['edges']}
 dist=lambda a,b:math.dist(m['position'](a),m['position'](b))
 err=max(dist(p,q) for row in d['previews'] for p,q in zip(row['curve'],new[tuple(row['edge'])]['curve']))
 unselected=max([dist(p,q) for k,e in old.items() if k not in chosen for p,q in zip(e['curve'],new[k]['curve'])] or [0])
 fixed=max(dist(on[m['identity'](n)]['position'],nn[m['identity'](n)]['position']) for n in (d['start'],d['end']))
 invariant=max(abs(n['position']['y']-nn[k]['position']['y']) if a.mode=='curve' else math.hypot(n['position']['x']-nn[k]['position']['x'],n['position']['z']-nn[k]['position']['z']) for k,n in on.items())
 report={'case':a.case,'mode':a.mode,'configuration':'Release','geometryToleranceMeters':.05,'sameTopology':old.keys()==new.keys() and on.keys()==nn.keys() and all(all(e[k]==new[key][k] for k in ('startNode','endNode','prefab')) for key,e in old.items()),'previewApplyMaxError':err,'unselectedCurveMaxError':unselected,'fixedEndpointMaxError':fixed,'preservedAxisMaxError':invariant,'directedConnectionsPreserved':all(m['lane_transitions'](b)==m['lane_transitions'](c) for b,c in zip(d['snapshots'],sn)),'physicalLaneMappingPreserved':all(m['_lane_module'].composition_signature(b)==m['_lane_module'].composition_signature(c) for b,c in zip(d['snapshots'],sn)),'afterFingerprint':m['fingerprint'](ns,es)}
 report['passed']=all(report[k] for k in ('sameTopology','directedConnectionsPreserved','physicalLaneMappingPreserved')) and max(err,unselected,fixed,invariant)<=.05
 (root/'report.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
 if not report['passed']:raise AssertionError('Independent native Release shape gate failed')
