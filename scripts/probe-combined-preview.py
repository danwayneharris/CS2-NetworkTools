"""Explicit preview-only combined smoke test on a captured paused toy region. Never Apply."""
import argparse,importlib.util,json,time,os,zipfile
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--capture',required=True);p.add_argument('--start-index',required=True,type=int);p.add_argument('--end-index',required=True,type=int)
p.add_argument('--output',required=True);p.add_argument('--expected-city-session',required=True);p.add_argument('--bridge',default='../bridge-terrain-profile');p.add_argument('--run',action='store_true')
a=p.parse_args()
if not a.run: p.error('Preview control requires explicit --run')
s=importlib.util.spec_from_file_location('regression',Path(__file__).with_name('live-regression.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
baseline=json.loads(Path(a.capture).read_text());r=m.Runner(a.bridge,a.output);r.city_session=a.expected_city_session
city=r.call('get_city_state')
if city['population']!=0 or city['selectedSpeed']!=0 or not city['controlEnabled']: raise RuntimeError('Paused controlled toy required')
n,e=r.network(baseline['region'])
if m.fingerprint(n,e)!=baseline['fingerprint']: raise RuntimeError('Captured geometry changed')
source={x['index']:x for x in baseline['nodes']}
start=m.resolve(n,m.position(source[a.start_index]['position']));end=m.resolve(n,m.position(source[a.end_index]['position']))
# Preserve any toy work before manipulating the active tool; save operation is never retried.
op=r.call('save_checkpoint',{'label':'combined-preview-before'})
saved=r.poll('get_operation',lambda x:x['status'] in ('complete','failed','interrupted'),{'id':op['id']})
if saved['status']!='complete': raise RuntimeError('Checkpoint failed')
Path(a.output,'checkpoint.json').write_text(json.dumps(saved,indent=2))
packages=list((Path(os.environ['USERPROFILE'])/'AppData/LocalLow/Colossal Order/Cities Skylines II/Saves').rglob(saved['saveName']+'.cok'))
if len(packages)!=1: raise RuntimeError('Checkpoint package missing or ambiguous')
with zipfile.ZipFile(packages[0]) as z:
 if z.testzip() is not None or not any(x.endswith('.SaveGameMetadata.cid') for x in z.namelist()): raise RuntimeError('Checkpoint integrity failure')

cat=r.call('list_providers');provider=next(x for x in cat['providers'] if x['id']=='networktools')
if not any(x['name']=='combined' for x in provider['commands']): raise RuntimeError('Combined provider unavailable')
def call(command,args): return r.call('invoke_provider',{'provider':'networktools','revision':provider['revision'],'command':command,'args':args})
call('activate',{})
state=call('state',{})
for _ in range(20):
 if state['active'] and state['smoothMode']: break
 time.sleep(.2);state=call('state',{})
else: raise RuntimeError('Activation incomplete')
state=call('combined',{'session':state['session'],'revision':state['revision'],'enabled':True})
state=call('select',{'session':state['session'],'revision':state['revision'],'start':start,'end':end})
deadline=time.monotonic()+35
while time.monotonic()<deadline:
 state=call('state',{})
 if state['previewReady'] or state.get('surfaceFailed'): break
 time.sleep(.4)
Path(a.output,'result.json').write_text(json.dumps(state,indent=2))
print(json.dumps(state,indent=2))
