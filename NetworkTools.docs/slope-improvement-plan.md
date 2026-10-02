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

## Terrain-deformation hypothesis to investigate

Moving the ramp away from the steep hillside, through strength changes or another
pinned node, made the visible jank disappear in the maintainer's test. Investigate
terrain modified by network placement versus terrain sampled without that network;
verify the game's actual sampling/deformation layers before assuming a particular
model. The measured centerline grade discontinuity does not rule out an additional
terrain/rendered-surface effect. See [observation and setup notes](session-notes/2026-10-01-terrain-observation-plugins.md).

## Active experiment: horizontal-distance Constant Slope

The terrain-profile sprint integrates an offset-aware fit for Constant Slope only.
See [model and offline limits](session-notes/2026-10-01-0330-offset-profile-research.md)
and [integration checkpoint](session-notes/2026-10-01-0331-constant-slope-integration.md).
The first forward/reverse native test now passes; see
[native validation](session-notes/2026-10-01-0338-profile-native-validation.md). Ease/Arch and coordinated
Curve/Slope remain separate work; this is not terrain-following behavior.


## October 1 acceptance priority: stable improvement before perfect grade

Dan visually confirmed the first surface-aware ramp candidate looks MUCH better
and the small discontinuity is gone. That is valuable evidence, but repeating it
moved an interior node by another 1.17 m. Idempotence (same operation again should
not keep moving the network) takes priority over exact constant grade. Investigate
and fix that feedback; retain the visual improvement. Small geometric residuals
through 5 cm are accepted. Pursuing perfectly constant generated slopes is a
much-lower-priority future refinement, not a reason to delay a stable useful tool.


Update: the current Debug native-preview loop resolves the observed repeated-Apply
feedback before Apply. It keeps the 5 cm threshold, requires two small consecutive
candidate changes, and fails closed on a lost correction or bounded-search failure.
See the [verification record](session-notes/2026-10-01-0552-profile-repeatability.md).
A combined Smooth tool and Connect reuse are proposed separately in
[combined smoothing options](combined-smoothing-options.md); no new UI policy has
been chosen or implemented during this fix.
