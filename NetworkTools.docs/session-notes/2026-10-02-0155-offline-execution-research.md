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
