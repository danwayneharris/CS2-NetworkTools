"""Sequential toy regression orchestration; every case reloads its verified baseline.
Explicit --run required. Never retries an uncertain case or force-kills a game.
"""
import argparse,importlib.util,json,os,subprocess,sys,time
from pathlib import Path

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--bridge',default='../cities2-agent-bridge-ndc');p.add_argument('--save-root',required=True)
    p.add_argument('--output',required=True);p.add_argument('--run',action='store_true')
    p.add_argument('--case',action='append',required=True,help='fixture.json:case-name')
    a=p.parse_args()
    if not a.run:p.error('Explicit --run required')
    root=Path(a.output).resolve();root.mkdir(parents=True,exist_ok=False)
    bridge=Path(a.bridge).resolve();here=Path(__file__).resolve().parent
    spec=importlib.util.spec_from_file_location('bridge_client',bridge/'adapter/bridge_client.py')
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    client=module.Client(Path(os.environ['LOCALAPPDATA'])/'CitiesIIAgentBridge',root/'status-intents')
    reports=[]
    def ready(previous=None):
        deadline=time.monotonic()+240
        while time.monotonic()<deadline:
            try:
                s=client.status()
                if s.get('gameMode')=='Game' and not s.get('loading') and (previous is None or s['session']!=previous):return s
            except (OSError,ValueError,RuntimeError):pass
            time.sleep(1)
        raise RuntimeError('Bounded game loading wait expired; no force kill')
    def invoke(script,arguments,log):
        with log.open('w') as f:
            result=subprocess.run([sys.executable,str(here/script),*arguments],stdout=f,stderr=subprocess.STDOUT,timeout=360)
        return result.returncode
    for i,case_spec in enumerate(a.case):
        fixture_name,case=case_spec.split(':',1);fixture=here/'fixtures'/fixture_name
        descriptor=json.loads(fixture.read_text())
        if case not in [c['name'] for c in descriptor['cases']]:raise ValueError('Unknown case '+case)
        state=ready();prefix=root/f'{i+1:02d}-{case}'
        print('Reloading baseline for '+case,flush=True)
        code=invoke('reload-toy-baseline.py',['--fixture',str(fixture),'--save-root',a.save_root,'--expected-city-session',state['citySession'],'--bridge',str(bridge),'--output',str(prefix)+'-reload'],Path(str(prefix)+'-reload.log'))
        if code:raise RuntimeError('Lifecycle failed; inspect '+str(prefix)+'-reload.log')
        ready(state['session'])
        print('Testing '+case,flush=True)
        code=invoke('live-regression.py',['--fixture',str(fixture),'--case',case,'--run','--save-root',a.save_root,'--bridge',str(bridge),'--output',str(prefix)],Path(str(prefix)+'.log'))
        report=prefix/'report.json'
        reports.append({'fixture':fixture_name,'case':case,'exitCode':code,'report':json.loads(report.read_text()) if report.exists() else None})
        (root/'summary.json').write_text(json.dumps(reports,indent=2))
        print(case+': '+('PASS' if code==0 else 'FAIL'),flush=True)
        # A completed assertion report is diagnosable. Unknown/incomplete execution
        # stops the suite instead of blindly trying more mutations.
        if code and not report.exists():raise RuntimeError('Incomplete case; inspect original capture before proceeding')
if __name__=='__main__':main()
