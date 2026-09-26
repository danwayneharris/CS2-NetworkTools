# Smooth Curve plan

**Status (Sept 26, 2026):** a standalone [node-position fitting module](curve-geometry.md)
and external tests exist. Game integration and Bézier reconstruction are not yet
implemented; Smooth Curve remains disabled. This document records the scope and open design
questions. See [system architecture](system-architecture.md#10-smooth-curve-current-infrastructure)
for the existing integration points.

## Goal

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
chord-length stations. Bézier reconstruction and the integration point in the
transformation pipeline remain undecided.

Before game integration, define:

- Which nodes are fixed, including path endpoints and intermediate junctions.
- How endpoint directions and connections outside the selection are preserved.
- How much movement is allowed and what each UI parameter controls.
- What continuity is required: matching positions, tangent directions, and curvature
  are distinct requirements.
- How degenerate geometry or unsuitable selections are reported to the user.

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
