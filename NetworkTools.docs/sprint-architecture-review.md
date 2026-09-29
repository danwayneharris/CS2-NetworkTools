# Bounded architecture review — 2026-09-29

This is a source review and design proposal, not evidence of new runtime behavior.

## Boundaries worth keeping

- `Geometry/PlanarPathTarget.cs` owns the pure horizontal target. It does not know
  entities, prefabs, native lanes, bridge permissions or UI state.
- `Transforms/CurveSmoothTransform.cs` translates traversal-oriented curves and
  node positions into pure inputs, then publishes only a valid full candidate.
- `RoadShapeToolSystem.JunctionSearch.cs` performs the Debug-only native candidate
  search. Its bounded scope and experimental timing must remain explicit.
- `RoadShapeToolSystem.Automation.cs` reuses the tool state machine. The sibling
  bridge owns permission and STOP checks. The runner owns fixtures and evidence.

Keep these boundaries; do not fold bridge transport or test orchestration into the
geometry solver. The managed split-target prototype is deliberately not called
from the Burst job. Port its allocation model before runtime integration.

## Focused issues exposed by automation

1. Installation is not activation: local `.NetworkTools` was disabled on the first
   overnight launch. A runner must preflight the actual API, not advertised bridge
   commands or an installed DLL hash. Playset identity needs a separate readiness check.
2. A city name and population are weak identifiers. Require a verified baseline
   package plus a geometry fingerprint, and reject city-session changes mid-test.
3. `PreviewProbe.cs` still calls itself diagnostic-only, but its matched submission
   now participates in automation readiness. Update the comment when strengthening
   that contract; repeated observations do not prove a formal rebuild fence.
4. The current runner checks bounded network identities/elevations and watched
   junction curves. It does not yet verify every selected preview edge, every remote
   lane, or traffic traversal. Do not advertise a complete regression suite yet.
5. Captures are large. Retain evidence, but future runs should avoid duplicate raw
   JSON in both `.txt` and `.json` on success; preserve raw output only on parse errors.

## What transfers to slope

`Transforms/SlopeLinearTransform.cs` builds a height reference and applies heights
using cached absolute path ratios. Its optional end matching changes only control
point heights. `SlopeEaseInOutTransform.cs` uses height derivatives with respect to
path ratio and multiplies by control-point ratio differences. These are not direct
world-distance grades unless the station mapping supports that interpretation.

`Core/TransformPipeline.cs` computes interior node positions by averaging adjacent
endpoint displacement; Smooth Curve deliberately bypasses this rule. Therefore
copying horizontal endpoint logic into slope without testing node offsets would be
unsafe. The shared improvements are fresh-input checks, preview/Apply comparisons,
reversed traversal fixtures and independent connection readback.

No slope bug has been established by this review. Recommended tests: unequal segment
lengths, reversed edge storage, nonzero node-to-endpoint offsets, bridge/tunnel flags,
and SmoothStart/End enabled individually and together. Measure resulting grade in
world distance rather than relying on parameter labels.

## Combined tool proposal

Keep independent horizontal alignment and vertical profile solvers under one user
workflow. First compute horizontal alignment, then recompute horizontal arc-length
stations, fit height versus those stations with explicit grade/vertical-curvature
limits, and construct one candidate for preview/Apply and connection validation.
Reusing pre-horizontal-edit stations would apply a different grade than intended.

Modes should make elevation policy explicit: Preserve node elevations (current
curve behavior), Fit slope, or later Respect terrain. Fitting slope intentionally
changes elevations and is outside this sprint's runtime scope. Do not silently
introduce it while adding split points. A combined candidate must fail as a whole
if either solver or native connection validation fails.

## Cleanup timing

Do targeted contract fixes now. Defer consolidation of preview/search/automation
state and larger transform-pipeline changes until repeatable live cases protect
behavior. Debug-only native validation and Release behavior currently diverge;
that deserves an explicit design decision before a player-facing release, not an
incidental cleanup during this sprint.
