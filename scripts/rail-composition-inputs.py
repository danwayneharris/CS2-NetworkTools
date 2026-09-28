"""Reconstruct the fresh-composition input path for ordinary track junction arms.
Research helper: not target sorting, existing-lane matching, or a connectivity verdict.
"""
import math

def flags(value): return set(value.split(', '))
def vec(p): return tuple(float(p[k]) for k in ('x','y','z'))
def connect_positions(snapshot):
    if snapshot.get('schemaVersion',0)<2 or not snapshot.get('complete'):
        raise ValueError('A complete schema-2 snapshot is required')
    junction=snapshot['junction']; result=[]
    for edge in snapshot['owners']:
        if 'startNode' not in edge: continue
        def matches(n): return all(n[k]==junction[k] for k in ('index','version'))
        is_end=matches(edge['endNode'])
        if is_end==matches(edge['startNode']): raise ValueError('Expected exactly one incident endpoint')
        if 'prefab' not in edge or 'netGeometryData' not in edge['prefab']:
            raise ValueError('Missing geometry prefab')
        if edge['prefab']['netGeometryData']['mergeLayers']!=next(o for o in snapshot['owners'] if all(o[k]==junction[k] for k in ('index','version')))['prefab']['netGeometryData']['mergeLayers']:
            raise ValueError('Side-connection geometry is unsupported')
        composition=edge['composition']['edge']
        side=edge['edgeGeometry']['end' if is_end else 'start']
        left,right=side['left'],side['right']
        if is_end: left,right=list(reversed(right)),list(reversed(left))
        for lane in composition['lanes']:
            f=flags(lane['flags'])
            if 'Track' not in f: continue
            if f & {'Master','Slave','FindAnchor','Road'}: raise ValueError('Unsupported special/shared track lane')
            source=is_end==('Invert' not in f)
            if ('DisconnectedEnd' if source else 'DisconnectedStart') in f: continue
            x=lane['position']['x']*(1 if is_end else -1)
            weight=x/max(1,composition['width'])+.5
            controls=[tuple((1-weight)*r+weight*l for r,l in zip(vec(rp),vec(lp))) for rp,lp in zip(right,left)]
            a,b=controls[:2]
            tangent=tuple(a[i]-b[i] for i in range(3))
            length=math.hypot(tangent[0],tangent[2])
            if length<1e-9: raise ValueError('Degenerate composition tangent')
            tangent=(tangent[0]/length,max(-1,min(1,tangent[1]/length)),tangent[2]/length)
            p=(a[0],a[1]+lane['position']['y'],a[2])
            track=lane['prefab']['trackLaneData']
            if not math.isfinite(track['maxCurviness']) or track['maxCurviness']<0: raise ValueError('Invalid track threshold')
            result.append({'edge':{'index':edge['index'],'version':edge['version']},'laneIndex':lane['index'],
                'group':lane['group'],'source':source or 'Twoway' in f,'target':not source or 'Twoway' in f,
                'position':p,'inwardTangent':tangent,'maxCurviness':track['maxCurviness'],'trackTypes':track['trackTypes']})
    return result