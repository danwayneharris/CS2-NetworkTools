"""Run existing strict toy regression and add diagnostic geometry/terrain metrics.
Metrics do not relax assertions or claim terrain/grade suitability. Never changes
terrain or simulation speed; all mutations remain in the guarded existing runner.
"""
import argparse,json,math,runpy
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))

def evaluate(curve,t):
    weights=((1-t)**3,3*(1-t)**2*t,3*(1-t)*t*t,t**3)
    return {k:sum(w*p[k] for w,p in zip(weights,curve)) for k in ('x','y','z')}

def metrics(edges):
    rows=[]
    for e in edges:
        c=e['curve'];points=[evaluate(c,j/100) for j in range(101)];grades=[];degenerate=0
        for j in range(101):
            t=j/100
            v={k:3*((1-t)**2*(c[1][k]-c[0][k])+2*(1-t)*t*(c[2][k]-c[1][k])+t*t*(c[3][k]-c[2][k])) for k in ('x','y','z')}
            horizontal=math.hypot(v['x'],v['z'])
            if horizontal<=1e-6:degenerate+=1
            else:grades.append(100*v['y']/horizontal)
        rows.append({'edge':m['identity'](e),'prefab':e['prefab'],
          'sampledHorizontalLength':sum(math.hypot(b['x']-a['x'],b['z']-a['z']) for a,b in zip(points,points[1:])),
          'sampledMaxAbsGradePercent':max(map(abs,grades)) if grades else None,
          'sampledGradeRange':[min(grades),max(grades)] if grades else None,
          'degenerateHorizontalTangents':degenerate,
          'curveElevationRange':[min(p['y'] for p in points),max(p['y'] for p in points)]})
    return rows

def observe(r,region,selected):
    nodes,edges=r.network(region);chosen=[e for e in edges if m['identity'](e) in selected]
    points=[evaluate(e['curve'],j/8) for e in chosen for j in range(9)]
    if len(points)>1024:raise RuntimeError('Terrain sample limit exceeded')
    ground=r.call('sample_terrain',{'points':[{'x':p['x'],'z':p['z']} for p in points]})
    samples=ground['samples']
    if len(samples)!=len(points):raise RuntimeError('Terrain sample count mismatch')
    offsets=[p['y']-s['position']['y'] for p,s in zip(points,samples)]
    return {'metrics':metrics(chosen),'sampledCenterlineMinusTerrainRange':[min(offsets),max(offsets)],
      'networkFingerprint':m['fingerprint'](nodes,edges),
      'limits':'Sampled centerline geometry only; terrain may be adapted by native rebuilding. Not clearance, structure classification, visual quality or vehicle traversal certification.'}

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--bridge',default='../cities2-agent-bridge-ndc');p.add_argument('--output',required=True)
    p.add_argument('--fixture',required=True);p.add_argument('--case',required=True);p.add_argument('--save-root',required=True);p.add_argument('--run',action='store_true')
    a=p.parse_args()
    if not a.run:p.error('Explicit --run required')
    f=json.loads(Path(a.fixture).read_text());case=next(c for c in f['cases'] if c['name']==a.case)
    r=m['Runner'](a.bridge,a.output);city=r.call('get_city_state')
    if city['selectedSpeed']!=0 or city['population']!=0:raise RuntimeError('Paused toy required')
    nodes,edges=r.network(f['region'])
    if m['fingerprint'](nodes,edges)!=f['fingerprint']:raise RuntimeError('Baseline mismatch')
    start,end=(m['resolve'](nodes,case[k]) for k in ('start','end'))
    path=r.call('trace_network',dict(fromIndex=start['index'],fromVersion=start['version'],toIndex=end['index'],toVersion=end['version']))
    selected={m['identity'](e) for e in path['edges']}
    diagnostic={'before':observe(r,f['region'],selected)}
    try:
        result=r.execute(f,case,a.save_root)
        print(json.dumps(result,indent=2))
    except Exception as error:
        diagnostic['regressionError']=str(error)
        raise
    finally:
        try:diagnostic['after']=observe(r,f['region'],selected)
        except Exception as error:diagnostic['observationError']=str(error)
        (r.output/'terrain-diagnostics.json').write_text(json.dumps(diagnostic,indent=2))

if __name__=='__main__':main()
