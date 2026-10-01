"""Plot retained native authored curves before/after Slope; optionally align historical XZ matches.
The plots are centerlines, not rendered surfaces or a terrain-causation test.
"""
import argparse,json,math,runpy
from pathlib import Path

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--capture',required=True);p.add_argument('--historical');p.add_argument('--output',required=True)
    a=p.parse_args();u=runpy.run_path(str(Path(__file__).with_name('summarize-profile-experiment.py')))
    data=json.loads((Path(a.capture)/'comparison.json').read_text())
    before=u['oriented_edges'](data['beforeEdges'],data['path']);after=u['oriented_edges'](data['afterEdges'],data['path'])
    series=[('Curve only',[e['curve'] for e in before]),('New Constant Slope',[e['curve'] for e in after])]
    if a.historical:
        old=json.loads(Path(a.historical).read_text())['result']['edges'];matched=[];used=set()
        for edge in after:
            choices=[]
            for e in old:
                if e['index'] in used:continue
                for cs in (e['curve'],list(reversed(e['curve']))):
                    error=max(math.hypot(x['x']-y['x'],x['z']-y['z']) for x,y in zip(cs,edge['curve']))
                    choices.append((error,e['index'],cs))
            error,key,cs=min(choices,key=lambda c:c[0])
            if error>.001:raise ValueError('Historical alignment does not match current native XZ')
            used.add(key);matched.append(cs)
        series.insert(1,('Previous Constant Slope',matched))
    import matplotlib.pyplot as plt
    fig,axes=plt.subplots(4,1,figsize=(10,11))
    colors=['#929292','#bd562e','#176eb2'] if len(series)==3 else ['#929292','#176eb2']
    for (label,curves),color in zip(series,colors):
        station=0
        for i,c in enumerate(curves):
            ts=[j/200 for j in range(201)];pts=[u['m']['evaluate'](c,t) for t in ts];stations=[station]
            for x,y in zip(pts,pts[1:]):stations.append(stations[-1]+math.hypot(x['x']-y['x'],x['z']-y['z']))
            axes[0].plot([x['x'] for x in pts],[x['z'] for x in pts],color=color,label=label if i==0 else None)
            axes[1].plot(stations,[x['y'] for x in pts],color=color)
            axes[2].plot(stations,[u['grade'](c,t) for t in ts],color=color)
            if label!='Curve only':axes[3].plot(stations,[u['grade'](c,t) for t in ts],color=color)
            station=stations[-1]
    axes[0].set_ylabel('World Z (m)');axes[0].set_xlabel('World X (m)');axes[0].set_aspect('equal',adjustable='datalim');axes[0].legend(loc='upper left')
    axes[1].set_ylabel('Authored elevation (m)');axes[2].set_ylabel('Grade (%)');axes[3].set_ylabel('Slope comparison (%)');axes[3].set_xlabel('Horizontal station on selected edges (m; junction gaps excluded)')
    for ax in axes:ax.grid(alpha=.25)
    fig.suptitle('Same off-ramp horizontal alignment: native authored geometry')
    fig.tight_layout();fig.savefig(a.output,dpi=130)
if __name__=='__main__':main()
