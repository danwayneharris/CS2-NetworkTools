# Combined smoothing: prototype and future options

## Current experiment (October 1)

The Debug-only prototype adds an opt-in **Constant Slope** option to Smooth Curve,
with the existing Smooth Start/End controls. It builds one complete 3D candidate
and uses one preview/Apply; it does not automate two sequential Apply operations.
Independent tools and their defaults remain available. Release promotion is not
part of this sprint.

Outer nodes, explicit split points and interior junction centers stay fixed in
XYZ. Free selected interior elevations may change. Ordinary split points must
have compatible original vertical grades; conflicting grades reject the candidate
rather than inventing a new transition policy. Interior junction attachments use
the existing individual boundary constraints, with native connectivity validation.
Unselected authored geometry remains fixed. Supported terminal ground ramps may
use the existing experimental native surface correction; perfect rendered slopes
and terrain/obstacle routing are not promised.

See [architecture checkpoint](combined-smoothing-architecture.md) for ownership,
validation and constraints, and [session evidence](session-notes/2026-10-01-1810-combined-architecture.md)
for the evolving test results. Initial mainline and terminal-ramp native Applies
passed; repeatability, broader fixture coverage and human review are still pending.
These initial passes are not general feature certification.

## Original design discussion and future options

The following discussion predates the prototype. Its generalized UI and Connect
reuse are proposals, not delivered capabilities.

Requested design review during the surface-repeatability investigation, October 1.

Recommendation: keep two player actions sharing one candidate-geometry pipeline:
**Smooth** edits an existing path; **Connect** constructs a new path. Start with
Curve + Slope in the existing shaping tool, retaining independent modes. Later
reuse the vertical-profile policies in Connect. Reserve Smart Connect for actual
terrain/obstacle routing rather than naming ordinary profile fitting as routing.

From an immutable selection snapshot, solve horizontal alignment, recompute station
distance along that alignment, fit vertical geometry with explicit constraints,
account for supported native surface/junction behavior, and validate the complete
3D candidate before one preview/Apply. If a junction adjustment changes horizontal
geometry, recompute its profile. Use bounded iteration. Do not implement this as
two sequential Apply operations.

Source seams at the original design checkpoint: RoadShapeToolSystem.Jobs.cs dispatches exclusive templates;
CurveSmoothTransform.cs changes XZ; SlopeLinearProfileTransform.cs recomputes
horizontal length before fitting Y; VerticalLinearProfile.cs handles endpoint/node
offsets. ConnectToolSystem.Jobs.cs and SimpleCurveGenerator.cs construct new edges.
Share mathematical constraints and candidate processing, not their different ECS
mutation semantics.

Possible UI: horizontal Preserve/Smooth/Straighten; vertical Preserve node heights/
Constant slope/Eased profile; optional boundary-grade matching. Pin guarantees
must explicitly distinguish horizontal position, elevation and grade. Existing
planar split points do not promise vertical-grade continuity. Preserving node
heights also need not mean freezing every vertical handle when horizontal lengths
change.

For a broader product, decide whether default combined smoothing retains hills
and valleys or seeks constant grade; what a pin fixes; how conflicting boundary
grades are prioritized; and whether neighboring edits are opt-in with a separate
scope tolerance. Terrain following, obstacles, bridges/tunnels and moving a crossing
railway are subsequent route-planning work, not this prototype.

Suggested sequence: finish surface-repeatability PR, prototype combined shaping
on terrain fixtures, reuse profile policies in Connect, then investigate routing.
