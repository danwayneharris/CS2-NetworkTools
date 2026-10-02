"""Replay the permanent, same-layer pairwise junction-height relaxation.
Restricted scalar formulation of installed GeometrySystem flattening behavior;
no Temp, mixed layers or fixed-node branch. Input fixture states those assumptions.
"""
import argparse,json,math,copy
from pathlib import Path

def replay(f):
 edges=copy.deepcopy(f['edges']);height=f['nodeHeight'];iterations=0
 for iterations in range(100):
  changes=[[0.,0.] for _ in edges];active=False
  for i,a in enumerate(edges):
   for j,b in enumerate(edges[:i]):
    pairs=[(a['left'],b['right']),(a['right'],b['left'])]
    ds=[math.hypot(p['x']-q['x'],p['z']-q['z']) for p,q in pairs];ys=[q['y']-p['y'] for p,q in pairs];limit=a['maxSlope']+b['maxSlope']
    if not any(y*y>d*d*limit*limit*1.0001 for y,d in zip(ys,ds)):continue
    excess=[max(0,abs(y)-d*limit) for y,d in zip(ys,ds)];sign=[y>=0 for y in ys]
    adjustment=sum(v if positive else -v for v,positive in zip(excess,sign))/2 if sign[0]!=sign[1] else max(excess)*(1 if sign[0] else -1)
    if adjustment>=0:room=[max(0,height-max(a['left']['y'],a['right']['y'])),min(0,height-min(b['left']['y'],b['right']['y']))]
    else:room=[min(0,height-min(a['left']['y'],a['right']['y'])),max(0,height-max(b['left']['y'],b['right']['y']))]
    scale=min(1,abs(adjustment)/max(.001,sum(abs(v) for v in room)))
    for k,v in zip((i,j),room):changes[k][0]=min(changes[k][0],v*scale);changes[k][1]=max(changes[k][1],v*scale)
    active=True
  if not active:break
  for e,change in zip(edges,changes):
   for side in ('left','right'):e[side]['y']+=sum(change)
 return {'iterations':iterations,'edges':[{'label':e['label'],'predicted':[e['left']['y'],e['right']['y']],'expected':e['expected'],'maxError':max(abs(e[side]['y']-target) for side,target in zip(('left','right'),e['expected']))} for e in edges]}
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('fixture');p.add_argument('--output',required=True);a=p.parse_args();f=json.loads(Path(a.fixture).read_text());result=replay(f);result['passed']=all(e['maxError']<=f['tolerance'] for e in result['edges']);Path(a.output).write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
 if not result['passed']:raise AssertionError('Native endpoint replay differs beyond accepted tolerance')
