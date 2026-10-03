"""Audit native bucket captures. Diagnostic storage analysis, not geometry prediction."""
import argparse,json,hashlib
from pathlib import Path

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('trace',type=Path);p.add_argument('--output',type=Path,required=True)
    a=p.parse_args()
    if a.output.exists():raise ValueError('Refusing to overwrite evidence')
    trace=json.loads(a.trace.read_text(encoding='utf-8-sig'))['result'];reports=[]
    if trace['fault'] or trace['remainingPasses'] or trace['insideCapturedPass']:raise ValueError('Incomplete trace')
    for event in trace['events']:
        if not event.get('job','').endswith('+FinishEdgeGeometryJob'):continue
        path=Path(event['path']);capture=json.loads(path.read_text());m=capture['fields']['m_EdgeHeightMap']
        if m['lookupStorageVersion']!=1:raise ValueError('Unknown native storage contract')
        heads=m['bucketHeads'];mask=m['bucketCapacityMask'];capacity=m['capacity']
        if len(heads)!=mask+1 or mask<0 or (mask+1)&mask:raise ValueError('Invalid bucket mask')
        slots={s['index']:s for s in m['slots']}
        if len(slots)!=len(m['slots']) or any(not 0<=i<capacity for i in slots):raise ValueError('Invalid slots')
        def lookup(key,bucket):
            index=heads[bucket];visited=set()
            while index!=-1:
                if index in visited or index not in slots:raise ValueError('Cycle or uncaptured bucket link')
                visited.add(index);s=slots[index]
                if s['key']==key:return s
                index=s['next']
            return None
        rows=[]
        for s in m['slots']:
            bucket=s['managedHash']&mask;found=lookup(s['key'],bucket)
            if (found is not None)!=s['managedLookupFound']:raise ValueError('Captured managed lookup contradicts bucket traversal')
            if found and found['value']!=s['managedLookupValue']:raise ValueError('Captured lookup value differs')
            locations=[b for b in range(len(heads)) if lookup(s['key'],b) is not None]
            rows.append(dict(key=s['key'],slot=s['index'],managedHash=s['managedHash'],expectedBucket=bucket,
                reachableBuckets=locations,managedLookupFound=s['managedLookupFound'],value=s['value']))
        entries={tuple(e['key']):e['value'] for e in m['entries']}
        values={tuple(s['key']):[s['value'][axis] for axis in 'xyzw'] for s in m['slots']}
        if entries!=values:raise ValueError('Entry enumeration and bucket slots differ')
        reports.append(dict(phase=event['phase'],capture=str(path),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
            capacity=capacity,bucketCapacityMask=mask,rows=rows))
    if not reports:raise ValueError('No Finish storage captures')
    a.output.write_text(json.dumps(dict(scope='Native bucket traversal using captured managed hash; no geometry prediction or Burst lookup claim',
        trace=str(a.trace.resolve()),reports=reports),indent=2))
    for r in reports:
        print(r['phase'],[(x['key'],x['expectedBucket'],x['reachableBuckets'],x['managedLookupFound']) for x in r['rows']])

if __name__=='__main__':main()
