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
    capture = dict(schemaVersion=1, gameSha256=HASH, loaded=False, nodes=[[1, 1]],
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
    (a.output / 'summary.json').write_text(json.dumps(dict(passed=True, cases=records,
        scope='Synthetic capture/schema contract; native differential pending'), indent=2))
    print(f'PASS {len(records)} JSON world capture cases')


if __name__ == '__main__': main()
