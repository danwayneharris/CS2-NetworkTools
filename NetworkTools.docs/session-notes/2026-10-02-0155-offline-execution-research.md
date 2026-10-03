# Offline execution research — 2026-10-02 01:55 PDT

Goal: reproduce the bounded native geometry pipeline behind the ramp-to-road
preview/permanent discrepancy; do not fix production feature behavior.

Isolated branch `dan/offline-game-execution` starts at d03bdc5. Worktree is under
the original checkout's ignored Temp/offline-game-execution directory so research
files stay within the authorized writable workspace. Existing worktrees and user
changes are preserved. Explicit absolute source/Bridge paths are required because
the usual sibling layout does not apply to this nested worktree.

Verified installed Game, Unity.Entities, Unity.Collections, Unity.Mathematics and
Colossal.Mathematics SHA256 values match the September 28 decompile manifest.
This establishes source identity, not loaded runtime identity.

Initial legacy replay invocation through uv failed before executing because the
sandbox cannot write uv's user cache. No dependency installation or game contact
occurred. Next use the existing Python interpreter directly or a workspace cache.

Source review confirms the existing scalar replay omits component-presence gates,
participant discovery, mixed temporary/permanent handling, edge-height-map writes
and downstream finishing. Its fixture uses the product's 5 cm tolerance and its
`iterations` is a zero-based loop index, not an explicit convergence report.
Preserve it as historical evidence; add discriminating research evidence separately.

Direct Python execution reproduced the historical replay: max error approximately
0.0001 m; loop index 99 (100 iterations). Raw report is in ignored
artifacts/offline-research/legacy-flatten-replay.json.

Added a standalone .NET assembly feasibility probe (no mod build/deployment).
First compile failed CS1654 because NativeArray's struct indexer cannot be assigned
through a using-declaration variable. This is a harness compile error, not evidence
that the native runtime is unavailable. Record this attempt before correcting it.
The installed SDK selected 10.0.401 for the net8.0 target; no SDK pin added yet.

## Original binary execution seam

Corrected the compile error with an explicit try/finally disposal. The probe runs
under .NET 8.0.31: original Game.Net.NetUtils.FitCurve executes, native-array allocation
throws SecurityException (`ECall methods must be packaged into a system module`).
Private GeometrySystem job metadata remains inspectable. This is concrete evidence
that a plain .NET host supports value-only calls but not this Unity allocation path.

Added fingerprint-gated direct calls to original private CalculateCutOffset, Cut,
StraightenMiddleHeights and LimitMiddleHeights methods. Their implementation is not
copied or translated. These methods read explicit value arguments, not populated
ECS lookups; the harness does not claim to execute their enclosing jobs.

Standalone build passes with zero warnings/errors. Seven subprocess cases plus
analytic output assertions pass: four original method calls on flat controls,
translation response, missing required input, malformed controls, unknown stage,
wrong game hash and empty input rejection (grouped as seven case invocations).
Results: artifacts/offline-research/seam-tests-1/summary.json. These are harness
tests, not native anchor/control/held-out differential qualification.

Next: freeze the retained evidence and build an actual participant/state capture
contract for initialization/flattening/finishing. Permanent divergence remains
unexplained; the goal is active. No game queries or mutations yet.

## Evidence inventory and live orientation

Created freeze-native-research.py and a manifest pinning 11 existing evidence files,
three decompiled source hashes and five matching installed assemblies. Bulk artifacts
remain untouched in the original investigation worktree. The manifest lists capture
gaps and does not relabel final-state observations as coherent pre-job inputs.

Read-only Bridge state at 09:02 UTC reports Wantagh, population zero, speed zero,
controls/rememberControl true. Process remains 54932, but citySession changed to
0100c8864c714d9f9805b29d558a3d44. Historical entities cannot be assumed current.
Unity status initially reported no attachment, suspension or breakpoints. A pure
value-only native cut-offset comparison was attempted, but connection failed BEFORE
evaluation: socket accepted with no Mono debugger greeting. No expression ran.
Do not retry blindly or take another debugger's slot. Asked whether the other
debugger has detached; continue offline while clarification is pending.

No saves, network edits, rebuilds, deployment or game lifecycle changes performed.
The direct read command's response confirms the city already remained paused.
Added an explicit capability registry: no live checks are retired yet.
