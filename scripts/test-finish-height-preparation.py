"""Offline-only comparison of height preparation + modeled native Finish versus managed Finish.
Old native observations remain unchanged and may intentionally disagree with either correction.
"""
import argparse,json,subprocess,sys
from pathlib import Path

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('manifest','dll','burst-binary','output'):p.add_argument('--'+name,required=True)
    p.add_argument('--dotnet',default='dotnet');a=p.parse_args();out=Path(a.output);out.mkdir(parents=True,exist_ok=False)
    manifest=json.loads(Path(a.manifest).read_text());rows=[]
    if not manifest['fixtures']:raise ValueError('Empty cohort')
    for f in manifest['fixtures']:
        reports={}
        for mode in ('native','full','prepared'):
            target=out/(f['id']+'-'+mode+'.json')
            command=[a.dotnet,a.dll,'--pipeline-'+mode,f['trace']]
            if mode!='full':command.append(a.burst_binary)
            command.append(str(target))
            r=subprocess.run(command,capture_output=True,text=True,timeout=180)
            (out/(f['id']+'-'+mode+'.log')).write_text(r.stdout+r.stderr)
            if r.returncode not in (0,1) or not target.exists():raise RuntimeError('Incomplete replay '+f['id']+'/'+mode)
            reports[mode]=json.loads(target.read_text())
        n,m,c=(reports[k] for k in ('native','full','prepared'))
        if not c['finishHeightPreparation'] or not c['computedEdges']:raise ValueError('Preparation coverage missing')
        equal=c['computedEdges']==m['computedEdges']
        row=dict(id=f['id'],nativeObservationReproduced=n['passed'],preparedEqualsManagedAllComputedEdges=equal,
            preparedEndpoints=len(c['preparedKeys']),edgeCount=len(c['computedEdges']),
            managedMatchesOldObservation=m['passed'],preparedMatchesOldObservation=c['passed'])
        rows.append(row);print(json.dumps(row),flush=True)
    discriminating=any(not r['managedMatchesOldObservation'] and not r['preparedMatchesOldObservation'] and r['preparedEndpoints']>0 for r in rows)
    report=dict(passed=discriminating and all(r['nativeObservationReproduced'] and r['preparedEqualsManagedAllComputedEdges'] for r in rows),discriminatingCounterexample=discriminating,rows=rows,
        boundary='Offline computed pipeline; compare EdgeGeometry and Start/EndNodeGeometry exactly. No runtime scheduler, performance, terrain deformation, reload or lane proof. Old observation mismatch is retained, not relabeled passing.')
    (out/'summary.json').write_text(json.dumps(report,indent=2));return 0 if report['passed'] else 1
if __name__=='__main__':raise SystemExit(main())
