# 2026-10-01 0205 — Applied Constant Slope off-ramp review

User visually approved Curve then Slope across the four toy networks, with some
remaining vertical unevenness. Highway off-ramp after Constant Slope is the exception.
Read-only capture; no game mutation, selection change, build or deployment.

City session a41734d7fe2947cd88002a6bc4feaa02, game paused. Current Slope state is
Idle/SlopeLinear, SmoothStart and SmoothEnd both true. This is current configuration,
not independent proof of the precise settings at the previous Apply.

Reusable scripts/audit-route-grades.py captures the permanent route and computes
analytic dy/d(horizontal arc length), sampled at 1001 points per cubic. The existing
highway-ramp-out fixture includes two mainline edges before the actual off-ramp.
Do not attribute their large grade jumps to the selected five-edge off-ramp.
The last Curve Smooth Apply trace identifies that ramp as edges 334827, 334831,
334835, 334839, 334845 (all version 7), from junction 334931 to terminal 334946.

Measured first off-ramp edge: 88.861 m horizontal length, grades -6.81946% at entry,
-8.02306% steepest, -6.81943% at exit; average -7.60616%. Remaining four segments
are approximately -6.82%. Ramp joins have zero height/planar gap in captured curves,
and endpoint grade differences below 0.0024 percentage points. Thus nonconstant
grade is confirmed, but a height/first-derivative discontinuity inside this ramp
is not. Curvature variation or generated junction/lane mesh may explain the visual
kink; no rendered inspection or native lane vertical-profile audit was performed.
The mainline-to-ramp boundary is a junction with separated curve ends, not a direct
C0/C1 join; do not treat its endpoint gap alone as a broken road.

Source findings:
- RoadShapeToolSystem.PathData.cs:210 uses stored 3D curve length; its cumulative
  distance also includes node-to-curve endpoint gaps. This exists in merged main
  4b5fc1c as well, predating the recent fixes.
- SlopeLinearTransform.cs:24 assigns heights using those absolute station ratios.
  EdgeState.CalculateControlPointRatios uses horizontal endpoint handle lengths.
  These metrics are not a consistent horizontal-distance grade parameterization.
- SlopeLinearTransform.PostProcess can override boundary grades when eligible and
  SmoothStart/End is checked. Checked options alone do not prove eligibility here.
- Recent Jobs.cs endpoint alignment restores endpoint/node height offsets and moves
  adjacent handles with them. It preserves each endpoint grade but can change the
  interior vertical shape. Do not categorically exclude our recent work as a
  contributor without replaying the captured input with/without that alignment.

Next proposed experiment: offline replay of the captured input separating stationing,
endpoint offset alignment and cubic interpolation. Define Constant Slope against
horizontal traveled distance and account explicitly for junction spans. Test sampled
interior grade, endpoint continuity and curvature changes, not just endpoint grades
or preview/Apply agreement. A single cubic vertical polynomial over an arbitrary
planar cubic does not generally represent exactly linear height versus arc length;
we must quantify fitting error rather than promise exactness without resegmentation.

Evidence: captures/offramp-constant-slope-review, captures/offramp-grade-audit.
No claim of vehicle traversal, visual approval of this specific ramp, or root-cause
isolation from this read-only audit. User's other toy-network visual approval is
recorded above separately from automated verification.
