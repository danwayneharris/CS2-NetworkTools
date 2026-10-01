"""Checkpoint/reload explicitly named toy saves and capture vertical profiles.
Read-only measurements after loading; never applies transformations. Uses the
existing guarded lifecycle (unique checkpoint, graceful close, bounded startup).
"""
import argparse, hashlib, json, math, os, runpy, subprocess, sys, time
from pathlib import Path
ROOT=Path(__file__).resolve().parent
reg=runpy.run_path(str(ROOT/'live-regression.py'))
geo=runpy.run_path(str(ROOT/'terrain-regression.py'))

def main():
 p=argparse.ArgumentParser(description=__doc__)
 p.add_argument('--save-root',required=True);p.add_argument('--save',action='append',required=True)
 p.add_argument('--output',required=True);p.add_argument('--run',action='store_true');a=p.parse_args()
 if not a.run:p.error('Explicit --run required for checkpoint and restart')
 out=Path(a.output);out.mkdir(parents=True,exist_ok=False)
 base=json.loads((ROOT/'fixtures/toy-terrain-v11.json').read_text())
 bridge=Path('../cities2-agent-bridge-ndc').resolve()
 adapter=runpy.run_path(str(bridge/'adapter/bridge_client.py'))
 client=adapter['Client'](Path(os.environ['LOCALAPPDATA'])/'CitiesIIAgentBridge',out/'status-intents')
 for i,name in enumerate(a.save):
  if not name.startswith('bridge test - terrain and elevation '):raise ValueError('Only identified terrain toy saves allowed')
  files=list(Path(a.save_root).rglob(name))
  if len(files)!=1:raise ValueError('Missing or ambiguous save')
  package=files[0];f={**base,'baseline':name,'baselineSha256':hashlib.sha256(package.read_bytes()).hexdigest().upper()}
  dest=out/str(i+1);dest.mkdir();fixture=dest/'fixture.json';fixture.write_text(json.dumps(f,indent=2))
  state=client.status();print('Loading '+name,flush=True)
  result=subprocess.run([sys.executable,str(ROOT/'reload-toy-baseline.py'),'--fixture',str(fixture),'--save-root',a.save_root,'--expected-city-session',state['citySession'],'--output',str(dest/'reload')],capture_output=True,text=True,timeout=110)
  (dest/'reload.log').write_text(result.stdout+result.stderr)
  if result.returncode:raise RuntimeError('Lifecycle failed; no retry')
  deadline=time.monotonic()+240
  while time.monotonic()<deadline:
   try:
    s=client.status()
    if s.get('gameMode')=='Game' and not s.get('loading') and s.get('session')!=state['session']:break
   except (OSError,ValueError,RuntimeError):pass
   time.sleep(1)
  else:raise RuntimeError('Load timeout; no force kill')
  r=reg['Runner'](bridge,dest/'capture');city=r.call('get_city_state')
  if city['selectedSpeed']!=0 or city['population']!=0:raise ValueError('Paused empty toy required')
  nodes,edges=r.network(base['region']);byid={reg['identity'](e):e for e in edges};cases=[]
  for case in base['cases']:
   endpoints=[]
   for key in ('start','end'):
    pos=case[key];found=[n for n in nodes if math.hypot(n['position']['x']-pos[0],n['position']['z']-pos[2])<1]
    if len(found)!=1:raise ValueError('Ambiguous/missing horizontal endpoint '+case['name'])
    endpoints.append(found[0])
   start,end=endpoints
   path=r.call('trace_network',{'fromIndex':start['index'],'fromVersion':start['version'],'toIndex':end['index'],'toVersion':end['version']})
   chosen=[byid[reg['identity'](e)] for e in path['edges']]
   cases.append({'case':case['name'],'metrics':geo['metrics'](chosen),'edges':chosen,'path':path})
  report={'save':name,'sha256':f['baselineSha256'],'city':city,'citySession':r.city_session,'nodes':nodes,'edges':edges,'cases':cases}
  (dest/'profiles.json').write_text(json.dumps(report,indent=2))
  print(json.dumps({'save':name,'cases':[{ 'case':c['case'],'peakGrade':max(x['sampledMaxAbsGradePercent'] for x in c['metrics'])} for c in cases]}),flush=True)
if __name__=='__main__':main()
