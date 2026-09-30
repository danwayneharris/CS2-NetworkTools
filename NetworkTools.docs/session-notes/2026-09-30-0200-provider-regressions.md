# Provider regression sprint — 2026-09-30 02:00

Starting checkpoint f0d2099, branch dan/provider-regression-sprint. Preserve original
user worktree changes. First harden generic bridge transport independently, then
replay each existing fixture from a hash-verified baseline with a unique checkpoint.
Current paused toy session contains prior rail smoothing; preserve it before reload.
No new geometry semantics or relaxed checks are authorized by this sprint.

## First automation and live checkpoints

Added a consumer-owned real MCP stdio smoke script; read-only state discovery and
invocation passed against the paused toy city. Added run-provider-suite.py to
sequence bounded graceful baseline reloads and existing per-case verification.
No assertion thresholds changed. Road-four-way-branch, highway-on-ramp and
highway-off-ramp passed through the generic provider. Captures and summary are in
captures/sprint-road-ramps-20260930. Prior unsaved toy changes were checkpointed.

The split/interior suite is in progress. One-split rail reproduced 3.008 mm fixed
junction-center drift, with directed connections and exact preview/Apply curves
preserved; it remains a strict failure. Two-split rail passed. No geometry fix or
tolerance change is being bundled into the transport sprint.

## Offline replay migration

Review found replay-regression.py still expected the removed flat nt_* transport,
so it could not consume new generic-provider captures. Factored the consumer-owned
routing into a shared method and used it for replay when the recorded envelope is
generic. A completed road-four-way capture now replays all current assertions
successfully without contacting the game. Eight runner guard tests still pass.
Older captures without smoothMode cannot establish the new activation predicate;
use their historical runner for historical evidence rather than assuming readiness.
