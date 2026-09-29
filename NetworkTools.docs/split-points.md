# Split-point prototype

Implementation on `dan/autonomous-regressions`; automated live results are listed below.
Human UI and visual validation remain pending.

Select an existing Smooth Curve path, then choose intermediate nodes from the
Split points list. Nodes are numbered from the start; distances are approximate
3D chord distances from the start. Junction nodes are disabled. Clearing,
extending or trimming the path clears the split choices.

A split is a **hard planar join constraint**, separate from smoothing strength:
the node stays fixed, adjacent curve endpoints meet at that node, and their
horizontal tangents share one forward direction. This constraint applies even at
strength zero. Thus enabling a split can change a previously kinked join at zero;
the slider then blends the remaining geometry toward the section targets. With
no split selected, existing behavior, including zero-strength identity, is retained.

This choice avoids claiming an impossible combination: a kinked input cannot be
both unchanged at zero and tangent-continuous at zero. It needs user UX evaluation.

## Geometry and ownership

`Geometry/PlanarSplitTarget.Native.cs` partitions the ordered path at split nodes,
derives one shared direction from the neighboring anchor chords, and fits each
section through the existing `PlanarPathTarget`. It constructs candidate arrays;
the game transform publishes only if the entire candidate and float conversion
pass. Scratch storage is caller-owned native memory. The managed wrapper calls
the same pointer implementation for executable tests.

All node and control-point elevations are retained by `CurveSmoothTransform`.
The continuity guarantee is planar tangent direction (G1), not matching derivative
magnitude, curvature, vertical grade, or physical vehicle traversal. Interior
junctions use a separate Debug validation path; a junction cannot itself be chosen
as a split. A split never overrides junction validation. Boundary junction
search rotations affect only the outer selection boundaries.

`RoadShapeToolSystem.Splits.cs` owns entity-based split choices and validates both
UI and bridge mutations. Each change invalidates the preview. Job scheduling
waits for the previous job before copying split flags to node-state inputs.

The bridge `nt_split` request carries current tool session/revision, a live node
identity and an enabled flag. `nt_get_state.splitChoices` supplies eligible
candidates. Apply still requires the current verified submission.

## Verification so far

- Existing geometry suite plus split position, displaced endpoint, tangent,
  0..1 strength sweep, reversal, multiple-split and nonfinite-input checks pass.
- C# Debug compilation and TypeScript no-emit check pass.
- Bridge compile and 61-command contract check pass.
- Full Debug postprocessing, webpack and deployment passed on 2026-09-29.
- Two interior rail splits passed native preview and permanent Apply at strengths
  1.0, 0.5 and 0, including pinned positions, planar tangent continuity, topology,
  elevations, lane composition/directed transitions and preview/Apply agreement.
- One-split run passes those join/connectivity checks but retains a strict-test
  failure for about 3 mm of native alignment drift at the outer junction center.
  Offline replay of installed NodeAlignSystem predicts that drift within 0.052 mm.
- Two interior road splits also passed full-strength Apply, including all 16
  directed car-lane transitions at the four-way endpoint (U-turns included).
- Release/Burst, human UI/visual validation and broader asset coverage remain pending.
  See the sprint session note and captures for exact case scope.
