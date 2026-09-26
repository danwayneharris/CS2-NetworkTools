# Curve geometry module

`NetworkTools.Mod/Geometry/PlanarFairing.cs` implements a game-independent fit of
horizontal node positions. It is compiled into the mod but is not called by a tool
yet. Smooth Curve remains disabled. This is the numerical foundation for a prototype,
not a complete road-curve generator.

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
  same source to run in ordinary .NET tests. Burst compatibility is intended but
  remains unverified until it is called from a Burst-compiled job.
- Coordinates must be finite, consecutive points at least 1 cm apart, and strength
  finite and within [0, 1]. Strength zero returns an exact copy of valid input.
- Invalid input, numerical failure, or a fitted chord that collapses or reverses
  relative to its original direction returns `false`. Discard all output on failure;
  buffers may have been partially written. Input remains unchanged.
- Positional pins do not constrain tangents. There is no self-intersection check,
  minimum turning radius, obstacle avoidance, terrain query, or displacement cap.
- There are no elevations, game entities, intersection offsets, or Bézier handles
  in this API. The game adapter must preserve elevations/topology, decide which
  junctions to pin, fit segment handles, and reject unsuitable results before Apply.

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
These establish numerical behavior; they do not verify rendering or game integration.

See the [Smooth Curve plan](smooth-curve-plan.md) for integration decisions still open.
