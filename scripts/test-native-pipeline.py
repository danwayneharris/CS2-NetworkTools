"""Negative and anti-replay-substitution tests on the actual captured pipeline."""
import argparse,copy,json,subprocess
from pathlib import Path

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for n in ('dll','trace','output'):p.add_argument('--'+n,type=Path,required=True)
    a=p.parse_args();a.output.mkdir(parents=True,exist_ok=False)
    original=json.loads(a.trace.read_text(encoding='utf-8-sig'));results=[]
    records=[json.loads(Path(e['path']).read_text()) for e in original['result']['events'] if 'path' in e]
    init_roots={tuple(e) for r in records if r['job'].endswith('+InitializeNodeGeometryJob') and r['phase']=='entry' for e in r['roots']}
    edge_roots={tuple(e) for r in records if r['job'].endswith('+CalculateEdgeGeometryJob') and r['phase']=='entry' for e in r['roots']}
    def run(name,edit=None,exit_code=0):
        trace=copy.deepcopy(original);folder=a.output/name;folder.mkdir()
        for i,e in enumerate(trace['result']['events']):
            if 'path' not in e:continue
            record=json.loads(Path(e['path']).read_text())
            if edit and edit(record):
                path=folder/(str(i)+'.json');path.write_text(json.dumps(record));e['path']=str(path.resolve())
        request=folder/'trace.json';request.write_text(json.dumps(trace));report=folder/'report.json'
        proc=subprocess.run(['dotnet',str(a.dll),'--pipeline',str(request),str(report)],capture_output=True,text=True)
        (folder/'stderr.txt').write_text(proc.stderr)
        assert proc.returncode==exit_code,(name,proc.returncode,proc.stderr)
        if exit_code==2:assert 'Native replay rejected:' in proc.stderr and not report.exists();data=None
        else:
            data=json.loads(report.read_text());assert data['passed']==(exit_code==0)
        results.append(dict(name=name,exitCode=proc.returncode));return data
    baseline=run('baseline')
    def poison(r):
        if r['phase']!='entry':return False
        job=r['job'].split('+')[-1];changed=False
        for row in r['entities']:
            cells=row['components'];e=tuple(row['id'])
            if job=='CalculateEdgeGeometryJob' and e in init_roots and cells.get('Game.Net.NodeGeometry',{}).get('presence')=='present':
                cells['Game.Net.NodeGeometry']['value']['m_Position']+=1000;changed=True
            if job in ('FlattenNodeGeometryJob','FinishEdgeGeometryJob') and e in edge_roots and cells.get('Game.Net.EdgeGeometry',{}).get('presence')=='present':
                cells['Game.Net.EdgeGeometry']['value']['m_Start']['m_Left']['a']['y']+=1000;changed=True
        return changed
    poisoned=run('recorded-intermediates-poisoned',poison)
    assert poisoned['stageReports']==baseline['stageReports'],'Recorded intermediate replaced computed output'
    def wrong_output(r):
        if r['job'].endswith('+FinishEdgeGeometryJob') and r['phase']=='exit':
            row=next(e for e in r['entities'] if e['id']==r['roots'][0])
            row['components']['Game.Net.EdgeGeometry']['value']['m_Start']['m_Left']['a']['x']+=1;return True
        return False
    run('wrong-final-output',wrong_output,1)
    def terrain(r,missing=False):
        if r['job'].endswith('+FinishEdgeGeometryJob') and r['phase']=='entry':
            if missing:del r['fields']['m_TerrainHeightData']
            else:r['fields']['m_TerrainHeightData']['heights']['sha256']='0'*64
            return True
        return False
    run('missing-terrain',lambda r:terrain(r,True),2);run('corrupt-terrain',terrain,2)
    def wrong_key(r):
        if r['job'].endswith('+FinishEdgeGeometryJob') and r['phase']=='entry':
            r['fields']['m_EdgeHeightMap']['entries'][0]['key'][0]+=999999;return True
        return False
    run('wrong-height-map-identity',wrong_key,2)
    def wrong_sentinel(r):
        if r['job'].endswith('+InitializeNodeGeometryJob') and r['phase']=='exit':
            r['fields']['m_NodeGeometryType']['values'][0]['m_Bounds']['min']['x']+=.001;return True
        return False
    run('small-nonspatial-sentinel-error',wrong_sentinel,1)
    (a.output/'summary.json').write_text(json.dumps(dict(passed=True,tests=results),indent=2))
    print(f'PASS {len(results)} pipeline execution, missing-data, identity, metric and anti-substitution checks')

if __name__=='__main__':main()
