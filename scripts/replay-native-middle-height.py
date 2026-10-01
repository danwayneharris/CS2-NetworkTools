"""Replay GeometrySystem.FinishEdgeGeometryJob.LimitMiddleHeights bounds.
Uses captured final XZ/outer heights (which that stage does not change). This can
identify an active bound, but does not claim the uncaptured pre-limiter middle Y.
"""
import argparse,json,math
from pathlib import Path

def horizontal_length(c,n=1000):
 def speed(t):return math.hypot(*[3*((1-t)**2*(c[1][k]-c[0][k])+2*t*(1-t)*(c[2][k]-c[1][k])+t*t*(c[3][k]-c[2][k])) for k in ('x','z')])
 return (speed(0)+speed(1)+sum((4 if i%2 else 2)*speed(i/n) for i in range(1,n)))/(3*n)

def bounds(start_y,end_y,start_length,end_length,max_slope,width):
 low=max(start_y-max_slope*start_length,end_y-max_slope*end_length)
 high=min(start_y+max_slope*start_length,end_y+max_slope*end_length)
 middle=(low+high)/2
 if high<low:return {'raw':[low,high],'final':[middle,middle],'incompatible':True}
 weight=1/(.5*(start_length+end_length)/max(.01,width)+1)
 return {'raw':[low,high],'final':[low+(middle-low)*weight,high+(middle-high)*weight],'incompatible':False}

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('capture');p.add_argument('--max-slope',type=float,required=True);p.add_argument('--edge-ordinal',type=int,default=4);p.add_argument('--output',required=True);a=p.parse_args()
 if not math.isfinite(a.max_slope) or a.max_slope<=0:raise ValueError('Positive finite prefab slope required')
 d=json.loads(Path(a.capture).read_text());e=d['edges'][a.edge_ordinal];o=next(o for s in d['snapshots'] for o in s['owners'] if (o['index'],o['version'])==(e['index'],e['version']));width=o['composition']['edge']['width'];rows=[]
 for side in ('left','right'):
  start=o['edgeGeometry']['start'][side];end=o['edgeGeometry']['end'][side];ls,le=horizontal_length(start),horizontal_length(end);result=bounds(start[0]['y'],end[-1]['y'],ls,le,a.max_slope,width);actual=start[-1]['y']
  rows.append({'side':side,'horizontalLengths':[ls,le],'outerHeights':[start[0]['y'],end[-1]['y']],'bounds':result,'actualMiddleY':actual,'distanceToNearestBound':min(abs(actual-v) for v in result['final']),'outerAverageGrade':(end[-1]['y']-start[0]['y'])/(ls+le)})
 report={'prefabMaxSlope':a.max_slope,'width':width,'rows':rows,'source':'Game.Net/GeometrySystem.cs:1809-1827, Game 1.6.2f1','limits':'Bounds replay, not full pipeline. Pre-limiter middle heights were not captured. Slope bounds constrain half-curve endpoint differences, not every derivative.'}
 Path(a.output).write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
if __name__=='__main__':main()
