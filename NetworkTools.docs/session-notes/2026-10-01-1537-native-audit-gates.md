# Native audit gates for PR14

Dan authorized the three remaining automated gates: two-ended incident edit, stale-input rejection, and native Release supported-envelope checks, followed by restoring Debug. Work remains in isolated nt-audit-sprint on dan/audit-correctness-sprint. Initial status clean; game is closed. Preserve toy baseline packages and checkpoint created fixtures before mutations. No vehicle/visual approval is implied.

First fixture attempt: native Medium Road curved alternate between two internal hill-road nodes completed, but created two edges. The explicit single-edge requirement rejected the fixture; no Slope mutation occurred. Captures: artifacts/native-two-ended-create. Retain the failed experiment, reload before a straight-branch attempt.

Straight placement created one alternate edge. The selected route initially used it: the prefab penalty is additive, not a multiplier, and was insufficient to prefer the original path. Bowing that single edge's two inner controls by 40 m in Z lengthened it without changing topology. Native selection then matched all nine original edge identities; saved a separate baseline.

Two-ended Slope Apply passed: endpoint Y deltas 3.861206 m and 0.247 m; composed translation error 0.00001 m; exact preview/permanent agreement; topology, directed connections and physical lane mappings preserved. The additional existing audit initially lacked its expected changedUnselectedEdges report field; this harness now emits it and the captured report was supplemented from the retained before/after data, without replaying Apply.

Atomic native stale-input test passed: CanApply True -> False after a 1 m control-point edit; TryRequestApply returned False; restoring the original yielded True and phase remained Ready. Exact recipes and raw result are retained. No production mod changes so far. Release native gate next.

## Release execution and restoration

Full Release build/postprocessing/UI/deployment passed (33 warnings, zero errors).
The native library was observed loaded and Burst enabled in CS2. The Debug-only
NetworkTools provider was absent as intended, so tests called existing selection
handlers and shared domain Apply via unity-devtools. Independent generic bridge
snapshots established permanent results.

- Rail branch ending at junction: CanApply false, ReleaseJunctionUnsupported,
  TryRequestApply false. Baseline fingerprint unchanged.
- Nonjunction hill-road Smooth Curve: exact preview/Apply agreement; topology,
  elevations, fixed endpoints, unselected geometry and lane mappings preserved.
- Nonjunction crest/dip Constant Slope: exact preview/Apply agreement; topology,
  node XZ, fixed endpoints, unselected geometry and lane mappings preserved.
  Its baseline was a unique checkpoint after the successful Curve operation.
- These establish a bounded Release smoke test, not ordinary UI coverage, every
  job's native execution mode, or universal Release qualification. Surface-aware
  correction remains Debug-only.

The first Windows PowerShell build invocation failed before compilation because
its default BridgePath expression saw an empty PSScriptRoot; the next found Git
missing from process PATH. Explicit BridgePath and a process-only Git PATH prefix
allowed normal bootstrap builds. No system settings changed. Incidental npm lock
churn was backed up and restored after root dependency equality checks.

Full Debug restoration passed (32 warnings, zero errors). Visibly reloaded the
settled surface-profile review save; verified exact region fingerprint, paused
population-zero toy city and restored provider discovery. No unsaved network edits
followed reload. See [compact evidence](native-audit-gates-20261001.json) for build
identities, final city session, checkpoint names and results.

Remaining review: Dan's ordinary UI and visual pass; vehicle traversal later.
Native two-ended and bounded Release gaps in the earlier handoff are now closed.
Live nonidentity Node rotation and arbitrary concurrent entity replacement remain
untested, not implied by these tests. No production mod changes in this follow-up.
