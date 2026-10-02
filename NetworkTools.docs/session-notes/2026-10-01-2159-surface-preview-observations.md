# Bounded preview/surface investigation

Dan confirmed the combined highway slope is visually fixed. Further observations:
preview differs visibly from Apply around the descending one-lane ramp attachment;
residual horizontal highway waviness; terrain/junction visual asymmetry between
on-ramp and off-ramp. Asked to record evidence without a deep fix, then explicitly
authorized Apply of the current preview and comparison afterward.

## Current selection and operation

Read-only Unity screenshots before/after are in the conversation. Live provider
reported independent SlopeEaseInOut, not combined: one selected edge, two nodes,
end junction degree four, other end degree two. Starting flatness UI 23% (internal
0.115), ending 100% (internal 0.5), both smooth-boundary options enabled. Captured
fresh submission 1483; verified unchanged session/revision/submission before Apply.
Checkpoint ZIP verified before mutation; no parameter or selection changes.

Before: `CitiesIIAgentBridge-surface-preview-before-apply-20261002-045548-ef974c79`
After: `CitiesIIAgentBridge-surface-preview-after-apply-20261002-045856-b9415c47`

## Findings

All five observed authored Curve cubics match preview versus permanent exactly.
Generated EdgeGeometry does not: selected-edge corresponding surface control points
have maximum 2.656899m 3D difference and 2.276855m vertical difference. These are
control-point comparisons, not mesh/Hausdorff error estimates. Side-road surface
controls also differ. The generated permanent surfaces were identical across
immediate, +3sec and +8sec observations, so the mismatch did not simply settle away.
At 225 identical sampled XZ points around incident curves, terrain height changed
by up to 0.414m after Apply. This supports investigating terrain/native generation
context; it does not isolate terrain as the sole cause. Junction cutting, boundary
constraints and composition effects remain possible contributors.

Important qualification: prior exact preview/Apply checks generally compared authored
Curve values and lane identities. They did not establish rendered surface or terrain
agreement. This capture is a concrete counterexample to making that stronger claim.

Source: RoadShapeToolSystem.Jobs.cs dispatches SlopeEaseInOut through TransformPipeline,
without the experimental Constant Slope surface correction. Even that correction's
supported envelope in SlopeSurfaceProfileTransform.cs excludes single-edge selections,
boundary smoothing and many junction/structure configurations. Do not classify this
as a demonstrated regression of combined mode without a matched control.

Horizontal waviness is a separate behavior: CurveSmoothTransform builds its planar
input with SmoothPinned nodes; junction XZ remains fixed. Smoothing therefore does
not promise an endpoint-to-endpoint straight line through displaced junctions. A
future generalized shaping/pin policy may relax that; current Straighten is the
separate user action, not an instruction to silently move protected junctions.

## Recommendation and scope

No runtime fix attempted. Prioritize a bounded native-surface preview/Apply comparison
fixture before further slope perfection: reproduce from this checkpoint, measure
junction surface boundaries as well as authored curves, then isolate terrain versus
junction generation. Keep minor residual slope/terrain aesthetics and automatic
junction-XZ relocation lower priority. Preserve existing degree-two lane-remapping
finding as a separate correctness item.

Raw captures: ignored artifacts/surface-observation-397e08de and
artifacts/slope-surface-preview-apply. Compact summary:
2026-10-01-surface-preview-comparison.json. Reusable tools:
compare-current-slope-preview.py (checkpoint + explicitly authorized one Apply),
summarize-slope-surface-preview.py (offline only). Summary regenerated successfully.
No build or deployment; runtime remains 00c789a. Game left paused, selection cleared
by Apply, resulting permanent state checkpointed; no subsequent unsaved edits.
