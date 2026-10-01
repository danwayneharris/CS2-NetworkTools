# Deferred Slope improvement sprint

Investigate a consistent vertical-profile fit, informed by the terrain toy cases.
Keep this separate from the current Smooth Curve work unless a regression requires
an immediate targeted correction.

- Define grade against horizontal traveled distance, rather than the old 3D shape.
- Include node-center/Bézier endpoint offsets and junction spans in the fit itself,
  instead of correcting endpoint heights after fitting and distorting the interior.
- Make Constant Slope boundary easing explicit; distinguish continuous height,
  continuous grade, and smooth changes in grade.
- Evaluate reuse of the geometry module's path sampling, constrained fitting,
  pinned sections and regression infrastructure. Do not assume a planar fitter can
  be reused unchanged: vertical profiles need their own station coordinate and constraints.
- Quantify interior grade error and curvature changes over complete segments.
  Endpoint checks and preview/Apply agreement are necessary but insufficient.
- Explore combined curve/slope fitting later, preserving the independent tools.
  Terrain avoidance and obstacle routing remain separate future work.

Important captured result: current endpoint alignment improves preview consistency
but deepens the first off-ramp segment's interior grade dip. See
[offline comparison](session-notes/2026-10-01-0215-slope-counterfactual.md).
Do not characterize that example as entirely pre-existing. Decide whether to accept
this limited tradeoff temporarily or address it before the broader sprint.

## Operation order and 3D continuity

The slope-first then curve-preview capture shows an existing Curve-only limitation:
keeping node and control-point heights while changing horizontal handle lengths
can break grade continuity. One ramp join went from matching -6.41% grades to
-8.21% versus -6.51%, with zero Y changes and exact native/requested curve agreement.
This is not established as a terrain reconstruction error. Preserve node elevations
as required, but investigate fitting vertical handles to compatible grades when
combining tools. See [the captured evidence](session-notes/2026-10-01-0230-slope-first-preview.md).
