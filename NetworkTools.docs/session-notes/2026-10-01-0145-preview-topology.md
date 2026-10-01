# 2026-10-01 0145 — Preserve RoadShape preview topology

The rail rejection comes from native NodeReductionSystem combining an unselected
incident edge with its neighbor. Apply directly edits existing curves and does not
perform that reduction. The shared BaseToolSystem already exposes a lifecycle-scoped
DisableVanillaNodeReduction switch (used by AddNode, RemoveNode, SlideNode and Parallel),
restoring the native system when the tool stops. Enable it for RoadShape as well.

Ground truth: Game.Tools/NodeReductionSystem.cs CanMove and NodeReductionJob;
GenerateEdgesSystem.cs resets a supplied NetCourse fixed index from the original,
so a course-only Fixed workaround would not address existing edges. No validators,
geometry tolerances, lane checks or Apply behavior are changed by this experiment.

Validation pending: compile and live rail/highway preview and permanent results.

## Native verification

Full Debug bootstrap build, postprocessing, UI build and deployment succeeded.
The build-generated lockfile churn was restored from the previously clean index
(the original user worktree was not touched). Native tests used game 1.6.2f1.

Preserved the user's current toy as
`CitiesIIAgentBridge-regression-before-reload-20261001-084309-aec178f7.cok`,
closed gracefully, and visibly loaded that checkpoint after deployment. Its
48-node/44-edge geometry differs from the earlier v1.1 baseline; the new
`scripts/fixtures/toy-preview-topology.json` records its exact hash/fingerprint.
No existing save was overwritten.

- Rail high branch: strengths 0, 0.5, 1 all accepted. Applied at 1. Five watched
  shared nodes retained all directed connections (four at the merge). Six selected
  edges changed. Selected and incident preview/Apply curves matched exactly;
  topology, elevations, pinned/unselected node positions and every unselected edge
  remained unchanged. Strict regression passed, including the previously separate
  fixed-node-drift check on this particular checkpoint.
- Highway mainline: after checkpointing the rail result, used the unchanged highway
  component (the new after-rail fixture records the complete current geometry).
  Strengths 0, 0.5, 1 accepted. Applied at 1. Both interior junctions accepted;
  three watched shared nodes retained directed connection sets of 9, 7 and 9.
  Three selected curves changed; strict topology/elevation/outside-selection and
  preview/Apply checks all passed, with zero captured curve error.
- Shared Slope regression: off-ramp ease applied with topology preserved and zero
  selected preview/Apply curve error. Independent audit preserved all six watched
  directed connection sets and node XZ. Slope intentionally translated one side-edge
  endpoint/handle down 6.177254 m with its moved junction; far end unchanged, maximum
  translation residual 0.000021 m. This is existing Slope behavior, not a new allowance
  for Curve Smooth outside-selection movement. Stale Apply revision was rejected.

Native interior validation logged 32–50 ms for the recorded later acceptance
observations; these are internal validation timings, not measured mouse-to-screen
latency. Rail and highway no longer emitted their former reduction-related rejection.
The highway success is consistent with the same root cause but the old highway
preview was not independently captured, so its exact prior reduction is unproven.

Evidence: captures/preview-topology-*; original failure in the linked previous
session note and rail-apply-blocked-native capture. No validator or tolerance was
relaxed. No math changed, so compilation and live pipeline regressions were the
relevant checks; existing offline math suites were not rerun merely for this switch.
Release/Burst, other assets, visual quality and vehicle traversal were not verified.

Final state: paused, toy citySession a5fae5bc608b485398d9ba453e25d324. Loaded the
pre-restart checkpoint above, then applied rail Smooth Curve, highway Smooth Curve,
and off-ramp Slope. Saved those results to
`CitiesIIAgentBridge-preview-topology-verified-20261001-084945-4e6972c4.cok`.
No subsequent network mutations. Original pre-test checkpoint remains available
for visual comparison. No bridge repository changes, push or PR.
