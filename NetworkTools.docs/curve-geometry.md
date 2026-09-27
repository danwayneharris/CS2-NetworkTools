# Curve geometry module

`NetworkTools.Mod/Geometry/PlanarFairing.cs` implements a game-independent fit of
horizontal node positions. `PlanarBezier.cs` supplies horizontal tangent and handle
calculations. The Smooth Curve integration prototype calls both. Full Debug and
Release builds pass, including postprocessing and Windows Burst compilation.
Debug is deployed; in-game behavior remains unverified.

## Objective

Let original points be `p[i]` in metres, fitted points `q[i]`, and original chord
lengths `h[i] = |p[i+1] - p[i]|`. Each point's fidelity weight `w[i]` is half the
sum of its incident chord lengths. The solver minimizes:

```text
E(q) = sum_i w[i] * |q[i] - p[i]|²
     + lambda * sum_interior_i 2/(h[i-1] + h[i])
         * |(q[i+1]-q[i])/h[i] - (q[i]-q[i-1])/h[i-1]|²

lambda = 50⁴ * strength², with strength in [0, 1]
```

The first term penalizes displacement; the second penalizes changes in direction
and spacing along the original chord-length parameter. It approximates an integral
of squared second derivatives, not exact geometric curvature. Original stations
remain fixed throughout the solve. The effective smoothing length is
`lambda^(1/4) = 50 * sqrt(strength)` metres; 50 metres is an initial tuning choice.
This is not a maximum displacement limit.

First/last points and any point marked `Fixed` are positional constraints. The
solver works on displacements, giving fixed points zero displacement and reducing
cancellation from large absolute map coordinates. Each second-difference term
couples three adjacent points; the resulting positive-definite system has five
diagonals. Banded Cholesky solves both coordinates in O(n) time and O(n) storage.

## API and limitations

- Input and output each contain `count` points; scratch storage contains `5*count`
  doubles. All buffers must be valid and non-overlapping. The unsafe API cannot
  verify buffer capacities; the caller owns allocation and lifetime.
- The core allocates no memory and depends only on `System`, allowing the exact
  same source to run in ordinary .NET tests. The integrated job passes Windows
  Burst compilation; executing that Release build in-game remains unverified.
- Coordinates must be finite, consecutive points at least 1 cm apart, and strength
  finite and within [0, 1]. Strength zero returns an exact copy of valid input.
- Invalid input, numerical failure, or a fitted chord that collapses or reverses
  relative to its original direction returns `false`. Discard all output on failure;
  buffers may have been partially written. Input remains unchanged.
- Positional pins do not constrain tangents. There is no self-intersection check,
  minimum turning radius, obstacle avoidance, terrain query, or displacement cap.
- There are no elevations, game entities, intersection offsets, or Bézier handles
  in the fitter's API. The game adapter preserves elevations/topology, pins junctions,
  fits segment handles, and rejects unsuitable results before Apply.

## Prototype cubic reconstruction

`PlanarBezier` uses a shared normalized bisector of incident chord directions at
ordinary interior nodes. The adapter retains original directions at endpoints and
junctions, translates intersection offsets with node displacement, and preserves
all original control-point Y coordinates. Each horizontal handle is one third of
its segment's horizontal endpoint distance. A direction projecting less than 0.05
onto the forward unit chord is rejected. Accepted control points have strictly
ordered chord projections, preventing a loop within that individual cubic.

This provides matching planar directions at movable nodes, not continuous curvature
or a guarantee about the game's intersection geometry. Inter-segment crossings and
terrain/obstacle conflicts are not checked. Strength zero preserves the input.
Handles now blend from their original positions, translated with their respective
endpoints, toward the reconstructed handles using the smoothing factor. Full
strength reaches the reconstructed target; intermediate strengths can retain
original tangent mismatches and non-monotone control polygons. The target's
ordered-projection check does not establish that a blended cubic is loop-free.
The fixed 50-metre node-fitting scale remains a prototype tuning limitation.

The initial implementation fully refitted handles at every positive strength,
causing a discontinuity observed in-game. The captured-handle regression now
checks a strength sweep and endpoint translation. See the
[session record](session-notes/2026-09-26-1806.md) for measurements and limitations.

## Run the external checks

From the repository root, with .NET 8 installed:

```powershell
dotnet run --project NetworkTools.Geometry.Tests/NetworkTools.Geometry.Tests.csproj --configuration Release
```

This is a dependency-free executable test harness, not a `dotnet test` project.
Failures throw and exit nonzero. It links the production source directly and needs
no installed game, Unity libraries, native allocation shims, or NuGet test packages.
It is intentionally separate from the game-dependent solution build.

Checks cover an analytic three-point solution, nonuniform straight paths, zero
strength, zigzag reduction, fixed points, traversal reversal, translation and
rotation invariance, and invalid inputs. A 257-point unevenly spaced path with
multiple pins is checked against the independently differentiated objective.
Handle checks cover straight cubics, shared tangents, reversal, and cusp/backward
direction rejection.
These establish numerical behavior; they do not verify rendering or game integration.

See the [Smooth Curve plan](smooth-curve-plan.md) for integration decisions still open.

## Debugging slider behavior

The first in-game trial reported abrupt movement around the slider midpoint and
side-to-side movement on another selection. Apply looked acceptable to the tester;
preview/Apply disagreement is not established. The original logs capture parameter
changes but not sufficient geometry to diagnose this report.

Debug builds now emit `[NetworkTools.SmoothTrace]` followed by JSON in `Player.log`
through Unity logging. Each actual smoothing job records its ID, Preview/Apply
mode, factor, validity, entity IDs, pins, input/output positions, edge orientation,
and input/output Bézier controls. Coordinates are world-space XYZ in metres;
edge controls retain stored edge order, with `forward` identifying path traversal.
On rejection, output arrays retain input geometry, not the rejected candidate.
Selections exceeding 128 nodes emit only a summary to bound logging cost.

Reproduce on a short path using explicit factors such as 0.45, 0.49, 0.50, 0.51,
and 0.55, then revisit the same values without applying. Preserve the logs before
restarting the game. Compare identical-input captures first; only then attribute
changes to the solver. These traces capture requested geometry before the game's
preview/rendering or subsequent network updates, not the resulting live entities.
Managed formatting and logging are excluded from Release builds. Debug logging
can affect timing, particularly during rapid slider dragging.
