"""Recreate a captured Slope preview after loading its baseline; no Apply.
Checks paused toy state and original incident curves before activating the tool.
"""
import argparse,json,math,runpy,time
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--capture',required=True);p.add_argument('--bridge',required=True);p.add_argument('--output',required=True)
a=p.parse_args();m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))
b=json.loads((Path(a.capture)/'before.json').read_text());r=m['Runner'](a.bridge,a.output)
c=r.call('get_city_state')
if c['selectedSpeed']!=0 or c['population']!=0 or not c['controlEnabled']:raise RuntimeError('Paused controlled toy required')
points=[]
for s in b['snapshots']:
 o=next(o for o in s['owners'] if m['identity'](o)==m['identity'](s['junction']))
 points.append([o['position'][k] for k in ('x','y','z')])
region={'x':sum(v[0] for v in points)/len(points),'z':sum(v[2] for v in points)/len(points),'radius':600}
nodes,edges=r.network(region)
ends=[m['resolve'](nodes,p) for p in points]
original={m['identity'](o):o for s in b['snapshots'] for o in s['owners'] if o.get('curve')}
for o in original.values():
 matches=[e for e in edges if len(e['curve'])==len(o['curve']) and all(math.dist([x[k] for k in ('x','y','z')],[y[k] for k in ('x','y','z')])<.01 for x,y in zip(e['curve'],o['curve']))]
 if len(matches)!=1:raise RuntimeError('Baseline incident geometry mismatch or ambiguous edge')
provider=next(x for x in r.call('list_providers')['providers'] if x['id']=='networktools')
def call(cmd,args=None):return r.call('invoke_provider',{'provider':'networktools','revision':provider['revision'],'command':cmd,'args':args or {}})
combined=b['state']['mode']=='CurveSmooth' and b['state']['combinedSlope']
if any(x['selected'] for x in b['state'].get('splitChoices',[])):raise RuntimeError('Split replay not supported')
if combined:
 call('activate');s=call('state')
 call('combined',dict(session=s['session'],revision=s['revision'],enabled=True,smoothStart=b['state']['slopeParameters']['smoothStart'],smoothEnd=b['state']['slopeParameters']['smoothEnd']))
 s=call('state');call('strength',dict(session=s['session'],revision=s['revision'],value=b['state']['strength']))
else:
 mode={'SlopeEaseInOut':'ease','SlopeLinear':'linear','SlopeArch':'arch'}.get(b['state']['mode'])
 if mode is None:raise RuntimeError('Unsupported captured mode')
 call('slope_activate');s=call('slope_state')
 call('slope_configure',dict(session=s['session'],revision=s['revision'],mode=mode,**b['state']['slopeParameters']))
s=call('state');call('select' if combined else 'slope_select',dict(session=s['session'],revision=s['revision'],start=ends[0],end=ends[1]))
for i in range(60):
 s=call('slope_state')
 if s['previewReady']:break
 time.sleep(.5)
else:raise RuntimeError('Preview did not settle')
print(json.dumps(s))
