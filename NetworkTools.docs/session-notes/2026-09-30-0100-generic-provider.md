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
