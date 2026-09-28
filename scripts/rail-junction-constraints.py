"""Offline constraint prototype for the supported single-track-per-arm captures.

Inputs must describe the candidate's reconstructed composition, not stale geometry
from before a centerline edit. Passing this gate is not a native connectivity verdict.
"""
import math


def identity(entity):
    return entity['index'], entity['version']


def pair(row):
    return identity(row['sourceEdge']), identity(row['targetEdge'])


def obligations(baseline, mode='preserve', intended=None):
    """Preservation uses observed connectors; repair must name intended directions."""
    known = {pair(row) for row in baseline['connections']}
    existing = {pair(row) for row in baseline['connections'] if row['observedConnector']}
    if mode == 'preserve':
        if intended is not None:
            raise ValueError('Explicit intent belongs to repair mode')
        required = existing
    elif mode == 'repair':
        if not intended:
            raise ValueError('Repair requires explicit directed connection intent')
        required = existing | set(intended)
    else:
        raise ValueError('Unknown constraint mode')
    if not required or not required <= known:
        raise ValueError('Missing or unknown required connections')
    return required


def evaluate(candidate, required, reserve=0.0):
    """Report feasibility and physical levers; reserve is fractional headroom."""
    if not math.isfinite(reserve) or not 0 <= reserve < 1:
        raise ValueError('Reserve must be in [0, 1)')
    by_pair = {}
    for row in candidate['connections']:
        key = pair(row)
        if key in by_pair:
            raise ValueError('Duplicate candidate connection')
        by_pair[key] = row
    if not required or not set(required) <= by_pair.keys():
        raise ValueError('Candidate lacks required connection identities')
    results = []
    for key in sorted(required):
        row = by_pair[key]
        limit, angle, distance, curvature = (row[k] for k in
            ('limit', 'angleDegrees', 'distanceMetres', 'curviness'))
        if not all(math.isfinite(x) for x in (limit, angle, distance, curvature)):
            raise ValueError('Non-finite geometry or limit')
        if limit <= 0 or distance < 1 or curvature < 0 or not 0 <= angle <= 180:
            raise ValueError('Unsupported metric inputs')
        expected = 2 * math.sin(math.radians(angle) / 2) / distance
        if not math.isclose(curvature, expected, rel_tol=1e-6, abs_tol=1e-9):
            raise ValueError('Metric inconsistent with candidate geometry')
        effective = limit * (1 - reserve)
        results.append(dict(source=key[0], target=key[1],
            passes=curvature <= effective, utilization=curvature / effective,
            minimumSpanMetres=2 * math.sin(math.radians(angle) / 2) / effective,
            maximumAngleDegrees=math.degrees(2 * math.asin(min(1, effective * distance / 2))),
            availableSpanMetres=distance, angleDegrees=angle))
    return dict(passesCurvatureConstraints=all(r['passes'] for r in results),
                constraints=results, validationReady=False,
                scope='Curvature gate only; requires candidate composition and native validation')
