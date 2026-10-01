# Smooth Curve plan

**Current status (Sept 29):** ordinary split points and Debug interior-junction
smoothing are implemented and have bounded native preview/Apply evidence. Read
[split points](split-points.md), [interior junctions](interior-junctions.md), and the
[sprint report](session-notes/2026-09-29-sprint-report.md) for current limits. Dated
Sept 26–28 sections below record the earlier design history. Side-bias controls,
junction-as-split semantics, and combined curve/slope smoothing remain proposals.

**Current checkpoint (Sept 28, 2026):** Debug builds now experiment with native
preview candidate search for one three-arm rail junction at a selection endpoint.
One saved regression passed preview and Apply: a +3 degree boundary correction
preserved all four directed track connections where the previous fit lost one.
This is not general junction support or Release validation. Interior junctions
remain rejected; roads retain previous behavior; already-broken connections are not
automatically repaired. See [validation confidence](offline-validation-confidence.md)
and [search experiment](session-notes/2026-09-28-1500-native-junction-search.md).
The Sept 26 status below describes the earlier baseline.

**Status (Sept 26, 2026):** the [geometry module](curve-geometry.md) has a first
game-integration prototype with Bézier reconstruction and the existing smoothing
control. External tests and full Debug/Release builds pass, including Windows
Burst compilation. Debug is deployed. The maintainer reports successful smoothing
of distorted test paths; real-city elevated-rail screenshots show improved curves
and a matching post-Apply view. Train traversal, save/reload, and Release execution
remain unverified.
This document records the scope and open design
questions. See [system architecture](system-architecture.md#10-smooth-curve-current-infrastructure)
for the existing integration points.

## Goal

The current experiment now integrates the boundary-target reconstruction tested
in the [session notes](session-notes/2026-09-26-1806.md). It replaces the active
node-fairing policy for simple forward-going selections, rejecting interior
junctions. Earlier approaches are preserved in the session notes and geometry guide.
Next broaden validation of game-generated connections, then generalize target selection beyond
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

The current implementation builds one planar cubic from the selected outer curve
endpoints and their tangent directions. It partitions the target by original node
chord-length ratios and blends all controls and interior nodes toward it. Endpoint
nodes stay fixed; positive-strength selections with interior junctions or backward
node chords are rejected. The dedicated transformation bypasses generic
edge-to-node displacement averaging. Partial strength may retain input defects.
See the [geometry guide](curve-geometry.md) for the implemented algorithm and
[session notes](session-notes/2026-09-26-1806.md) for the experiments that led to it.

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

## Junction investigation checkpoint (Sept 28)

See [rail junction rules](rail-junction-rules.md) for the installed game's curviness
filter and an offline comparison of connected/disconnected merges. Exact eligibility
replay still needs prefab limits and composition-derived connection geometry.
Keep junction work local until it is further along. Bridge fork/push is a later
repository-management task; the bridge remains a development-only tool.

## Signed length controls and split points (research, September 28)

The current 0..1 factor blends original geometry toward one boundary target; it is
not an arc-length controller and has no general shorter-length guarantee. Merely
allowing negative input extrapolates existing errors. Run
`uv run python scripts/explore-smoothing-controls.py`: the illustrative S-curve
increases from 103.57 m to 113.02 m at -1 while doubling lateral excursion. This
counterexample is not an execution of PlanarPathTarget, whose boundary tangents
remain fixed. It refutes the general claim that negative blending is smoothing.

A signed control should choose a target arc length relative to the original,
then minimize bending/curvature variation under endpoint, tangent, topology,
elevation and junction constraints. Some lengths are infeasible (below endpoint
chord distance, or longer within a bounded corridor with restrictive tangents).
Report infeasibility rather than generate loops. Expose length bias separately
from smoothing strength initially; signed UI semantics need in-game evaluation.

For split points, retain separate ordered split-node identities inside the existing
selected path; existing selected waypoints currently extend path selection and are
not automatically split constraints. Convert splits to inclusive node ranges with
shared boundary nodes and disjoint edges; a tested research helper is in
scripts/explore-smoothing-controls.py. Fit each range from the same original
snapshot, then publish all outputs only if every range succeeds. Fix split node
positions/elevations, but choose one consistent tangent direction on both sides
for a smooth join, or explicitly allow a player-designated corner. Independently
preserving each side's original tangent can preserve a kink. Keep endpoint offsets
separate from node positions. A split at an intersection does not exempt unselected
connections from validation. No runtime UI or fitter changes are implemented here.

## User clarification: centered side-bias control

Dan clarified that "negative" smoothing is a UX description, not a requirement to
negate weights or increase path length. The desired exploratory behavior is a
centered slider: one extreme biases the smoothed result toward one side of a janky
path, the opposite extreme toward the other side, and the middle is equivalent to
zero. This supersedes treating extra route length as the primary requirement.
Side orientation (selection direction versus world/screen direction) and the exact
relationship between bias and smoothing remain design questions; do not silently
choose them as established requirements. Lower priority than live regression and
split points. Retain the longer-route experiment as research, not the chosen UX.

## Sprint scope addition: interior junctions (2026-09-29)

Dan explicitly added smoothing a selected path whose junction is an interior node,
not only its start/end. This is separate from split points. Finish the current
split validation before starting it, then prioritize using the regression harness.
Preserve intended lane/rail connections and absent crossing connections, topology,
elevations and account explicitly for unselected incident branches. Existing
interior-junction rejection stays until a tested replacement exists; do not simply
remove the guard or silently treat a junction as an ordinary degree-two split.

### Follow-up test: interior junction selected as a split

After interior-junction smoothing is working, explicitly test selecting that junction
as a player split. Unlike an ordinary degree-two split, selected junction branches
can have an intentional nonzero relative tangent angle. Do not automatically force
G1 continuity across them. Pin the junction, minimize change to the original
relative angle where feasible, and preserve intended/forbidden connections on all
incident branches. Consider coordinated tangent rotation as a later refinement;
connectivity and geometric validity take precedence over angle preference. The initial
prototype preserved both selected incident handles exactly; the current bounded
common-rotation search preserves their relative angle. It still rejects combining
junction and ordinary split flags. That rejection must remain explicit until this
separate semantic case is designed and tested.

## Side-bias UX experiment (2026-09-29)

Run `uv run --with matplotlib python scripts/explore-side-bias.py --output <folder>`
for the saved comparison in `session-notes/plots/side-bias-20260929`. This is an
offline analytic research example, not the production fitter or native feasibility.

Two coherent choices remain for discussion:

1. One centered slider: strength is absolute slider displacement. Center restores
   the original path; either extreme smooths fully toward a side-biased target.
   Passing through zero necessarily reintroduces the original wiggles.
2. Separate strength and side bias: zero bias gives the ordinary smooth target at
   the chosen strength. Sweeping sides need not unsmooth the route, but center does
   not mean original geometry unless strength is also zero.

The plotted displacement uses a bounded sextic envelope with zero value, first
and second derivatives at section endpoints. It demonstrates endpoint-preserving
side choice, not a solution for arbitrary hairpins or intersections. Its fixed
chord normal has a clear side only in this example; selection-relative side flips
when selection order reverses, world-canonical orientation can jump near its sign
boundary, and screen-relative side changes with the camera. Do not silently choose
one of these conventions. Junctions/splits would require constrained section fits
and native validation; neither alternative is integrated yet.

## Deferred: controlled changes outside the selection (2026-10-01)

Dan clarified that preserving outside geometry is a default constraint, not an
absolute product requirement. Small changes to adjacent, unselected geometry may
be worthwhile when they improve the player's requested result.

Revisit an explicit player-facing policy: disallow changes, allow bounded changes,
or configure a tolerance. Decide what the tolerance measures (node displacement,
curve displacement, tangent/grade change), which neighboring segments it covers,
and how the preview communicates that expanded affected area. These are open UX
and geometry questions, not an agreed numeric threshold or implementation.

Permission to adjust geometry does not imply permission to lose intended lane/rail
connections, add forbidden connections, or change topology. Those constraints need
independent verification even when small neighboring adjustments are allowed.

Keep this out of the current temporary-edge combining bugfix. That fix must first
interpret equivalent preview representations correctly under existing preservation
checks. Do not confuse a temporary entity reorganization with a geometric change,
or loosen geometry tolerances merely to get past an unresolved mapping failure.
