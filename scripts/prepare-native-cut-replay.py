"""Generate pure native-math debugger expressions from a captured ramp.
No EntityManager writes, live network edits or dependency on live entity identities.
Replays CutCurve, shared-height substitution, middle adjustment and offset curves;
then locates captured boundary endpoints in XZ, before flatten/retention stages.
"""
import argparse,json,math
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('capture');p.add_argument('--output',required=True);p.add_argument('--edge-index',type=int);p.add_argument('--reverse',action='store_true');a=p.parse_args();x=json.loads(Path(a.capture).read_text());e=x['edges'][4]
if a.edge_index is not None:
 import copy
 root=Path(a.capture).parent
 edges=json.loads(next(root.glob('*-get_network_edges.json')).read_text())['result']['edges']
 nodes=json.loads(next(root.glob('*-get_network.json')).read_text())['result']['nodes']
 e=copy.deepcopy(next(v for v in edges if v['index']==a.edge_index))
 if a.reverse:e['curve'].reverse();e['startNode'],e['endNode']=e['endNode'],e['startNode']
 for endpoint in ('startNode','endNode'):
  if any(v['junction']==e[endpoint] for v in x['snapshots']):continue
  node=next(n for n in nodes if n['index']==e[endpoint]['index']);incident=[v for v in edges if node['index'] in (v['startNode']['index'],v['endNode']['index'])]
  if any('Highway' not in v['prefab'] for v in incident):raise ValueError('Supplemental replay restricted to verified highway prefab family')
  owners=[{'index':node['index'],'position':node['position']}]
  for v in incident:owners.append({**v,'prefab':{'netGeometryData':{'flags':'SmoothElevation'}}})
  x['snapshots'].append({'junction':e[endpoint],'owners':owners})
def vec(p):return 'new Unity.Mathematics.float3('+','.join(str(p[k])+'f' for k in ('x','y','z'))+')'
heights=[];nodes=[]
for key in ('startNode','endNode'):
 s=next(s for s in x['snapshots'] if s['junction']==e[key]);o=next(o for o in s['owners'] if o['index']==s['junction']['index']);num=den=0;count=0
 for q in s['owners']:
  if not q.get('curve'):continue
  if 'SmoothElevation' not in q['prefab']['netGeometryData']['flags']:raise ValueError('Unsupported height branch')
  c=q['curve'] if q['startNode']['index']==o['index'] else list(reversed(q['curve']));length=math.hypot(c[1]['x']-c[0]['x'],c[1]['z']-c[0]['z'])
  if length>=.1:num+=c[1]['y']/length;den+=1/length;count+=1
 heights.append(.5*(o['position']['y']+num/den) if count>=2 else o['position']['y']);nodes.append(o['position'])
o=next(o for s in x['snapshots'] for o in s['owners'] if (o['index'],o['version'])==(e['index'],e['version']))
if a.reverse:
 import copy
 o=copy.deepcopy(o);o['edgeGeometry']['start']={'left':list(reversed(o['edgeGeometry']['end']['right'])),'right':list(reversed(o['edgeGeometry']['end']['left']))}
code='var c=new Colossal.Mathematics.Bezier4x3 { '+','.join(k+'='+vec(p) for k,p in zip('abcd',e['curve']))+' };'
code+='Colossal.Mathematics.MathUtils.Distance(c.xz,'+vec(nodes[0])+'.xz,out var u);Colossal.Mathematics.MathUtils.Distance(c.xz,'+vec(nodes[1])+'.xz,out var v);u=u<0.001f?0f:u;v=v>0.999f?1f:v;'
code+='c=Colossal.Mathematics.MathUtils.Cut(c,new Unity.Mathematics.float2(u,v));c.a.y='+str(nodes[0]['y'])+'f;c.d.y='+str(nodes[1]['y'])+'f;'
code+='var t=Game.Net.NetUtils.FindMiddleTangentPos(c.xz,new Unity.Mathematics.float2(0f,1f));Colossal.Mathematics.MathUtils.Divide(c,out var s,out var z,t);s.a.y='+str(heights[0])+'f;z.d.y='+str(heights[1])+'f;'
code+='var l=Unity.Mathematics.math.distance(s.c.xz,s.d.xz);var r=Unity.Mathematics.math.distance(z.b.xz,z.a.xz);var h=Unity.Mathematics.math.lerp(s.c.y,z.b.y,l/Unity.Mathematics.math.max(0.1f,l+r))-s.d.y;s.c.y-=h*0.4f;s.d.y+=h*0.6f;z.a.y+=h*0.6f;z.b.y-=h*0.4f;'
result={}
width=o['composition']['edge']['width']
for side,offset in [('left',width/2),('right',-width/2)]:
 target=o['edgeGeometry']['start'][side][0]
 tail='var a=Game.Net.NetUtils.OffsetCurveLeftSmooth(s,new Unity.Mathematics.float2('+str(offset)+'f));var b=Game.Net.NetUtils.OffsetCurveLeftSmooth(z,new Unity.Mathematics.float2('+str(offset)+'f));var p='+vec(target)+';'
 tail+='var da=Colossal.Mathematics.MathUtils.Distance(a.xz,p.xz,out var ta);var db=Colossal.Mathematics.MathUtils.Distance(b.xz,p.xz,out var tb);var predicted=da<db?Colossal.Mathematics.MathUtils.Position(a,ta):Colossal.Mathematics.MathUtils.Position(b,tb);'
 tail+='$"cut={u},{v}; split={t}; distance={Unity.Mathematics.math.min(da,db)}; predictedY={predicted.y}; capturedY={p.y}; residual={p.y-predicted.y}"'
 result[side]=code+tail
result={k:v.replace('s.c.y-=h*0.4f','s.c.y=s.c.y-h*0.4f').replace('s.d.y+=h*0.6f','s.d.y=s.d.y+h*0.6f').replace('z.a.y+=h*0.6f','z.a.y=z.a.y+h*0.6f').replace('z.b.y-=h*0.4f','z.b.y=z.b.y-h*0.4f') for k,v in result.items()}
Path(a.output).write_text(json.dumps(result,indent=2))
