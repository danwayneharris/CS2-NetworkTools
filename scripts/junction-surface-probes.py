"""Emit bounded read-only Unity eval expressions for junction surfaces.
Input: before.json from compare-current-slope-preview.py. No game calls here.
Run expressions through unity eval and retain its raw responses with this manifest.
"""
import argparse,json
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__);p.add_argument('capture');p.add_argument('output');a=p.parse_args()
b=json.loads((Path(a.capture)/'before.json').read_text())
def key(v):return str(v['index'])+':'+str(v['version'])
original={key(o):o for s in b['snapshots'] for o in s['owners'] if o.get('curve')}
temporary={}
for s in b['previews']:
 rows=s['connectedSnapshot']['owners'] if s.get('connectedSnapshot') else s['relatedPreviewEdges']['edges']
 for o in rows:
  if o.get('curve'):
   k=key(o['temp']['original'])
   if k in temporary and key(temporary[k])!=key(o):raise ValueError('Ambiguous preview edge')
   temporary[k]=o
if original.keys()!=temporary.keys():raise ValueError('Edge coverage mismatch')
fields=[]
for side in ('m_Left','m_Right'):
 for border in ('m_Left','m_Right'):
  for point in 'abcd':
   for axis in 'xyz':fields.append(f'{side}.{border}.{point}.{axis}')
for point in 'abcd':
 for axis in 'xyz':fields.append(f'm_Middle.{point}.{axis}')
for target in ('m_SyncVertexTargetsLeft','m_SyncVertexTargetsRight'):
 for axis in 'xyzw':fields.append(target+'.'+axis)
fields+=['m_MiddleRadius']
rows=[]
for k,o in original.items():
 for component in ('StartNodeGeometry','EndNodeGeometry'):
  def code(e):
   entity=f"entity({e['index']},{e['version']})"
   values='|'.join('{g.'+f+'}' for f in fields)
   return f'var g=em.GetComponentData<Game.Net.{component}>({entity}).m_Geometry; $"{values}"'
  rows.append(dict(original=k,component=component,fields=fields,previewCode=code(temporary[k]),permanentCode=code(o)))
Path(a.output).write_text(json.dumps(rows,indent=2));print('Wrote',len(rows),'read-only probes')
