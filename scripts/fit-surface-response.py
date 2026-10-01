"""Fit a restricted ramp surface using measured native affine responses.
Requires numpy; no game access. Does not certify lane/mesh behavior or native
rebuild retention. Probe points use the captured first-edge cutback branch (>1).
"""
import argparse
import json
import runpy
from pathlib import Path
import numpy as np


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('directory')
    args=p.parse_args()
    root=Path(args.directory)
    values=json.loads((root/'responses.json').read_text(encoding='utf-8-sig'))
    meta=json.loads((root/'parameters.json').read_text())
    middle=runpy.run_path(str(Path(__file__).with_name('replay-native-middle-height.py')))
    length=middle['horizontal_length']
    def asdict(c):return [dict(zip(('x','y','z'),v)) for v in c]
    originals={side:np.array(values['base-'+side]).reshape(8,3) for side in ('left','right')}
    lengths={side:[length(asdict(c[:4])),length(asdict(c[4:]))] for side,c in originals.items()}
    anchor=float(np.mean([c[0,1] for c in originals.values()]))
    end=602.321167
    center=(originals['left']+originals['right'])/2
    center_lengths=[length(asdict(center[:4])),length(asdict(center[4:]))]
    grade=(end-anchor)/(sum(center_lengths)+sum(meta['lengths'][1:]))
    matrices=[];targets=[];base=[]
    for side,c in originals.items():
        derivative=np.stack([np.array(values[name+'-'+side]).reshape(8,3)[:,1]-c[:,1] for name in ('height','b','c')],axis=1)
        ls,le=center_lengths
        target=[]
        # Constant grade belongs to the road center profile. Inner and outer
        # boundaries have different arc lengths around a bend.
        for half,start,span in [(center[:4],0,ls),(center[4:],ls,le)]:
            h0=np.linalg.norm(half[1,[0,2]]-half[0,[0,2]])
            h1=np.linalg.norm(half[3,[0,2]]-half[2,[0,2]])
            stations=np.array([start,start+h0,start+span-h1,start+span])
            target.extend(anchor+grade*stations+(c[0,1]-anchor)*(1-stations/(ls+le)))
        matrices.append(derivative);targets.extend(target);base.extend(c[:,1])
    matrix=np.concatenate(matrices)
    parameters=np.linalg.lstsq(matrix,np.array(targets)-base,rcond=None)[0]
    predicted=np.array(base)+matrix@parameters
    report={'responses':matrix.tolist(),'residual':(np.array(targets)-base).tolist(),'parameters':parameters.tolist(),'targetGrade':grade,'maxTargetHeightResidual':float(np.max(np.abs(predicted-targets))),'rmsTargetHeightResidual':float(np.sqrt(np.mean((predicted-targets)**2))),'boundaries':{}}
    for i,(side,c) in enumerate(originals.items()):
        c=c.copy();c[:,1]=predicted[8*i:8*i+8]
        bounds=middle['bounds'](c[0,1],c[-1,1],*lengths[side],.2,8)
        report['boundaries'][side]={'curves':c.tolist(),'limiter':bounds,'middleY':float(c[3,1]),'limiterWouldMove':not(bounds['final'][0]-.05<=c[3,1]<=bounds['final'][1]+.05)}
    (root/'fit.json').write_text(json.dumps(report,indent=2))
    print(json.dumps({k:v for k,v in report.items() if k!='boundaries'},indent=2))
    print(json.dumps({k:v['limiterWouldMove'] for k,v in report['boundaries'].items()}))

if __name__=='__main__':main()
