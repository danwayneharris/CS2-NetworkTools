"""Exercise the original-binary value-stage seam; no live game access.

This validates the harness and simple analytic controls, not the anchor pipeline.
Pass the already-built executable DLL; fixtures/reports remain in a new output dir.
"""
import argparse
import copy
import json
import subprocess
from pathlib import Path

GAME_HASH = 'AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dll', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=False)
    start = [[0, 0, 0], [10, 0, 0], [20, 0, 0], [30, 0, 0]]
    end = [[30, 0, 0], [40, 0, 0], [50, 0, 0], [60, 0, 0]]
    cases = [dict(id=stage, stage=stage, start=start, end=end, maxSlope=.2,
                  width=10, startOffset=0, endOffset=0, cutOffset=1)
             for stage in ('StraightenMiddleHeights', 'LimitMiddleHeights', 'CalculateCutOffset', 'Cut')]
    fixture = dict(schemaVersion=1, gameSha256=GAME_HASH, cases=cases)
    checks = []

    def run(name, data, success):
        source = args.output / (name + '.fixture.json')
        output = args.output / (name + '.result.json')
        source.write_text(json.dumps(data), encoding='utf-8')
        child = subprocess.run(['dotnet', str(args.dll.resolve()), '--stages', str(source), str(output)],
                               capture_output=True, text=True, timeout=30)
        (args.output / (name + '.log')).write_text(child.stdout + child.stderr, encoding='utf-8')
        if (child.returncode == 0) != success:
            raise AssertionError(name + ': unexpected exit ' + str(child.returncode))
        if not success and (child.returncode != 2 or 'Native replay rejected:' not in child.stderr):
            raise AssertionError(name + ': expected controlled diagnostic, not a process crash')
        if not success and output.exists():
            raise AssertionError(name + ': failed capture published success evidence')
        checks.append(name)
        return json.loads(output.read_text()) if success else None

    result = run('analytic-control', fixture, True)
    rows = {r['id']: r['result'] for r in result['cases']}
    for stage in ('StraightenMiddleHeights', 'LimitMiddleHeights'):
        assert rows[stage]['start'] == start and rows[stage]['end'] == end
    assert rows['Cut']['curve'] == start
    assert abs(rows['CalculateCutOffset']['value'] - 6 / 7) < 1e-6
    for name in ('missing-input', 'unknown-stage', 'wrong-game', 'empty-cases', 'bad-control'):
        changed = copy.deepcopy(fixture)
        if name == 'missing-input': del changed['cases'][1]['maxSlope']
        elif name == 'unknown-stage': changed['cases'][0]['stage'] = 'PretendEverythingPassed'
        elif name == 'wrong-game': changed['gameSha256'] = '0' * 64
        elif name == 'empty-cases': changed['cases'] = []
        else: changed['cases'][0]['start'][0] = [0, 0]
        run(name, changed, False)
    # Different inputs really execute, rather than returning captured fixture outputs.
    changed = copy.deepcopy(fixture)
    changed['cases'] = [changed['cases'][0]]
    for point in changed['cases'][0]['start'] + changed['cases'][0]['end']:
        point[1] += 7
    result = run('translated-control', changed, True)['cases'][0]['result']
    assert all(p[1] == 7 for p in result['start'] + result['end'])
    summary = dict(scope='offline original assembly seam; no native city differential validation',
                   passed=True, checks=checks)
    (args.output / 'summary.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
    print(f'PASS: {len(checks)} native-stage harness cases plus analytic output assertions')


if __name__ == '__main__':
    main()
