"""Plot captured Curve/Slope outcomes; no game access or mutation."""
import argparse,json,math,runpy
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('capture');a=p.parse_args();root=Path(a.capture)
g=runpy.run_path(str(Path(__file__).with_name('terrain-regression.py')))
f=json.loads((Path(__file__).parent/'fixtures/toy-terrain-v11.json').read_text())
import matplotlib.pyplot as plt
fig,axs=plt.subplots(2,1,figsize=(11,7),sharex=True);summary=[]
for directory,label in [('1','Curve-smoothed save'),('2','Slope-smoothed save')]:
 data=json.loads((root/directory/'profiles.json').read_text())
 row={'save':data['save'],'cases':[]}
 for case in data['cases']:
  origin=next(c for c in f['cases'] if c['name']==case['case'])['start'];current=min(data['nodes'],key=lambda n:math.hypot(n['position']['x']-origin[0],n['position']['z']-origin[2]))['index']
  ds=0;previous=None;jumps=[];segments=[]
  for e in case['edges']:
   forward=e['startNode']['index']==current
   if not forward and e['endNode']['index']!=current:raise ValueError('Unordered path')
   current=e['endNode' if forward else 'startNode']['index'];c=e['curve'] if forward else list(reversed(e['curve']))
   pts=[g['evaluate'](c,j/100) for j in range(101)];xs=[ds];grades=[]
   for b,d in zip(pts,pts[1:]):xs.append(xs[-1]+math.hypot(d['x']-b['x'],d['z']-b['z']))
   for j in range(101):
    t=j/100;v={k:3*((1-t)**2*(c[1][k]-c[0][k])+2*(1-t)*t*(c[2][k]-c[1][k])+t*t*(c[3][k]-c[2][k])) for k in ('x','y','z')}
    grades.append(100*v['y']/max(1e-9,math.hypot(v['x'],v['z'])))
   if previous is not None:jumps.append({'atHorizontalDistance':ds,'gradeJumpPercentagePoints':grades[0]-previous,'edge':e['index']})
   previous=grades[-1];ds=xs[-1]
   segments.append({'edge':e['index'],'startGrade':grades[0],'endGrade':grades[-1],'minGrade':min(grades),'maxGrade':max(grades)})
   if case['case']=='highway-ramp-out':
    color='tab:blue' if directory=='1' else 'tab:orange'
    axs[0].plot(xs,[pt['y'] for pt in pts],color=color,label=label if len(segments)==1 else None)
    axs[1].plot(xs,grades,color=color)
  row['cases'].append({'case':case['case'],'maxAbsGrade':max(m['sampledMaxAbsGradePercent'] for m in case['metrics']),'maxAdjacentEdgeGradeJump':max((abs(j['gradeJumpPercentagePoints']) for j in jumps),default=0),'jumps':jumps,'segments':segments})
 summary.append(row)
axs[0].set_ylabel('World elevation (m)');axs[1].set_ylabel('Signed grade (%)');axs[1].set_xlabel('Cumulative horizontal edge length (m; junction gaps omitted)');axs[0].legend()
for ax in axs:ax.grid(alpha=.3)
axs[0].set_title('Highway off-ramp path: observed saved outcomes, mixed operation histories')
fig.tight_layout();fig.savefig(root/'offramp-profiles.png',dpi=130);(root/'profile-analysis.json').write_text(json.dumps(summary,indent=2))
print(json.dumps([{'save':r['save'],'cases':[{k:c[k] for k in ('case','maxAbsGrade','maxAdjacentEdgeGradeJump')} for c in r['cases']]} for r in summary],indent=2))
