# Terrain/profile baseline investigation — 2026-10-01 03:26

## Verified baseline

Installed Game.dll matches the local decompile SHA-256
AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A.
The visibly launched terrain v1.1 save matches both the fixture package checksum
and complete region fingerprint (48 nodes, 44 edges). Controls are enabled and
remembered; the toy city is paused. No production changes or deployment yet.

Baseline Curve ramp-out at 0/0.5/1 then Apply passed strict geometry, topology,
elevation, directed connection and physical lane mapping checks. Constant Slope
with both boundary options false then passed exact native preview/permanent
comparison at all selected and incident edges. Node XZ and outer endpoint positions
are unchanged. The unselected branch translates with the moved junction by
-9.9235 m; independent audit confirms its endpoints/handles follow precisely that
translation (maximum float error 0.000037 m). This is existing Slope policy, not
permission for uncontrolled neighboring edits. Visual and traversal remain untested.

Raw evidence: artifacts/baseline-curve-ramp and artifacts/baseline-curve-then-linear.
Recoverable checkpoints were created before both operations:
- CitiesIIAgentBridge-regression-highway-ramp-out-20261001-102344-684940f7
- CitiesIIAgentBridge-provider-slope-20261001-102454-3c4510b6
These are unique saves; the v1.1 baseline is untouched.

The fresh non-deploying compile passes 96 existing numerical assertions. The
preserved five-edge off-ramp replay reproduces the historical permanent output
within 0.00006104 m and its -8.02303 percent dip. This five-edge example differs
from the seven-edge whole-ramp fixture; do not conflate their grades or endpoints.

## Installed terrain layers: source evidence, not a visual diagnosis

Paths below are relative to the setup-record decompile root, under src/Game.

- Game.Simulation/TerrainSystem.cs:2500 exposes the base heightmap separately.
- TerrainSystem.cs:4886 RenderCascade begins with the base heightmap, then calls
  DrawHeightAdjustments for lots, lanes and areas (near 4950). Its output is the
  cascade texture, not merely the original map surface.
- TerrainSystem.cs:3672 requests GPU readback of that cascade; :3583/:3592 copy
  readback values into CPU heights; :2615 GetHeightData returns those CPU arrays.
- Game.Simulation/TerrainUtils.cs:82 samples those data. The bridge's sample_terrain
  uses this API (src/CityCommands.cs:122). Thus our existing live terrain samples
  include the network-adjusted layer and may lag an outstanding GPU readback.
- Game.Net/GeometrySystem.cs:1725 onward changes generated edge boundary heights
  from its edge-height map and limits/straightens their middle heights, with
  different paths for composition flags. Its terrain sampling near :1764 expands
  bounds; that particular code does NOT demonstrate centerline clinging.
- Bridge JunctionInputs already exposes generated EdgeGeometry boundary cubics and
  composition state/flags. Those can be measured without adding mod-specific bridge
  code. Rendered mesh appearance still needs separate observation.

This supports investigating terrain feedback but does not prove that the observed
jank comes from terrain, or that road deletion recovers a unique original surface.
Keep base terrain, network-adjusted CPU samples, authored centerline, generated
boundaries and rendered mesh distinct in the next experiment.

## Runner changes

Existing exercise-tool-provider.py now accepts an explicit bridge path, Slope mode,
individual boundary toggles and reversed selection. Defaults use no boundary easing;
callers wanting the earlier smoke-test configuration must pass both smooth flags.
It records before/after geometry and checks permanent directed transitions, physical
lane mappings, node XZ, fixed endpoints and all observed incident preview curves.
The separate audit-slope-capture.py validates side-edge translation. Python syntax
check and the live linear case above passed. No weakened tolerances.