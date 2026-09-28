"""Reproducible counterexample: longer does not imply smoother.

No game integration. Split ranges describe independent fits, not tangent continuity.
"""
import json
import math


def blend(a, b, strength):
    return tuple(x + strength * (y - x) for x, y in zip(a, b))


def evaluate(c, t):
    u = 1 - t
    return tuple(u**3*c[0][i] + 3*u*u*t*c[1][i] +
                 3*u*t*t*c[2][i] + t**3*c[3][i] for i in range(2))


def length(c):
    return sum(math.dist(evaluate(c, i/1000), evaluate(c, (i+1)/1000)) for i in range(1000))


def split_ranges(node_count, split_indices):
    """Ordered inclusive node ranges; a split node belongs to both adjacent fits."""
    if node_count < 2 or any(type(i) is not int for i in split_indices):
        raise ValueError('Invalid path/split indices')
    if len(set(split_indices)) != len(split_indices) or any(i <= 0 or i >= node_count-1 for i in split_indices):
        raise ValueError('Splits must be unique interior nodes')
    boundaries = [0] + sorted(split_indices) + [node_count-1]
    return list(zip(boundaries, boundaries[1:]))


def experiment():
    original = ((0, 0), (30, 20), (70, -20), (100, 0))
    target = ((0, 0), (30, 0), (70, 0), (100, 0))
    rows = []
    for strength in (-1, -.5, 0, .5, 1):
        curve = tuple(blend(a, b, strength) for a, b in zip(original, target))
        rows.append(dict(strength=strength, length=length(curve),
                         maximumLateralExcursion=max(abs(evaluate(curve, i/1000)[1]) for i in range(1001))))
    return dict(scope='Counterexample to naive signed extrapolation; not the installed fitter',
                rows=rows, splitExample=split_ranges(7, [2, 4]))


if __name__ == '__main__':
    print(json.dumps(experiment(), indent=2))
