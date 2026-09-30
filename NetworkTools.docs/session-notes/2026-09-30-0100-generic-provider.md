# Generic provider sprint — 2026-09-30 01:00

Dan authorized autonomous work on new branches with complete removal of
mod-specific bridge code. First implement the provider boundary and migrate callers;
then measure the client path and prototype schema-driven MCP. No game is running
at initial inspection, so initial verification is offline, not native integration.
No legacy nt_* aliases will remain in the bridge. Historical docs/captures remain
as evidence; active instructions will describe the changed protocol.

## First implementation checkpoint

Generic discovery/dispatch replaces the named adapter and hardcoded command list.
Removed the external-mod assembly-name reporting allowlist as well. Native junction
queries have no third-party mod dependency and remain unchanged. Both components
can release independently; NT owns its optional Debug provider and command schemas.

13 registry checks passed, including two providers, duplicate rejection, revision
guards, mutation policy and discovery. Bridge compiles to non-deploying output and
56-command API verification passes. NT's first Compile failed due to fresh-worktree
missing NuGet assets; explicit restore resolved it and Compile now passes. Six
runner tests pass. Existing mailbox test run hit its net10 target with only SDK8
installed; investigate an override before changing project policy. No live tests.

## Persistent regression transport follow-up

The live regression runner now imports the bridge's generic Python client instead
of starting PowerShell for every query. NetworkTools command mapping stays in
this repository. Both bridge process and city session are pinned before submission;
caller-provided city expectations are checked before the first command as well.
Failed requests retain the original intent ID and immutable packet for inspection,
with no automatic mutation retry. Captured response JSON remains compatible with
existing analysis; optional PowerShell journal events are not emitted by this path.

Verification: eight offline runner tests pass, including presend city mismatch and
request-ID retention on transport failure. Bridge adapter tests and actual stdio
smoke pass separately. This is transport verification, not native preview/Apply
verification. Live provider deployment and testing remain outstanding.

Tooling policy: Python owns reusable transport/test orchestration. Keep PowerShell
for Windows bootstrap and thin build/deployment entry points where it avoids a
Python prerequisite. Do not maintain parallel implementations of regression logic.


## Live deployment, failure and repair

Full Debug build (postprocessor and UI included) passed. Initial bootstrap could
not find Git on PATH; reused the existing cmder Git installation via process PATH.
Reviewed and discarded only npm-generated optional-platform lockfile churn in this
isolated worktree. The original NetworkTools worktree's user edits are untouched.

Generic discovery found the provider and seven commands in the paused toy city.
The first rail regression checkpointed successfully, then stopped before selection
or Apply: active=true was insufficient because Smooth Curve mode was overwritten.
Source: AutomationCommand set Template before OnStartRunning; the base lifecycle
then called RestoreParameters. Added a pending activation flag consumed after base
startup restoration, plus explicit smoothMode state and runner readiness checking.
No geometry algorithm changed. The failed capture is retained.

The first graceful restart failed reading session.json due to file sharing;
the checkpoint had completed and the game was responsive. Fixed the narrow
PowerShell lifecycle read to share read/write/delete, retry boundedly, and reject
stale heartbeat. A subsequent unique checkpoint and graceful close succeeded.
No force kill was used. Rebuilt/deployed and visibly relaunched the hash-verified
original toy baseline; fresh process activation then succeeded.

Successful capture: captures/provider-rail-merge-fixed-20260930/report.json.
Strengths 0.5 and 0.8, checkpoint before mutations, fresh entity resolution,
preview polling and permanent Apply inspection all passed. Three changed edges;
three watched nodes retained directed connection identities (4/2/2), exact
preview/permanent curve agreement, and no fixed-node drifts. Topology and elevation
checks passed. Eight offline runner tests pass. Offline guards: high confidence for
covered cases; native preview and permanent result: verified for this fixture only;
vehicle traversal and human visual approval: not newly verified.

Game left paused on baseline 'bridge test - rail smoothing breaks merge junction
highway jank roads', with unsaved rail smoothing test changes after the preserved
checkpoint CitiesIIAgentBridge-regression-rail-merge-20260930-084943-9e84f0c0.
Original baseline save was never overwritten. The bridge's final capability-field
cleanup was compiled but not deployed during this live session.

Final read-only check confirmed city session 9525cf0c9e75408493fa9349ef17c0cb,
selectedSpeed=0, population=0 and controls enabled. Capture: provider-final-state-20260930.
Next proposed sprint: harden generic provider/MCP lifecycle and failure recovery,
then run existing rail/road/highway/slip/split/interior fixtures through the new
boundary. Keep new geometry semantics and vehicle traversal outside this milestone.
