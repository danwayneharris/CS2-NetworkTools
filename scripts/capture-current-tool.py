"""Read-only capture of the current NetworkTools selection and its native previews."""
import argparse,json,runpy
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))
p=argparse.ArgumentParser();p.add_argument('--output',required=True);a=p.parse_args()
r=m['Runner']('../cities2-agent-bridge-ndc',a.output)
print(json.dumps(r.call('get_city_state'),indent=2))
s=r.state();print(json.dumps(s,indent=2))
for choice in s.get('splitChoices',[]):
 if not choice['eligible']:
  node={k:choice[k] for k in ('index','version')}
  r.call('get_junction_snapshot',node)
  preview=r.call('get_junction_preview',node)
  print(json.dumps({k:v for k,v in preview.items() if k!='connectedSnapshot'},indent=2)[:5000])
