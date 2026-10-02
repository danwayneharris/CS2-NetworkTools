"""Capture one preview-only research variation in the current paused toy city."""
import argparse,json,runpy,time
from pathlib import Path

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('bridge','output','start','end'):p.add_argument('--'+name,required=True)
    p.add_argument('--kind',choices=['linear','combined','arch'],required=True);p.add_argument('--strength',type=float,default=.5)
    p.add_argument('--arch-height',type=float,default=6)
    a=p.parse_args()
    def entity(s):
        i,v=map(int,s.split(':'));return dict(index=i,version=v)
    start,end=entity(a.start),entity(a.end)
    Runner=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))['Runner']
    r=Runner(a.bridge,a.output);city=r.call('get_city_state')
    if city['selectedSpeed']!=0 or city['population']!=0 or not city['controlEnabled']:raise RuntimeError('Paused controlled toy required')
    # Validate current entity versions/types before using these explicit endpoints.
    for e in (start,end):
        observed=r.call('get_junction_snapshot',e)
        if not observed['complete']:raise RuntimeError('Endpoint inspection incomplete')
    provider=next(x for x in r.call('list_providers')['providers'] if x['id']=='networktools')
    def call(command,args=None):return r.call('invoke_provider',dict(provider='networktools',revision=provider['revision'],command=command,args=args or {}))
    call('activate' if a.kind=='combined' else 'slope_activate')
    state_command='state' if a.kind=='combined' else 'slope_state'
    def change(command,**values):
        s=call(state_command);return call(command,dict(session=s['session'],revision=s['revision'],**values))
    change('clear' if a.kind=='combined' else 'slope_clear')
    if a.kind=='combined':
        change('combined',enabled=True,smoothStart=True,smoothEnd=True);change('strength',value=a.strength)
    else:change('slope_configure',mode=a.kind,easeIn=0,easeOut=0,archHeight=a.arch_height if a.kind=='arch' else 0,archPosition=.5,smoothStart=False,smoothEnd=False)
    preflight=call(state_command)
    armed=r.call('begin_geometry_schedule_trace',dict(citySession=r.city_session,operationId=Path(a.output).name,maxPasses=1))
    if not armed['patched'] or armed['remainingPasses']!=1:raise RuntimeError('Trace not armed')
    try:
        change('select' if a.kind=='combined' else 'slope_select',start=start,end=end)
        for _ in range(40):
            status=r.call('get_geometry_schedule_trace')
            if status['fault']:raise RuntimeError('Capture fault: '+status['fault'])
            if status['passesStarted']==1 and not status['insideCapturedPass']:break
            time.sleep(.25)
        else:raise RuntimeError('No native geometry pass observed')
    finally:
        final=r.call('end_geometry_schedule_trace')
        (Path(a.output)/'trace-completed.json').write_text(json.dumps(dict(result=final),indent=2))
    ready=call(state_command)
    for _ in range(20):
        if ready['previewReady'] or ready['surfaceFailed']:break
        time.sleep(.25);ready=call(state_command)
    (Path(a.output)/'variation.json').write_text(json.dumps(dict(kind=a.kind,strength=a.strength if a.kind=='combined' else None,
        start=start,end=end,city=city,preflight=preflight,result=ready,applied=False),indent=2))
    print(json.dumps(dict(kind=a.kind,previewReady=ready['previewReady'],surfaceFailed=ready['surfaceFailed'],captures=len([e for e in final['events'] if 'path' in e]))))

if __name__=='__main__':main()
