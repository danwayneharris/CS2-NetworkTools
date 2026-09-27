# Smooth Curve plan

**Status (Sept 26, 2026):** the [geometry module](curve-geometry.md) has a first
game-integration prototype with Bézier reconstruction and the existing smoothing
control. External tests and full Debug/Release builds pass, including Windows
Burst compilation. Debug is deployed. In-game verification, including execution
of the Release build, remains pending.
This document records the scope and open design
questions. See [system architecture](system-architecture.md#10-smooth-curve-current-infrastructure)
for the existing integration points.

## Goal

The current experiment now integrates the boundary-target reconstruction tested
in the [session notes](session-notes/2026-09-26-1806.md). It replaces the active
node-fairing policy for simple forward-going selections, rejecting interior
junctions. The earlier node-fitter discussion below records the preceding design.
Next validate game-generated connections, then generalize target selection beyond
a single cubic. Explicit boundary UI controls remain deferred.

Produce realistic, non-wiggly road and track geometry, including networks built
with Anarchy. The first prototype should let a user select an awkward path,
adjust smoothing parameters, preview the result, and apply it using the familiar
road-shaping workflow.

## Agreed first-prototype scope

- Move existing nodes in the horizontal plane while preserving their elevations.
- Preserve topology: do not add or remove nodes or network segments.
- Reuse the existing node markers, start/end selection, parameter controls,
  preview, and Apply workflow.
- Isolate the geometry computation so it can be unit-tested outside the game.

Preserving node elevations alone does not guarantee unchanged grades along segments:
horizontal movement changes distances, and curve control points also affect the
vertical profile.

## Geometry and integration questions

Path-wide fitting should account for alignment and boundary constraints across the
selection. The initial node fit uses regularized second differences on original
chord-length stations. Smooth Curve now has a dedicated path-wide transformation
inside the shape job, bypassing the generic edge-to-node displacement averaging.
Endpoint nodes and nodes with other than two incident edges are fixed. Original
horizontal tangent directions are retained at these fixed nodes; ordinary interior
nodes share the bisector of adjacent fitted chord directions.
These directions describe the full-strength reconstruction target. A subsequent
slider-continuity experiment blends original handles toward that target, so
partial strength can retain tangent mismatches. See the
[session notes](session-notes/2026-09-26-1806.md) for observed failures and tests.

Next investigate segment endpoint offsets and game connection reconstruction,
then a fit using samples of the actual centerline. Automatic corner classification
is deferred; explicit player choices for boundary direction (such as perpendicular
connections) may be preferable initially. No additional UI option is implemented.

Remaining design questions include:

- Whether to add optional boundary matching to unselected connections, beyond
  preserving the selected edges' existing directions at fixed nodes.
- How much movement is allowed and what each UI parameter controls.
- What continuity is required: matching positions, tangent directions, and curvature
  are distinct requirements.
- Whether rejected selections need more detailed feedback than the current hint
  and disabled Apply button.

Implementation must account for edge direction relative to path traversal and for
the offsets between intersection centers and Bézier endpoints. The existing
[geometry representation](system-architecture.md#8-geometry-representation-and-transformation-pipeline)
and [preview/apply mechanisms](system-architecture.md#9-preview-and-apply-are-different-output-mechanisms)
describe those boundaries.

## Validation direction

Use out-of-game tests for the geometry computation, then test representative roads
and tracks in-game. Acceptance criteria and numerical tolerances remain to be
specified. Checks should cover the agreed elevation/topology constraints, boundary
behavior, and preview/apply agreement. Include networks constructed with Anarchy;
disabled game validation is not a substitute for checking the resulting geometry.

## Later explorations

These possibilities are outside the first prototype:

- Remove or redistribute nodes, or rebuild long paths as subsegments using Connect.
- Combine horizontal curve smoothing and slope editing.
- Account for obstacles and terrain, including crossing, tunnel, and bridge options.
- Explore a more general Connect workflow that routes between selected endpoints
  subject to those constraints.

Update this plan as decisions are made. Once behavior is implemented, document its
actual architecture in the architecture guide and retain unresolved design work here.
