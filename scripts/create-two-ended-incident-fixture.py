"""Create a checkpointed toy loop for the two-ended incident-edge Slope test.
Requires the exact terrain-v1.1 baseline; one native build, no automatic retries.
The alternate prefab makes NT prefer the original road under its existing cost.
A bridge BFS trace is NOT a route-selection oracle once the loop exists.
"""
import argparse,json,runpy,hashlib
from pathlib import Path
H=Path(__file__).resolve().parent
m=runpy.run_path(str(H/'live-regression.py'))
verified=runpy.run_path(str(H/'reload-toy-baseline.py'))['verified_package']
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--bridge',required=True);p.add_argument('--save-root',required=True);p.add_argument('--output',required=True);p.add_argument('--run',action='store_true');a=p.parse_args()
 if not a.run:p.error('Explicit --run required')
 f=json.loads((H/'fixtures/toy-terrain-v11.json').read_text());r=m['Runner'](a.bridge,a.output)
 city=r.call('get_city_state');assert city['selectedSpeed']==0 and city['population']==0 and city['controlEnabled']
 verified(a.save_root,f['baseline'],f['baselineSha256']);nodes,edges=r.network(f['region']);assert m['fingerprint'](nodes,edges)==f['fingerprint']
 def save(label):
  op=r.call('save_checkpoint',{'label':label});s=r.poll('get_operation',lambda x:x['status'] in ('complete','failed','interrupted'),{'id':op['id']});assert s['status']=='complete';return verified(a.save_root,s['saveName']+'.cok')
 recovery=save('before-two-ended-fixture')
 case=next(c for c in f['cases'] if c['name']=='hill-road');start,end=(m['resolve'](nodes,case[k]) for k in ('start','end'))
 path=r.call('trace_network',dict(fromIndex=start['index'],fromVersion=start['version'],toIndex=end['index'],toVersion=end['version']));assert path['connected']
 points=[[-3918.03735,606.342041,-1854.291],[-3826.88647,615.6897,-1816.07153]]
 ends=[m['resolve'](nodes,q) for q in points];byid={m['identity'](n):n for n in nodes}
 prefabs=r.call('get_build_prefabs',{'kind':'network','filter':'Medium Road','limit':4096});assert prefabs['nextOffset'] is None
 prefab=next(x for x in prefabs['prefabs'] if x['name']=='Medium Road');assert not prefab['locked'] and prefab['bridgePlacementSupported']
 endpoints=[dict(byid[m['identity'](n)]['position'],**n) for n in ends]
 args={'prefabIndex':prefab['index'],'prefabVersion':prefab['version'],'start':endpoints[0],'end':endpoints[1],'control':{'x':-3870,'y':610,'z':-1885},'maxCost':50000}
 op=r.call('build_road',args);result=r.poll('get_operation',lambda x:x['status'] in ('complete','failed','interrupted'),{'id':op['id']});(r.output/'build-result.json').write_text(json.dumps(result,indent=2))
 if result['status']!='complete':raise RuntimeError('Native build failed/uncertain; inspect before retry')
 _,after,ae=r.settled_permanent(ends,f['region']);old={m['identity'](e) for e in edges};new=[e for e in ae if m['identity'](e) not in old]
 shared=[e for e in new if {m['identity'](e['startNode']),m['identity'](e['endNode'])}=={m['identity'](n) for n in ends}]
 if len(shared)!=1:raise RuntimeError('Native result is not a single two-ended edge; inspect fixture, do not replay')
 checkpoint=save('two-ended-incident-baseline')
 fixture={'baseline':checkpoint.name,'baselineSha256':hashlib.sha256(checkpoint.read_bytes()).hexdigest().upper(),'region':f['region'],'fingerprint':m['fingerprint'](after,ae),'cases':[case],'incidentEndpointPositions':[byid[m['identity'](n)]['position'] for n in ends],'originalPath':path,'incidentEdge':shared[0],'recovery':recovery.name}
 (r.output/'fixture.json').write_text(json.dumps(fixture,indent=2));print(json.dumps({'baseline':checkpoint.name,'newEdges':len(new),'twoEndedEdge':shared[0]['index'],'pathEdgeCount':len(path['edges'])}))
if __name__=='__main__':main()
