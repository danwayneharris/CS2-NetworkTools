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
 """Minimum worst control-point error over a bijection, allowing edge reversal.

 A nearest-neighbor comparison can reuse one preview edge and silently omit another.
 Thresholded bipartite matching covers every preview and permanent edge exactly once.
 """
 curves=[];original=None
 # ReadControlPreview emits entity, Temp, Edge, cubic, prefab, then lane IDs.
 # Existing-edge preview rows are checked separately by preservation assertions.
 for value in state['previewObservation']:
  if not isinstance(value,dict):continue
  if 'm_Original' in value:
   if original is not None:raise ValueError('Preview Temp without cubic')
   original=value['m_Original']
   if not isinstance(original,dict) or not all(k in original for k in ('Index','Version')):raise ValueError('Missing preview original identity')
  if any(k in value for k in ('a','b','c','d')):
   if original is None:raise ValueError('Preview cubic without Temp identity')
   if original['Index']==0 and original['Version']==0:curves.append(value)
   original=None
 if original is not None:raise ValueError('Preview Temp without cubic')
 if not curves or len(curves)!=len(new_edges):raise ValueError('Preview/permanent curve count mismatch')
 ids=[m['identity'](edge) for edge in new_edges]
 if len(set(ids))!=len(ids):raise ValueError('Duplicate permanent edge identity')
 def points(curve):
  if len(curve)!=4:raise ValueError('Expected four cubic controls')
  values=[m['position'](p) if isinstance(p,dict) else p for p in curve]
  if any(len(p)!=3 or any(not math.isfinite(v) for v in p) for p in values):raise ValueError('Invalid cubic control')
  return values
 previews=[points([c[k] for k in 'abcd']) for c in curves]
 actual=[points(edge['curve']) for edge in new_edges]
 errors=[[min(max(math.dist(p,q) for p,q in zip(edge,order)) for order in (curve,list(reversed(curve)))) for curve in previews] for edge in actual]
 def matches(limit):
  owners={}
  def assign(i,seen):
   for j,error in enumerate(errors[i]):
    if error>limit or j in seen:continue
    seen.add(j)
    if j not in owners or assign(owners[j],seen):
     owners[j]=i;return True
   return False
  return all(assign(i,set()) for i in range(len(actual)))
 limits=sorted(set(v for row in errors for v in row));low,high=0,len(limits)-1
 while low<high:
  mid=(low+high)//2
  if matches(limits[mid]):high=mid
  else:low=mid+1
 return limits[low]


def connect_preservation(ns, es, an, ae):
 """Connect adds entities; all existing identities, topology and geometry survive."""
 def unique(rows):
  result={m['identity'](row):row for row in rows}
  if len(result)!=len(rows):raise ValueError('Duplicate network entity identity')
  return result
 oldnodes,newnodes=unique(ns),unique(an);old,new=unique(es),unique(ae)
 def close(a,b):
  distance=math.dist(m['position'](a),m['position'](b))
  return math.isfinite(distance) and distance<=TOLERANCE
 edges=all(key in new and all(e[k]==new[key][k] for k in ('prefab','startNode','endNode')) and len(e['curve'])==len(new[key]['curve'])==4 and all(close(p,q) for p,q in zip(e['curve'],new[key]['curve'])) for key,e in old.items())
 nodes=all(key in newnodes and close(n['position'],newnodes[key]['position']) for key,n in oldnodes.items())
 return {'unchangedExistingEdges':edges,'unchangedExistingNodes':nodes}


def assert_connect_report(report):
 flags=('connected','prefabInherited','unchangedExistingEdges','unchangedExistingNodes')
 error=report['previewApplyMaxError']
 if any(report.get(k) is not True for k in flags) or not report['newEdges'] or not math.isfinite(error) or error>TOLERANCE:
  raise AssertionError('Connect preservation or preview/permanent verification failed')


