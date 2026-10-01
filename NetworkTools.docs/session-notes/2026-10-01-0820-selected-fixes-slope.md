# 2026-10-01 — Selected upstream fixes and Slope validation

## Scope

Selectively integrate PR #74's editor toolbar and prefab-default improvements;
investigate the guarded Slope/native-preview mismatch; adapt deterministic terrain
test ideas. Full Tunnel behavior is deferred. Work continues from merged main
4b5fc1c on dan/terrain-regressions. Original user worktree is untouched.

## Prefab and editor integration

Adapted Morgan Touverey Quilling's b37a700 and ee6e824. The editor button now uses
the actual panel binding. Connect and Parallel permit an empty asset override;
Connect inherits the selected network and Parallel inherits each source edge's
asset independently. Other tools prefer the last selected network. Kept the
existing local tooltip. Omitted tunnel-specific branches and unrelated icon sizing.

The first patch check failed on Connect's changed context; no partial patch was
applied. Applied compatible paths separately and adapted Connect/Parallel directly.
Validation pending. Previous road-only Connect evidence did not establish rail
inheritance: the picker could populate a road default.

Debug compile passed with existing warnings. Added per-submission control-point mismatch evidence to the Debug preview log; unchanged guard thresholds. Native retest pending.

TypeScript checking passed. Full Debug build/postprocessing/UI/deployment passed after correcting the diagnostic converter namespace. The initial failure log is retained. Generated npm lock backed up before restoring the clean pre-build version.
