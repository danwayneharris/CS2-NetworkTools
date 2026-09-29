"""Connection-space experiment, NOT an authored-curve solver or native verdict.

Rotate both lane tangents of one arm together, keeping reconstructed positions
fixed. Intersect exact angular feasibility intervals for all required directions.
The intervals may be empty; no mainline or branch priority is inferred.
"""
import math


def intersect(left, right):
    return [(max(a, c), min(b, d)) for a, b in left for c, d in right
            if max(a, c) <= min(b, d)]


def project(constraints, max_rotation=15.0, reserve=0.02):
    """Rows: signed angle, rotation coefficient (-1/0/1), span, prefab limit.

    Angles use source heading minus reversed target heading. A selected source
    has coefficient +1; a selected target -1. Non-selected pairs have zero.
    """
    if not (math.isfinite(max_rotation) and 0 <= max_rotation <= 180
            and math.isfinite(reserve) and 0 <= reserve < 1):
        raise ValueError('Invalid search bounds')
    if not constraints:
        raise ValueError('No required connections')
    intervals = [(-max_rotation, max_rotation)]
    for row in constraints:
        angle, coefficient, span, limit = (row[k] for k in
            ('signedAngle', 'coefficient', 'span', 'limit'))
        if not all(math.isfinite(v) for v in (angle, coefficient, span, limit)):
            raise ValueError('Non-finite constraint')
        if coefficient not in (-1, 0, 1) or span < 1 or limit <= 0:
            raise ValueError('Unsupported constraint')
        angle = (angle + 180) % 360 - 180
        bound = math.degrees(2 * math.asin(min(1, limit * (1-reserve) * span / 2)))
        if coefficient == 0:
            allowed = [(-max_rotation, max_rotation)] if abs(angle) <= bound else []
        else:
            allowed = []
            for winding in (-1, 0, 1):
                ends = [(-bound-angle+360*winding)/coefficient,
                        (bound-angle+360*winding)/coefficient]
                allowed.append((min(ends), max(ends)))
        intervals = intersect(intervals, allowed)
    # Tie-breaking is deterministic; no arm ordering or preferred route enters it.
    choices = [min(max(0, a), b) for a, b in intervals]
    rotation = min(choices, key=lambda x: (abs(x), x)) if choices else None
    return dict(rotationDegrees=rotation, feasibleIntervals=sorted(intervals),
                validationReady=False,
                scope='Fixed-position lane-tangent projection only; native rebuild required')


def from_points(points, required, selected):
    """Restricted to one lane in each direction on every incident arm."""
    def identity(p): return (p['edge']['index'], p['edge']['version'])
    def lane(edge, role):
        matches = [p for p in points if identity(p) == edge and p[role]]
        if len(matches) != 1:
            raise ValueError('Requires exactly one lane per arm and direction')
        return matches[0]
    if selected not in {identity(p) for p in points}:
        raise ValueError('Selected arm missing')
    rows = []
    for source, target in sorted(required):
        a, b = lane(source, 'source'), lane(target, 'target')
        sa, tb = a['inwardTangent'], b['inwardTangent']
        signed = math.degrees(math.atan2(sa[2], sa[0]) - math.atan2(-tb[2], -tb[0]))
        rows.append(dict(source=source, target=target, signedAngle=signed,
            coefficient=int(source == selected)-int(target == selected),
            span=max(1, math.dist(a['position'], b['position'])), limit=a['maxCurviness']))
    return rows