def connect_selected_lane_proof(before, after, selected, new_edges):
 """Independent permanent graph reachability; no preview/provider success shortcut."""
 if len(before)!=2 or len(after)!=2 or len(selected)!=2:raise ValueError('Two selected endpoint lanes required')
 new_owners={edge[0] for edge in new_edges};proof=[]
 for ordinal,(old,current,chosen) in enumerate(zip(before,after,selected)):
  matches=[lane for lane in old['lanes'] if m['identity'](lane)==m['identity'](chosen)]
  if len(matches)!=1:raise ValueError('Original selected lane missing or duplicated')
  lane=matches[0];owner=lane['owner']['index']
  if owner==old['junction']['index']:raise ValueError('Expected approach edge lane')
  indices={lane[p]['laneIndex'] & 255 for p in ('start','middle','end') if lane[p]['ownerIndex']==owner}
  if len(indices)!=1:raise ValueError('Ambiguous original composition lane')
  label=(owner,next(iter(indices)),lane.get('secondary',False))
  original_composition=m['_lane_module'].composition_signature(old)
  current_composition=m['_lane_module'].composition_signature(current)
  if original_composition.get(owner)!=current_composition.get(owner):raise ValueError('Selected lane composition changed')
  transitions=m['_lane_module'].transitions(current,'car')
  if ordinal==0:witnesses=[(source,target) for source,target in transitions if source==label and target[0] in new_owners]
  else:witnesses=[(source,target) for source,target in transitions if target==label and source[0] in new_owners]
  if not witnesses:raise AssertionError('Chosen permanent directed lane connection absent')
  proof.append({'selected':label,'witnesses':witnesses})
 return proof


def preview_curves_to_rows(curves):
 return [{'edge':key,'curve':curve} for key,curve in curves.items()]

