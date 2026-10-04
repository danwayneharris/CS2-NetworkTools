"""Checkpoint and exercise the captured sloped Connect case; --run mutates preview.

--apply additionally applies once and independently checks permanent results.
Requires the already loaded, explicitly identified paused toy session. No restart.
"""
import argparse
import json
import runpy
import time
from pathlib import Path

HERE=Path(__file__).resolve().parent
live=runpy.run_path(str(HERE/'live-regression.py'))
checks=runpy.run_path(str(HERE/'exercise-tool-provider.py'))
package=runpy.run_path(str(HERE/'reload-toy-baseline.py'))['verified_package']

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--output',required=True)
    p.add_argument('--expected-city-session',required=True)
    p.add_argument('--baseline-fixture',required=True)
    p.add_argument('--save-root',required=True)
    p.add_argument('--run',action='store_true')
    p.add_argument('--apply',action='store_true')
    a=p.parse_args()
    if a.apply and not a.run: p.error('--apply requires --run')
    baseline=json.loads(Path(a.baseline_fixture).read_text(encoding='utf-8-sig'))
    if baseline['baseline']!='bridge test - connect repro.cok': raise ValueError('Wrong toy baseline')
    package(a.save_root,baseline['baseline'],baseline['baselineSha256'])
    fixture=json.loads((HERE.parent/'NetworkTools.ConnectCoverage.Tests/Fixtures/connect-course-height.json').read_text(encoding='utf-8-sig'))
    c=fixture['authored'][0]
    r=live['Runner'](HERE.parent.parent/'cities2-agent-bridge-ndc',a.output)
    city=r.call('get_city_state')
    if r.city_session!=a.expected_city_session or city['selectedSpeed']!=0 or city['population']!=0 or not city['controlEnabled']:
        raise ValueError('Identified paused control-enabled toy city required')
    query={'x':-3450,'z':-1260,'radius':400}
    ns=r.call('get_network',query)['nodes'];es=r.call('get_network_edges',query)['edges']
    start,end=live['resolve'](ns,c[0]),live['resolve'](ns,c[3])
    if not a.run: print('Baseline package and endpoints identified; no mutation');return
    op=r.call('save_checkpoint',{'label':'connect-course-restoration-before'})
    saved=r.poll('get_operation',lambda s:s['status'] in ('complete','failed','interrupted'),{'id':op['id']})
    if saved['status']!='complete':raise RuntimeError('Save did not complete; no retry')
    package(a.save_root,saved['saveName']+'.cok')
    catalog=r.call('list_providers')
    if not catalog['complete']:raise RuntimeError('Incomplete provider discovery')
    provider=next(x for x in catalog['providers'] if x['id']=='networktools')
    def call(action,args=None):
        return r.call('invoke_provider',{'provider':'networktools','revision':provider['revision'],'command':'connect_'+action,'args':args or {}})
    def token(s):return {k:s[k] for k in ('session','revision')}
    def poll(predicate):
        for _ in range(40):
            s=call('state')
            if predicate(s):return s
            time.sleep(.25)
        raise TimeoutError('Preview did not settle; inspect captured responses before further mutations')
    call('activate');s=poll(lambda s:s['active'] and s['phase']=='Idle')
    call('select',dict(token(s),start=start,end=end));s=poll(lambda s:s['phase']=='Ready')
    call('configure',dict(token(s),smoothElevationProfile=True,startControl=c[1],endControl=c[2]))
    s=poll(lambda s:s['previewReady'] or str(s['rejectionReason']).startswith('profile_native_'))
    report={'checkpoint':saved['saveName'],'state':s,'applied':False,'limits':'No rendered surface, lane identity or vehicle certification'}
    (r.output/'preview-report.json').write_text(json.dumps(report,indent=2))
    print(json.dumps({k:s.get(k) for k in ('previewReady','rejectionReason','profileRestoration','build')}))
    if not a.apply:return
    if not s['previewReady']:raise RuntimeError('Refusing Apply of rejected preview')
    call('apply',dict(token(s),submission=s['submission']))
    poll(lambda x:x['phase']=='Idle')
    an=r.call('get_network',query)['nodes'];ae=r.call('get_network_edges',query)['edges']
    before={live['identity'](x) for x in es}
    added=[x for x in ae if live['identity'](x) not in before]
    trace=r.call('trace_network',{'fromIndex':start['index'],'fromVersion':start['version'],'toIndex':end['index'],'toVersion':end['version']})
    report.update(checks['connect_preservation'](ns,es,an,ae))
    source=[e for e in es if live['identity'](start) in (live['identity'](e['startNode']),live['identity'](e['endNode']))]
    report.update(applied=True,newEdges=len(added),previewApplyMaxError=checks['connect_preview_error'](s,added),trace=trace,
                  connected=trace['connected'],prefabInherited=len(source)==1 and all(e['prefab']==source[0]['prefab'] for e in added))
    (r.output/'apply-report.json').write_text(json.dumps(report,indent=2))
    checks['assert_connect_report'](report)
    print(json.dumps({k:v for k,v in report.items() if k not in ('state','trace')}))

if __name__=='__main__':main()
