"""Plot trace 239 and an illustrative endpoint-alignment experiment.

Run: uv run --with matplotlib --with numpy scripts/plot-connection.py
The candidate is exploratory Python geometry, not the production transformation.
"""
import json
from pathlib import Path
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

root = Path(__file__).resolve().parents[1]
record = json.loads((root / 'NetworkTools.Geometry.Tests/Fixtures/endpoint-offset-239.json').read_text(encoding='utf-8-sig'))
out = root / 'NetworkTools.docs/session-notes/plots'
out.mkdir(exist_ok=True)
nodes = np.array([n['input'] for n in record['nodes']])[:, [0, 2]]
origin = nodes[1].copy()
axis = nodes[-1] - nodes[0]
axis /= np.linalg.norm(axis)
rotation = np.array([axis, [-axis[1], axis[0]]]).T
def local(p):
    return (p - origin) @ rotation
def curves(key):
    return [np.array(e[key])[:, [0, 2]][::1 if e['forward'] else -1].copy() for e in record['edges']]
original, fitted = curves('input'), curves('output')
center = np.array(record['nodes'][1]['output'])[[0, 2]]
direction = fitted[0][3] - fitted[0][2]
direction /= np.linalg.norm(direction)
candidate = [c.copy() for c in fitted]
# Preserve endpoint-to-center distances, align on opposite sides of the node.
# Translate adjacent handles too, preserving the full-strength tangent direction.
for edge, endpoint, handle, sign in [(0, 3, 2, -1), (1, 0, 1, 1)]:
    old = candidate[edge][endpoint].copy()
    new = center + sign * np.linalg.norm(old - center) * direction
    candidate[edge][endpoint] = new
    candidate[edge][handle] += new - old

def draw(ax, cs, color, label):
    t = np.linspace(0, 1, 401)[:, None]
    for i, c in enumerate(cs):
        p = local(c)
        q = (1-t)**3*p[0] + 3*(1-t)**2*t*p[1] + 3*(1-t)*t*t*p[2] + t**3*p[3]
        ax.plot(q[:, 0], q[:, 1], color=color, label=label if i == 0 else None)
        ax.scatter(p[[0, 3], 0], p[[0, 3], 1], color=color, marker='s', s=30)
    # This dashed line indicates the gap only. It is NOT CS2's connection curve.
    gap = local(np.array([cs[0][3], cs[1][0]]))
    ax.plot(gap[:, 0], gap[:, 1], ':', color=color, alpha=.7)

for filename, include_candidate in [('01-captured', False), ('02-alignment-candidate', True)]:
    fig, axes = plt.subplots(1, 2, figsize=(13, 6))
    for ax in axes:
        draw(ax, original, '#777777', 'Captured input')
        draw(ax, fitted, '#1976d2', 'Current full-strength output')
        if include_candidate:
            draw(ax, candidate, '#dc6b00', 'Exploratory endpoint alignment')
        p = local(nodes)
        ax.scatter(p[:, 0], p[:, 1], marker='x', color='black', s=65, label='Original node centers')
        p = local(center)
        ax.scatter(*p, marker='+', color='#1976d2', s=90, label='Fitted middle node')
        ax.grid(alpha=.25)
        ax.set_xlabel('Distance along selection chord (m)')
        ax.set_ylabel('Lateral offset (m)')
    axes[0].set_title('Whole selection — lateral axis exaggerated')
    axes[1].set_title('Connection close-up — equal axis scale')
    axes[1].set_xlim(-12, 12)
    axes[1].set_ylim(-12, 12)
    axes[1].set_aspect('equal', adjustable='box')
    axes[0].legend(fontsize=8)
    fig.suptitle('Trace 239: three nodes, two segments, strength 1')
    fig.text(.5, .02, 'Squares = segment endpoints. Dotted lines mark gaps, not game-generated connection geometry.', ha='center', fontsize=9)
    fig.tight_layout(rect=[0, .05, 1, .94])
    fig.savefig(out / (filename + '.png'), dpi=160)
    plt.close(fig)
    print(out / (filename + '.png'))
