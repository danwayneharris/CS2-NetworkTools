"""Verify recorded NT adapter smoke test; never contacts the game."""
import json
import math
import sys
from pathlib import Path

root = Path(sys.argv[1])
def read(name):
    return json.loads((root / (name + '.json')).read_bytes())

def result(name):
    packet = read(name)
    assert packet['ok'], name
    return packet['result']

assert 'stale_tool_revision' in read('rejected-stale')['error']
assert 'stale_or_unverified_preview' in read('rejected-old-submission')['error']
assert result('preview-05')['previewReady']
assert result('preview-08')['previewReady']
assert result('preview-08')['strength'] == 0.8
assert result('after-state')['phase'] == 'Idle'
before, after = result('junction-before'), result('junction-after')
assert before['complete'] and after['complete']
assert before['citySession'] == after['citySession']
def connections(snapshot):
    def key(endpoint):
        return tuple(endpoint[k] for k in ('ownerIndex','laneIndex','secondary'))
    return {(key(l['start']), key(l['end'])) for l in snapshot['lanes']
            if 'track' in l and l.get('owner') == snapshot['junction']}
assert connections(before) == connections(after)
preview = result('junction-preview')['connectedSnapshot']
assert preview['complete']
owners = {(o['index'], o['version']): o for o in after['owners']}
errors = []
for owner in preview['owners']:
    if not owner.get('curve'):
        continue
    original = owner['temp']['original']
    actual = owners[(original['index'], original['version'])]['curve']
    for p, q in zip(owner['curve'], actual):
        errors.append(math.dist([p[k] for k in 'xyz'], [q[k] for k in 'xyz']))
assert errors and max(errors) <= 0.001
edges_before = {(e['index'],e['version']): e for e in result('edges-before')['edges']}
edges_after = {(e['index'],e['version']): e for e in result('edges-after')['edges']}
assert edges_before.keys() == edges_after.keys()
changed = [key for key in edges_before if edges_before[key]['curve'] != edges_after[key]['curve']]
assert changed, 'Apply must actually change geometry'
print(json.dumps({'preservedDirectedConnections':len(connections(after)),
    'incidentPreviewMaxControlErrorMetres':max(errors), 'changedEdges':changed,
    'staleRequestsRejected':True}, indent=2))
