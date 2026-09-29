"""Side-bias UX study, not the production fitter or a native-feasibility test.

Uses a monotone chord-aligned example. A sextic envelope preserves both endpoints
and the first two derivatives of its underlying route. World/chord side is fixed
in this plot; selection-relative and screen-relative side remain UX decisions.
"""
import argparse
import json
import math
from pathlib import Path


def original(t):
    return 8*math.sin(math.pi*t)**2 + 10*math.sin(6*math.pi*t)*math.sin(math.pi*t)**2


def target(t):
    return 8*math.sin(math.pi*t)**2


def sample(bias, coupled, amplitude=12, count=1000):
    if not math.isfinite(bias) or not -1 <= bias <= 1:
        raise ValueError('Bias outside [-1, 1]')
    if not math.isfinite(amplitude) or amplitude < 0 or count < 2:
        raise ValueError('Invalid corridor or sample count')
    strength=abs(bias) if coupled else 1
    return [(100*t, (1-strength)*original(t)+strength*target(t)
             + bias*amplitude*64*t**3*(1-t)**3)
            for t in (i/count for i in range(count+1))]


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',required=True)
    args=parser.parse_args();root=Path(args.output);root.mkdir(parents=True,exist_ok=True)
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    fig,axes=plt.subplots(2,1,figsize=(10,7),sharex=True,sharey=True)
    rows=[]
    for ax,coupled in zip(axes,(True,False)):
        for bias in (-1,-.5,0,.5,1):
            points=sample(bias,coupled)
            ax.plot(*zip(*points),label=f'{bias:+.1f}',linewidth=2 if bias==0 else 1)
            rows.append(dict(coupled=coupled,bias=bias,
                length=sum(math.dist(a,b) for a,b in zip(points,points[1:])),
                centerIsOriginal=coupled,amplitudeLimitMetres=12))
        ax.set_title('One slider: center restores original, smoothing grows with |bias|' if coupled
                     else 'Separate controls: fixed smoothing strength, center has zero side bias')
        ax.set_ylabel('Lateral position (m)');ax.legend(ncol=5,fontsize=8);ax.grid(alpha=.2)
    axes[-1].set_xlabel('Along-chord distance (m)')
    fig.suptitle('Research only: side choice is not equivalent to route-length choice')
    fig.tight_layout();fig.savefig(root/'side-bias.png',dpi=140);fig.savefig(root/'side-bias.svg')
    (root/'side-bias.json').write_text(json.dumps(rows,indent=2)+'\n')
    # Research invariants only: these are not production geometry/native checks.
    assert all(p[1] == original(i/1000) for i,p in enumerate(sample(0,True)))
    for coupled in (True,False):
        left,right=sample(-1,coupled),sample(1,coupled)
        assert all(abs((a[1]+b[1])/2-target(i/1000))<1e-10 for i,(a,b) in enumerate(zip(left,right)))
        assert max(abs(a[1]-target(i/1000)) for i,a in enumerate(right))<=12+1e-10
    print('Research center, mirror and corridor checks passed; no game validation.')


if __name__=='__main__':main()
