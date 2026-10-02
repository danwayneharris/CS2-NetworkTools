"""Sequential toy regression orchestration; every case reloads its verified baseline.
Explicit --run required. Never retries an uncertain case or force-kills a game.
"""
import argparse,importlib.util,json,os,subprocess,sys,time
from pathlib import Path

def case_status(code, report):
    """Only an explicit completed assertion report permits the next baseline reload."""
    if not isinstance(report, dict) or type(report.get('passed')) is not bool:
        return 'execution-incomplete'
    if code == 0 and report['passed']:
        return 'passed'
    if not report['passed']:
        return 'assertion-failed'
    # A successful report followed by a failed process may indicate a late failure.
    return 'execution-incomplete'


def run_cases(cases, execute, summary_path):
    """Persist all planned outcomes, stop uncertainty, aggregate completed failures."""
    reports = [dict(fixture=f, case=c, status='not-run', exitCode=None, report=None)
               for f, c in cases]
    def save():
        summary_path.write_text(json.dumps(reports, indent=2), encoding='utf-8')
    save()
    if not reports:
        return 2
    for i, row in enumerate(reports):
        try:
            code, report = execute(i, row['fixture'], row['case'])
            row.update(exitCode=code, report=report, status=case_status(code, report))
        except Exception as error:
            row.update(status='execution-incomplete', error=str(error))
        save()
        print(row['case'] + ': ' + row['status'], flush=True)
        if row['status'] == 'execution-incomplete':
            return 2
    return 1 if any(r['status'] != 'passed' for r in reports) else 0


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--bridge',default='../cities2-agent-bridge-ndc');p.add_argument('--save-root',required=True)
    p.add_argument('--runner',choices=['live-regression.py','terrain-regression.py'],default='live-regression.py');p.add_argument('--output',required=True);p.add_argument('--run',action='store_true')
    p.add_argument('--case',action='append',required=True,help='fixture.json:case-name')
    a=p.parse_args()
    if not a.run:p.error('Explicit --run required')
    root=Path(a.output).resolve();root.mkdir(parents=True,exist_ok=False)
    bridge=Path(a.bridge).resolve();here=Path(__file__).resolve().parent
    spec=importlib.util.spec_from_file_location('bridge_client',bridge/'adapter/bridge_client.py')
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    client=module.Client(Path(os.environ['LOCALAPPDATA'])/'CitiesIIAgentBridge',root/'status-intents')
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
    def execute(i, fixture_name, case):
        fixture=here/'fixtures'/fixture_name
        descriptor=json.loads(fixture.read_text())
        if case not in [c['name'] for c in descriptor['cases']]:raise ValueError('Unknown case '+case)
        state=ready();prefix=root/f'{i+1:02d}-{case}'
        print('Reloading baseline for '+case,flush=True)
        code=invoke('reload-toy-baseline.py',['--fixture',str(fixture),'--save-root',a.save_root,'--expected-city-session',state['citySession'],'--bridge',str(bridge),'--output',str(prefix)+'-reload'],Path(str(prefix)+'-reload.log'))
        if code:raise RuntimeError('Lifecycle failed; inspect '+str(prefix)+'-reload.log')
        ready(state['session'])
        print('Testing '+case,flush=True)
        code=invoke(a.runner,['--fixture',str(fixture),'--case',case,'--run','--save-root',a.save_root,'--bridge',str(bridge),'--output',str(prefix)],Path(str(prefix)+'.log'))
        report=prefix/'report.json'
        return code, json.loads(report.read_text(encoding='utf-8')) if report.exists() else None
    return run_cases([case.split(':', 1) for case in a.case], execute, root/'summary.json')
if __name__=='__main__':sys.exit(main())
