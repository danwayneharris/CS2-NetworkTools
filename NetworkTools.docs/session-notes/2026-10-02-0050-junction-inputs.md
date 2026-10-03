# Junction preview input investigation — 2026-10-02 00:50

Continuing the ramp-to-road preview/Apply mismatch from 16d7326. Preserve the
unrelated UI/package-lock.json change. Runtime remains 04792dd Debug initially.

## Evidence correction

Re-ran summarize-slope-surface-preview.py on artifacts/junction-surface-apply.
Four of five EdgeGeometry components match, but incident ramp 54262:1 differs by
1.844445 m in Y. All authored cubics match, compositions match, and sampled terrain
is unchanged. The earlier conversational claim that all five edge surfaces match
was incorrect; the retained raw data already contains this difference. Direct live
Unity reads agree with the saved permanent capture; no stale bridge read established.
The four junction-end differences in the previous note remain valid and are
consistent with this one differing incident edge input.

Native GeometrySystem InitializeNodeGeometry and FlattenNodeGeometry depend on
Updated membership, mixed temporary/permanent incident edges, and node height.
Trace these before changing junction fitting. The native junction fitter itself
has not yet been shown defective.

Preserved checkpoint CitiesIIAgentBridge-regression-before-reload-20261002-075006-c840f0cd,
then requested visible reload of the exact pre-Apply baseline ending 070428-c983f9ef.
No baseline overwrite or force kill. Further tests pending.

## Pause / handoff — 2026-10-02 01:34 PDT

Dan requested a pause to pursue another investigation. Do not resume live edits
merely because this note lists next steps. No runtime correction was made in this
session; the cause remains unresolved.

### Current branch and deployed build

- Worktree: `C:\Users\danwa\dev\cs2-mods\nt-combined-sprint`.
- Branch: `dan/combined-smoothing-sprint`; parent of this note's commit: `16d7326`.
- Draft PR: https://github.com/danwayneharris/CS2-NetworkTools/pull/15.
- Deployed runtime: `04792dd`, Debug; NetworkTools.dll SHA256:
  `48EF3D8D36626D73C61F80133850509F6E8688337F05CF185533225636D0AEE9`.
- Bridge worktree: sibling `bridge-terrain-profile`; no bridge edits.
- Preserve the unrelated `NetworkTools.Mod/UI/package-lock.json` modification and
  the original CS2-NetworkTools checkout's user changes. No push this session.

### What the latest experiment established

Replayed the exact pre-Apply baseline, recreated Combined Smooth Curve at strength
1.0 with both smooth-end options, captured, checkpointed, and applied again.
`artifacts/junction-cut-apply/comparison.json` reproduces the earlier result:
all five authored curves and compositions match; four generated EdgeGeometry
components match, but incident ramp 54262:1 differs by 1.844445 m vertically.
All 1548 sampled terrain heights remain unchanged. Generated surfaces were stable
across the post-Apply sampling interval. This is not a terrain-causality proof.

Added `scripts/prepare-native-edge-cut-probe.py`: generates read-only Unity debugger
expressions that construct a local native CalculateEdgeGeometryJob with fresh
lookups and invoke CalculateOffsets/CalculateCutOffset/Cut. It does not execute
expressions or write ECS components. It is a restricted, version-sensitive probe,
not a general surface predictor. Check entity existence/version before evaluating;
check its listed assumptions (zero flatness, ordinary cutting, no offset clamp,
no auxiliary-offset linkage/fixed-node handling). It omits endpoint retention and
junction flattening. Generated expressions were exercised on all four original
and corresponding temporary incident edges in this fixture.

Native pre-flatten cut endpoints agree between original and temporary edges.
For ramp 54262, the calculated end boundary is Y=614.5127/613.7286, exactly the
permanent surface. Preview is Y=612.6683/611.884155. The existing restricted offline
pairwise junction-flattening replay, fed those cut endpoints for all four edges,
predicts the preview endpoints within 0.0001 m. This supports a difference in
later native junction processing; it does NOT yet explain why permanent processing
differs. Do not claim the native game algorithm itself is defective.

Compact replay fixture retained beside this note:
`2026-10-02-junction-cut-flatten-fixture.json`. Expected values are native PREVIEW
observations, not expected permanent behavior. The model iterates to its 100-iteration
cap; the numerical match is useful evidence, not proof of algorithmic convergence.

### Diagnostic attempts and limits

- Original/temp ramp Curve, endpoint positions and calculated node heights matched.
  Composition IDs and relevant flags matched. Node PrefabRef differs at the central
  temporary junction (8 m versus 20 m asset); do not assume that is irrelevant to
  every stage just because the observed cut output matches.
- Marked original central node and all four incident edges Updated/BatchesUpdated;
  native rebuild did not remove the discrepancy. Confirmed those tags at OnUpdate.
- Temporarily reordered original ConnectedEdge buffer; no meaningful effect.
  Restored its original order: [54263, 54261, 54262, 54264].
