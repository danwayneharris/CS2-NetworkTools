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
