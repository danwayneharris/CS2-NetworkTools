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


## Split/interior/slip batch complete

Every case began with its preserved hash-verified baseline and fresh entity IDs.
Every Apply had a unique verified checkpoint. No expectation/tolerance was loosened.

| Case | Strict result | Maximum fixed-node drift (mm) |
|---|---|---:|
| road-four-way-branch | PASS | 0.000 |
| highway-on-ramp | PASS | 0.000 |
| highway-off-ramp | PASS | 0.000 |
| rail-one-split | FAIL | 3.008 |
| rail-two-splits | PASS | 0.000 |
| rail-two-splits-half | PASS | 0.000 |
| rail-two-splits-zero | PASS | 0.000 |
| road-two-splits | PASS | 0.000 |
| rail-interior-merge | FAIL | 5.174 |
| road-interior-junction | PASS | 0.000 |
| highway-two-interior-merges | PASS | 0.000 |
| slip-lane | PASS | 0.000 |

Both strict failures reproduce previously captured native center-alignment drift.
All completed cases retained directed connection identities and matched captured
preview/permanent curves. No vehicle traversal or human visual approval is inferred.
The ordinary rail merge is running once more as the final baseline case.

Recommended next decisions: review these two independently shippable changesets;
prioritize a generic MCP payload envelope and developer examples before promising
arbitrary schema compatibility. For NetworkTools, keep known millimetric drift
visible and decide its practical policy separately; next feature semantics for
junction-as-split and directional smoothing need maintainer input. Release/Burst,
non-merging crossings and traversal remain separate coverage gaps. No broad
refactoring or new geometry semantics were introduced in this sprint.
