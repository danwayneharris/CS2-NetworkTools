"""Summarize bounded Debug intermediate captures; offline and read-only to CS2.
Writes a compact report. Original/final geometry remains in the source log.
"""
import argparse,json,math
from pathlib import Path

def summarize(text):
    groups={}
    for line in text.splitlines():
        if not any(prefix in line for prefix in ('[NetworkTools.CombinedStage]','[NetworkTools.SmoothTrace]')):continue
        begin=line.find('{')
        if begin<0:continue
        row=json.loads(line[begin:]);stage=row.get('stage','final')
        key=(row['session'],row['id'],row['mode'])
        groups.setdefault(key,{})[stage]=row
    results=[];incomplete=0
    for key,stages in groups.items():
        if 'horizontal' not in stages:continue
        if not all(s in stages and stages[s].get('valid') and not stages[s].get('detailsOmitted') for s in ('horizontal','vertical','final')):
            incomplete+=1;continue
        rows=[stages[s] for s in ('horizontal','vertical','final')]
        if any(len(r['edges'])!=r['edgeCount'] or any(len(e['output'])!=4 or len(e['input'])!=4 for e in r['edges']) for r in rows):raise ValueError('Incomplete cubic coverage')
        entities=[[e['entity'] for e in r['edges']] for r in rows]
        if not entities[0] or len(set(entities[0]))!=len(entities[0]) or entities[0]!=entities[1] or entities[0]!=entities[2]:raise ValueError('Stage edge coverage mismatch')
        if any(r['nodeCount']!=rows[0]['nodeCount'] for r in rows):raise ValueError('Stage node coverage mismatch')
        if any([e['input'] for e in r['edges']]!=[e['input'] for e in rows[0]['edges']] for r in rows[1:]):raise ValueError('Original inputs changed between stages')
        changes=[]
        for before,after in zip(rows,rows[1:]):
            pairs=[(p,q) for e,f in zip(before['edges'],after['edges']) for p,q in zip(e['output'],f['output'])]
            if any(p is None or q is None or len(p)!=3 or len(q)!=3 or not all(math.isfinite(v) for v in p+q) for p,q in pairs):raise ValueError('Invalid stage geometry')
            changes.append({'maxXZChange':max(math.hypot(p[0]-q[0],p[2]-q[2]) for p,q in pairs),'maxYChange':max(abs(p[1]-q[1]) for p,q in pairs)})
        if any(c['maxXZChange']>.05 for c in changes):raise ValueError('Vertical/surface stage changed horizontal candidate')
        results.append({'session':key[0],'submission':key[1],'mode':key[2],'edges':len(entities[0]),'horizontalToVertical':changes[0],'verticalToFinal':changes[1]})
    if not results:raise ValueError('No complete valid combined stage capture')
    return {'completeCaptures':results,'incompleteOrRejectedCaptures':incomplete,'limits':'Intermediate authored geometry, not final mesh or vehicle evidence.'}

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('log');p.add_argument('--output',required=True);a=p.parse_args()
    report=summarize(Path(a.log).read_text(encoding='utf-8',errors='replace'))
    Path(a.output).write_text(json.dumps(report,indent=2));print(json.dumps({'complete':len(report['completeCaptures']),'incompleteOrRejected':report['incompleteOrRejectedCaptures']}))
if __name__=='__main__':main()
