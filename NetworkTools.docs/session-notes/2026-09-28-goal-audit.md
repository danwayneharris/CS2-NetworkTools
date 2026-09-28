# Goal audit and supervised handoff — 2026-09-28

## Requirement evidence

- Setup first: NetworkTools 7b3aebc changes README.md, AGENTS.md, BOOTSTRAP.md,
  scripts/bootstrap.ps1 and adds check-bridge.ps1. Bridge 12fcf98 updates INSTALL.md
  (its setup guide) with local build/install and cs2-modding plugin instructions.
  Standalone check passed on the then-current matching build/deployment; missing
  dependency rejected; bootstrap parsed. New resolver build is intentionally newer
  than installed DLL, so CheckBridge should now reject mismatch until deployment.
- Automation reconnaissance: Workflow.cs implements asynchronous save_checkpoint.
  No load command in catalog. Launch requires an external process controller;
  the in-process bridge cannot start itself. Custom tool mode/parameter fields are
  public, but selection requires protected lifecycle and path construction. A full
  restart/load/select/restore loop is not a small mailbox addition; deferred per
  the request to avoid this rabbit hole. Latest user test save name:
  `bridge tes - rail broken and working3`. No save/load/restart was attempted.
- Preview discovery: bridge 06a8fed adds pure C# topology resolver and connectedSnapshot,
  retaining original-node match. Actual resolver tested against saved capture and
  missing, duplicate, ambiguous, incomplete, version and ordering cases. The bridge
  compiles against installed assemblies. Its new native snapshot path is not yet
  live-verified. This satisfies saved-capture validation, not runtime acceptance.
- Timing/stale investigation: constraints session note traces shape completion before
  native reconstruction and explains why paused simulationFrame/Updated/repeated
  content cannot establish selection revision. Diagnostic validationReady remains
  false. No stale result can enable Apply through this new query because it is not
  connected to Apply at all. A provenance-aware runtime gate remains future work.
- Constraint prototype: bee9671, actual prefab limits and captured compositions;
  explicit directional repair intent versus preservation. Four tests pass. Reports
  necessary span/angle bounds. This is not a complete game reconstruction or fitted
  centerline optimizer. The requested out-of-game constraint prototype is present.
- Stretch investigation: 2463456 documents target-length optimization and explicit
  split/tangent policy; reproducible negative-extrapolation counterexample and edge
  partition tests. No signed slider or split UI shipped; permitted investigation
  path taken pending design/in-game evaluation.
- Invariants: git diff 1c73f23 -- NetworkTools.Mod is empty. Runtime fitter, topology,
  elevations and interior-junction rejection unchanged. Both worktrees clean before
  this audit note. No push/PR/fork publication or game mutation/deployment this goal.

## Verification rerun

All five Python suites pass: 4 terminal-proxy, 4 composition-input, 2 composition
capture, 4 junction-constraint and 2 stretch tests (16 total). Bridge actual C#
resolver regression passes; installed native component/read-operation contract and
54-command registration checks pass. Prior bridge compile passes with two existing
obsolete-updater warnings. These are offline/assembly tests, not train traversal.

## Next supervised test

1. Save the toy layout and close CS2; deploy rebuilt bridge after manifest/hash checks.
2. Load `bridge tes - rail broken and working3`, pause, keep bridge controls off.
3. Select input branch endpoint to junction at strength 0.5, leave unapplied.
4. Resolve fresh IDs. get_junction_preview should report topologyResolution=resolved
   and connectedSnapshot with the replacement node's actual incident edges/lanes.
5. Capture at 0, 0.5 and 1, then change selection and confirm old identity/revision
   evidence cannot be reused. Compare to an explicitly authorized Apply later.
6. Only after native preview evidence is reliable should a runtime junction-aware
   fitter or Apply gate be integrated. The current work does not claim that feature
   is complete or that a passing curvature gate proves native train connectivity.
