"""Read-only grade audit of an applied toy route; no selection or game mutations."""
import argparse,json,math,runpy
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))
p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('--case',default='highway-ramp-out');a=p.parse_args()
r=m['Runner']('../cities2-agent-bridge-ndc',a.output)
f=json.loads(Path('scripts/fixtures/toy-terrain-v11.json').read_text());c=next(c for c in f['cases'] if c['name']==a.case)
city=r.call('get_city_state');s=r.state()
cat=r.call('list_providers');rev=next(x['revision'] for x in cat['providers'] if x['id']=='networktools')
slope=r.call('invoke_provider',{'provider':'networktools','revision':rev,'command':'slope_state','args':{}})
ns,es=r.network(f['region']);start,end=(m['resolve'](ns,c[k]) for k in ('start','end'))
path=r.call('trace_network',dict(fromIndex=start['index'],fromVersion=start['version'],toIndex=end['index'],toVersion=end['version']))
by={m['identity'](e):e for e in es};current=start;rows=[];last=None
for eid in path['edges']:
 e=by[m['identity'](eid)];forward=e['startNode']==current
 q=[m['position'](v) for v in e['curve']];q=q if forward else q[::-1]
 current=e['endNode'] if forward else e['startNode']
 def point(t):return [sum(w*v[k] for w,v in zip(((1-t)**3,3*(1-t)**2*t,3*(1-t)*t*t,t**3),q)) for k in range(3)]
 def grade(t):
  d=[3*sum(w*(q[i+1][k]-q[i][k]) for i,w in enumerate(((1-t)**2,2*(1-t)*t,t*t))) for k in range(3)]
  return 100*d[1]/math.hypot(d[0],d[2])
 pts=[point(i/1000) for i in range(1001)];length=sum(math.hypot(v[0]-u[0],v[2]-u[2]) for u,v in zip(pts,pts[1:]));gs=[grade(i/1000) for i in range(1001)]
 row={'edge':eid,'horizontalLength':length,'startHeight':q[0][1],'endHeight':q[3][1],'startGradePercent':gs[0],'endGradePercent':gs[-1],'minGradePercent':min(gs),'maxGradePercent':max(gs),'averageGradePercent':100*(q[3][1]-q[0][1])/length}
 if last:row.update(joinHeightGap=q[0][1]-last[0][1],joinPlanarGap=math.hypot(q[0][0]-last[0][0],q[0][2]-last[0][2]),gradeJumpPercentagePoints=gs[0]-last[1])
 rows.append(row);last=(q[3],gs[-1])
report={'case':a.case,'slopeState':slope,'edges':rows,'limits':'Edge centerline analytic grades. Junction-generated lane/mesh interpolation not included; no rendered-frame inspection.'}
(r.output/'grade-report.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
