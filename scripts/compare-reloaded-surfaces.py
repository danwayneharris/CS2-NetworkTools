"""Read-only reload comparison against captured permanent surfaces.
Resolve fresh nodes by position and edges by endpoints; report authored/surface differences.
No Apply, selection, checkpoint or simulation changes. Requires paused empty toy city.
"""
import argparse,json,math,runpy
from pathlib import Path

def vectors(value,path=''):
    if isinstance(value,dict):
        if all(k in value for k in ('x','y','z')): yield path,tuple(value[k] for k in ('x','y','z'))
        else:
            for k,v in value.items(): yield from vectors(v,path+'/'+k)
    elif isinstance(value,list):
        for i,v in enumerate(value): yield from vectors(v,path+'/'+str(i))

def difference(a,b):
    a,b=dict(vectors(a)),dict(vectors(b))
    if not a or a.keys()!=b.keys(): raise ValueError('Missing geometry coverage')
    if not all(math.isfinite(v) for x in (a,b) for xyz in x.values() for v in xyz): raise ValueError('Nonfinite geometry')
    return max(math.dist(a[k],b[k]) for k in a)

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--reference',required=True);p.add_argument('--bridge',required=True);p.add_argument('--output',required=True)
    args=p.parse_args();m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))
    old=json.loads(Path(args.reference).read_text())['snapshots']
    if not old or any(not s['complete'] or s['errors'] for s in old): raise ValueError('Incomplete reference')
    r=m['Runner'](args.bridge,args.output);city=r.call('get_city_state')
    if city['population']!=0 or city['selectedSpeed']!=0: raise ValueError('Paused toy required')
    identity=m['identity'];position=m['position']
    all_old={identity(o):o for s in old for o in s['owners']}
    centers=[all_old[identity(s['junction'])]['position'] for s in old]
    region=dict(x=sum(x['x'] for x in centers)/len(centers),z=sum(x['z'] for x in centers)/len(centers),radius=600)
    nodes,edges=r.network(region)
    remap={identity(s['junction']):m['resolve'](nodes,position(pos)) for s,pos in zip(old,centers)}
    snapshots=[r.call('get_junction_snapshot',remap[identity(s['junction'])]) for s in old]
    if any(not s['complete'] or s['errors'] for s in snapshots): raise ValueError('Incomplete live snapshot')
    new={identity(o):o for s in snapshots for o in s['owners'] if o.get('curve')}
    # Match each edge by directed endpoint positions, not by its generated surface or old entity ID.
    rows=[];used=set()
    for key,o in all_old.items():
        if not o.get('curve'):continue
        endpoints=[]
        for field in ('startNode','endNode'):
            oldnode=o[field]
            if oldnode is None: raise ValueError('Reference endpoint coverage missing')
            endpoints.append(m['resolve'](nodes,position(oldnode['position'])))
        matches=[e for e in edges if identity(e['startNode'])==identity(endpoints[0]) and identity(e['endNode'])==identity(endpoints[1])]
        if len(matches)!=1: raise ValueError('Ambiguous endpoint edge correspondence')
        nk=identity(matches[0])
        if nk in used or nk not in new:raise ValueError('Duplicate/missing mapped edge')
        used.add(nk);n=new[nk]
        rows.append(dict(original=key,reloaded=nk,authoredError=difference(o['curve'],n['curve']),surfaceError=difference(o['edgeGeometry'],n['edgeGeometry'])))
    if used != new.keys(): raise ValueError('Unexpected extra live edge coverage')
    owner_map={row['original'][0]:row['reloaded'][0] for row in rows}
    owner_map.update({k[0]:v['index'] for k,v in remap.items()})
    lane_rows=[]
    def mapped_lane(lane):
        owner,index,secondary=lane
        if owner not in owner_map:raise ValueError('Unmapped lane owner after reload')
        return owner_map[owner],index,secondary
    for before,after in zip(old,snapshots):
        expected={(kind,mapped_lane(source),mapped_lane(target)) for kind,source,target in m['lane_transitions'](before)}
        actual=m['lane_transitions'](after)
        lane_rows.append(dict(original=before['junction'],reloaded=after['junction'],expected=len(expected),observed=len(actual),removed=sorted(expected-actual),added=sorted(actual-expected)))
    lanes_preserved=all(not x['removed'] and not x['added'] for x in lane_rows)
    report=dict(city=city,rows=rows,laneRows=lane_rows,lanesPreserved=lanes_preserved,toleranceMeters=.05,withinTolerance=lanes_preserved and all(max(x['authoredError'],x['surfaceError'])<=.05 for x in rows),limitations='Directed lane pairs mapped through fresh owner identities; no vehicle traversal or mesh-distance assertion. Endpoint position changes fail discovery; cannot certify broader world.')
    (r.output/'snapshots.json').write_text(json.dumps(dict(snapshots=snapshots),indent=2))
    (r.output/'summary.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
    return 0 if report['withinTolerance'] else 1
if __name__=='__main__':raise SystemExit(main())
