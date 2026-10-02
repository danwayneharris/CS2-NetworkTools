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
import time
import importlib.util

# Dan's October 1 acceptance policy: small world-space discrepancies are
# observations, not failures. This does not relax topology/connection assertions.
GEOMETRY_TOLERANCE_METERS = 0.05

_lane_spec = importlib.util.spec_from_file_location('lane_connectivity', Path(__file__).with_name('lane-connectivity.py'))
_lane_module = importlib.util.module_from_spec(_lane_spec)
_lane_spec.loader.exec_module(_lane_module)


def lane_transitions(snapshot):
    return {(kind, source, target) for kind in ('track','car')
            for source,target in _lane_module.transitions(snapshot,kind)}


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


def permanent_signature(snapshots, nodes, edges):
    """Observed stability, not a native job-completion fence."""
    junctions = []
    for snapshot in snapshots:
        pairs = sorted(lane_transitions(snapshot))  # Includes native direct joins.
        owners = snapshot['owners']
        if any(o.get('updated') or o.get('created') for o in owners):
            return None
        junctions.append((identity(snapshot['junction']), pairs,
            sorted((identity(o), o.get('position'), o.get('curve')) for o in owners)))
    return json.dumps([fingerprint(nodes, edges),
        sorted(identity(n) for n in nodes), sorted(identity(e) for e in edges),
        junctions], sort_keys=True)


