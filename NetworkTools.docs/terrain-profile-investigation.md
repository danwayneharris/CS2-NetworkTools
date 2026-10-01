# Terrain/profile investigation (experimental, October 1)

## What the evidence supports

The new Constant Slope fitter removes the measured post-alignment grade dip on the
same horizontal off-ramp geometry. It fits node/endpoint offsets up front and uses
horizontal arc length. Native authored curves match the independently predicted
fit to sub-millimeter precision. That agreement does not certify the final mesh.

Curve then Slope and Slope then Curve are different operations. Curve retains Y
coordinates while changing horizontal distances and handles, so previously matching
grades can diverge. The new fitter does not silently change Curve's contract.

## Installed source: terrain is not one height field

Source root: the configured local cs2-decompile repository, Game.dll 1.6.2f1,
SHA256 AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A.
Relative references below are under src/Game; no game source is redistributed.

- Game.Simulation/TerrainSystem.cs:2500 exposes `heightmap`, backed by the base
  RenderTexture. `SerializeHeightmap` at 3057 reads texture data to serialize it;
  it is not a cheap per-point public CPU sampler.
- `RenderCascade` at 4886 starts from that texture, then `DrawHeightAdjustments`
  at 4951 incorporates building lots, lanes and areas into the cascade.
- Cascade GPU readback near 3672 populates the CPU height representation. The
  `GetHeightData` method at 2615 returns that data. Its optional wait can complete
  pending readback; the bridge deliberately does not force GPU synchronization.
- Game.Simulation/TerrainUtils.cs:82 samples this CPU height data. The bridge's
  `sample_terrain` therefore does **not** expose untouched pre-network terrain.
  Network-adjusted heights and readback timing are confounding factors.
- Game.Net/GeometrySystem.cs:1725 updates generated left/right boundary heights
  and applies middle-height limits/straightening. Terrain sampling at 1764 expands
  geometry bounds for applicable composition flags; that code alone does **not**
  establish that the road centerline is snapped onto terrain.

Live Unity debugger type discovery confirms the separate base/cascade textures and
CPU array in the running game. No base-texture numeric readback, terrain edit or
road-deletion isolation test was performed. Deleting a road is not assumed to
restore a unique original surface. A clean base/deformed comparison needs an
explicit bounded GPU readback or a carefully controlled terrain experiment; that
is different from interpreting current `sample_terrain` as ground truth without roads.

## What remains unresolved

Generated EdgeGeometry half-curves meet in height on the observed ramp, but they
are not the complete rendered road, junction mesh or retaining structures. Dan's
observation that moving/pinning the ramp away from the steep hillside removes jank
is compatible with both a geometric-grade effect and an additional terrain or
rendering interaction. This sprint's authored-profile improvement does not choose
between those explanations or dismiss the latter.

The current bridge has no controlled terraforming query/mutation contract. A broad
new terrain-control API would distract from the profile test. Comparing alternate
Curve strengths changes both centerline geometry and its terrain placement, so it
is explicitly a confounded comparison, not terrain isolation.

## Review

See the sprint session notes and final review checklist for exact builds, operation
orders, checkpoints and acceptance categories. Terrain following, clearance routing,
combined pin/elevation policy and vehicle traversal remain separate work.

## Fixed-station neighboring-highway experiment (October 1, 04:43 PDT)

A controlled before/after comparison sampled 525 identical XZ positions across five
off-ramp edges, at 21 stations per edge and lateral offsets -12, -6, 0, 6, 12 m.
Three repeated CPU-terrain reads agreed exactly in each state. The pre-highway
network fingerprint exactly matched the named full-ramp review checkpoint.

Two effects are now measured:

1. The first ramp edge changes through the shared junction: authored controls A/B
   move down 3.8208 m, with C/D unchanged. Generated boundary controls change by
   up to 3.6003 m; sampled adjusted terrain by up to 4.788 m.
2. The next ramp edge retains exactly the same authored and generated boundary
   controls, yet nearby adjusted terrain changes by up to 3.0514 m. At its center
   the terrain difference is 0.0409 m, acceptable under the 5 cm policy. Three
   remaining ramp edges show no sampled terrain or authored/boundary change.

There is also a substantial authored-to-generated mismatch. At t=0.8 on the first
edge, the midpoint of its generated left/right boundaries is 2.7495 m above the
original authored curve (horizontal match residual 0.0112 m). After highway Apply,
the gap is 0.4097 m (horizontal residual 0.00325 m). This boundary midpoint is a
useful surface proxy, not proof of the final rendered lane/mesh height.

Generated edge boundaries do not cover the whole authored edge in this large merge:
the early stations have no close horizontal match. The analysis rejects matches
beyond 0.25 m and does not extrapolate those boundaries through the junction span.
This matching bound is a diagnostic association rule, not a replacement for the
user's 5 cm geometric acceptance threshold.

[Layer comparison plot](session-notes/plots/neighbor-highway-terrain-20261001.png)
shows authored profile, matched boundary midpoint and adjusted terrain. Solid-line
agreement alone is insufficient validation of player-visible slope. GeometrySystem's
height-map application and middle-height limiting (source lines 1725-1756) remain
relevant, but the responsible upstream junction calculation has not yet been isolated.

The experiment confirms neighboring terrain influence and derived-surface mismatch;
it does not yet establish that feeding pristine terrain to the fitter fixes either.
Next inspect the junction-height derivation and actual lane/surface profile, separating
that from terrain deformation. No production behavior was changed in this experiment.

## Native junction-height and lane-profile follow-up

The generated car-lane curves confirm the concern beyond the boundary-midpoint
proxy. The first ramp edge's two lane pieces reach sampled grades of 32.50% and
27.20% before highway slope, versus 17.50% and 7.86% afterward. These are absolute
magnitudes, distinct from the roughly 7.15% authored profile and from traffic tests.

A read-only live NodeGeometry check also confirms the source's shared-height stage:
the restored post-highway junction's derived height is 612.990 m, versus authored
node height 613.5095 m. Inverse-handle-length weighting of incident handle heights,
then halfway blending with node height, predicts 612.989985 m. The resulting
micrometre difference is insignificant. This validates one native stage, not a
complete inverse model of the generated road surface.

See [source trace and evidence](session-notes/2026-10-01-0446-generated-junction-heights.md).
The new `scripts/analyze-generated-ramp.py` retains the restricted formula replay
and lane measurements. Next isolate subsequent junction cutting/flattening and
middle-height limiting before designing a surface-aware fitting correction.

## Middle-height limiter isolated

[The native bounds replay](session-notes/2026-10-01-0455-middle-height-isolation.md)
reproduces both captured middle heights. Before highway slope, the trimmed boundary
requires a 20.5-21.4% average grade, exceeding its actual prefab's 20% setting.
The native middle-height interval becomes inconsistent and collapses to a fixed
fallback height. After the highway edit, that interval is feasible and captured
middle heights lie on its contracted lower bound. This explains a decisive
height-generation stage; it does not yet isolate every upstream anchor adjustment.

The limiter constrains endpoint differences over each half, not maximum cubic
slope, which explains why generated lane peaks can exceed the prefab setting.
Do not bypass it as a fix. Fit against feasible generated boundary conditions and
validate lane profiles; terrain deformation is a separate coupled consideration.
Reusable replay: `scripts/replay-native-middle-height.py`; analytic checks:
`scripts/test-native-middle-height.py`.
