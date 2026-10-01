"""Run a bounded, checkpointed Curve/Slope sequence on a freshly loaded toy fixture.
Each invocation returns to its verified baseline. Explicit --run required. Uses
existing lifecycle/verification scripts; stops on uncertain mutation or failed gate.
"""
import argparse,json,os,runpy,subprocess,sys,time
from pathlib import Path
HERE=Path(__file__).resolve().parent
reg=runpy.run_path(str(HERE/'live-regression.py'))

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--bridge',default='../bridge-terrain-profile');p.add_argument('--fixture',default=str(HERE/'fixtures/toy-terrain-v11.json'))
    p.add_argument('--case',required=True);p.add_argument('--order',choices=['slope','curve-slope','slope-curve'],required=True)
    p.add_argument('--strength',type=float,default=1.0);p.add_argument('--mode',choices=['linear','ease','arch'],default='linear');p.add_argument('--reverse',action='store_true')
    p.add_argument('--smooth-start',action='store_true');p.add_argument('--smooth-end',action='store_true')
    p.add_argument('--save-root',required=True);p.add_argument('--output',required=True);p.add_argument('--run',action='store_true')
    a=p.parse_args()
    if not a.run:p.error('Explicit --run required')
    if not 0 <= a.strength <= 1:p.error('Strength must be in [0, 1]')
    root=Path(a.output).resolve();root.mkdir(parents=True,exist_ok=False)
    fixture=json.loads(Path(a.fixture).read_text());case=next(c for c in fixture['cases'] if c['name']==a.case)
    bridge=Path(a.bridge).resolve();client=runpy.run_path(str(bridge/'adapter/bridge_client.py'))['Client'](Path(os.environ['LOCALAPPDATA'])/'CitiesIIAgentBridge',root/'status-intents')
    state=client.status()
    def execute(script,args,name,seconds=180):
        with (root/(name+'.log')).open('w') as log:
            process=subprocess.run([sys.executable,str(HERE/script),*args],stdout=log,stderr=subprocess.STDOUT,timeout=seconds)
        if process.returncode:raise RuntimeError(name+' failed; inspect original results, no automatic retry')
    execute('reload-toy-baseline.py',['--fixture',a.fixture,'--save-root',a.save_root,'--expected-city-session',state['citySession'],'--bridge',str(bridge),'--output',str(root/'reload')],'reload',110)
    deadline=time.monotonic()+240
    while time.monotonic()<deadline:
        try:
            current=client.status()
            if current['session']!=state['session'] and current['gameMode']=='Game' and not current['loading']:break
        except (OSError,ValueError,RuntimeError):pass
        time.sleep(1)
    else:raise TimeoutError('Baseline startup timeout; no force kill')
    stages=a.order.split('-');reports=[]
    for i,stage in enumerate(stages):
        r=reg['Runner'](bridge,root/('discovery-'+str(i)))
        city=r.call('get_city_state')
        if city['population']!=0 or city['selectedSpeed']!=0 or not city['controlEnabled']:raise ValueError('Paused controlled toy required')
        nodes,edges=r.network(fixture['region']);fingerprint=reg['fingerprint'](nodes,edges)
        if i==0 and fingerprint!=fixture['fingerprint']:raise ValueError('Original baseline geometry mismatch')
        destination=root/(str(i)+'-'+stage)
        if stage=='curve':
            modified={**fixture,'fingerprint':fingerprint,'cases':[{**case,'strengths':[a.strength]}]}
            if a.reverse:modified['cases'][0].update(start=case['end'],end=case['start'])
            path=root/('curve-fixture-'+str(i)+'.json');path.write_text(json.dumps(modified,indent=2))
            execute('terrain-regression.py',['--bridge',str(bridge),'--fixture',str(path),'--case',a.case,'--save-root',a.save_root,'--output',str(destination),'--run'],str(i)+'-'+stage)
        else:
            args=['--bridge',str(bridge),'--fixture',a.fixture,'--stage','slope','--slope-mode',a.mode,'--case',a.case,'--expected-fingerprint',fingerprint,'--save-root',a.save_root,'--output',str(destination),'--run']
            for flag in ('reverse','smooth_start','smooth_end'):
                if getattr(a,flag):args.append('--'+flag.replace('_','-'))
            execute('exercise-tool-provider.py',args,str(i)+'-'+stage)
            execute('audit-slope-capture.py',[str(destination)],str(i)+'-audit')
            args=[str(destination)]
            if a.mode=='linear' and not a.smooth_start and not a.smooth_end:args.append('--verify-offset-fit')
            execute('summarize-profile-experiment.py',args,str(i)+'-analysis')
        reports.append({'stage':stage,'report':json.loads((destination/'report.json').read_text())})
        (root/'summary.json').write_text(json.dumps({'case':a.case,'order':a.order,'stages':reports,'status':'partial' if i<len(stages)-1 else 'complete'},indent=2))
        print(a.case+' '+stage+': verified',flush=True)
if __name__=='__main__':main()