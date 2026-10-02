"""Summarize a captured Slope comparison and generated edge boundaries offline.
Does not infer terrain causation, rendered-mesh quality or vehicle traversal.
"""
import argparse,json,math,runpy
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('terrain-regression.py')))
id=m['m']['identity']

def grade(c,t):
    d=[3*((1-t)**2*(c[1][k]-c[0][k])+2*(1-t)*t*(c[2][k]-c[1][k])+t*t*(c[3][k]-c[2][k])) for k in ('x','y','z')]
    distance=math.hypot(d[0],d[2])
    return 100*d[1]/distance if distance>1e-8 else None

def grade_rate(c,t):
    # d(dy/ds)/ds, expressed as percentage points per horizontal meter.
    d=[3*((1-t)**2*(c[1][k]-c[0][k])+2*(1-t)*t*(c[2][k]-c[1][k])+t*t*(c[3][k]-c[2][k])) for k in ('x','y','z')]
    dd=[6*((1-t)*(c[2][k]-2*c[1][k]+c[0][k])+t*(c[3][k]-2*c[2][k]+c[1][k])) for k in ('x','y','z')]
    h2=d[0]*d[0]+d[2]*d[2]
    return 100*(dd[1]/h2-d[1]*(d[0]*dd[0]+d[2]*dd[2])/(h2*h2)) if h2>1e-16 else None

def oriented_edges(edges,path):
    byid={id(e):e for e in edges};selected=[byid[id(e)] for e in path['edges']]
    if len(selected)<2:raise ValueError('At least two path edges required for orientation')
    first,second=selected[:2]
    common={id(first[k]) for k in ('startNode','endNode')}&{id(second[k]) for k in ('startNode','endNode')}
    if len(common)!=1:raise ValueError('Ambiguous path')
    cursor=id(first['endNode']) if id(first['startNode']) in common else id(first['startNode'])
    result=[]
    for edge in selected:
        forward=id(edge['startNode'])==cursor
        if not forward and id(edge['endNode'])!=cursor:raise ValueError('Unordered path')
        result.append({**edge,'pathForward':forward,'curve':edge['curve'] if forward else list(reversed(edge['curve']))})
        cursor=id(edge['endNode']) if forward else id(edge['startNode'])
    return result

def summarize(edges):
    rows=m['metrics'](edges)
    for i,(row,e) in enumerate(zip(rows,edges)):
        row['startGrade']=grade(e['curve'],0);row['endGrade']=grade(e['curve'],1)
        row['startGradeRate']=grade_rate(e['curve'],0);row['endGradeRate']=grade_rate(e['curve'],1)
        rates=[grade_rate(e['curve'],j/100) for j in range(101)]
        row['sampledMaxAbsGradeRate']=max(abs(x) for x in rates if x is not None)
        if i:
            row['joinGradeJump']=row['startGrade']-rows[i-1]['endGrade']
            row['joinGradeRateJump']=row['startGradeRate']-rows[i-1]['endGradeRate']
            row['joinHeightGap']=e['curve'][0]['y']-edges[i-1]['curve'][-1]['y']
    return rows

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('capture');p.add_argument('--verify-offset-fit',action='store_true');a=p.parse_args();root=Path(a.capture)
    data=json.loads((root/'comparison.json').read_text());before=oriented_edges(data['beforeEdges'],data['path']);after=oriented_edges(data['afterEdges'],data['path'])
    selected={id(e) for e in before};native={}
    for path in sorted(root.glob('*-get_junction_snapshot.json')):
        result=json.loads(path.read_text())['result']
        if not result['complete'] or result['errors']:raise ValueError('Incomplete snapshot')
        for owner in result['owners']:
            if id(owner) not in selected or 'edgeGeometry' not in owner:continue
            key=str(id(owner));native.setdefault(key,[]).append(owner)
    rows=[]
    for key,observations in native.items():
        row={'edge':key,'before':{},'after':{}}
        for phase,owner in zip(('before','after'),(observations[0],observations[-1])):
            for side in ('left','right'):
                c=owner['edgeGeometry']['start'][side];d=owner['edgeGeometry']['end'][side]
                g1,g2=grade(c,1),grade(d,0)
                row[phase][side]={'middleGradeJump':g2-g1 if g1 is not None and g2 is not None else None,'middleHeightGap':d[0]['y']-c[-1]['y'],'startGrade':grade(c,0),'endGrade':grade(d,1)}
            row[phase]['composition']=owner.get('composition')
        rows.append(row)
    report={'authoredBefore':summarize(before),'authoredAfter':summarize(after),'nativeBoundaryProfiles':rows,'limits':'Generated EdgeGeometry is not the final rendered mesh. Gaps across junction spans are not assumed defects. Sampled grades are diagnostics, not certified extrema.'}
    if a.verify_offset_fit:
        numerical=runpy.run_path(str(Path(__file__).with_name('explore-vertical-profile.py')))
        oldnodes={id(n):n['position'] for n in data['beforeNodes']}
        start=before[0]['startNode' if before[0]['pathForward'] else 'endNode']
        nodeids=[start]+[e['endNode' if e['pathForward'] else 'startNode'] for e in before]
        nodes=[[oldnodes[id(n)][k] for k in ('x','y','z')] for n in nodeids]
        curves=[[[pt[k] for k in ('x','y','z')] for pt in e['curve']] for e in before]
        predicted,heights,g=numerical['fit'](curves,nodes)
        errors=[math.dist(p,[q[k] for k in ('x','y','z')]) for expected,actual in zip(predicted,after) for p,q in zip(expected,actual['curve'])]
        report['independentOffsetFit']={'nominalGradePercent':100*g,'maxCurveError':max(errors),'geometryToleranceMeters':m['m']['GEOMETRY_TOLERANCE_METERS'],'passed':max(errors)<=m['m']['GEOMETRY_TOLERANCE_METERS']}
    (root/'profile-analysis.json').write_text(json.dumps(report,indent=2))
    if a.verify_offset_fit and not report['independentOffsetFit']['passed']:raise AssertionError('Native result differs from independent offline fit')
    print(json.dumps({'capture':str(root),'maxAbsGradeBefore':max(x['sampledMaxAbsGradePercent'] for x in report['authoredBefore']),'maxAbsGradeAfter':max(x['sampledMaxAbsGradePercent'] for x in report['authoredAfter']),'maxJoinGradeJumpAfter':max(abs(x.get('joinGradeJump',0)) for x in report['authoredAfter']),'offlinePrediction':report.get('independentOffsetFit')},indent=2))
if __name__=='__main__':main()