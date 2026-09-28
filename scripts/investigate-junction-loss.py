"""Replay a captured loss and quantify connection-space corrections, not a road fit."""
import json
import math
from pathlib import Path
import runpy
import sys
sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
analyze = runpy.run_path(str(HERE / 'analyze-rail-composition.py'))['analyze']
rules = runpy.run_path(str(HERE / 'rail-junction-constraints.py'))
DATA = HERE.parent / 'NetworkTools.docs/session-notes/captures/broken-preview-20260928'


def investigate():
    before = analyze(DATA / '54998-get_junction_snapshot-before.json')
    after = analyze(DATA / '54998-applied.json')
    required = rules['obligations'](before)
    by_pair = {rules['pair'](r): r for r in before['connections']}
    evaluation = rules['evaluate'](after, required)
    rows = []
    for r in after['connections']:
        pair = rules['pair'](r)
        if pair not in required: continue
        bound = next(b for b in evaluation['constraints']
                     if tuple(b['source']) == pair[0] and tuple(b['target']) == pair[1])
        rows.append(dict(source=pair[0], target=pair[1], before=by_pair[pair], after=r,
                         requiredAngleReductionDegrees=max(0, r['angleDegrees']-bound['maximumAngleDegrees']),
                         requiredSpanIncreaseMetres=max(0, bound['minimumSpanMetres']-r['distanceMetres'])))
    return dict(requiredConnections=len(required), baseline=rules['evaluate'](before, required),
                candidate=evaluation, connections=rows,
                meaning='Fixed-span/fixed-angle bounds in generated lane-connection space. '
                        'Not authored Bezier control corrections; requires fresh native geometry.')


if __name__ == '__main__':
    report = investigate()
    output = DATA / 'constraint-loss-analysis.json'
    output.write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    for r in report['connections']:
        if not r['after']['observedConnector']:
            print(json.dumps(r, indent=2))
    print('Saved', output)
