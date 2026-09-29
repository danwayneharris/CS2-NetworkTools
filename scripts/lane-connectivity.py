"""Directed local lane transitions, including native direct joins.

Composition lane indices are the low byte of edge-owned PathNodes. Segment bytes
and node-owned connector indices are graph plumbing, not physical lane labels.
This checks native lane graph reachability, not traffic rules or vehicle traversal.
"""
from collections import defaultdict


def key(node):
    # equalityId is snapshot-local PathNode.Equals, including curve position.
    return node['equalityId']


def transitions(snapshot, kind):
    if not snapshot['complete'] or snapshot['errors']:
        raise ValueError('Incomplete snapshot')
    junction=snapshot['junction']['index']
    incident={e['index'] for e in snapshot['incidentEdges']}
    arcs=defaultdict(list)
    edge_arcs=[]
    for lane in snapshot['lanes']:
        if kind not in lane:
            continue
        owner=lane['owner']['index']
        if owner not in incident and owner!=junction:
            raise ValueError('Unexpected lane owner')
        label=None
        if owner in incident:
            indices={lane[p]['laneIndex'] & 255 for p in ('start','middle','end')
                     if lane[p]['ownerIndex']==owner}
            if len(indices)!=1:
                raise ValueError('Missing or ambiguous composition lane index')
            label=(owner, next(iter(indices)), lane.get('secondary',False))
        arc=(key(lane['end']),label)
        arcs[key(lane['start'])].append(arc)
        if label is not None:
            edge_arcs.append(arc)
    result=set()
    for end,source in edge_arcs:
        pending=[end];visited=set()
        while pending:
            vertex=pending.pop()
            if vertex in visited:
                continue
            visited.add(vertex)
            for target,destination in arcs[vertex]:
                if destination is not None and destination[0]!=source[0]:
                    result.add((source,destination))
                    # Do not traverse a second network edge and infer remote routes.
                else:
                    pending.append(target)
    return result
