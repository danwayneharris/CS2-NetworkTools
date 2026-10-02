"""Project paired native InitializeNodeGeometry captures into the bounded world schema.
No recorded output enters replay inputs. Unknown cells remain unknown, never absent.
"""
import argparse
import hashlib
import json
from pathlib import Path

GAME_HASH = 'AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A'

def xyz(v): return [v[k] for k in ('x', 'y', 'z')]
def curve(v): return [xyz(v[k]) for k in ('a', 'b', 'c', 'd')]
def project(name, v):
    if name == 'Node': return {'position': xyz(v['m_Position'])}
    if name == 'NodeGeometry':
        return dict(position=v['m_Position'], flatness=v['m_Flatness'], offset=v['m_Offset'],
                    boundsMin=xyz(v['m_Bounds']['min']), boundsMax=xyz(v['m_Bounds']['max']))
    if name == 'Edge': return dict(start=v['m_Start'], end=v['m_End'])
    if name == 'Curve': return curve(v['m_Bezier'])
    if name == 'PrefabRef': return v['m_Prefab']
    if name == 'Composition': return dict(edge=v['m_Edge'], start=v['m_StartNode'], end=v['m_EndNode'])
    if name == 'Temp': return dict(original=v['m_Original'], flags=v['m_Flags'])
    if name in ('Hidden', 'Updated'): return True
    if name == 'Owner': return v['m_Owner']
    if name == 'NetGeometryData': return dict(mergeLayers=v['m_MergeLayers'], flags=v['m_Flags'], maxSlope=v['m_MaxSlopeSteepness'])
    if name == 'NetCompositionData':
        flags=v['m_Flags']
        return dict(flags=dict(general=flags['m_General'],left=flags['m_Left'],right=flags['m_Right']),
                    width=v['m_Width'],state=v['m_State'],heightMin=v['m_HeightRange']['min'],heightMax=v['m_HeightRange']['max'])
    if name == 'ConnectedEdge': return [x['m_Edge'] for x in v]
    if name == 'EdgeGeometry':
        def segment(s): return dict(left=curve(s['m_Left']),right=curve(s['m_Right']))
        return dict(start=segment(v['m_Start']),end=segment(v['m_End']))
    raise ValueError('Unsupported component: '+name)

def cells(captures):
    records={}
    def put(e, name, cell):
        name=name.rsplit('.',1)[-1]
        row=records.setdefault(tuple(e), {})
        if name in row and row[name] != cell: raise ValueError(f'Incoherent captured cell {e}/{name}')
        row[name]=cell
    for c in captures:
        for row in c['entities']:
            for name, value in row['components'].items(): put(row['id'], name, value)
        for field in c['fields'].values():
            if not isinstance(field,dict) or 'component' not in field or 'presence' not in field: continue
            if field['presence']=='present':
                if len(field['values']) != len(c['roots']): raise ValueError('Chunk/root length mismatch')
                for e,v in zip(c['roots'],field['values']): put(e,field['component'],dict(presence='present',exists=True,value=v))
            elif field['presence']=='absent':
                for e in c['roots']: put(e,field['component'],dict(presence='absent',exists=True,value=None))
    return records

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--trace-status',required=True);p.add_argument('--output',required=True)
    p.add_argument('--stage',choices=['InitializeNodeGeometry','FlattenNodeGeometry'],default='InitializeNodeGeometry')
    a=p.parse_args(); out=Path(a.output);out.mkdir(parents=True,exist_ok=False)
    status=json.loads(Path(a.trace_status).read_text(encoding='utf-8-sig'))['result']
    if status['gameSha256']!=GAME_HASH: raise ValueError('Game hash changed')
    if status['fault'] or status['remainingPasses'] or status['insideCapturedPass']: raise ValueError('Trace not completed cleanly')
    events=[e for e in status['events'] if e.get('job','').endswith('+'+a.stage+'Job')]
    raw=[json.loads(Path(e['path']).read_text(encoding='utf-8-sig')) for e in events]
    if any(r['schemaVersion']!=1 or r['gameModuleVersionId']!='e15551e9-3dea-4855-8dde-1da3cea7d7ef' for r in raw): raise ValueError('Native capture version changed')
    if not raw or len({(r['operationId'],r['pass'],r['citySession']) for r in raw})!=1: raise ValueError('Need one coherent native pass')
    entries=[r for r in raw if r['phase']=='entry'];exits=[r for r in raw if r['phase']=='exit']
    if [r['roots'] for r in entries] != [r['roots'] for r in exits]: raise ValueError('Entry/exit membership mismatch')
    loaded={r['fields']['m_Loaded'] for r in entries} if a.stage=='InitializeNodeGeometry' else {False}
    if len(loaded)!=1: raise ValueError('Loaded flag mismatch')
    nodes=[e for r in entries for e in r['roots']]
    records=cells(entries); omitted=[]; entities=[]
    for e, values in records.items():
        projected={}
        for name,cell in values.items():
            presence=cell['presence']
            if presence=='notCaptured': omitted.append(dict(entity=e,component=name,error=cell.get('error')))
            elif presence=='absent': projected[name]=None
            elif presence=='present': projected[name]=project(name,cell['value'])
            else: raise ValueError('Unknown presence: '+presence)
        entities.append(dict(id=e,components=projected))
    fixture=dict(schemaVersion=2,gameSha256=GAME_HASH,loaded=loaded.pop(),nodes=nodes,finishEdges=[],stages=[a.stage],entities=entities)
    expected=[];after=cells(exits)
    for e in nodes:
        v=project('NodeGeometry',after[tuple(e)]['NodeGeometry']['value'])
        expected.append(dict(id=e,position=v['position'],flatness=v['flatness'],offset=v['offset'],retentionSentinel=v['boundsMin'][0]))
    evidence=dict(scope='Native '+a.stage+' stage only; diagnostic synchronization; outputs excluded from inputs',
                  sourceFiles=[dict(path=e['path'],sha256=hashlib.sha256(Path(e['path']).read_bytes()).hexdigest()) for e in events],
                  omittedUnknownCells=omitted,rawCaptureErrors=[r['errors'] for r in entries],expectedNodes=expected)
    if a.stage=='FlattenNodeGeometry':
        finish=[e for e in status['events'] if e.get('job','').endswith('+FinishEdgeGeometryJob') and e['phase']=='entry']
        if len(finish)!=1: raise ValueError('Need one immediate downstream map observation')
        f=finish[0];data=json.loads(Path(f['path']).read_text(encoding='utf-8-sig'))
        evidence['expectedHeightMap']=data['fields']['m_EdgeHeightMap']['entries']
        evidence['mapObservation']=dict(path=f['path'],sha256=hashlib.sha256(Path(f['path']).read_bytes()).hexdigest())
        evidence['emptyInputMapBasis']='GeometrySystem.OnUpdate allocates a fresh map; AllocateBuffers sets capacity; flatten is its producer. Recorded output map is comparison-only.'
    for name,value in [('fixture.json',fixture),('expected.json',evidence)]:
        (out/name).write_text(json.dumps(value,indent=2),encoding='utf-8')
    print(json.dumps(dict(nodes=len(nodes),entities=len(entities),omittedUnknownCells=len(omitted))))

if __name__=='__main__': main()