- Forced temporary junction rebuild; mismatch persisted.
- After the last Apply, set GeometrySystem.m_Loaded=true and invoked its Update,
  completed tracked jobs and read again later. This full geometry-rebuild diagnostic
  also retained permanent ramp Y=614.5127 and road end Y=611.88416. m_Loaded returned
  false. No authored Curve/Node writes were performed by these diagnostic probes.
- An earlier preview eventually became rejected after initial acceptance. Its Apply
  attempt stopped BEFORE mutation (`artifacts/junction-input-apply`). Cleared and
  recreated it. Long debugger pauses and explicit native updates occurred during
  that investigation, so causality is unknown; do not label it an ordinary UI repro.
- Reflection FieldInfo.SetValue on the local boxed job and cached generated lookup
  fields produced invalid probes. Discard those results. Direct job field assignment
  with fresh system GetComponentLookup/GetBufferLookup worked. A stale temporary
  entity read also produced garbage; discarded. Eval itself does not verify entity
  versions. No active breakpoint or held debugger suspension remains.

### Retained local evidence and replay

Bulk captures remain ignored under `artifacts/`; these are local, not downloadable
from the PR. Retain them when cleaning this worktree:

- `junction-surface-apply`: earlier full comparison and terrain capture.
- `junction-input-*`, `junction-far-input*`, `junction-temp-force`: intermediate
  recreation, capture, rejected Apply and fresh temporary identities.
- `junction-cut-preview`, `junction-cut-snapshot`: latest recreation and junction.
- `junction-native-cut-results.json`: native probe outputs for original/temp edges.
- `junction-cut-expressions.json`: exact native expressions used.
- `junction-cut-apply`: latest successful checkpoint/Apply and comparison.
- `junction-native-cut-flatten-fixture.json`: source of the compact committed fixture.

Offline replay (writes only the report; no game contact):

```powershell
uv run --no-project python scripts/replay-junction-flattening.py `
  NetworkTools.docs/session-notes/2026-10-02-junction-cut-flatten-fixture.json `
  --output artifacts/junction-cut-flatten-replay.json
```

For live replay, existing `prepare-surface-preview-replay.py` uses
`artifacts/combined-mismatch-native2` as the captured selection and validates its
original curves before selecting. `compare-current-slope-preview.py --apply`
checkpoints and captures before/after; do not bypass its freshness checks.

### Game / saves at pause

Loaded baseline (same save root under LocalLow/Colossal Order/Cities Skylines II):
`CitiesIIAgentBridge-surface-preview-before-apply-20261002-070428-c983f9ef.cok`.
SHA256: `fb4e5f5628bd4b8ee7b1fbee75c2eda77b14a8b148ec409db91dbf5ca5aed376`.

Latest pre-Apply checkpoint:
`CitiesIIAgentBridge-surface-preview-before-apply-20261002-083125-43658a9a.cok`.
Earlier preserved checkpoint:
`CitiesIIAgentBridge-regression-before-reload-20261002-075006-c840f0cd.cok`.

Last verified toy city Wantagh, population 0, simulation paused. Current process
54932; citySession `14b1445c455a418f84cd8862e9b94900`. Apply cleared selection.
There are UNSAVED diagnostic Apply/derived-geometry changes. Do not overwrite the
baseline or close/reload without preserving anything the user adds afterward.
Game was found closed at continuation and visibly relaunched from the checksummed
baseline. No new build/deployment and no force-kill. Recheck all state on resume.

Original selected edges 54264:1 and 54263:1; selected nodes start54969:1,
interior54966:1, end54968:1. Incident ramps54261:1/54262:1; elevated neighbor81714:1.
These are historical identities: rediscover after loading. Temporary IDs are stale
following Apply and MUST NOT be reused.

### Resume point / open question

Native source: sibling `cs2-decompile/src/Game/Game.Net/GeometrySystem.cs`, game
1.6.2f1. Key stages: InitializeNodeGeometry ~82-175 (Updated-dependent retention
sentinel), CalculateEdgeGeometry ~274-443 (cut/retention), CalculateOffsets ~448,
CalculateCutOffset/Cut ~736, FlattenNodeGeometry ~1505-1677 (Temp classification,
shared pairwise height map), FinishEdgeGeometry ~1725 (height map/middle limiting).
OnUpdate schedules the dependency chain around3565. EdgeIterator.cs determines
which incident edges participate.

Next useful evidence is actual intermediate permanent versus temporary data at the
flattening/height-map stages, including retention sentinels, participant membership,
and whether a later system overwrites results. Read-only cut replay plus full native
rebuild has not explained this discrepancy. Avoid further broad terrain changes,
copying preview meshes into permanent state, or weakening validation to hide it.
The new task Dan proposes may provide better tracing/offline scaffolding.

Confidence: native preview and permanent discrepancy reproduced; restricted offline
math matches preview; root cause and correction unverified. No new Release/Burst,
vehicle traversal, general regression-suite or human visual qualification claimed.
