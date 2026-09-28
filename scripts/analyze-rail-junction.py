"""Compare rail connector evidence. Edge-lane terminals approximate generator inputs.
No live game calls; do not treat the optional threshold as captured prefab data.
"""
import argparse, json, math
from pathlib import Path

def point(p): return tuple(p[k] for k in ('x','y','z'))
def sub(a,b): return tuple(x-y for x,y in zip(a,b))
def key(n): return (n['ownerIndex'],n['laneIndex'],n['curvePosition'],n['secondary'])
def unit_xz(v):
    d=math.hypot(v[0],v[2])
    if d < 1e-9: raise ValueError('Degenerate terminal tangent')
    return (v[0]/d,0,v[2]/d)
def metric(p,t,q,u):
    t,u=unit_xz(t),unit_xz(u)
    angle=math.acos(max(-1,min(1,sum(a*b for a,b in zip(t,u)))))
    distance=max(1,math.dist(p,q))
    return {'angleDegrees':math.degrees(angle),'distanceMetres':distance,
            'curviness':2*math.sin(angle/2)/distance}
def read(path):
    packet=json.loads(Path(path).read_text(encoding='utf-8-sig'))
    if not packet['ok'] or not packet['result']['complete']: raise ValueError('Incomplete snapshot')
    return packet['result']
def connections(s):
    return {(key(l['start']),key(l['end'])) for l in s['lanes']
            if 'track' in l and l.get('owner')==s['junction']}
def terminals(s):
    result={}
    for lane in s['lanes']:
        if 'track' not in lane or lane.get('owner')==s['junction']: continue
        a,b,c,d=map(point,lane['curve'])
        for end,p,t in [('start',a,sub(a,b)),('end',d,sub(d,c))]:
            k=key(lane[end])
            result.setdefault(k,[]).append((p,t))
    return result
def compare(before,after):
    if before['citySession']!=after['citySession'] or before['junction']!=after['junction']:
        raise ValueError('Snapshots must share city and junction identity')
    if sorted((e['index'],e['version']) for e in before['incidentEdges']) != sorted((e['index'],e['version']) for e in after['incidentEdges']):
        raise ValueError('Incident edge identities changed; labels cannot be matched safely')
    rows=[]
    for pair in sorted(connections(before)|connections(after)):
        row={'source':pair[0],'target':pair[1]}
        for name,s in [('before',before),('after',after)]:
            ends=terminals(s)
            if any(len(ends.get(k,[]))!=1 for k in pair): raise ValueError('Ambiguous or missing edge terminal')
            p,t=ends[pair[0]][0]; q,outward=ends[pair[1]][0]
            row[name]={'connectorPresent':pair in connections(s),**metric(p,t,q,tuple(-v for v in outward))}
        rows.append(row)
    return rows
if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('before');parser.add_argument('after');parser.add_argument('--output')
    args=parser.parse_args()
    rows=compare(read(args.before),read(args.after))
    output=json.dumps({'inputQuality':'edge-lane terminal proxy, not native ConnectPosition capture','connections':rows},indent=2)
    if args.output: Path(args.output).write_text(output+'\n',encoding='utf-8')
    print(output)