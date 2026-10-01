"""Research horizontal-distance Constant Slope with endpoint offsets fitted first.
No game mutation. Reads the retained off-ramp trace; compares against captured
permanent cubics. This prototype is not a native reconstruction or terrain model.
"""
import argparse,json,math
from pathlib import Path

def point(p):
    return list(p.values()) if isinstance(p,dict) else p

def derivative(c,t):
    return [3*((1-t)**2*(c[1][k]-c[0][k])+2*(1-t)*t*(c[2][k]-c[1][k])+t*t*(c[3][k]-c[2][k])) for k in range(3)]

def length(c):
    # Composite Simpson integration of horizontal speed, independent of old Y.
    n=512
    def speed(t):
        d=derivative(c,t);return math.hypot(d[0],d[2])
    return (speed(0)+speed(1)+sum((4 if j%2 else 2)*speed(j/n) for j in range(1,n)))/(3*n)

def fit(curves,nodes):
    lengths=[length(c) for c in curves]
    offsets=[(c[0][1]-nodes[i][1],c[3][1]-nodes[i+1][1]) for i,c in enumerate(curves)]
    grade=(nodes[-1][1]-nodes[0][1]+sum(b-a for a,b in offsets))/sum(lengths)
    heights=[nodes[0][1]];result=[]
    for c,l,(a,d) in zip(curves,lengths,offsets):
        end=heights[-1]+grade*l+a-d
        out=[p[:] for p in c]
        out[0][1]=heights[-1]+a;out[3][1]=end+d
        out[1][1]=out[0][1]+grade*math.hypot(c[1][0]-c[0][0],c[1][2]-c[0][2])
        out[2][1]=out[3][1]-grade*math.hypot(c[3][0]-c[2][0],c[3][2]-c[2][2])
        heights.append(end);result.append(out)
    assert abs(heights[-1]-nodes[-1][1])<1e-8
    return result,heights,grade

def metrics(curves):
    result=[]
    for c in curves:
        grades=[]
        for j in range(1001):
            d=derivative(c,j/1000);grades.append(100*d[1]/math.hypot(d[0],d[2]))
        result.append(dict(startGrade=grades[0],endGrade=grades[-1],minGrade=min(grades),maxGrade=max(grades)))
    return result

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--trace',required=True);p.add_argument('--permanent',required=True);p.add_argument('--output',required=True);a=p.parse_args()
    out=Path(a.output);out.mkdir(parents=True,exist_ok=False)
    text=Path(a.trace).read_text();trace=json.loads(text[text.index('{'):]);nodes=[n['output'] for n in trace['nodes']]
    curves=[e['output'] if e['forward'] else list(reversed(e['output'])) for e in trace['edges']]
    predicted,heights,grade=fit(curves,nodes)
    permanent=json.loads(Path(a.permanent).read_text())['result']['edges'];byid={str(e['index'])+':'+str(e['version']):e for e in permanent}
    actual=[[[p[k] for k in ('x','y','z')] for p in byid[e['entity']]['curve']] for e in trace['edges']]
    for i,e in enumerate(trace['edges']):
        if not e['forward']:actual[i].reverse()
    report={'nominalGradePercent':100*grade,'nodeHeights':heights,'before':metrics(curves),'capturedSlope':metrics(actual),'offsetFit':metrics(predicted),'limits':'Research only: cubic vertical coordinates approximate horizontal arc-length height; native reconstruction/terrain/mesh untested.'}
    (out/'report.json').write_text(json.dumps(report,indent=2))
    (out/'candidate.json').write_text(json.dumps({'curves':predicted,'nodeHeights':heights},indent=2))
    try:import matplotlib.pyplot as plt
    except ImportError:print(json.dumps(report,indent=2));return
    fig,axes=plt.subplots(2,1,figsize=(10,7));colors=['#a64b38','#1673a6']
    for label,cs,color in zip(('Current captured Slope','Fit offsets before profile'),(actual,predicted),colors):
        station=0
        for c in cs:
            ts=[j/200 for j in range(201)];pts=[[sum(w*p[k] for w,p in zip(((1-t)**3,3*(1-t)**2*t,3*(1-t)*t*t,t**3),c)) for k in range(3)] for t in ts]
            ss=[station]
            for x,y in zip(pts,pts[1:]):ss.append(ss[-1]+math.hypot(y[0]-x[0],y[2]-x[2]))
            gs=[100*derivative(c,t)[1]/math.hypot(derivative(c,t)[0],derivative(c,t)[2]) for t in ts]
            axes[0].plot(ss,[x[1] for x in pts],color=color,label=label if station==0 else None)
            axes[1].plot(ss,gs,color=color)
            station=ss[-1]
    axes[0].set_ylabel('Centerline elevation (m)');axes[1].set_ylabel('Grade (%)');axes[1].set_xlabel('Horizontal traveled distance on selected edges (m)');axes[0].legend()
    for ax in axes:ax.grid(alpha=.25)
    fig.suptitle('Captured off-ramp: offset-aware profile research (not native-tested)');fig.tight_layout();fig.savefig(out/'comparison.png',dpi=140)
    print(json.dumps(report,indent=2))
if __name__=='__main__':main()