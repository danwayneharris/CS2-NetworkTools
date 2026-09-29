"""Paused toy-save regression. Explicit --run mutates; capture mode only reads.

Fresh identities are resolved by unique node position, then the complete local
geometry fingerprint must match the fixture. Never retries an uncertain mutation.
Launch/reload remains an explicit preceding lifecycle step.
"""
import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import subprocess
import time


def identity(entity):
    return entity['index'], entity['version']


def position(p):
    return tuple(p[k] for k in ('x', 'y', 'z'))


def fingerprint(nodes, edges):
    positions = {identity(n): position(n['position']) for n in nodes}
    rows = []
    for edge in edges:
        rows.append([edge['prefab'], positions.get(identity(edge['startNode']), 'outside-node-query'),
                     positions.get(identity(edge['endNode']), 'outside-node-query'),
                     [position(p) for p in edge['curve']]])
    # Identity-independent; retains full recorded floating-point precision.
    return hashlib.sha256(json.dumps(sorted(rows, key=lambda r: json.dumps(r)), separators=(',', ':')).encode()).hexdigest()


def resolve(nodes, point):
    found = [n for n in nodes if math.dist(position(n['position']), point) < 0.01]
    if len(found) != 1:
        raise ValueError('Missing or ambiguous fixture node: ' + str(point))
    return {k: found[0][k] for k in ('index', 'version')}


def connections(snapshot):
    if not snapshot['complete'] or snapshot['errors']:
        raise ValueError('Incomplete lane snapshot')
    def key(p):
        return tuple(p[k] for k in ('ownerIndex', 'laneIndex', 'secondary'))
    return {(kind, key(l['start']), key(l['end']))
            for l in snapshot['lanes'] if l.get('owner') == snapshot['junction']
            for kind in ('track', 'car') if kind in l}


