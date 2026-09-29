"""Bounded XZ replay of CS2 1.6.2f1 NodeAlignSystem for captured rail junctions.

Assumes all incident curves participate in the same layer and node is not
Standalone. Double-precision diagnostic; not bit-exact Unity float execution.
Source: installed NodeAlignSystem.AlignNode and MathUtils.Distance(Line2 segments).
"""
import argparse
import itertools
import json
import math
from pathlib import Path


def add(a,b): return tuple(x+y for x,y in zip(a,b))
def sub(a,b): return tuple(x-y for x,y in zip(a,b))
def scale(a,t): return tuple(x*t for x in a)
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def xz(p): return p['x'],p['z']
def cross(a,b): return a[0]*b[1]-a[1]*b[0]


def closest(a,b,c,d):
    def project(p,u,v):
        delta=sub(v,u); den=dot(delta,delta)
        t=max(0,min(1,dot(sub(p,u),delta)/den)) if den else 0
        return add(u,scale(delta,t))
    candidates=[(a,project(a,c,d)),(b,project(b,c,d)),
                (project(c,a,b),c),(project(d,a,b),d)]
    u,v=sub(b,a),sub(d,c);den=cross(u,v)
    if den:
        t=cross(sub(c,a),v)/den;s=cross(sub(c,a),u)/den
        if 0<=t<=1 and 0<=s<=1:
            p=add(a,scale(u,t));return p,p
    return min(candidates,key=lambda pair:math.dist(*pair))


def predict(position,curves):
    lines=[]
    for endpoint,handle in curves:
        tangent=sub(handle,endpoint);length=math.hypot(*tangent)
        if length>1e-8:lines.append((sub(endpoint,position),scale(tangent,1/length)))
    if len(lines)<2:raise ValueError('Requires two nondegenerate incident tangents')
    weighted=(0,0);total=0
    for (a,u),(b,v) in itertools.combinations(sorted(lines),2):
        cosine=abs(dot(u,v));weight=1.01-cosine
        if cosine>0.999:
            pair=add(a,b)
        else:
            extent=math.dist(a,b)*(1-cosine)
            p,q=closest(add(a,scale(u,extent)),sub(a,scale(u,extent)),
                        add(b,scale(v,extent)),sub(b,scale(v,extent)))
            pair=add(p,q)
        weighted=add(weighted,scale(pair,weight));total+=2*weight
    return add(position,scale(weighted,1/total))


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--before',required=True);parser.add_argument('--preview',required=True)
    parser.add_argument('--output',required=True);args=parser.parse_args()
    before=json.loads(Path(args.before).read_text())['result']
    preview=json.loads(Path(args.preview).read_text())['result']['connectedSnapshot']
    node=before['junction']['index'];owners={o['index']:o for o in before['owners']}
    origin=xz(owners[node]['position']);curves=[]
    for o in preview['owners']:
        if not o.get('curve'):continue
        original=owners[o['temp']['original']['index']];curve=o['curve']
        at_end=original['endNode']['index']==node
        curves.append((xz(curve[3 if at_end else 0]),xz(curve[2 if at_end else 1])))
    observed=xz(next(o for o in preview['owners'] if o['index']==preview['junction']['index'])['position'])
    predicted=predict(origin,curves)
    report=dict(original=origin,predicted=predicted,observed=observed,
        predictedShift=math.dist(origin,predicted),observedShift=math.dist(origin,observed),
        predictionError=math.dist(predicted,observed),
        limits='Same-layer non-Standalone XZ diagnostic; double precision, not native bit parity.')
    Path(args.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))


if __name__=='__main__':main()
