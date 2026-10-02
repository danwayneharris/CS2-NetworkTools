"""Offline authored/native-surface comparison for compare-current-slope-preview.py captures."""
import argparse,json,math
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__);p.add_argument('capture');a=p.parse_args();root=Path(a.capture)
b=json.loads((root/'before.json').read_text());after=json.loads((root/'after-later.json').read_text())
def identity(x):return (x['index'],x['version'])
def owners(snapshots,preview=False):
    result={}
    for s in snapshots:
        if not s['complete']:raise ValueError('Incomplete snapshot')
        for o in s['owners']:
            key=identity(o['temp']['original']) if preview and o.get('temp',{}).get('original') else identity(o)
            if o.get('curve'):result[key]=o
    return result
def vectors(value,path=''):
    if isinstance(value,dict):
        if all(k in value for k in ('x','y','z')):yield path,[value[k] for k in ('x','y','z')]
        else:
            for k,v in value.items():yield from vectors(v,path+'/'+k)
    elif isinstance(value,list):
        for i,v in enumerate(value):yield from vectors(v,path+'/'+str(i))
def error(a,b):
    x,y=dict(vectors(a)),dict(vectors(b))
    if not x or x.keys()!=y.keys():raise ValueError('Missing or mismatched geometry coverage')
    if not all(math.isfinite(v) for d in (x,y) for p in d.values() for v in p):raise ValueError('Nonfinite geometry')
    return dict(maxDistance=max(math.dist(x[k],y[k]) for k in x),maxY=max(abs(x[k][1]-y[k][1]) for k in x),points=len(x))
o=owners(b['snapshots']);v=owners([s['connectedSnapshot'] for s in b['previews']],True);n=owners(after['snapshots']);selected={identity(e) for e in b['path']['edges']}
rows=[dict(edge=k,selected=k in selected,authoredPreviewVsApply=error(v[k]['curve'],n[k]['curve']),surfacePreviewVsApply=error(v[k]['edgeGeometry'],n[k]['edgeGeometry']),surfaceBeforeVsAfter=error(o[k]['edgeGeometry'],n[k]['edgeGeometry'])) for k in v]
x=b['terrain']['samples'];y=after['terrain']['samples']
if len(x)!=len(y) or not x:raise ValueError('Terrain sample coverage mismatch')
report=dict(mode=b['state']['mode'],submission=b['state']['submission'],rows=rows,terrainSampleCount=len(x),terrainMaxYChange=max(abs(a['position']['y']-z['position']['y']) for a,z in zip(x,y)),limits='Corresponding generated Bezier controls, not a mesh-distance or terrain-causality proof.')
immediate=owners(json.loads((root/'after-immediate.json').read_text())['snapshots'])
report['surfacesStableAfterApply']=all(immediate[k]['edgeGeometry']==n[k]['edgeGeometry'] for k in n)
(root/'comparison.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
