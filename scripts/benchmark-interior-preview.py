"""Checkpointed toy preview timing and directed-connection checks. Never Applies.
Requires --run; mailbox elapsed time includes transport, use native logs for latency.
"""
import argparse, hashlib, json, runpy, time, zipfile
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--fixture',required=True);p.add_argument('--save-root',required=True)
p.add_argument('--output',required=True);p.add_argument('--run',action='store_true')
a=p.parse_args();f=json.loads(Path(a.fixture).read_text());case=f['cases'][0]
r=m['Runner']('../cities2-agent-bridge-ndc',a.output)
city=r.call('get_city_state')
if city['selectedSpeed']!=0 or city['population']!=0 or not city['controlEnabled']:raise RuntimeError('Paused control-enabled toy required')
saves=list(Path(a.save_root).rglob(f['baseline']))
if len(saves)!=1 or hashlib.sha256(saves[0].read_bytes()).hexdigest().upper()!=f['baselineSha256']:raise RuntimeError('Baseline package mismatch')
nodes,edges=r.network(f['region'])
if m['fingerprint'](nodes,edges)!=f['fingerprint']:raise RuntimeError('Toy geometry mismatch')
start,end=(m['resolve'](nodes,case[k]) for k in ('start','end'))
path=r.call('trace_network',dict(fromIndex=start['index'],fromVersion=start['version'],toIndex=end['index'],toVersion=end['version']))
selected={m['identity'](e) for e in path['edges']}
path_nodes={m['identity'](n) for e in edges if m['identity'](e) in selected for n in (e['startNode'],e['endNode'])}
watched=[{k:n[k] for k in ('index','version')} for n in nodes if m['identity'](n) in path_nodes and len(n['edges'])>2]
if not watched:raise RuntimeError('No interior junction')
before=[r.call('get_junction_snapshot',n) for n in watched]
if not a.run:print('Baseline verified; no mutation requested');raise SystemExit()
op=r.call('save_checkpoint',{'label':'interior-preview-latency'})
saved=r.poll('get_operation',lambda s:s['status'] in ('complete','failed','interrupted'),{'id':op['id']})
if saved['status']!='complete':raise RuntimeError('Checkpoint failed')
packages=list(Path(a.save_root).rglob(saved['saveName']+'.cok'))
if len(packages)!=1:raise RuntimeError('Checkpoint missing')
with zipfile.ZipFile(packages[0]) as z:
 if z.testzip() is not None:raise RuntimeError('Checkpoint damaged')
r.call('nt_activate');r.poll('nt_get_state',lambda s:s['active'] and s['smoothMode'])
r.control('nt_clear');r.control('nt_strength',value=1.0)
t=time.monotonic();r.control('nt_select',start=start,end=end)
results=[]
for i,value in enumerate([1.0,0.8,0.5,1.0,0.99,1.0]):
 if i:t=time.monotonic();r.control('nt_strength',value=value)
 state=r.poll('nt_get_state',lambda s:s['previewReady'],seconds=30)
 elapsed=time.monotonic()-t
 for node,original in zip(watched,before):
  preview=r.call('get_junction_preview',node)['connectedSnapshot']
  if not preview or not preview['complete'] or preview['errors']:raise RuntimeError('Incomplete preview')
  owners={o['index']:o.get('temp',{}).get('original',o)['index'] for o in preview['owners']}
  def normalized(endpoint):return (owners.get(endpoint[0],endpoint[0]),*endpoint[1:])
  actual={(k,normalized(s),normalized(e)) for k,s,e in m['lane_transitions'](preview)}
  if actual!=m['lane_transitions'](original):raise RuntimeError('Directed connections changed')
 after=r.state()
 if any(after[k]!=state[k] for k in ('session','revision','submission')) or not after['previewReady']:raise RuntimeError('Preview changed during capture')
 result={'strength':value,'mailboxElapsedSeconds':elapsed,'submission':state['submission'],'directedConnectionsPreserved':True}
 results.append(result);print(json.dumps(result),flush=True)
after_nodes,after_edges=r.network(f['region'])
if m['fingerprint'](after_nodes,after_edges)!=f['fingerprint']:raise RuntimeError('Permanent geometry changed during preview')
(r.output/'benchmark.json').write_text(json.dumps({'checkpoint':saved['saveName'],'results':results,'permanentGeometryUnchanged':True,'citySession':r.city_session},indent=2))
