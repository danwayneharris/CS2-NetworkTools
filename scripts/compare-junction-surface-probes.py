"""Compare flattened read-only Unity junction probes; no game access."""
import argparse,json,math
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__);p.add_argument('manifest');p.add_argument('preview');p.add_argument('permanent');p.add_argument('output');a=p.parse_args()
rows=json.loads(Path(a.manifest).read_text())
def read(path):
 result=[]
 for r in json.loads(Path(path).read_text()):
  if r.get('isError'):raise ValueError('Failed Unity probe')
  v=json.loads(json.loads(r['content'][0]['text'])['result'])
  values=[float(x) for x in v.split('|')]
  if not all(math.isfinite(x) for x in values):raise ValueError('Nonfinite geometry')
  result.append(values)
 return result
before,after=read(a.preview),read(a.permanent)
if not rows or len(rows)!=len(before) or len(rows)!=len(after):raise ValueError('Probe coverage mismatch')
report=[]
for row,b,c in zip(rows,before,after):
 if len(row['fields'])!=len(b) or len(b)!=len(c):raise ValueError('Field coverage mismatch')
 d=[abs(x-y) for x,y in zip(b,c)];i=max(range(len(d)),key=d.__getitem__)
 report.append(dict(edge=row['original'],component=row['component'],maxScalarDifference=d[i],field=row['fields'][i],preview=b[i],permanent=c[i]))
Path(a.output).write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
