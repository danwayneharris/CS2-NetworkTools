"""Explicit checkpointed toy smoke test for newly exposed Slope/Connect controls.
Uses generic provider discovery; saves raw evidence. Does not claim visual or
vehicle validation. Stops on uncertain operations; never retries Apply.
"""
import argparse,json,math,runpy,time,zipfile
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))
TOLERANCE=m['GEOMETRY_TOLERANCE_METERS']
verified=runpy.run_path(str(Path(__file__).with_name('reload-toy-baseline.py')))['verified_package']
def connect_preview_error(state, new_edges):
 curves=[v for v in state['previewObservation'] if isinstance(v,dict) and all(k in v for k in ('a','b','c','d'))]
 if not curves or not new_edges:raise ValueError('Missing native preview/permanent curves')
 errors=[]
 for edge in new_edges:
  points=[m['position'](p) for p in edge['curve']]
  errors.append(min(min(max(math.dist(p,c[k]) for p,k in zip(points,order)) for order in ('abcd','dcba')) for c in curves))
 return max(errors)

def preview_curves_to_rows(curves):
 return [{'edge':key,'curve':curve} for key,curve in curves.items()]

def main():
 p=argparse.ArgumentParser();p.add_argument('--fixture',default=str(Path(__file__).parent/'fixtures/toy-terrain-v11.json'));p.add_argument('--bridge',default='../cities2-agent-bridge-ndc');p.add_argument('--slope-mode',choices=['linear','ease','arch'],default='ease');p.add_argument('--smooth-start',action='store_true');p.add_argument('--smooth-end',action='store_true');p.add_argument('--reverse',action='store_true');p.add_argument('--output',required=True);p.add_argument('--save-root',required=True);p.add_argument('--run',action='store_true');p.add_argument('--stage',choices=['curve','slope','connect'],required=True);p.add_argument('--expected-fingerprint');p.add_argument('--case',default='highway-ramp-out');p.add_argument('--connect-kind',choices=['road','rail'],default='road');a=p.parse_args()
 if not a.run:p.error('Explicit --run required')
 f=json.loads(Path(a.fixture).read_text());case=next(c for c in f['cases'] if c['name']==a.case);r=m['Runner'](a.bridge,a.output)
 city=r.call('get_city_state')
 if city['selectedSpeed']!=0 or city['population']!=0 or not city['controlEnabled']:raise ValueError('Paused controlled toy required')
 verified(a.save_root,f['baseline'],f['baselineSha256'])
 if a.stage=='curve':r.execute(f,case,a.save_root);return
 ns,es=r.network(f['region'])
 if not a.expected_fingerprint or m['fingerprint'](ns,es)!=a.expected_fingerprint:raise ValueError('Explicit known geometry fingerprint required')
 op=r.call('save_checkpoint',{'label':'provider-'+a.stage});saved=r.poll('get_operation',lambda s:s['status'] in ('complete','failed','interrupted'),{'id':op['id']})
 if saved['status']!='complete':raise RuntimeError('Checkpoint failed')
 verified(a.save_root,saved['saveName']+'.cok')
 catalog=r.call('list_providers');providers=[x for x in catalog['providers'] if x['id']=='networktools']
 if not catalog['complete'] or len(providers)!=1:raise RuntimeError('Provider discovery failed')
 revision=providers[0]['revision']
 def invoke(cmd,args=None):return r.call('invoke_provider',{'provider':'networktools','revision':revision,'command':a.stage+'_'+cmd,'args':args or {}})
 def state():return invoke('state')
 def control(cmd,**args):
  s=state();return invoke(cmd,dict(session=s['session'],revision=s['revision'],**args))
 def poll(predicate):
  end=time.monotonic()+40
  while time.monotonic()<end:
   s=state()
   if predicate(s):return s
   time.sleep(.5)
  raise TimeoutError('Tool preview/state did not settle; inspect captures')
 invoke('activate');poll(lambda s:s['active']);control('clear')
 if a.stage=='slope':
  control('configure',mode=a.slope_mode,easeIn=.1,easeOut=.1,archHeight=10,archPosition=.5,smoothStart=a.smooth_start,smoothEnd=a.smooth_end)
  start,end=(m['resolve'](ns,case[k]) for k in ('start','end'))
 else:
  first,second,first_key=('hill-road','crest-dip-road','end') if a.connect_kind=='road' else ('rail-high-branch','rail-low-branch','start')
  start=m['resolve'](ns,next(c for c in f['cases'] if c['name']==first)[first_key]);end=m['resolve'](ns,next(c for c in f['cases'] if c['name']==second)['start'])
 if a.reverse:start,end=end,start
 control('select',start=start,end=end);s=poll(lambda s:s['previewReady'])
 # Deliberately invalid old revision must fail before any mutation.
 try:invoke('apply',{'session':s['session'],'revision':s['revision']-1,'submission':s['submission']})
 except RuntimeError as error:
  if 'stale_tool_revision' not in str(error):raise
 else:raise AssertionError('Old revision unexpectedly accepted')
 # Capture native previews independently at all affected shared nodes.
 watched=[];preview_curves={}
 if a.stage=='slope':
  path=r.call('trace_network',{'fromIndex':start['index'],'fromVersion':start['version'],'toIndex':end['index'],'toVersion':end['version']})
  chosen={m['identity'](e) for e in path['edges']};selected={m['identity'](n) for e in es if m['identity'](e) in chosen for n in (e['startNode'],e['endNode'])}
  watched=[{k:n[k] for k in ('index','version')} for n in ns if m['identity'](n) in selected and len(n['edges'])>1]
  for n in watched:
   preview=r.call('get_junction_preview',n).get('connectedSnapshot')
   if not preview or not preview['complete']:raise RuntimeError('Incomplete native preview')
   for o in preview['owners']:
    if o.get('curve'):
     key=m['identity'](o['temp']['original'])
     if key in preview_curves and preview_curves[key]!=o['curve']:raise RuntimeError('Preview changed during capture')
     preview_curves[key]=o['curve']
  if not chosen.issubset(preview_curves):raise RuntimeError('Missing selected preview edges')
 else:watched=[start,end]
 before=[r.call('get_junction_snapshot',n) for n in watched]
 final=state()
 if not final['previewReady'] or any(final[k]!=s[k] for k in ('session','revision','submission')):raise RuntimeError('Preview token changed')
 invoke('apply',{k:s[k] for k in ('session','revision','submission')});poll(lambda s:s['phase']=='Idle')
 after,an,ae=r.settled_permanent(watched,f['region']);new={m['identity'](e):e for e in ae};old={m['identity'](e):e for e in es}
 report={'geometryToleranceMeters':TOLERANCE,'stage':a.stage,'checkpoint':saved['saveName'],'beforeFingerprint':m['fingerprint'](ns,es),'afterFingerprint':m['fingerprint'](an,ae),'staleRevisionRejected':True,'nodeCount':[len(ns),len(an)],'edgeCount':[len(es),len(ae)],'parameters':{'slopeMode':a.slope_mode,'smoothStart':a.smooth_start,'smoothEnd':a.smooth_end,'reverse':a.reverse},'limits':'No visual/vehicle or rendered-surface validation.'}
 if a.stage=='slope':
  report['sameTopology']=old.keys()==new.keys() and all(all(e[k]==new[key][k] for k in ('startNode','endNode','prefab')) for key,e in old.items())
  report['previewApplyMaxError']=max(math.dist(m['position'](p),m['position'](q)) for key in chosen for p,q in zip(preview_curves[key],new[key]['curve']))
  report['changedUnselectedEdges']=[key for key,e in old.items() if key not in chosen and e['curve']!=new[key]['curve']]
  report['directedConnectionsPreserved']=all(m['lane_transitions'](b)==m['lane_transitions'](c) for b,c in zip(before,after))
  report['physicalLaneMappingPreserved']=all(m['_lane_module'].composition_signature(b)==m['_lane_module'].composition_signature(c) for b,c in zip(before,after))
  report['incidentPreviewApplyMaxError']=max(math.dist(m['position'](p),m['position'](q)) for key,curve in preview_curves.items() for p,q in zip(curve,new[key]['curve']))
  oldnodes={m['identity'](n):n for n in ns};newnodes={m['identity'](n):n for n in an}
  report['nodeHorizontalMaxError']=max(math.hypot(n['position']['x']-newnodes[key]['position']['x'],n['position']['z']-newnodes[key]['position']['z']) for key,n in oldnodes.items())
  report['fixedEndpointMaxError']=max(math.dist(m['position'](oldnodes[m['identity'](n)]['position']),m['position'](newnodes[m['identity'](n)]['position'])) for n in (start,end))
  (r.output/'comparison.json').write_text(json.dumps({'beforeNodes':ns,'beforeEdges':es,'afterNodes':an,'afterEdges':ae,'path':path,'previews':preview_curves_to_rows(preview_curves)},indent=2))
 else:
  trace=r.call('trace_network',{'fromIndex':start['index'],'fromVersion':start['version'],'toIndex':end['index'],'toVersion':end['version']})
  source=[e for e in es if m['identity'](start) in (m['identity'](e['startNode']),m['identity'](e['endNode']))]
  if len(source)!=1:raise AssertionError('Expected one source edge')
  report['expectedPrefab']=source[0]['prefab']
  report['prefabInherited']=all(e['prefab']==source[0]['prefab'] for key,e in new.items() if key not in old)
  report['unchangedExistingEdges']=all(key in new and all(e[k]==new[key][k] for k in ('curve','prefab','startNode','endNode')) for key,e in old.items())
  report['connected']=trace['connected'];report['newEdges']=[key for key in new if key not in old]
  report['previewApplyMaxError']=connect_preview_error(final,[new[key] for key in report['newEdges']])
 (r.output/'report.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
 if a.stage=='slope' and (not report['sameTopology'] or report['previewApplyMaxError']>TOLERANCE or report['incidentPreviewApplyMaxError']>TOLERANCE or report['nodeHorizontalMaxError']>TOLERANCE or report['fixedEndpointMaxError']>TOLERANCE or not report['directedConnectionsPreserved'] or not report['physicalLaneMappingPreserved']):raise AssertionError('Slope verification failed')
 if a.stage=='connect' and (not report['connected'] or not report['prefabInherited'] or not report['newEdges'] or report['previewApplyMaxError']>TOLERANCE):raise AssertionError('Connect permanent topology failed')
if __name__=='__main__':main()

