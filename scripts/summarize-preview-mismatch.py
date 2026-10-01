"""Extract compact native Slope mismatch evidence from a Player.log capture."""
import argparse,json
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('log');p.add_argument('output');a=p.parse_args()
rows=[]
for line in Path(a.log).read_text(encoding='utf-8',errors='replace').splitlines():
 marker='[NetworkTools.PreviewMismatch] '
 if marker not in line:continue
 for row in json.loads(line.split(marker,1)[1]):
  for key in ('expected','actual'):
   if isinstance(row[key],dict):row[key]=[row[key][point] for point in 'abcd']
  row['deltas']=[[b-a for a,b in zip(x,y)] for x,y in zip(row['expected'],row['actual'])]
  rows.append(row)
Path(a.output).write_text(json.dumps(rows,indent=2)+'\n')
print(f'{len(rows)} mismatched edges retained')