class Runner:
    def __init__(self, bridge, output):
        self.bridge, self.output = Path(bridge).resolve(), Path(output)
        self.output.mkdir(parents=True, exist_ok=False)
        self.sequence = 0
        self.city_session = None

    def call(self, command, args=None):
        self.sequence += 1
        packet = subprocess.run(['powershell.exe', '-NoProfile', '-File',
            str(self.bridge / 'bridge.ps1'), '-Command', command,
            '-ArgsJson', json.dumps(args or {}, separators=(',', ':'))],
            capture_output=True, text=True, timeout=55)
        prefix = self.output / f'{self.sequence:03d}-{command}'
        prefix.with_suffix('.request.json').write_text(json.dumps(args or {}, indent=2))
        prefix.with_suffix('.txt').write_text(packet.stdout + packet.stderr)
        if packet.returncode:
            raise RuntimeError('Bridge failure; inspect original request, never blindly retry: ' + str(prefix))
        response = json.loads(packet.stdout)
        prefix.with_suffix('.json').write_text(json.dumps(response, indent=2))
        if self.city_session is None:
            self.city_session = response['citySession']
        if response['citySession'] != self.city_session:
            raise RuntimeError('City changed during regression')
        if not response['ok']:
            raise RuntimeError(response['error'])
        return response['result']

    def poll(self, command, predicate, args=None, seconds=35):
        deadline = time.monotonic() + seconds
        while time.monotonic() < deadline:
            result = self.call(command, args)
            if predicate(result):
                return result
            time.sleep(0.5)
        raise TimeoutError('Bounded polling expired: ' + command)

    def network(self, region):
        nodes = self.call('get_network', region)['nodes']
        edges = self.call('get_network_edges', region)
        if len(nodes) >= 512 or edges['possiblyTruncated']:
            raise ValueError('Capture limit reached')
        return nodes, edges['edges']

    def state(self):
        return self.call('nt_get_state')

    def control(self, command, **args):
        state = self.state()
        return self.call(command, dict(session=state['session'], revision=state['revision'], **args))

    def execute(self, fixture, case, save_root):
        city = self.call('get_city_state')
        if city['selectedSpeed'] != 0 or city['population'] != 0 or not city['controlEnabled']:
            raise ValueError('Requires paused, control-enabled empty toy city')
        self.state()  # Fail before checkpointing if the deployed adapter is unavailable.
        baseline = list(Path(save_root).rglob(fixture['baseline']))
        if len(baseline) != 1 or hashlib.sha256(baseline[0].read_bytes()).hexdigest().upper() != fixture['baselineSha256']:
            raise ValueError('Recoverable baseline package missing or changed')
        nodes, edges = self.network(fixture['region'])
        if fingerprint(nodes, edges) != fixture['fingerprint']:
            raise ValueError('Baseline geometry mismatch; reload the named baseline before this case')
        start, end = (resolve(nodes, case[k]) for k in ('start', 'end'))
        watched = [resolve(nodes, p) for p in case.get('watch', [])]
        before = [self.call('get_junction_snapshot', n) for n in watched]
        checkpoint = self.call('save_checkpoint', {'label': 'regression-' + case['name']})
        saved = self.poll('get_operation', lambda s: s['status'] in ('complete','failed','interrupted'),
                          {'id': checkpoint['id']})
        if saved['status'] != 'complete':
            raise RuntimeError('Checkpoint failed')
        # File verification supplements Save()'s unreliable boolean result.
        packages = list(Path(save_root).rglob(saved['saveName'] + '.cok'))
        if len(packages) != 1 or packages[0].stat().st_size < 1024:
            raise RuntimeError('Checkpoint package unavailable')
        import zipfile
        with zipfile.ZipFile(packages[0]) as z:
            if z.testzip() is not None or not any(n.endswith('.SaveGameMetadata.cid') for n in z.namelist()):
                raise RuntimeError('Checkpoint ZIP verification failed')
        self.call('nt_activate')
        self.poll('nt_get_state', lambda s: s['active'])
        self.control('nt_clear')
        self.control('nt_strength', value=case.get('strengths', [0.5,0.8])[0])
        self.control('nt_select', start=start, end=end)
        for value in case.get('strengths', [0.5,0.8]):
            self.control('nt_strength', value=value)
            state = self.poll('nt_get_state', lambda s: s['previewReady'])
        previews = [self.call('get_junction_preview', n) for n in watched]
        # Apply uses the observed token, not a freshly substituted submission.
        self.call('nt_apply', {k: state[k] for k in ('session','revision','submission')})
        self.poll('nt_get_state', lambda s: s['phase'] == 'Idle')
        after = [self.call('get_junction_snapshot', n) for n in watched]
        after_nodes, after_edges = self.network(fixture['region'])
        old_nodes, new_nodes = ({identity(n): n for n in ns} for ns in (nodes, after_nodes))
        old_edges, new_edges = ({identity(e): e for e in es} for es in (edges, after_edges))
        if old_nodes.keys() != new_nodes.keys() or old_edges.keys() != new_edges.keys():
            raise AssertionError('Topology identities changed')
        for key, node in old_nodes.items():
            if abs(node['position']['y'] - new_nodes[key]['position']['y']) > 0.001:
                raise AssertionError('Node elevation changed')
        for key, edge in old_edges.items():
            if any(edge[k] != new_edges[key][k] for k in ('startNode','endNode','prefab')):
                raise AssertionError('Topology or prefab changed')
        changes = [key for key in old_edges if old_edges[key]['curve'] != new_edges[key]['curve']]
        report = {'case':case['name'], 'checkpoint':saved['saveName'], 'changedEdges':changes,
                  'junctions':[], 'limits':'Local snapshot checks; not vehicle traversal or visual approval.'}
        for b, p, a in zip(before, previews, after):
            missing, added = connections(b)-connections(a), connections(a)-connections(b)
            if missing or added:
                raise AssertionError(f'Directed connections changed: missing={missing}, added={added}')
            temp = p.get('connectedSnapshot')
            if not temp or not temp['complete']:
                raise AssertionError('Missing complete connected preview')
            actual = {identity(o):o for o in a['owners']}
            errors=[]
            for owner in temp['owners']:
                if owner.get('curve'):
                    curve = actual[identity(owner['temp']['original'])]['curve']
                    errors += [math.dist(position(x),position(y)) for x,y in zip(owner['curve'],curve)]
            if not errors or max(errors)>0.001:
                raise AssertionError('Incident preview/permanent curve mismatch')
            report['junctions'].append({'connections':len(connections(a)), 'maxCurveError':max(errors)})
        (self.output/'report.json').write_text(json.dumps(report, indent=2))
        return report


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--bridge', default='../cities2-agent-bridge-ndc')
    parser.add_argument('--output', required=True)
    parser.add_argument('--fixture', required=True)
    parser.add_argument('--case')
    parser.add_argument('--run', action='store_true')
    parser.add_argument('--save-root', help='Required with --run; CSII_USERDATAPATH/Saves')
    args=parser.parse_args()
    fixture=json.loads(Path(args.fixture).read_bytes())
    runner=Runner(args.bridge,args.output)
    if args.run:
        if not args.save_root:
            parser.error('--run requires --save-root')
        case=next(c for c in fixture['cases'] if c['name']==args.case)
        print(json.dumps(runner.execute(fixture,case,args.save_root),indent=2))
    else:
        nodes,edges=runner.network(fixture['region'])
        (runner.output/'discovery.json').write_text(json.dumps({'nodes':nodes,'edges':edges,
            'fingerprint':fingerprint(nodes,edges)},indent=2))


if __name__=='__main__':
    main()
