"""Compare fixed-station captures without assuming a final rendered surface."""
import argparse,json,math
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('before');p.add_argument('after');p.add_argument('--output',required=True);p.add_argument('--plot');a=p.parse_args();b=json.loads(Path(a.before).read_text());c=json.loads(Path(a.after).read_text())
def heights(x,n):return [s['position']['y'] for batch in x['terrain'][n] for s in batch['samples']]
bh,ch=heights(b,-1),heights(c,-1)
if [(x['x'],x['z']) for x in b['stations']]!=[(x['x'],x['z']) for x in c['stations']]:raise ValueError('Different sample coordinates')
def owner(x,e):
 found=[o for s in x['snapshots'] for o in s['owners'] if o['index']==e['index'] and o['version']==e['version']]
 if not found:raise ValueError('Missing generated edge')
 return found[0]
rows=[]
for i,(be,ce) in enumerate(zip(b['edges'],c['edges'])):
 bo,co=owner(b,be),owner(c,ce);indices=[j for j,s in enumerate(b['stations']) if s['edgeOrdinal']==i]
 def delta(xs,ys):return max(abs(x['y']-y['y']) for x,y in zip(xs,ys))
 boundary=max(delta(bo['edgeGeometry'][half][side],co['edgeGeometry'][half][side]) for half in ('start','end') for side in ('left','right'))
 row={'ordinal':i,'startXZ':[be['curve'][0][k] for k in ('x','z')],'authoredMaxYChange':delta(be['curve'],ce['curve']),'generatedBoundaryMaxYChange':boundary,'terrainMaxYChange':max(abs(ch[j]-bh[j]) for j in indices),'terrainCenterMaxYChange':max(abs(ch[j]-bh[j]) for j in indices if b['stations'][j]['offset']==0)};rows.append(row)
def bezier(c,t):return {k:sum(w*v[k] for w,v in zip(((1-t)**3,3*t*(1-t)**2,3*t*t*(1-t),t**3),c)) for k in ('x','y','z')}
def boundary_profile(dataset,ordinal):
 e=dataset['edges'][ordinal];o=owner(dataset,e);samples=[]
 for half in ('start','end'):
  sides=o['edgeGeometry'][half]
  center=[{k:(left[k]+right[k])/2 for k in ('x','y','z')} for left,right in zip(sides['left'],sides['right'])]
  samples.extend(bezier(center,i/1000) for i in range(1001))
 result=[]
 for station in dataset['stations']:
  if station['edgeOrdinal']!=ordinal or station['offset']!=0:continue
  q=min(samples,key=lambda q:(q['x']-station['x'])**2+(q['z']-station['z'])**2)
  distance=math.hypot(q['x']-station['x'],q['z']-station['z'])
  result.append({'t':station['t'],'horizontalResidual':distance,'boundaryMidpointY':q['y'] if distance<=.25 else None,'authoredY':station['authoredY']})
 return result
report={'beforeFingerprint':b['fingerprint'],'afterFingerprint':c['fingerprint'],'edges':rows,'terrainRepeatedReadMaxDifference':{name:max(abs(v-w) for v,w in zip(heights(x,0),heights(x,-1))) for name,x in [('before',b),('after',c)]},'limits':'CPU terrain is adjusted and asynchronous; EdgeGeometry is not the final mesh. This comparison includes changed shared-node geometry.'}
report['firstEdgeBoundaryProfiles']={label:boundary_profile(dataset,4) for label,dataset in [('before',b),('after',c)]}
Path(a.output).write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))

if a.plot:
 import matplotlib.pyplot as plt
 fig,axes=plt.subplots(2,1,figsize=(10,7),constrained_layout=True)
 for ax,ordinal,title in zip(axes,(4,1),('First ramp segment: shared junction moves','Next ramp segment: authored and boundary curves unchanged')):
  for dataset,color,label in ((b,'tab:blue','Before highway slope'),(c,'tab:orange','After highway slope')):
   hs=heights(dataset,-1);idx=[j for j,x in enumerate(dataset['stations']) if x['edgeOrdinal']==ordinal and x['offset']==0]
   points=[dataset['stations'][j] for j in idx];distance=[0.]
   for u,v in zip(points,points[1:]):distance.append(distance[-1]+math.hypot(u['x']-v['x'],u['z']-v['z']))
   ax.plot(distance,[x['authoredY'] for x in points],color=color,label=label+' authored')
   ax.plot(distance,[x['boundaryMidpointY'] for x in boundary_profile(dataset,ordinal)],color=color,ls='-.',lw=2,label=label+' boundary midpoint')
   ax.plot(distance,[hs[j] for j in idx],color=color,ls=':',label=label+' terrain at center')
   side=[j for j,x in enumerate(dataset['stations']) if x['edgeOrdinal']==ordinal and x['offset']==12]
   ax.plot(distance,[hs[j] for j in side],color=color,ls='--',label=label+' terrain 12 m beside')
  ax.set(title=title,xlabel='Horizontal distance along segment (m)',ylabel='Height (m)');ax.grid(alpha=.2);ax.legend(fontsize=8,ncol=2)
 fig.suptitle('Fixed-XZ observations: network geometry and adjusted CPU terrain\nTerrain samples are not pristine ground or final road mesh')
 fig.savefig(a.plot,dpi=140);plt.close(fig)
