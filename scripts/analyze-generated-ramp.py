"""Offline lane-grade diagnostics and one source-derived junction-height prediction.
The weighted prediction covers SmoothElevation incident edges only, and rejects
special flattened junction flags. It does not replay the entire GeometrySystem.
"""
import argparse,json,math,runpy
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('capture');p.add_argument('--edge-ordinal',type=int,default=4);p.add_argument('--output',required=True);a=p.parse_args()
d=json.loads(Path(a.capture).read_text());edge=d['edges'][a.edge_ordinal];ident=lambda e:(e['index'],e['version'])
s=next(s for s in d['snapshots'] if ident(s['junction'])==ident(edge['startNode']));node=next(o for o in s['owners'] if ident(o)==ident(s['junction']))
weighted=0.;weights=0.;inputs=[]
for o in s['owners']:
 if not o.get('startNode'):continue
 flags=o['prefab']['netGeometryData']['flags']
 if 'SmoothElevation' not in flags:raise ValueError('This replay requires SmoothElevation incident edges')
 forward=ident(o['startNode'])==ident(s['junction']);endpoint='startNode' if forward else 'endNode'
 if any(flag in str(o['composition'][endpoint]) for flag in ('Roundabout','LevelCrossing','FixedNodeSize')):raise ValueError('Special node-height branch is not modeled')
 c=o['curve'] if forward else list(reversed(o['curve']));length=math.hypot(c[1]['x']-c[0]['x'],c[1]['z']-c[0]['z'])
 if length<.1:continue
 weighted+=c[1]['y']/length;weights+=1/length;inputs.append({'edge':ident(o),'handleY':c[1]['y'],'handleHorizontalLength':length})
if len(inputs)<2:raise ValueError('Weighted height branch requires two eligible edges')
grade=runpy.run_path(str(Path(__file__).with_name('summarize-profile-experiment.py')))['grade'];lanes=[]
for lane in s['lanes']:
 if 'car' not in lane or not (ident(lane['owner'])==ident(edge) or edge['index'] in (lane['start']['ownerIndex'],lane['end']['ownerIndex'])):continue
 values=[grade(lane['curve'],i/1000) for i in range(1001)]
 if any(v is None for v in values):raise ValueError('Degenerate lane derivative')
 peak=max(range(len(values)),key=lambda i:abs(values[i]))
 lanes.append({'entity':ident(lane),'scope':'edge' if ident(lane['owner'])==ident(edge) else 'junction','startGrade':values[0],'endGrade':values[-1],'sampledPeakGrade':values[peak],'peakT':peak/1000,'curve':lane['curve']})
report={'junction':ident(s['junction']),'authoredNodeY':node['position']['y'],'predictedNodeGeometryPosition':.5*(node['position']['y']+weighted/weights),'inputs':inputs,'lanes':lanes,'source':'Game.Net/GeometrySystem.cs:119-160; Game.Net/LaneSystem.cs:2657-2700; game 1.6.2f1','limits':'Double precision replay of one native stage, not full surface prediction. Sampled lane extrema are not certified maxima or vehicle traversal evidence.'}
Path(a.output).write_text(json.dumps(report,indent=2));print(json.dumps({'predictedNodeGeometryPosition':report['predictedNodeGeometryPosition'],'edgeLanePeakGrades':[x['sampledPeakGrade'] for x in lanes if x['scope']=='edge']}))
