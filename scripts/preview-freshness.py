"""Offline evidence fingerprint, NOT a native rebuild completion certificate."""
import hashlib
import json
from pathlib import Path


def identity(value):
    return (value['index'], value['version'])


def fingerprint(packet):
    if not packet.get('ok') or not packet.get('citySession'):
        raise ValueError('successful city-scoped response required')
    result = packet['result']
    if result.get('topologyResolution', {}).get('status') != 'resolved':
        raise ValueError('unique resolved preview required')
    snapshot = result.get('connectedSnapshot', {})
    if snapshot.get('complete') is not True or snapshot.get('errors'):
        raise ValueError('complete connected snapshot required')
    # Include all captured lane/composition data. Ignore only observation metadata
    # outside owners/lanes; a changed hash is evidence of change, not its cause.
    evidence = {'citySession': packet['citySession'], 'original': result['original'],
                'owners': sorted(snapshot['owners'], key=identity),
                'lanes': sorted(snapshot['lanes'], key=identity)}
    encoded = json.dumps(evidence, sort_keys=True, separators=(',', ':'), allow_nan=False)
    return hashlib.sha256(encoded.encode()).hexdigest()


def may_accept(current, observed, rebuild_complete):
    """Contract prototype; caller must PROVE native completion for this revision.

    Tokens are (city session, tool session, monotonic revision). Geometry hashes,
    equal entity IDs, repeated reads, and simulation frames cannot supply that proof.
    This function is deliberately not wired to the mod's Apply path.
    """
    def valid(token):
        return (isinstance(token, tuple) and len(token) == 3
                and all(isinstance(x, str) and x for x in token[:2])
                and type(token[2]) is int and token[2] > 0)
    return valid(current) and valid(observed) and current == observed and rebuild_complete is True


def original_fingerprint(packet):
    """Conservative permanent-snapshot identity; bounded to captured data only."""
    s = packet['result']
    if (not packet.get('ok') or not packet.get('citySession')
            or s.get('citySession') != packet['citySession']
            or s.get('scope') != 'permanent' or s.get('schemaVersion') != 2
            or s.get('complete') is not True or s.get('errors')):
        raise ValueError('complete, same-session schema-2 permanent snapshot required')
    owners = s['owners']
    expected = {identity(s['junction']), *(identity(e) for e in s['incidentEdges'])}
    actual = [identity(o) for o in owners]
    if len(actual) != len(set(actual)) or set(actual) != expected:
        raise ValueError('unique complete owner coverage required')
    if any(o.get('live') is not True for o in owners):
        raise ValueError('live owners required')
    # Conservative: retain lane identities and derived compositions too. This may
    # invalidate more than necessary, but never ignore an observed input change.
    def clean(value):
        if isinstance(value, dict):
            return {k: clean(v) for k, v in value.items()
                    if k not in ('updated', 'created', 'equalityId')}
        if isinstance(value, list):
            return [clean(v) for v in value]
        return value
    evidence = dict(citySession=packet['citySession'], junction=s['junction'],
                    incidentEdges=sorted(s['incidentEdges'], key=identity),
                    owners=sorted(owners, key=identity),
                    lanes=sorted(s['lanes'], key=identity))
    return hashlib.sha256(json.dumps(clean(evidence), sort_keys=True,
                         separators=(',', ':'), allow_nan=False).encode()).hexdigest()


def observation_rejection(current_token, observed_token, submitted_inputs, current_inputs):
    """Return an explicit invalidation reason, never an Apply permission."""
    if not may_accept(current_token, observed_token, True):
        return 'revision_or_session_changed'
    try:
        if (current_token[0] != current_inputs.get('citySession')
                or observed_token[0] != submitted_inputs.get('citySession')):
            return 'revision_or_session_changed'
        if original_fingerprint(submitted_inputs) != original_fingerprint(current_inputs):
            return 'original_network_changed'
    except (KeyError, TypeError, ValueError):
        return 'original_snapshot_unavailable'
    return None  # No detected invalidation is NOT native rebuild completion.


if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('captures', nargs='+', type=Path)
    args = parser.parse_args()
    for path in args.captures:
        print(path.name, fingerprint(json.loads(path.read_text(encoding='utf-8-sig'))))
