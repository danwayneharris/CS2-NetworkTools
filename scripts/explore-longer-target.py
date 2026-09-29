"""Bounded longer-route research, not the shipped fitter. Requires matplotlib for plots.

Straight 100 m baseline with fixed position, horizontal tangent and zero endpoint
curvature. Mirror choices have equal length; length alone cannot choose the side.
"""
import argparse
import json
import math
from pathlib import Path


def sample(amplitude, count=1000):
    return [(100*t, 64*amplitude*t**3*(1-t)**3) for t in (i/count for i in range(count+1))]


def arc_length(amplitude):
    points=sample(amplitude)
    return sum(math.dist(a,b) for a,b in zip(points,points[1:]))


def solve(extra_length, corridor=20):
    if not math.isfinite(extra_length) or extra_length<0 or not math.isfinite(corridor) or corridor<0:
        raise ValueError('Invalid length/corridor')
    target=100+extra_length
    if arc_length(corridor)<target-1e-8:
        raise ValueError('Length infeasible within corridor for this target family')
    lo,hi=0,corridor
    for _ in range(55):
        mid=(lo+hi)/2
        if arc_length(mid)<target: lo=mid
        else: hi=mid
    return (lo+hi)/2


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',required=True)
    args=parser.parse_args()
    root=Path(args.output); root.mkdir(parents=True,exist_ok=True)
    rows=[]
    for extra in (0,2,5,10,20):
        try:
            a=solve(extra)
            rows.append(dict(extraMetres=extra,amplitude=a,length=arc_length(a),feasible=True))
        except ValueError as e:
            rows.append(dict(extraMetres=extra,feasible=False,reason=str(e)))
    (root/'longer-target.json').write_text(json.dumps(rows,indent=2))
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    fig,ax=plt.subplots(figsize=(10,4))
    for row in rows:
        if row['feasible']:
            points=sample(row['amplitude'])
            ax.plot(*zip(*points),label=f"+{row['extraMetres']} m")
    mirrored=sample(-solve(5))
    ax.plot(*zip(*mirrored),'--',label='+5 m, equally valid opposite side')
    ax.axhline(20,color='gray',linestyle=':'); ax.axhline(-20,color='gray',linestyle=':')
    ax.set(xlabel='Along-route distance (m)',ylabel='Lateral distance (m)',
        title='Longer-route research: fixed boundary tangents, ±20 m corridor')
    ax.legend(fontsize=8); ax.set_aspect('equal'); fig.tight_layout()
    fig.savefig(root/'longer-target.png',dpi=140)
    fig.savefig(root/'longer-target.svg')
    print(json.dumps(rows,indent=2))


if __name__=='__main__': main()
