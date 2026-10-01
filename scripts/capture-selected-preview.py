"""Read-only selected interior preview capture; never changes tool inputs."""
import argparse,runpy,json,time
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--output',required=True);a=p.parse_args()
m=runpy.run_path(str(Path(__file__).with_name('live-regression.py')));r=m['Runner']('../cities2-agent-bridge-ndc',a.output)
initial=r.state()
city=r.call('get_city_state')
if city['selectedSpeed']!=0:raise RuntimeError('Paused city required')
interior=[n for n in initial['splitChoices'] if not n['eligible']]
if not interior:raise RuntimeError('No interior junction candidate')
node={k:interior[0][k] for k in ('index','version')}
r.call('get_junction_snapshot',node)
summary=[]
for i in range(10):
 before=r.state()
 if any(before[k]!=initial[k] for k in ('session','start','end','strength')):raise RuntimeError('Selection changed')
 preview=r.call('get_junction_preview',node)
 after=r.state()
 item={'sample':i,'before':before,'after':after,'previewKeys':list(preview),'node':node}
 summary.append(item)
 print(json.dumps({'sample':i,'beforeSubmission':before['submission'],'afterSubmission':after['submission'],'ready':before['previewReady'],'previewKeys':list(preview)}),flush=True)
 time.sleep(1)
(r.output/'summary.json').write_text(json.dumps(summary,indent=2))
