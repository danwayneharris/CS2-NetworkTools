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


if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('captures', nargs='+', type=Path)
    args = parser.parse_args()
    for path in args.captures:
        print(path.name, fingerprint(json.loads(path.read_text(encoding='utf-8-sig'))))
