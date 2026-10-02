"""Exercise real captured edge inputs, rejecting gaps and discriminating wrong output."""
import argparse, copy, json, subprocess
from pathlib import Path

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ('dll','entry','exit','output'):p.add_argument('--'+name,type=Path,required=True)
    a=p.parse_args();a.output.mkdir(parents=True,exist_ok=False)
    entry=json.loads(a.entry.read_text());expected=json.loads(a.exit.read_text());results=[]
    def run(name, e, x, rejected=False):
        ep=a.output/(name+'-entry.json');xp=a.output/(name+'-exit.json');report=a.output/(name+'-report.json')
        ep.write_text(json.dumps(e));xp.write_text(json.dumps(x))
        proc=subprocess.run(['dotnet',str(a.dll),'--raw-edge',str(ep),str(xp),str(report)],capture_output=True,text=True)
        (a.output/(name+'-stderr.txt')).write_text(proc.stderr)
        if rejected:
            assert proc.returncode==2 and 'Native replay rejected:' in proc.stderr and not report.exists(),proc.stderr
            result=None
        else:
            assert proc.returncode in (0,1) and report.exists(),proc.stderr
            result=json.loads(report.read_text())
            assert (proc.returncode==0)==result['passed']
        results.append(dict(name=name,exitCode=proc.returncode,rejected=rejected))
        return result
    baseline=run('captured',entry,expected)
    for name,mutate in [
        ('incomplete',lambda e:e.update(complete=False)),
        ('wrong-version',lambda e:e.update(gameModuleVersionId='00000000-0000-0000-0000-000000000000')),
        ('missing-terrain-bound',lambda e:e['fields']['m_TerrainBounds']['min'].pop('x')),
        ('missing-curve-field',lambda e:next(r for r in e['entities'] if r['id']==e['roots'][0])['components']['Game.Net.Curve']['value'].pop('m_Length')),
        ('missing-curve-cell',lambda e:next(r for r in e['entities'] if r['id']==e['roots'][0])['components'].pop('Game.Net.Curve')),
    ]:
        e=copy.deepcopy(entry);mutate(e);run(name,e,expected,True)
    bad=copy.deepcopy(expected)
    target=next(r for r in bad['entities'] if r['id']==bad['roots'][0])
    target['components']['Game.Net.EdgeGeometry']['value']['m_Start']['m_Left']['a']['x']=123456.0
    incorrect=run('wrong-output',entry,bad)
    assert not incorrect['passed'] and any(d['native']==123456.0 for d in incorrect['differences'])
    (a.output/'summary.json').write_text(json.dumps(dict(tests=results,
        nativeDifferentialPassed=baseline['passed'],nativeMismatchCount=len(baseline['differences']),
        note='Contract tests passing does not qualify nonzero native differential errors'),indent=2))
    print(f"PASS {len(results)} capture/negative checks; native differential mismatches remain {len(baseline['differences'])}")

if __name__=='__main__':main()