class Runner:
    def __init__(self, bridge, output):
        self.bridge, self.output = Path(bridge).resolve(), Path(output)
        self.output.mkdir(parents=True, exist_ok=False)
        self.sequence = 0
        self.city_session = None

    def route_provider(self, command, args=None):
        if command.startswith('nt_'):
            action = {'nt_get_state': 'state', 'nt_activate': 'activate', 'nt_clear': 'clear',
                      'nt_select': 'select', 'nt_strength': 'strength', 'nt_split': 'split', 'nt_combined': 'combined', 'nt_apply': 'apply'}[command]
            if not getattr(self, 'provider_revision', None):
                catalog = self.call('list_providers')
                if not catalog.get('complete'):
                    raise RuntimeError('Provider discovery incomplete: ' + str(catalog.get('errors')))
                matches = [p for p in catalog['providers'] if p['id'] == 'networktools']
                if len(matches) != 1:
                    raise RuntimeError('NetworkTools provider unavailable')
                self.provider_revision = matches[0]['revision']
            args = {'provider': 'networktools', 'revision': self.provider_revision,
                    'command': action, 'args': args or {}}
            command = 'invoke_provider'
        return command, args

    def call(self, command, args=None):
        command, args = self.route_provider(command, args)
        self.sequence += 1
        prefix = self.output / f'{self.sequence:03d}-{command}'
        prefix.with_suffix('.request.json').write_text(json.dumps(args or {}, indent=2))
        if not hasattr(self, 'client'):
            client_path = self.bridge / 'adapter' / 'bridge_client.py'
            spec = importlib.util.spec_from_file_location('cities_bridge_client', client_path)
            module = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(module)
            self.client = module.Client(Path(os.environ['LOCALAPPDATA']) / 'CitiesIIAgentBridge',
                                        self.output / 'intents')
            self.transport_session = self.client.status()
        # Pin both process and city before publication, including callers which
        # supplied an expected city_session before their first request.
        if self.city_session is not None and self.transport_session['citySession'] != self.city_session:
            raise RuntimeError('City changed before regression request')
        try:
            response = self.client.call(command, args or {}, expected=self.transport_session)
        except Exception as error:
            prefix.with_suffix('.error.json').write_text(json.dumps({
                'error': str(error), 'requestId': self.client.last_request,
                'retryPolicy': 'Inspect original intent; never blindly retry a mutation.'}, indent=2))
            raise
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

    def settled_permanent(self, watched, region, seconds=35):
        deadline = time.monotonic() + seconds
        previous, matches = None, 0
        while time.monotonic() < deadline:
            snapshots = [self.call('get_junction_snapshot', n) for n in watched]
            nodes, edges = self.network(region)
            signature = permanent_signature(snapshots, nodes, edges)
            matches = matches + 1 if signature is not None and signature == previous else 1
            if signature is None:
                matches = 0
            previous = signature
            if matches >= 3:
                return snapshots, nodes, edges
            time.sleep(1)
        raise TimeoutError('Permanent results did not settle across three observations')

    def control(self, command, **args):
        state = self.state()
        return self.call(command, dict(session=state['session'], revision=state['revision'], **args))

    def execute(self, fixture, case, save_root):
        combined = case.get('combined', False)
        if not isinstance(combined, bool): raise ValueError('combined must be boolean')
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
        split_nodes = [resolve(nodes, p) for p in case.get('splits', [])]
        path = self.call('trace_network', dict(fromIndex=start['index'],fromVersion=start['version'],
            toIndex=end['index'],toVersion=end['version']))
        if not path['connected'] or not path['edges']:
            raise ValueError('Fixture endpoints have no path')
        selected_edges = {identity(e) for e in path['edges']}
        selected_nodes = {identity(n) for e in edges if identity(e) in selected_edges
                          for n in (e['startNode'],e['endNode'])}
        if len(selected_nodes) != len(selected_edges)+1:
            raise ValueError('Path outside bounded capture or ambiguous topology')
        watched = [{k:n[k] for k in ('index','version')} for n in nodes
                   if identity(n) in selected_nodes and len(n['edges'])>1]
        if not watched:
            raise ValueError('No observable shared-node preview; capture coverage unsupported')
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
        self.poll('nt_get_state', lambda s: s['active'] and s.get('smoothMode', False))
        self.control('nt_clear')
        if combined: self.control('nt_combined', enabled=True)
        self.control('nt_strength', value=case.get('strengths', [0.5,0.8])[0])
        self.control('nt_select', start=start, end=end)
        for node in split_nodes:
            self.control('nt_split', node=node, enabled=True)
        for value in case.get('strengths', [0.5,0.8]):
            self.control('nt_strength', value=value)
            preview_timeout = case.get('previewTimeoutSeconds', 35)
            if not isinstance(preview_timeout, (int, float)) or not 1 <= preview_timeout <= 180:
                raise ValueError('Preview timeout must be bounded to 1-180 seconds')
            state = self.poll('nt_get_state', lambda s: s['previewReady'], seconds=preview_timeout)
        previews = [self.call('get_junction_preview', n) for n in watched]
        preview_curves = {}
        for p in previews:
            temp = p.get('connectedSnapshot')
            if not temp or not temp['complete']:
                raise ValueError('Complete connected preview required before Apply')
            for owner in temp['owners']:
                if owner.get('curve'):
                    key=identity(owner['temp']['original'])
                    if key in preview_curves and preview_curves[key] != owner['curve']:
                        raise ValueError('Preview changed across snapshot reads')
                    preview_curves[key]=owner['curve']
        if not selected_edges.issubset(preview_curves):
            raise ValueError('Not every selected edge has an independently captured preview')
        final_state=self.state()
        if any(final_state[k]!=state[k] for k in ('session','revision','submission')) or not final_state['previewReady']:
            raise ValueError('Preview changed during independent capture')
        # Apply uses the observed token, not a freshly substituted submission.
        self.call('nt_apply', {k: state[k] for k in ('session','revision','submission')})
        self.poll('nt_get_state', lambda s: s['phase'] == 'Idle')
        after, after_nodes, after_edges = self.settled_permanent(watched, fixture['region'])
        old_nodes, new_nodes = ({identity(n): n for n in ns} for ns in (nodes, after_nodes))
        old_edges, new_edges = ({identity(e): e for e in es} for es in (edges, after_edges))
        if old_nodes.keys() != new_nodes.keys() or old_edges.keys() != new_edges.keys():
            raise AssertionError('Topology identities changed')
        fixed_node_drifts = []
        for key, node in old_nodes.items():
            if not combined and abs(node['position']['y'] - new_nodes[key]['position']['y']) > GEOMETRY_TOLERANCE_METERS:
                raise AssertionError('Node elevation changed')
            if key not in selected_nodes or len(node['edges']) > 2 or key in {identity(start),identity(end),*(identity(n) for n in split_nodes)}:
                drift=math.dist(position(node['position']),position(new_nodes[key]['position']))
                if drift>0.001:
                    fixed_node_drifts.append({'node':key,'distance':drift})
        for key, edge in old_edges.items():
            if any(edge[k] != new_edges[key][k] for k in ('startNode','endNode','prefab')):
                raise AssertionError('Topology or prefab changed')
        changes = [key for key in old_edges if old_edges[key]['curve'] != new_edges[key]['curve']]
        outside_error=max((math.dist(position(a),position(b)) for key in changes if key not in selected_edges for a,b in zip(old_edges[key]['curve'],new_edges[key]['curve'])),default=0)
        if outside_error>GEOMETRY_TOLERANCE_METERS:
            raise AssertionError('Unselected edge geometry changed beyond accepted tolerance')
        for key in selected_edges:
            if max(math.dist(position(a),position(b)) for a,b in
                   zip(preview_curves[key],new_edges[key]['curve']))>GEOMETRY_TOLERANCE_METERS:
                raise AssertionError('Selected preview/permanent geometry mismatch')
        report = {'case':case['name'], 'combined':combined, 'checkpoint':saved['saveName'], 'changedEdges':changes,
                  'junctions':[], 'fixedNodeDrifts':fixed_node_drifts,
                  'geometryToleranceMeters':GEOMETRY_TOLERANCE_METERS,'unselectedCurveMaxError':outside_error,
                  'limits':'Local snapshot checks; not vehicle traversal or visual approval.'}
        for node in split_nodes:
            incident=[new_edges[identity(e)] for e in path['edges'] if identity(node) in
                      (identity(new_edges[identity(e)]['startNode']),identity(new_edges[identity(e)]['endNode']))]
            if len(incident)!=2:
                raise AssertionError('Split does not have two selected incident edges')
            incoming,outgoing=incident
            left=incoming['curve'] if incoming['endNode']==node else list(reversed(incoming['curve']))
            right=outgoing['curve'] if outgoing['startNode']==node else list(reversed(outgoing['curve']))
            p=new_nodes[identity(node)]['position']
            for endpoint in (left[-1],right[0]):
                if math.hypot(endpoint['x']-p['x'],endpoint['z']-p['z'])>GEOMETRY_TOLERANCE_METERS:
                    raise AssertionError('Split curve endpoint is not at pinned node')
            a=(left[3]['x']-left[2]['x'],left[3]['z']-left[2]['z'])
            b=(right[1]['x']-right[0]['x'],right[1]['z']-right[0]['z'])
            if combined:
                la,lb=math.hypot(*a),math.hypot(*b)
                if min(la,lb)<1e-8: raise AssertionError('Undefined split vertical grade')
                ga=(left[3]['y']-left[2]['y'])/la;gb=(right[1]['y']-right[0]['y'])/lb
                if abs(ga-gb)>1e-5: raise AssertionError('Split vertical grades do not agree')
            lengths=math.hypot(*a)*math.hypot(*b)
            if lengths<1e-8 or (a[0]*b[0]+a[1]*b[1])/lengths<1-1e-6:
                raise AssertionError('Split planar tangents do not agree')
        for b, p, a in zip(before, previews, after):
            if _lane_module.composition_signature(b)!=_lane_module.composition_signature(a):
                raise AssertionError('Incident lane composition changed; lane indices may no longer have the same physical meaning')
            missing, added = lane_transitions(b)-lane_transitions(a), lane_transitions(a)-lane_transitions(b)
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
            if not errors or max(errors)>GEOMETRY_TOLERANCE_METERS:
                raise AssertionError('Incident preview/permanent curve mismatch')
            report['junctions'].append({'connections':len(lane_transitions(a)),
                'rawConnectorPairsChanged':connections(b)!=connections(a), 'maxCurveError':max(errors)})
        report['passed']=not any(d['distance']>GEOMETRY_TOLERANCE_METERS for d in fixed_node_drifts)
        (self.output/'report.json').write_text(json.dumps(report, indent=2))
        if not report['passed']:
            raise AssertionError('Fixed or unselected node moved; other checks completed, inspect report.json')
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
