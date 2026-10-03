"""Discriminating JSON capture contract tests; synthetic state, no game access."""
import argparse
import copy
import json
import subprocess
from pathlib import Path

HASH = 'AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A'


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--dll', type=Path, required=True)
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    a.output.mkdir(parents=True, exist_ok=False)
    # Node with explicitly empty incidence: initialization must set its own height
    # and zero the stale retention marker. Distinguishes executed writes from echo.
    capture = dict(schemaVersion=2, gameSha256=HASH, loaded=False, nodes=[[1, 1]], finishEdges=[],
        stages=['InitializeNodeGeometry', 'FlattenNodeGeometry'], entities=[
            dict(id=[1, 1], components={'Node': {'position': [0, 12, 0]},
                'NodeGeometry': dict(position=-5, flatness=1, offset=3, boundsMin=[1, 0, 0], boundsMax=[0, 0, 0]),
                'Temp': None, 'PrefabRef': [100, 1], 'ConnectedEdge': []}),
            dict(id=[100, 1], components={'NetGeometryData': dict(mergeLayers=1, flags=0, maxSlope=.2)})])
    records = []

    def run(name, data, expected_success):
        path = a.output / (name + '.json')
        report = a.output / (name + '.report.json')
        path.write_text(json.dumps(data), encoding='utf-8')
        process = subprocess.run(['dotnet', str(a.dll.resolve()), '--world', str(path), str(report)],
                                 capture_output=True, text=True, timeout=30)
        (a.output / (name + '.log')).write_text(process.stdout + process.stderr, encoding='utf-8')
        assert (process.returncode == 0) == expected_success, name
        if not expected_success:
            assert process.returncode == 2 and 'Native replay rejected:' in process.stderr, name + ': uncontrolled crash'
        assert expected_success or not report.exists(), name + ': published success after failure'
        records.append(dict(name=name, passed=True))
        return json.loads(report.read_text()) if expected_success else None

    report = run('computed-transition', capture, True)
    assert report['stages'][0]['nodes'][0]['position'] == 12
    assert report['stages'][1]['nodes'][0]['retentionSentinel'] == 0
    assert report['stages'][1]['heightMap'] == []
    assert len(report['writes']) == 1
    for name in ('unknown-presence', 'missing-field', 'duplicate-entity', 'duplicate-node',
                 'unsupported-component', 'unsupported-stage', 'empty-stages', 'wrong-version'):
        data = copy.deepcopy(capture)
        components = data['entities'][0]['components']
        if name == 'unknown-presence': del components['Temp']
        elif name == 'missing-field': del components['NodeGeometry']['flatness']
        elif name == 'duplicate-entity': data['entities'].append(copy.deepcopy(data['entities'][0]))
        elif name == 'duplicate-node': data['nodes'].append([1, 1])
        elif name == 'unsupported-component': components['PretendTerrain'] = {}
        elif name == 'unsupported-stage': data['stages'] = ['PretendPipeline']
        elif name == 'empty-stages': data['stages'] = []
        else: data['gameSha256'] = '0' * 64
        run(name, data, False)
    def surface(height, x):
        return dict(start=dict(left=[[-x,height,0],[-x,height+.5,1],[-x,height,2],[-x,height,3]],
                               right=[[x,height,0],[x,height+.5,1],[x,height,2],[x,height,3]]),
                    end=dict(left=[[-x,height,3],[-x,height,4],[-x,height,5],[-x,height,6]],
                             right=[[x,height,3],[x,height,4],[x,height,5],[x,height,6]]))
    pair = copy.deepcopy(capture)
    pair['stages'] = ['FlattenNodeGeometry', 'FinishEdgeGeometry']
    pair['finishEdges'] = [[2,1], [3,1]]
    node = pair['entities'][0]['components']
    node['NodeGeometry'].update(position=1, boundsMin=[0,0,0])
    node['ConnectedEdge'] = [[2,1], [3,1]]
    pair['entities'].append(dict(id=[101,1], components={'NetCompositionData':
        dict(flags=dict(general=0,left=0,right=0),width=2,state=0,heightMin=-1,heightMax=2)}))
    for edge_id, height, side in ((2,2,-1),(3,0,1)):
        pair['entities'].append(dict(id=[edge_id,1], components={
            'Edge':dict(start=[1,1],end=[10+edge_id,1]),'PrefabRef':[100,1],
            'Composition':dict(edge=[101,1],start=[101,1],end=[101,1]),
            'Temp':None,'Hidden':None,'Owner':None,'EdgeGeometry':surface(height,side)}))
    finished = run('flatten-then-finish', pair, True)
    def check_finished(report):
        for edge in report['stages'][1]['edges']:
            assert edge['startLeft'][0][1] == edge['startRight'][0][1] == 1
            assert edge['startLeft'][1][1] == edge['startRight'][1][1] == 1.5
            assert all(length > 0 for length in edge['lengths'])
        assert len(report['writes']) == 2
    check_finished(finished)
    corrupt = copy.deepcopy(finished)
    corrupt['stages'][1]['edges'][0]['startLeft'][0][1] += 1
    try:
        check_finished(corrupt)
    except AssertionError:
        records.append(dict(name='incorrect-result-rejected',passed=True))
    else:
        raise AssertionError('Incorrect result escaped differential assertion')
    for name in ('finish-before-flatten', 'missing-owner', 'missing-height-range', 'duplicate-finish-edge', 'old-schema'):
        data = copy.deepcopy(pair)
        if name == 'finish-before-flatten': data['stages'].reverse()
        elif name == 'missing-owner': del data['entities'][-1]['components']['Owner']
        elif name == 'missing-height-range': del data['entities'][2]['components']['NetCompositionData']['heightMin']
        elif name == 'duplicate-finish-edge': data['finishEdges'].append([2,1])
        else: data['schemaVersion'] = 1
        run(name, data, False)
    (a.output / 'summary.json').write_text(json.dumps(dict(passed=True, cases=records,
        scope='Synthetic capture/schema contract; native differential pending'), indent=2))
    print(f'PASS {len(records)} JSON world capture cases')


if __name__ == '__main__': main()
