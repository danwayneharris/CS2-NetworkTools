"""Offline single-track-per-arm curvature-gate evidence, not a full junction verdict."""
import argparse
import json
import runpy
import sys
from pathlib import Path
sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
connect_positions = runpy.run_path(str(HERE / 'rail-composition-inputs.py'))['connect_positions']
metric = runpy.run_path(str(HERE / 'analyze-rail-junction.py'))['metric']


def analyze(path):
    packet = json.loads(Path(path).read_text(encoding='utf-8-sig'))
    if not packet.get('ok'):
        raise ValueError('Failed snapshot request')
    snapshot = packet['result']
    points = connect_positions(snapshot)
    # Native grouping is unnecessary only for this restricted one-lane-per-direction case.
    for edge in {p['edge']['index'] for p in points}:
        for role in ('source', 'target'):
            if sum(p['edge']['index'] == edge and p[role] for p in points) != 1:
                raise ValueError('Requires exactly one source and target track per arm')
    rows = []
    for source in points:
        for target in points:
            if not source['source'] or not target['target'] or source['edge'] == target['edge']:
                continue
            values = metric(source['position'], source['inwardTangent'],
                            target['position'], tuple(-x for x in target['inwardTangent']))
            present = any(l.get('owner') == snapshot['junction'] and 'track' in l
                          and l['start']['ownerIndex'] == source['edge']['index']
                          and l['end']['ownerIndex'] == target['edge']['index']
                          for l in snapshot['lanes'])
            limit = source['maxCurviness']
            rows.append(dict(sourceEdge=source['edge'], targetEdge=target['edge'],
                             **values, limit=limit, passesCurvatureGate=values['curviness'] <= limit,
                             observedConnector=present))
    return dict(junction=snapshot['junction'], connections=rows,
                scope='Fresh-composition curvature gate only; excludes other native gates and target sorting')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('snapshots', nargs='+')
    parser.add_argument('--output')
    args = parser.parse_args()
    result = json.dumps([analyze(p) for p in args.snapshots], indent=2)
    if args.output:
        Path(args.output).write_text(result + '\n', encoding='utf-8')
    print(result)
