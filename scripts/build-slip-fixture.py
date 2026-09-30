"""Build an isolated toy slip-lane fixture using native validated placement.

Explicit --run required. No mutation retries, unlock bypass, or baseline overwrite.
A partially failed construction is retained with captures and a pre-build checkpoint.
"""
import argparse,json,hashlib,runpy,time
from pathlib import Path
HERE=Path(__file__).resolve().parent
reg=runpy.run_path(str(HERE/'live-regression.py'))
verified=runpy.run_path(str(HERE/'reload-toy-baseline.py'))['verified_package']
REGION={'x':-650,'z':-1100,'radius':360}
A=(-750,-1100);B=(-650,-1100);C=(-550,-1100);D=(-350,-1100)
E=(-550,-1300);F=(-550,-1200);G=(-550,-900)
H=(-655,-1150);I=(-600,-1215)
A,B,C,D,E,F,G,H,I=[(p[0]-100,p[1]) for p in (A,B,C,D,E,F,G,H,I)]
PLAN=[(A,B),(B,H),(H,I),(I,F),(F,E),(F,C),(C,B),(C,G),(C,D)]
SLIP={(B,H),(H,I),(I,F)}


def checkpoint(r,root,label):
    op=r.call('save_checkpoint',{'label':label})
    done=r.poll('get_operation',lambda s:s['status'] in ('complete','failed','interrupted'),{'id':op['id']})
    if done['status']!='complete':raise RuntimeError('Checkpoint did not finish')
    return verified(root,done['saveName']+'.cok')


def main():
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--output',required=True);ap.add_argument('--save-root',required=True)
    ap.add_argument('--expected-city-session',required=True);ap.add_argument('--run',action='store_true')
    args=ap.parse_args();r=reg['Runner']('../cities2-agent-bridge-ndc',args.output)
    city=r.call('get_city_state')
    if r.city_session!=args.expected_city_session or city['selectedSpeed']!=0 or city['population']!=0 or not city['controlEnabled']:
        raise ValueError('Identified paused toy city required')
    base=json.loads((HERE/'fixtures/toy-highway-jank.json').read_text())
    verified(args.save_root,base['baseline'],base['baselineSha256'])
    nodes,edges=r.network(base['region'])
    if reg['fingerprint'](nodes,edges)!=base['fingerprint']:raise ValueError('Reload original baseline first')
    localnodes,localedges=r.network(REGION)
    for edge in localedges:
        xs=[p['x'] for p in edge['curve']];zs=[p['z'] for p in edge['curve']]
        if max(xs)>=-890 and min(xs)<=-410 and max(zs)>=-1340 and min(zs)<=-860:
            raise ValueError('Existing curve bounding box overlaps construction area')
    tiles=r.call('get_tiles')['tiles']
    for x,z in set(p for segment in PLAN for p in segment):
        if not any(t['purchased'] and min(p['x'] for p in t['polygon'])<x<max(p['x'] for p in t['polygon'])
                   and min(p['z'] for p in t['polygon'])<z<max(p['z'] for p in t['polygon']) for t in tiles):
            raise ValueError('Construction outside purchased tile')
    prefabs={}
    for name in ('Small Road','Small Road Oneway - 1 lane'):
        result=r.call('get_build_prefabs',{'kind':'network','filter':name,'limit':100})
        if result['nextOffset'] is not None:raise ValueError('Unexpected paginated prefab search')
        matches=[p for p in result['prefabs'] if p['name']==name and not p['locked'] and p['bridgePlacementSupported']]
        if len(matches)!=1:raise ValueError('Missing unique unlocked road')
        prefabs[name]=matches[0]
        r.call('get_prefab_details',{k:matches[0][k] for k in ('index','version')})
    if not args.run:print('Preflight complete; no construction');return
    checkpoint(r,args.save_root,'slip-before-build')
    known={}
    for number,(start,end) in enumerate(PLAN):
        city=r.call('get_city_state')
        if city['selectedSpeed']!=0 or city['population']!=0:raise ValueError('Toy state changed')
        prefab=prefabs['Small Road Oneway - 1 lane' if (start,end) in SLIP else 'Small Road']
        def point(p):
            value={'x':p[0],'z':p[1]}
            if p in known:value.update(known[p])
            return value
        op=r.call('build_road',{'prefabIndex':prefab['index'],'prefabVersion':prefab['version'],
                              'start':point(start),'end':point(end),'maxCost':10000})
        done=r.poll('get_operation',lambda s:s['status'] in ('complete','failed','interrupted'),{'id':op['id']})
        if done['status']!='complete':raise RuntimeError('Native placement failed; inspect before any retry')
        localnodes,localedges=r.network(REGION)
        for p in (start,end):
            found=[n for n in localnodes if abs(n['position']['x']-p[0])<0.25 and abs(n['position']['z']-p[1])<0.25]
            if len(found)!=1:raise ValueError('Unexpected snapped endpoint; stop construction')
            known[p]={k:found[0][k] for k in ('index','version')}
        print('Verified placement',number+1,flush=True)
    baseline=checkpoint(r,args.save_root,'slip-baseline')
    nodes,edges=r.network(REGION)
    def position(p):
        n=next(n for n in nodes if all(n[k]==known[p][k] for k in ('index','version')))
        return [n['position'][k] for k in ('x','y','z')]
    fixture={'region':REGION,'baseline':baseline.name,'baselineSha256':hashlib.sha256(baseline.read_bytes()).hexdigest().upper(),
             'fingerprint':reg['fingerprint'](nodes,edges),'cases':[{'name':'slip-lane','start':position(H),'end':position(F),'strengths':[0.5,0.8]}]}
    (r.output/'fixture.json').write_text(json.dumps(fixture,indent=2)+'\n')
    print('Fixture saved; inspect actual native connectivity before calling it a valid test.')


if __name__=='__main__':main()
