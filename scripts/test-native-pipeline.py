"""Negative and anti-replay-substitution tests on the actual captured pipeline."""
import argparse,copy,json,subprocess
from pathlib import Path

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for n in ('dll','trace','output'):p.add_argument('--'+n,type=Path,required=True)
    p.add_argument('--junction',action='store_true')
    p.add_argument('--full',action='store_true')
    p.add_argument('--burst-binary',type=Path)
    a=p.parse_args();a.output.mkdir(parents=True,exist_ok=False)
    a.full=a.full or a.burst_binary is not None;a.junction=a.junction or a.full
    original=json.loads(a.trace.read_text(encoding='utf-8-sig'));results=[]
    records=[json.loads(Path(e['path']).read_text()) for e in original['result']['events'] if 'path' in e]
    init_roots={tuple(e) for r in records if r['job'].endswith('+InitializeNodeGeometryJob') and r['phase']=='entry' for e in r['roots']}
    edge_roots={tuple(e) for r in records if r['job'].endswith('+CalculateEdgeGeometryJob') and r['phase']=='entry' for e in r['roots']}
    def run(name,edit=None,exit_code=0,managed=False,binary=None,status_edit=None):
        trace=copy.deepcopy(original);folder=a.output/name;folder.mkdir()
        if status_edit:status_edit(trace['result'])
        for i,e in enumerate(trace['result']['events']):
            if 'path' not in e:continue
            record=json.loads(Path(e['path']).read_text())
            if edit and edit(record):
                path=folder/(str(i)+'.json');path.write_text(json.dumps(record));e['path']=str(path.resolve())
        request=folder/'trace.json';request.write_text(json.dumps(trace));report=folder/'report.json'
        command=['dotnet',str(a.dll)]
        if a.burst_binary and not managed:command+=['--pipeline-native',str(request),str(binary or a.burst_binary),str(report)]
        else:command+=['--pipeline-full' if a.full else '--pipeline-junction' if a.junction else '--pipeline',str(request),str(report)]
        proc=subprocess.run(command,capture_output=True,text=True)
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
            if job in ('FlattenNodeGeometryJob','FinishEdgeGeometryJob','CalculateNodeGeometryJob','CalculateIntersectionGeometryJob','CopyNodeGeometryJob','UpdateNodeGeometryJob') and e in edge_roots and cells.get('Game.Net.EdgeGeometry',{}).get('presence')=='present':
                cells['Game.Net.EdgeGeometry']['value']['m_Start']['m_Left']['a']['y']+=1000;changed=True
            if job in ('CalculateNodeGeometryJob','CalculateIntersectionGeometryJob','CopyNodeGeometryJob','UpdateNodeGeometryJob') and e in edge_roots:
                for name in ('Game.Net.StartNodeGeometry','Game.Net.EndNodeGeometry'):
                    if name in cells:cells[name]['value']['m_Geometry']['m_Left']['m_Left']['a']['y']+=1000;changed=True
        if job=='CopyNodeGeometryJob':
            for value in r['fields']['m_BufferedData']:value['m_StartMiddle']['a']['x']+=1000
            changed=True
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
    if a.junction:
        def junction_output(r,marker=False):
            if r['job'].endswith('+CalculateNodeGeometryJob') and r['phase']=='exit' and r['fields']['m_IterationIndex']==1:
                row=next(e for e in r['entities'] if e['id']==r['roots'][0])
                geometry=row['components']['Game.Net.StartNodeGeometry']['value']['m_Geometry']
                if marker:geometry['m_Middle']['d']['x']+=.001
                else:geometry['m_Left']['m_Left']['a']['x']+=1
                return True
            return False
        run('wrong-junction-output',junction_output,1)
        run('small-junction-branch-marker-error',lambda r:junction_output(r,True),1)
        def missing_junction(r):
            if r['phase']=='entry':
                for row in r['entities']:row['components'].pop('Game.Prefabs.NetCompositionPiece',None)
                return True
            return False
        run('missing-junction-composition-pieces',missing_junction,2)
    if a.full:
        def wrong_publication(r,scratch=False):
            if scratch and r['job'].endswith('+CalculateIntersectionGeometryJob') and r['phase']=='exit':
                r['fields']['m_BufferedData'][0]['m_StartMiddle']['a']['x']+=1;return True
            if not scratch and r['job'].endswith('+UpdateNodeGeometryJob') and r['phase']=='exit':
                r['fields']['m_NodeGeometryType']['values'][0]['m_Bounds']['min']['x']+=1;return True
            return False
        run('wrong-intersection-scratch',lambda r:wrong_publication(r,True),1)
        run('wrong-published-node-bounds',wrong_publication,1)
    if a.burst_binary:
        run('managed-lookup-counterfactual',exit_code=1,managed=True)
        corrupt=a.output/'wrong-burst.dll';data=bytearray(a.burst_binary.read_bytes());data[-1]^=1;corrupt.write_bytes(data)
        run('wrong-native-binary',exit_code=2,binary=corrupt)
        run('wrong-execution-context',exit_code=2,status_edit=lambda s:s.update(burstEnabledAtArm=False))
        def wrong_allocation(r):
            if r['job'].endswith('+FinishEdgeGeometryJob') and r['phase']=='entry':
                r['fields']['m_EdgeHeightMap']['capacity']=999;return True
            return False
        run('wrong-native-map-allocation',wrong_allocation,2)
    (a.output/'summary.json').write_text(json.dumps(dict(passed=True,tests=results),indent=2))
    print(f'PASS {len(results)} pipeline execution, missing-data, identity, metric and anti-substitution checks')

if __name__=='__main__':main()
