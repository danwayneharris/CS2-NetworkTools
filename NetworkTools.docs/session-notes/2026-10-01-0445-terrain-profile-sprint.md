# Terrain/profile sprint — 2026-10-01 04:45

## Setup and scope

PR #12 is squash-merged as ed3ab81; its tree equals the reviewed 03e956a tip.
Fresh NetworkTools branch dan/terrain-profile-sprint starts there. Bridge branch
dan/terrain-observation-sprint starts at origin/main 8843041 in a separate worktree.
Existing worktrees and user edits are preserved, including the bridge's local
unpublished e797920 debugger setup. No game process was found during setup.

## Plan and acceptance criteria

1. Read current source and replay the captured off-ramp before changing algorithms.
2. Trace installed terrain sampling, deformation and generated network geometry;
   distinguish centerline evidence from mesh/terrain hypotheses.
3. Run controlled baseline comparisons of Curve/Slope order and boundary settings,
   with verified toy identity, checkpoint, fresh IDs and permanent readback.
4. Fit a justified vertical-profile correction incorporating horizontal stations
   and endpoint/node offsets before fitting. Preserve native validation.
5. Run focused numerical counterexamples and native terrain regressions; prepare
   a few uniquely named review saves. Coordinated fitting is a stretch goal.

Acceptance is an evidence-supported diagnosis and useful experimental candidate,
not universal terrain correctness or release qualification. Keep topology and
connection checks strict. Record offline, source, preview, Apply, persistence,
human visual and traversal confidence independently. Raw runs go in artifacts/.
No push, PR, publication, baseline overwrite, real-city load or force-kill.

## Initial source observation

SlopeLinearTransform currently fits heights using EdgeState absolute ratios, then
TransformPipeline averages endpoint displacement to compute interior node heights.
The subsequent alignment is documented to preserve native endpoint/node offsets
but deepen a captured grade dip. The existing comments claiming a globally linear
profile are stronger than the captured evidence. No production changes yet.