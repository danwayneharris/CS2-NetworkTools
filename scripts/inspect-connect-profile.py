"""Read a captured connect_state response; report geometry, never mutate the game.

This is diagnostic, not an acceptance oracle. Projection inversion assumes the
captured authored curve is monotone along its chord, as the production gate does.
"""
import argparse
import json
import math
from pathlib import Path


def lerp(a, b, t):
    return [x + (y - x) * t for x, y in zip(a, b)]


def split(c, t):
    ab, bc, cd = (lerp(c[i], c[i + 1], t) for i in range(3))
    abc, bcd = lerp(ab, bc, t), lerp(bc, cd, t)
    mid = lerp(abc, bcd, t)
    return [c[0], ab, abc, mid], [mid, bcd, cd, c[3]]


def grade(a, b):
    length = math.hypot(b[0] - a[0], b[2] - a[2])
    return (b[1] - a[1]) / length if length > 1e-9 else None


def inspect(state):
    config = state['authoredCandidate']
    if config['ComplexProfile']:
        raise ValueError('This diagnostic currently supports Simple Curve only')
    authored = [config['Curve' + key + 'Position'] for key in
                ('StartPoint', 'StartControlPoint', 'EndControlPoint', 'EndPoint')]
    chord = [authored[3][i] - authored[0][i] for i in (0, 2)]
    denominator = sum(v * v for v in chord)
    if denominator < 1e-8:
        raise ValueError('Degenerate authored chord')

    def projection(p):
        return sum((p[i] - authored[0][i]) * v for i, v in zip((0, 2), chord)) / denominator

    def parameter(p):
        target = projection(p)
        if target <= 0:
            return 0.0
        if target >= 1:
            return 1.0
        lo, hi = 0.0, 1.0
        for _ in range(60):
            mid = (lo + hi) / 2
            if projection(split(authored, mid)[0][-1]) < target:
                lo = mid
            else:
                hi = mid
        return (lo + hi) / 2

    rows, original = [], None
    for item in state['previewObservation']:
        if 'm_Original' in item:
            original = item['m_Original']
        if not all(k in item for k in 'abcd'):
            continue
        c = [item[k] for k in 'abcd']
        if original is None:
            raise ValueError('Curve without original identity')
        if original['Index'] or original['Version']:
            continue
        lo, hi = parameter(c[0]), parameter(c[3])
        if hi < lo:
            lo, hi, c = hi, lo, c[::-1]
        left = split(authored, hi)[0]
        expected = split(left, lo / hi)[1] if hi else left
        rows.append({'interval': [lo, hi], 'curve': c,
                     'maxXZControlError': max(math.hypot(p[0]-q[0], p[2]-q[2]) for p,q in zip(c,expected)),
                     'maxYControlError': max(abs(p[1]-q[1]) for p,q in zip(c,expected)),
                     'startGrade': grade(c[0],c[1]), 'endGrade': grade(c[2],c[3])})
    rows.sort(key=lambda r: r['interval'])
    if not rows:
        raise ValueError('No new native preview curves')
    return {'rejection': state['rejectionReason'], 'build': state['build'],
            'authoredStartGrade': grade(authored[0],authored[1]),
            'authoredEndGrade': grade(authored[2],authored[3]),
            'outerEndpointHeightErrors': [abs(rows[0]['curve'][0][1]-authored[0][1]),
                                          abs(rows[-1]['curve'][3][1]-authored[3][1])],
            'internalHeightGaps': [abs(a['curve'][3][1]-b['curve'][0][1]) for a,b in zip(rows,rows[1:])],
            'nativeCurves': rows}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('capture', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    record = json.loads(args.capture.read_text(encoding='utf-8-sig'))
    result = inspect(record.get('result', record))
    text = json.dumps(result, indent=2)
    if args.output:
        args.output.write_text(text + '\n', encoding='utf-8')
    print(text)
