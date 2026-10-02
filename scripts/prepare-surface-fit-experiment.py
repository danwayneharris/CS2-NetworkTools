"""Prepare pure native-math response probes for the captured five-edge ramp.
Research-only: fixed XZ and observed cut positions, SmoothElevation/zero-flatness,
retained outer junction heights. Outputs expressions; never edits the live world.
"""
import argparse
import copy
import json
import runpy
import subprocess
import sys
from pathlib import Path


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('capture')
    p.add_argument('--output', required=True)
    args = p.parse_args()
    root = Path(args.output)
    root.mkdir(parents=True, exist_ok=False)
    data = json.loads(Path(args.capture).read_text())
    order = [57022, 57034, 57031, 57028, 57038]
    edges = [next(e for e in data['edges'] if e['index'] == i) for i in order]
    length = runpy.run_path(str(Path(__file__).with_name('replay-native-middle-height.py')))['horizontal_length']
    lengths = [length(e['curve']) for e in edges]
    total = sum(lengths)
    coefficients = {}
    distance = 0.
    for e, size in zip(edges, lengths):
        coefficients[e['startNode']['index']] = 1 - distance / total
        distance += size
        coefficients[e['endNode']['index']] = 1 - distance / total
    expressions = {}
    for label, parameters in [('base', [0,0,0]), ('height', [1,0,0]), ('b', [0,1,0]), ('c', [0,0,1])]:
        d = copy.deepcopy(data)
        for e in d['edges']:
            i = order.index(e['index'])
            curve = e['curve']
            start = coefficients[e['startNode']['index']]
            end = coefficients[e['endNode']['index']]
            import math
            h0 = math.hypot(curve[1]['x']-curve[0]['x'], curve[1]['z']-curve[0]['z'])
            h1 = math.hypot(curve[3]['x']-curve[2]['x'], curve[3]['z']-curve[2]['z'])
            weights = [start, start-h0/total, end+h1/total, end]
            for c,w in zip(curve,weights): c['y'] += parameters[0]*w
            if i == 0:
                curve[0]['y'] -= parameters[0]
                curve[1]['y'] += parameters[1]
                curve[2]['y'] += parameters[2]
        updated = {e['index']:e for e in d['edges']}
        for snapshot in d['snapshots']:
            for o in snapshot['owners']:
                if o.get('curve') and o['index'] in updated:
                    o['curve'] = copy.deepcopy(updated[o['index']]['curve'])
                elif o.get('position') and o['index'] in coefficients and o['index'] != edges[0]['startNode']['index']:
                    o['position']['y'] += parameters[0]*coefficients[o['index']]
        capture = root / (label+'.json')
        capture.write_text(json.dumps(d))
        out = root / (label+'-expressions.json')
        subprocess.run([sys.executable, str(Path(__file__).with_name('prepare-native-cut-replay.py')), str(capture), '--output', str(out)], check=True)
        raw = json.loads(out.read_text())
        owner = next(o for s in d['snapshots'] for o in s['owners'] if o['index']==order[0])
        for side, expression in raw.items():
            # Keep offset curves a/b and discard the diagnostic suffix.
            code = expression.split('var p=')[0]
            def vec(q): return 'new Unity.Mathematics.float3('+','.join(str(q[k])+'f' for k in ('x','y','z'))+')'
            start = owner['edgeGeometry']['start'][side][0]
            end = owner['edgeGeometry']['end'][side][-1]
            code += 'var p='+vec(start)+';var q='+vec(end)+';'
            code += 'var da=Colossal.Mathematics.MathUtils.Distance(a.xz,p.xz,out var ta);var db=Colossal.Mathematics.MathUtils.Distance(b.xz,p.xz,out var tb);var so=da<db?ta:1f+tb;'
            code += 'var ea=Colossal.Mathematics.MathUtils.Distance(a.xz,q.xz,out var ua);var eb=Colossal.Mathematics.MathUtils.Distance(b.xz,q.xz,out var ub);var eo=ea<eb?2f-ua:1f-ub;'
            code += 'var length=Colossal.Mathematics.MathUtils.Length(b.xz,new Colossal.Mathematics.Bounds1(so-1f,1f-eo));var co=1f-(2f-so-eo)*0.5f/(length/8f+1f);var first=Colossal.Mathematics.MathUtils.Cut(b,new Unity.Mathematics.float2(so-1f,so-co));'
            code += 'var last=Colossal.Mathematics.MathUtils.Cut(b,new Unity.Mathematics.float2(so-co,1f-eo));'
            # Retained junction boundary: preserve A and translate B by the same delta.
            code += 'first.b.y=first.b.y+p.y-first.a.y;first.a.y=p.y;'
            fields = [f'first.{v}.{k}' for v in 'abcd' for k in ('x','y','z')]+[f'last.{v}.{k}' for v in 'abcd' for k in ('x','y','z')]
            code += '$"'+','.join('{'+v+'}' for v in fields)+'"'
            expressions[label+'-'+side] = code
    (root/'probes.json').write_text(json.dumps(expressions,indent=2))
    (root/'parameters.json').write_text(json.dumps({'order':order,'lengths':lengths,'totalLength':total,'nodeWeights':coefficients},indent=2))

if __name__ == '__main__':
    main()
