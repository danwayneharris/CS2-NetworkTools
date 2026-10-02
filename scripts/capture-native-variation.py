"""Capture one preview-only research variation in the current paused toy city."""
import argparse,json,runpy,time
from pathlib import Path

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('bridge','output','start','end'):p.add_argument('--'+name,required=True)
    p.add_argument('--kind',choices=['linear','combined','arch'],required=True);p.add_argument('--strength',type=float,default=.5)
    p.add_argument('--arch-height',type=float,default=6)
    p.add_argument('--managed-finish',action='store_true',help='Explicitly change captured Finish execution to managed; toy checkpoint required')
    p.add_argument('--ordinary',action='store_true',help='Do not arm scheduling instrumentation')
    p.add_argument('--observe',help='Original junction index:version to observe between matching provider tokens')
    p.add_argument('--allow-burst',action='store_true',help='Explicitly permit scheduling-boundary capture with Burst enabled')
    a=p.parse_args()
    if a.managed_finish and a.ordinary:raise ValueError("Managed finishing requires an explicitly armed trace")
    def entity(s):
        i,v=map(int,s.split(':'));return dict(index=i,version=v)
    start,end=entity(a.start),entity(a.end)
    Runner=runpy.run_path(str(Path(__file__).with_name('live-regression.py')))['Runner']
    r=Runner(a.bridge,a.output);r.command_timeout=45;city=r.call('get_city_state')
    if city['selectedSpeed']!=0 or city['population']!=0 or not city['controlEnabled']:raise RuntimeError('Paused controlled toy required')
    # Validate current entity versions/types before using these explicit endpoints.
    for e in (start,end,*([entity(a.observe)] if a.observe else [])):
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
    final={'events':[]}
    if a.ordinary:
        status=r.call('get_geometry_schedule_trace')
        if status['patched'] or status['insideCapturedPass'] or status['remainingPasses']:raise RuntimeError('Ordinary observation requires disarmed tracing')
    else:
        armed=r.call('begin_geometry_schedule_trace',dict(citySession=r.city_session,operationId=Path(a.output).name,maxPasses=1,allowBurst=a.allow_burst,finishExecution="managed" if a.managed_finish else "native"))
        if not armed['patched'] or armed['remainingPasses']!=1:raise RuntimeError('Trace not armed')
    try:
        change('select' if a.kind=='combined' else 'slope_select',start=start,end=end)
        for _ in range(0 if a.ordinary else 40):
            status=r.call('get_geometry_schedule_trace')
            if status['fault']:raise RuntimeError('Capture fault: '+status['fault'])
            if status['passesStarted']==1 and not status['insideCapturedPass']:break
            time.sleep(.25)
        else:
            if not a.ordinary:raise RuntimeError('No native geometry pass observed')
    finally:
        if not a.ordinary:
            final=r.call('end_geometry_schedule_trace')
            (Path(a.output)/'trace-completed.json').write_text(json.dumps(dict(result=final),indent=2))
    ready=call(state_command)
    for _ in range(20):
        if ready['previewReady'] or ready['surfaceFailed']:break
        time.sleep(.25);ready=call(state_command)
    if a.observe:
        observation=r.call('get_junction_preview',entity(a.observe));after=call(state_command)
        correlated=ready['previewReady'] and after['previewReady'] and all(ready[k]==after[k] for k in ('session','revision','submission'))
        (Path(a.output)/'observation.json').write_text(json.dumps(dict(before=ready,observation=observation,after=after,matchingProviderTokens=correlated),indent=2))
        if not correlated or not observation.get('connectedSnapshot',{}).get('complete'):raise RuntimeError('No complete correlated preview observation; evidence retained')
    (Path(a.output)/'variation.json').write_text(json.dumps(dict(kind=a.kind,ordinary=a.ordinary,strength=a.strength if a.kind=='combined' else None,
        start=start,end=end,city=city,preflight=preflight,result=ready,applied=False),indent=2))
    print(json.dumps(dict(kind=a.kind,previewReady=ready['previewReady'],surfaceFailed=ready['surfaceFailed'],captures=len([e for e in final['events'] if 'path' in e]))))

if __name__=='__main__':main()
