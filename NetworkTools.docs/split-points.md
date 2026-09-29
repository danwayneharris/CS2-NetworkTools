# Split-point prototype

Implementation on `dan/autonomous-regressions`; not yet live-verified.

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
junction restrictions remain; a split never overrides them. Boundary junction
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
- Full postprocessed build, Release/Burst, live UI and live split Apply: pending.
