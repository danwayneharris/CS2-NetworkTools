"""Summarize recorded terrain runs without issuing game commands."""
import argparse,json
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('suite');a=p.parse_args();root=Path(a.suite);rows=[]
for directory in sorted(root.iterdir()):
    path=directory/'terrain-diagnostics.json'
    if not path.exists():continue
    d=json.loads(path.read_text());report=directory/'report.json';result=json.loads(report.read_text()) if report.exists() else {}
    row={'case':directory.name,'strictPassed':result.get('passed'),'error':d.get('regressionError'),'fixedNodeDrifts':result.get('fixedNodeDrifts',[])}
    for phase in ('before','after'):
        if phase not in d:continue
        obs=d[phase];row[phase]={'horizontalLengthM':sum(x['sampledHorizontalLength'] for x in obs['metrics']),
            'sampledMaxGradePercent':max((x['sampledMaxAbsGradePercent'] for x in obs['metrics'] if x['sampledMaxAbsGradePercent'] is not None),default=None),
            'sampledCenterlineTerrainOffsetM':obs['sampledCenterlineMinusTerrainRange']}
    rows.append(row)
(root/'terrain-summary.json').write_text(json.dumps(rows,indent=2));print(json.dumps(rows,indent=2))
try:import matplotlib.pyplot as plt
except ImportError:raise SystemExit()
complete=[r for r in rows if 'after' in r and r['strictPassed'] is not None]
if not complete:raise SystemExit()
fig,axes=plt.subplots(1,2,figsize=(12,5));xs=list(range(len(complete)))
for ax,metric,title in zip(axes,('horizontalLengthM','sampledMaxGradePercent'),('Selected horizontal length (m)','Maximum sampled absolute grade (%)')):
    for phase,offset,color in [('before',-.18,'#778da9'),('after',.18,'#dd8452')]:
        ax.bar([x+offset for x in xs],[r[phase][metric] for r in complete],width=.36,label=phase,color=color)
    ax.set_xticks(xs);ax.set_xticklabels([r['case'][3:] for r in complete],rotation=25,ha='right');ax.set_title(title);ax.legend();ax.grid(axis='y',alpha=.2)
fig.suptitle('Full-strength terrain tests: measured geometry, not grade suitability certification');fig.tight_layout();fig.savefig(root/'terrain-summary.png',dpi=130)
