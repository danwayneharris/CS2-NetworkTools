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

## Demonstrated Slope defects and focused correction

The production-code regression failed: a bent segment's 20 m final handle was
measured as 27.888977 m. `EdgeState.CalculateControlPointRatios` used distance a-c;
now it measures c-d backwards from the end. This preserves the physical grade
intended by the height-profile derivative, including reverse traversal.

Native preview captured two mismatches at the ramp junction: expected endpoint
heights 607.4662 and 607.2302 became 607.3482. Handles and XZ were unchanged.
Installed 1.6.2f1 `Game.Net/GeometrySystem.cs:491,510` sets curve endpoint heights
from node geometry (with composition exceptions); `CutCurve` at 640 also uses
node heights. The existing Slope pipeline independently fitted endpoints and
averaged their displacements into a shared node. These requests disagreed.

After that averaging, Slope now restores each original endpoint-to-node vertical
offset at the new node height, shifting the adjacent handle equally. Preview and
Apply receive the same curve; the strict 1 mm probe is unchanged. This is not a
relaxation of validation or a claim to handle every composition's native rules.
Curve Smooth bypasses this Slope-only alignment.

Added NetworkTools.Slope.Tests and scripts/test-slope.ps1. Eight formula fixtures
(four terrains x two directions) pass 96 assertions using compiled production
code without a native ECS world. The initial harness accidentally read bin output
rather than the fresh Compile-only obj output; corrected the reference and provided
a wrapper that always compiles first. Initial failing evidence is retained.
Debug deployment, Release compile, geometry suite, and 8 runner guards pass.
Release compilation does not establish Burst/postprocessor/runtime success.
Native post-fix test pending. No native test-map loader or tunnel feature imported.

## Native results and final state

The corrected off-ramp ease test passes: all selected native preview curves equal
permanent Apply curves exactly; node/edge counts and topology unchanged; stale
Apply revision rejected. Independent audit found identical directed lane-transition
sets at six watched nodes. Node XZ is unchanged. The one unselected side edge
moved only at the incident endpoint and handle, both by the junction's -9.9821 m
height change; its far end stayed fixed. This exposes the existing terrain-agnostic
Slope behavior, not a terrain-aware road solution. Visual review remains necessary.

Road Connect inherited Small Road; rail Connect inherited Double Train Track.
Both matched native preview/permanent control points exactly, preserved all old
edge geometry/prefabs/endpoints, and rejected stale revisions. Road added 2 edges
and 1 node; rail added 4 edges and 3 nodes. Rail case had two harness mistakes
before geometry mutation (mistyped fingerprint, then shortened fixture names);
both are retained and corrected. No Apply was blindly retried.

Final unique checkpoint:
`CitiesIIAgentBridge-selected-fixes-slope-connect-20261001-081617-88a57dac.cok`.
Game remains paused in the toy checkpoint session; all test changes saved, original
v1.1 and user saves untouched. Checkpoint contains curve-then-slope ramp plus road
and rail Connect examples. Vehicle traversal, visual quality, editor interaction,
mixed-prefab Parallel native behavior, and full Release/Burst are not certified.

Bridge repository has no changes. Full Tunnel import remains deferred.
