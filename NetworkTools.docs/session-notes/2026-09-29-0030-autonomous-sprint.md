# 2026-09-29 0030 - Autonomous regression and split-point sprint

Verified merged main checkpoints: NT 2180aec, bridge f0d6882. New local dan/autonomous-regressions branches start from these. Existing NT UI package-lock change remains user-owned and unstaged. No publishing authorized.

Priorities: reusable live regression runner; fresh baseline discovery and independent Apply verification; representative toy networks; tangent-continuous pinned split nodes; bounded longer-route and slope architecture investigations. Preserve baseline saves, use unique checkpoints, stay paused, no force killing. Record failures as well as successes and separate offline/native/permanent/visual confidence.

## Runner increment and first live blocker

Implemented scripts/live-regression.py: explicit mutation opt-in, fresh geometry-based endpoint discovery, identity-independent baseline fingerprint, fixed city-session check, request/response capture, no mutation replay, bounded polling, verified unique checkpoint package, strength sweep and independent permanent topology/elevation/connection/incident-curve comparisons. Four offline guard tests pass. Fixture has rail, four-way road branch, highway on/off ramp cases.

First discovery failed because get_network_edges includes long edges with far nodes outside get_network radius. Fingerprint now explicitly marks absent far-node positions; scope remains bounded, not whole-map validation. Discovery found 37 nodes and 39 edges. First live attempt saved a valid checkpoint then stopped at nt_activate: Debug adapter unavailable. No smoothing occurred. Added earlier adapter preflight and baseline save hash guard.

Environment evidence: local mod folder is .NetworkTools (disabled); content_load.json lists the large normal playset. User-specified reduced test playset is not currently active. Gracefully closed PID 49932 after preserving checkpoint; no forced termination. Asked for reduced playset name while continuing offline. Current runner remains a prototype: broader polling stability, complete selected-path preview comparison and forbidden crossing fixtures still pending. Existing connection comparison uses directed lane endpoint identities, not counts.

## Split target research increment

Added managed PlanarSplitTarget research builder and executable tests. It partitions at explicit split flags, derives one shared tangent from adjacent anchor chords, reconstructs split curve endpoints at the pinned node (rather than preserving a dogleg offset), calls the existing target fitter per section, and publishes only after every section succeeds. Interior junction pins still reject even when also selected as splits. Full-strength target test checks positions, shared positive tangent direction, outside endpoints and no partial result on failure. Existing geometry suite passes.

Not integrated or deployed. Managed arrays are unsuitable inside the current Burst job without adaptation. Partial-strength source/target blending can reintroduce source tangent mismatch: this must be addressed explicitly before claiming split continuity throughout the slider range. At strength zero, identity and correction of an existing kink cannot both hold. Current test establishes planar target continuity only, not curvature or 3D grade continuity.

## Longer-route and architecture review

Added sprint-architecture-review.md with source-grounded boundaries, runner readiness gaps, slope test recommendations and a combined horizontal/vertical candidate design. No slope bug claimed or runtime slope changes made.

Added scripts/explore-longer-target.py and retained PNG/SVG/JSON plots. A sextic single-bulge profile preserves straight boundary tangents and zero endpoint curvature, with explicit +/-20 m corridor. Numerically solving sampled arc length gives +2 m with 8.74 m amplitude and +5 m with 13.99 m amplitude. +10/+20 m are infeasible within this particular target family and corridor (not universally impossible). Mirrored routes have equal length, exposing the missing side-selection policy. No negative slider implementation: length bias and smoothing strength remain separate concepts. Plot generation succeeded through isolated uv matplotlib environment.

## Runtime split wiring (not deployed)

Replaced the managed-only target with a shared pointer implementation and managed test wrapper. Added per-node split flags, selection-owned identity set, UI list, and shared mutation validation. Split constraints are hard at every strength including zero; paths without splits retain prior behavior. Offline sweep/reversal/multiple-split/nonfinite tests pass, C# Compile and TypeScript --noEmit pass. Bridge nt_split compiled and 61-command catalog check passed. Full deployment and live tests still pending while user manually launches corrected playset.

User reported local mod absent. Verified original DLL remained intact in Mods/.NetworkTools with SHA C093D6FB7005F2AEB97663318F6E8E1FE993835FF69967BB850A3D13F780BB41. With no game process running, renamed only that directory to NetworkTools; destination absent, parent paths checked, DLL hash unchanged. This restored the milestone binary, not sprint code. A new game process 37508 appeared afterward; no city loaded at last observation.

Expanded runner now traces the selected path, captures every selected preview edge through shared nodes, rechecks the exact submission token before Apply, and checks fixed/unselected geometry afterward. It rejects missing preview coverage. Successful response duplication (.txt plus .json) removed for future captures. No claim that these new checks have passed live yet.