def main():
 p=argparse.ArgumentParser();p.add_argument('--fixture',default=str(Path(__file__).parent/'fixtures/toy-terrain-v11.json'));p.add_argument('--bridge',default='../cities2-agent-bridge-ndc');p.add_argument('--slope-mode',choices=['linear','ease','arch'],default='ease');p.add_argument('--smooth-start',action='store_true');p.add_argument('--smooth-end',action='store_true');p.add_argument('--reverse',action='store_true');p.add_argument('--output',required=True);p.add_argument('--save-root',required=True);p.add_argument('--run',action='store_true');p.add_argument('--stage',choices=['curve','slope','connect'],required=True);p.add_argument('--expected-fingerprint');p.add_argument('--case',default='highway-ramp-out');p.add_argument('--connect-lanes',action='store_true');p.add_argument('--connect-start-lane-index',type=int);p.add_argument('--connect-end-lane-index',type=int);p.add_argument('--connect-start-endpoint',choices=['start','end'],default='end');p.add_argument('--connect-straight-controls',action='store_true');p.add_argument('--connect-profile',action='store_true');p.add_argument('--connect-mode',choices=['SimpleCurve','ComplexCurve'],default='SimpleCurve');p.add_argument('--preview-only',action='store_true');p.add_argument('--connect-kind',choices=['road','rail'],default='road');a=p.parse_args()
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
  first,second,first_key=('hill-road','crest-dip-road',a.connect_start_endpoint) if a.connect_kind=='road' else ('rail-high-branch','rail-low-branch','start')
  start=m['resolve'](ns,next(c for c in f['cases'] if c['name']==first)[first_key]);end=m['resolve'](ns,next(c for c in f['cases'] if c['name']==second)['start'])
 if a.reverse:start,end=end,start
 control('select',start=start,end=end)
 if a.stage=='connect' and (a.connect_profile or a.connect_mode!='SimpleCurve'):
  control('configure',mode=a.connect_mode,smoothElevationProfile=a.connect_profile)
  # Native may reuse the previous temp graph when controls produce identical
  # geometry. Reselect from a cleared observation rather than certify reused IDs.
  control('clear');time.sleep(1);control('select',start=start,end=end)
 chosen_lanes=None
 if a.stage=='connect' and a.connect_lanes:
  choices=state()['laneChoices'];chosen_lanes=[]
  for endpoint,index in zip(choices,(a.connect_start_lane_index,a.connect_end_lane_index)):
   eligible=[c for c in endpoint['choices'] if c['eligible'] and (index is None or c['index']==index)]
   if len(eligible)!=1:raise ValueError('Explicit unique eligible lane index required: '+json.dumps(endpoint))
   chosen_lanes.append(eligible[0]['lane'])
  control('configure',laneAwareDirection=True,startLanes=[chosen_lanes[0]],endLanes=[chosen_lanes[1]])
 if a.stage=='connect' and a.connect_straight_controls:
  node_map={m['identity'](n):n for n in ns}
  pa=m['position'](node_map[m['identity'](start)]['position']);pb=m['position'](node_map[m['identity'](end)]['position'])
  def lerp(t):return [x+(y-x)*t for x,y in zip(pa,pb)]
  opts={'startControl':lerp(1/3),'endControl':lerp(2/3)} if a.connect_mode=='SimpleCurve' else {'startControl':lerp(1/6),'endControl':lerp(5/6),'midPoint':lerp(.5),'midStartControl':lerp(1/3),'midEndControl':lerp(2/3)}
  control('configure',**opts)
 if a.preview_only:
  if a.stage!='connect':raise ValueError('Preview-only currently supports Connect')
  s=poll(lambda s:s['previewReady'] or str(s.get('rejectionReason','')).startswith(('profile_native_','profile_endpoint_','profile_Horizontal','profile_Degenerate','lane_native_')))
  blocked_apply=False
  if not s['previewReady']:
   try:invoke('apply',{k:s[k] for k in ('session','revision','submission')})
   except RuntimeError as error:
    if 'stale_or_unverified_preview' not in str(error):raise
    blocked_apply=True
   else:raise AssertionError('Unverified profile Apply unexpectedly accepted; stop mutations')
   current_nodes,current_edges=r.network(f['region'])
   if m['fingerprint'](current_nodes,current_edges)!=m['fingerprint'](ns,es):raise AssertionError('Rejected Apply changed geometry')
  (r.output/'preview-report.json').write_text(json.dumps({'stage':'connect','applied':False,'blockedApplyVerified':blocked_apply,'checkpoint':saved['saveName'],'state':s,'limits':'Native preview only; no permanent result, rendered-surface, visual or vehicle verification.'},indent=2))
  print(json.dumps({'previewReady':s['previewReady'],'reason':s['rejectionReason'],'checkpoint':saved['saveName']}));return
 s=poll(lambda s:s['previewReady'])
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
 report={'geometryToleranceMeters':TOLERANCE,'stage':a.stage,'checkpoint':saved['saveName'],'beforeFingerprint':m['fingerprint'](ns,es),'afterFingerprint':m['fingerprint'](an,ae),'staleRevisionRejected':True,'nodeCount':[len(ns),len(an)],'edgeCount':[len(es),len(ae)],'parameters':{'slopeMode':a.slope_mode,'smoothStart':a.smooth_start,'smoothEnd':a.smooth_end,'reverse':a.reverse,'connectProfile':a.connect_profile,'connectMode':a.connect_mode,'connectStraightControls':a.connect_straight_controls,'connectLanes':a.connect_lanes,'selectedLanes':chosen_lanes},'limits':'No visual/vehicle or rendered-surface validation.'}
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
  report.update(connect_preservation(ns,es,an,ae))
  report['connected']=trace['connected'];report['newEdges']=[key for key in new if key not in old]
  report['previewApplyMaxError']=connect_preview_error(final,[new[key] for key in report['newEdges']])
  if chosen_lanes:report['selectedLaneConnections']=connect_selected_lane_proof(before,after,chosen_lanes,report['newEdges'])
 (r.output/'report.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
 if a.stage=='slope' and (not report['sameTopology'] or report['previewApplyMaxError']>TOLERANCE or report['incidentPreviewApplyMaxError']>TOLERANCE or report['nodeHorizontalMaxError']>TOLERANCE or report['fixedEndpointMaxError']>TOLERANCE or not report['directedConnectionsPreserved'] or not report['physicalLaneMappingPreserved']):raise AssertionError('Slope verification failed')
 if a.stage=='connect':assert_connect_report(report)
if __name__=='__main__':main()

