"""Prepare a verified toy Slope preview without Apply; capture inputs/tokens for debugger gates."""
import argparse,json,runpy
from pathlib import Path
H=Path(__file__).resolve().parent
m=runpy.run_path(str(H/'live-regression.py'))
p=argparse.ArgumentParser();p.add_argument('--fixture',required=True);p.add_argument('--case',required=True);p.add_argument('--bridge',required=True);p.add_argument('--save-root',required=True);p.add_argument('--output',required=True);p.add_argument('--run',action='store_true');a=p.parse_args()
if not a.run:p.error('Explicit --run required (selects/configures preview)')
f=json.loads(Path(a.fixture).read_text());r=m['Runner'](a.bridge,a.output);city=r.call('get_city_state');assert city['selectedSpeed']==0 and city['population']==0 and city['controlEnabled']
runpy.run_path(str(H/'reload-toy-baseline.py'))['verified_package'](a.save_root,f['baseline'],f['baselineSha256']);n,e=r.network(f['region']);assert m['fingerprint'](n,e)==f['fingerprint']
catalog=r.call('list_providers');assert catalog['complete'];v=next(x['revision'] for x in catalog['providers'] if x['id']=='networktools')
def invoke(cmd,args=None):return r.call('invoke_provider',{'provider':'networktools','revision':v,'command':'slope_'+cmd,'args':args or {}})
def control(cmd,**kw):
 s=invoke('state');return invoke(cmd,dict(session=s['session'],revision=s['revision'],**kw))
invoke('activate');r.poll('invoke_provider',lambda s:s['active'],{'provider':'networktools','revision':v,'command':'slope_state','args':{}});control('clear');control('configure',mode='linear',easeIn=.1,easeOut=.1,archHeight=10,archPosition=.5,smoothStart=False,smoothEnd=False)
case=next(c for c in f['cases'] if c['name']==a.case);start,end=(m['resolve'](n,case[k]) for k in ('start','end'));control('select',start=start,end=end)
s=r.poll('invoke_provider',lambda s:s['previewReady'],{'provider':'networktools','revision':v,'command':'slope_state','args':{}},seconds=45)
(r.output/'prepared.json').write_text(json.dumps({'state':s,'providerRevision':v,'citySession':r.city_session,'nodes':n,'edges':e,'fixture':f},indent=2));print(json.dumps(s))
