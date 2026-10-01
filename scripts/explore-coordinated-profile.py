"""Offline composition experiment: refit height after captured Curve output.
No game mutation. Outer heights/offsets stay fixed; interior elevations may change.
This does not define split/junction pin semantics or certify a joint native preview.
"""
import argparse,json,runpy,math
from pathlib import Path

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--capture',required=True);p.add_argument('--output',required=True);a=p.parse_args()
    root=Path(a.capture);out=Path(a.output);out.mkdir(parents=True,exist_ok=False)
    u=runpy.run_path(str(Path(__file__).with_name('summarize-profile-experiment.py')))
    fit=runpy.run_path(str(Path(__file__).with_name('explore-vertical-profile.py')))['fit']
    def last(pattern,key):return json.loads(sorted(root.glob(pattern))[-1].read_text())['result'][key]
    nodes=last('*-get_network.json','nodes');edges=last('*-get_network_edges.json','edges')
    path=json.loads(next(root.glob('*-trace_network.json')).read_text())['result'];selected=u['oriented_edges'](edges,path)
    nodebyid={n['index']:n for n in nodes};ids=[selected[0]['startNode' if selected[0]['pathForward'] else 'endNode']['index']]+[e['endNode' if e['pathForward'] else 'startNode']['index'] for e in selected]
    positions=[[nodebyid[i]['position'][k] for k in ('x','y','z')] for i in ids]
    curves=[[[p[k] for k in ('x','y','z')] for p in e['curve']] for e in selected]
    candidate,heights,grade=fit(curves,positions)
    output=[{**e,'curve':[dict(zip(('x','y','z'),p)) for p in c]} for e,c in zip(selected,candidate)]
    before=u['summarize'](selected);after=u['summarize'](output)
    report={'source':str(root),'nominalGradePercent':100*grade,'maxInteriorNodeHeightChange':max(abs(a[1]-b) for a,b in zip(positions,heights)),'before':before,'predicted':after,'limits':'Offline joint candidate only; no split/junction elevation policy, native candidate validation, terrain model, or rendered mesh claim.'}
    (out/'report.json').write_text(json.dumps(report,indent=2));(out/'candidate.json').write_text(json.dumps({'edges':output,'nodeHeights':heights},indent=2))
    import matplotlib.pyplot as plt
    fig,axes=plt.subplots(2,1,figsize=(10,6))
    for name,cs,color in [('Slope then Curve (native)',selected,'#bd562e'),('Refit height after Curve (offline)',output,'#176eb2')]:
        station=0
        for i,e in enumerate(cs):
            c=e['curve'];ts=[j/200 for j in range(201)];pts=[u['m']['evaluate'](c,t) for t in ts];ss=[station]
            for x,y in zip(pts,pts[1:]):ss.append(ss[-1]+math.hypot(x['x']-y['x'],x['z']-y['z']))
            axes[0].plot(ss,[p['y'] for p in pts],color=color,label=name if i==0 else None)
            axes[1].plot(ss,[u['grade'](c,t) for t in ts],color=color);station=ss[-1]
    axes[0].legend();axes[0].set_ylabel('Elevation (m)');axes[1].set_ylabel('Grade (%)');axes[1].set_xlabel('Horizontal station (m; junction gaps excluded)')
    for ax in axes:ax.grid(alpha=.25)
    fig.tight_layout();fig.savefig(out/'comparison.png',dpi=130)
    print(json.dumps({k:v for k,v in report.items() if k not in ('before','predicted')},indent=2))
if __name__=='__main__':main()
