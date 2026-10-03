"""Read-only bounded capture around the current toy camera; does not select or Apply."""
import argparse, importlib.util, json
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',required=True);p.add_argument('--bridge',default='../bridge-terrain-profile');a=p.parse_args()
s=importlib.util.spec_from_file_location('regression',Path(__file__).with_name('live-regression.py'));r=importlib.util.module_from_spec(s);s.loader.exec_module(r)
c=r.Runner(a.bridge,a.output)
city=c.call('get_city_state')
if city['population']!=0 or city['selectedSpeed']!=0: raise RuntimeError('Expected paused toy scenario')
camera=c.call('get_camera');print('camera',json.dumps(camera))
catalog=c.call('list_providers');provider=next(x for x in catalog['providers'] if x['id']=='networktools')
print('provider',provider['version'])
# Bounded camera region; report it explicitly for later fixture construction.
region={'x':camera['pivot']['x'],'z':camera['pivot']['z'],'radius':1000}
nodes,edges=c.network(region)
report={'city':city,'camera':camera,'region':region,'fingerprint':r.fingerprint(nodes,edges),'nodes':nodes,'edges':edges}
Path(a.output,'network.json').write_text(json.dumps(report,indent=2))
print('nodes',len(nodes),'edges',len(edges))
for n in nodes:
 print(json.dumps(n))
