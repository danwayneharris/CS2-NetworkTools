"""Compare native/replay initialization at float32 precision, with no metric tolerance."""
import argparse
import copy
import json
import struct
from pathlib import Path

FIELDS=('position','flatness','offset','retentionSentinel')

def compare(expected, actual):
    left={tuple(n['id']):n for n in expected};right={tuple(n['id']):n for n in actual}
    if len(left)!=len(expected) or len(right)!=len(actual): raise ValueError('Duplicate identity')
    if left.keys()!=right.keys(): raise ValueError('Node identity sets differ')
    differences=[]
    for e,n in left.items():
        for k in FIELDS:
            a,b=n[k],right[e][k]
            if struct.pack('<f',a)!=struct.pack('<f',b): differences.append(dict(entity=e,field=k,native=a,replay=b,delta=b-a))
    return dict(passed=not differences,nodes=len(left),comparedFloat32Values=len(left)*len(FIELDS),
                comparison='Exact IEEE754 binary32 after JSON decoding',differences=differences)

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('directory');p.add_argument('--output',required=True)
    a=p.parse_args();d=Path(a.directory);out=Path(a.output)
    if out.exists(): raise FileExistsError('Refusing to overwrite evidence')
    evidence=json.loads((d/'expected.json').read_text()); replay=json.loads((d/'replay.json').read_text())['stages'][0]
    expected=evidence['expectedNodes'];actual=replay['nodes']
    result=compare(expected,actual)
    if 'expectedHeightMap' in evidence:
        def maprows(rows): return [dict(id=r['key'],**dict(zip(FIELDS,r['value']))) for r in rows]
        result['heightMap']=compare(maprows(evidence['expectedHeightMap']),maprows(replay['heightMap']))
        result['passed'] &= result['heightMap']['passed']
    bad=copy.deepcopy(actual);bad[0]['position']+=1
    if compare(expected,bad)['passed']: raise AssertionError('Deliberately incorrect output accepted')
    try: compare(expected,actual[1:])
    except ValueError: pass
    else: raise AssertionError('Missing node accepted')
    result['negativeTests']=['Wrong output rejected','Missing identity rejected']
    out.write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
    return 0 if result['passed'] else 1

if __name__=='__main__': raise SystemExit(main())
