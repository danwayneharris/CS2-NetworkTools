"""Checkpointed NT022 fixed-node preview probe; NEVER applies geometry.
Requires explicit --nt-connect-fixed-anchor-probe launch and exact toy fingerprint.
This tests output feasibility, not selected lane alignment or traffic.
"""
import argparse,json,math,runpy,time
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))
verified=runpy.run_path(str(Path(__file__).with_name('reload-toy-baseline.py')))['verified_package']

def main():
 p=argparse.ArgumentParser();p.add_argument('--fixture',default='scripts/fixtures/toy-terrain-v11.json');p.add_argument('--bridge',default='../cities2-agent-bridge-ndc');p.add_argument('--save-root',required=True);p.add_argument('--output',required=True);p.add_argument('--run',action='store_true');a=p.parse_args()
 if not a.run:p.error('Explicit --run required')
 f=json.loads(Path(a.fixture).read_text());r=m['Runner'](a.bridge,a.output)
 city=r.call('get_city_state')
 if city['population']!=0 or city['selectedSpeed']!=0 or not city['controlEnabled']:raise ValueError('Paused controlled toy required')
 verified(a.save_root,f['baseline'],f['baselineSha256']);ns,es=r.network(f['region'])
 if m['fingerprint'](ns,es)!=f['fingerprint']:raise ValueError('Exact baseline required')
 op=r.call('save_checkpoint',{'label':'nt022-fixed-anchor-probe'});saved=r.poll('get_operation',lambda s:s['status'] in ('complete','failed','interrupted'),{'id':op['id']})
 if saved['status']!='complete':raise RuntimeError('Checkpoint failed')
 verified(a.save_root,saved['saveName']+'.cok')
 catalog=r.call('list_providers');providers=[x for x in catalog['providers'] if x['id']=='networktools']
 if not catalog['complete'] or len(providers)!=1:raise RuntimeError('Incomplete provider discovery')
 def invoke(command,args=None):return r.call('invoke_provider',{'provider':'networktools','revision':providers[0]['revision'],'command':'connect_'+command,'args':args or {}})
 def state():return invoke('state')
 def control(command,**args):
  s=state();return invoke(command,dict(session=s['session'],revision=s['revision'],**args))
 invoke('activate');deadline=time.monotonic()+40
 while not state()['active']:
  if time.monotonic()>deadline:raise TimeoutError('Activation')
  time.sleep(.5)
 control('clear')
 cases={c['name']:c for c in f['cases']};start=m['resolve'](ns,cases['hill-road']['end']);end=m['resolve'](ns,cases['crest-dip-road']['start'])
 control('select',start=start,end=end)
 control('configure',mode='SimpleCurve',smoothElevationProfile=False,laneAwareDirection=False)
 last=None;stable=0;deadline=time.monotonic()+40
 while time.monotonic()<deadline:
  s=state();key=json.dumps([s.get('authoredCandidate'),s.get('previewObservation')],sort_keys=True)
  good=(s.get('authoredCandidate') or {}).get('FixedNodeAlignmentProbe') and s.get('previewObservation') and s['rejectionReason']=='alignment_probe_preview_only'
  stable=stable+1 if good and key==last else 0;last=key
  if stable>=3:break
  time.sleep(.5)
 else:raise TimeoutError('Probe preview did not settle')
 snapshots=[r.call('get_junction_preview',n) for n in (start,end)]
 report={'build':s.get('build'),'checkpoint':saved['saveName'],'probe':'1.5 m lateral offset; fixed original outer CoursePos entities/positions','applied':False,'authoredCandidate':s['authoredCandidate'],'endpoints':[],'limits':'Preview observations only; no selected lane correspondence, Apply, rendered-surface, reload or vehicle claim.'}
 original={m['identity'](n):n for n in ns}
 for ordinal,(node,observed) in enumerate(zip((start,end),snapshots)):
  snapshot=observed.get('snapshot')
  if not snapshot or not snapshot['complete']:raise RuntimeError('Incomplete native snapshot')
  j=snapshot['junction'];owners=snapshot['owners'];jo=[o for o in owners if m['identity'](o)==m['identity'](j)]
  if len(jo)!=1:raise RuntimeError('Ambiguous preview node')
  new=[o for o in owners if o.get('curve') and o.get('temp',{}).get('original',{}).get('index')==0]
  if len(new)!=1:raise RuntimeError('Ambiguous new incident edge')
  e=new[0];at_start=m['identity'](e['startNode'])==m['identity'](j);point=e['curve'][0 if at_start else 3]
  authored=s['authoredCandidate']['CurveStartPointPosition' if ordinal==0 else 'CurveEndPointPosition']
  report['endpoints'].append({'node':node,'nodeErrorMeters':math.dist(m['position'](jo[0]['position']),m['position'](original[m['identity'](node)]['position'])),'curveEndpointToNodeMeters':math.dist(m['position'](point),m['position'](jo[0]['position'])),'curveEndpointToAuthoredMeters':math.dist(m['position'](point),tuple(authored) if isinstance(authored,list) else m['position'](authored))})
 try:invoke('apply',{k:s[k] for k in ('session','revision','submission')})
 except RuntimeError as error:
  if 'stale_or_unverified_preview' not in str(error):raise
  report['applyBlocked']=True
 else:raise AssertionError('Diagnostic Apply unexpectedly accepted; stop')
 an,ae=r.network(f['region']);report['permanentUnchanged']=m['fingerprint'](an,ae)==f['fingerprint']
 report['fixedNodeInvariantPassed']=all(e['nodeErrorMeters']<=.05 for e in report['endpoints'])
 report['status']='prerequisite_observed' if report['fixedNodeInvariantPassed'] else 'blocked_fixed_node_invariant'
 (r.output/'report.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
 if not report['permanentUnchanged']:raise AssertionError('Permanent geometry changed')
if __name__=='__main__':main()
