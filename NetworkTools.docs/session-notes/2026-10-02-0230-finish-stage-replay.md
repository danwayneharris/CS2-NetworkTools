# Finishing-stage replay — 2026-10-02

Continued from 6d3a319 in the isolated research branch. No game mutation, deployment,
restart or debugger reconnection in this increment. The previous debugger ownership
question remains unanswered; independent offline work continues.

Dan clarified the objective: reproduce the discrepancy outside the game and learn.
A causal explanation is valuable but is not a separate acceptance requirement.
Reproduction must still use explicit inputs and computed outputs, with native
differential evidence; synthetic agreement alone is insufficient.

Extended the hash-pinned local generator to include FinishEdgeGeometryJob from
GeometrySystem.cs:1688. It consumes the computed shared map from flattening,
updates end controls, applies native middle-height processing, calculates lengths
and bounds, and publishes EdgeGeometry through captured storage. Arithmetic and
branch bodies remain local generated source, not tracked proprietary code.
Terrain sampling explicitly throws; no zero/default terrain is invented.

World capture schema 2 adds explicit finishEdges and the finishing-read fields of
NetCompositionData. Old schema input is rejected. The replay enforces a preceding
flatten stage, carries its map into finishing, and reports four curves, lengths
and bounds. CalculateEdgeGeometry is still absent: supplied initial surfaces are
bounded-stage inputs, not an end-to-end prediction from authored curves.

Validation: standalone build succeeded without warnings. 37 source-world assertions
passed, including computed map publication on permanent/temporary worlds and
rejection of terrain-dependent output before publication. 16 JSON contract/output
cases passed, including missing Owner/height range, invalid stage ordering, duplicate
edge, schema mismatch and deliberately corrupted output. Negative subprocess cases
require handled exit 2, not a crash. Outputs remain local under
artifacts/offline-research/world-tests-finish.json and world-contract-v2-finish.

This increment adds executable propagation, not native anchor qualification. No
live checks retired. Actual stage snapshots and native anchor/control/held-out
comparison remain required. Modal crash dialogs were not directly observable by
shell; the corrected process boundary handles all expected test exceptions.

Dan requested extensive up-front instrumentation and captures to reduce repeated game sessions. Added a separate Bridge research worktree at cities2-agent-bridge-ndc/artifacts/offline-geometry-capture, based on terrain-profile 9ffceb8. Its commits e35aed5 and da796d8 implement a full-field 29-type geometry dependency-closure command, explicit absent/uncaptured states, ordered buffers, synchronization disclosure and capture limits. Build and installed type/API checks pass; no deployment/live qualification yet. See that worktree's docs/GEOMETRY-RESEARCH-CAPTURE.md. Native job-local tracing remains separate required work.
At 09:36 UTC, OS socket inspection confirms the game PID54932 has an established debugger connection from dotnet PID46288 on local endpoint60130 to game endpoint56192. This session's Unity status reports attached=false and heldSuspends=0. Requested an ownership handoff rather than disconnecting or restarting another session. The heartbeat still reports citySession0100c8864c714d9f9805b29d558a3d44; this alone does not establish ownership or authorize mutation. Offline implementation continues.
