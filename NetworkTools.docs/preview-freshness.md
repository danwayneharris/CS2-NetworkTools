# Preview freshness: observation versus acceptance

The live 0.5 and 0.8 captures resolve the same temporary junction `54484:25`
in the same city session, despite a 4.74 m change in an incident curve's controls.
Entity identity is not a preview revision. Both captures contain four directed
rail connectors; the user-applied 0.8 result matches preview exactly at serialized
precision. Evidence: session notes `2026-09-28-0747.md` and its captures.

## Current implementation

`RoadShapeToolSystem.JobMethods.cs`, `SchedulePathTransformJob`, waits for the
previous shape job and resets the fit result before scheduling another job.
`RoadShapeToolSystem.Update.cs`, `SmoothPreviewResult`, rejects dirty path data or
unfinished jobs. These protect the math result, not completion of native lane
reconstruction after preview definitions pass through the tool barrier.

`RequestApply` checks `CanApply`, then the Applying phase schedules a fresh
transformation. It does not promote the captured temporary entities. Any future
native-connectivity verdict must therefore be checked again at execution time,
against the same original network inputs and settings used for Apply.

## Proposed contract, not implemented runtime behavior

1. Give each tool activation a session identity and each relevant input change a
   monotonically increasing revision. Invalidate immediately on selection,
   parameter, cached network input, or tool lifecycle changes, before rebuilding.
2. Carry `(city session, tool session, revision)` through submission and observation.
   Include exact original entity versions, topology, geometry and relevant prefab
   inputs in the candidate association. A debug trace ID assigned on scheduling
   alone does not invalidate a result at the moment a parameter changes.
3. Establish an observation point after the native lane-producing jobs and their
   command buffers have completed for that candidate. The source-grounded hook and
   propagation of revision association are still unresolved. Do not replace this
   with a fixed delay, equal consecutive snapshots, `Updated == false`, or the
   simulation frame (the game is paused).
4. Accept a verdict only for the current token and known-completed reconstruction.
   Missing/ambiguous/incomplete observations stay pending or unavailable; they are
   not automatically disconnected geometry. Invalidate on cancel, Apply or unload.
5. Recheck at Apply execution; if relevant original inputs have changed, rebuild
   and validate again. Returning to an earlier slider value must not revive an old
   verdict (the ABA problem: same apparent value, different intervening history).

The bridge remains optional diagnostic infrastructure. Production validation must
belong to NetworkTools, not depend on a mailbox query or installed bridge.
Interior-junction rejection stays in place until a tested replacement exists.

## Offline evidence checks

Run `uv run python scripts/test-preview-freshness.py`.
`scripts/preview-freshness.py` fingerprints captured owners and lanes, scoped to the
city and original junction. It ignores response timestamps and top-level ordering.
It is intentionally conservative: temporary identity or lane-order changes can
change the fingerprint without a meaningful shape change. Equal fingerprints do
not establish freshness or completion. Snapshot-local equality IDs are retained
as observations, not interpreted as cross-snapshot connection identities.

The small `may_accept` function models required token equality and a separately
proven completion condition. Tests exercise stale revisions, city/tool replacement,
missing completion, and returning to an old value. The completion argument is a
placeholder for a future proven native integration hook, not evidence we have one.

## Installed scheduling evidence (September 28)

Targeted ILSpy 9.1.0.7988 extraction of installed Game.dll, SHA256
AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A.
Local source is under %TEMP%/nt-preview-order-source; copyrighted source is not
committed. Full decompile setup record is absent. Wrong namespace probes for
Game.Tools.ModificationSystem and Game.Common.SafeCommandBufferSystem failed;
correct namespaces are Game.Common and Game respectively.

- Game.Common.SystemOrder.cs:58-60 registers ToolSystem before ModificationSystem
  in MainLoop. Lines 82-93 bracket modification phases with allowance and playback
  barriers. LaneSystem is registered at Modification4 (155), SecondaryLaneSystem
  at Modification4B (184), LaneConnectionSystem at Modification5 (211).
- Game.Common.ModificationSystem.OnUpdate invokes Modification1 through
  ModificationEnd sequentially; it is not a GameSimulation-phase system.
- Game.Tools.ToolSystem.cs:251-260 invokes PreTool, ToolUpdate, PostTool. A PostTool
  observer would therefore be too early for the subsequent modification pipeline.
- LaneSystem.cs:9234,9258,9382-9386 (earlier targeted extraction under
  %TEMP%/nt-rail-rule-source) uses ModificationBarrier4, creates its command buffer,
  and registers its lane job as a producer on that barrier.
- ModificationBarrier4 and ModificationEndBarrier derive from
  Game.SafeCommandBufferSystem. Its OnUpdate disables further command-buffer
  creation for that phase and calls EntityCommandBufferSystem.OnUpdate.
- Game.UpdateSystem.cs:178-180 supports an explicit UpdateAfter<T, Other> anchor.

Candidate diagnostic registration:
`UpdateAfter<Observer, ModificationEndBarrier>(SystemUpdatePhase.ModificationEnd)`.
This is a source-supported observation location, not yet an implemented or
live-verified completion certificate. An observer must use appropriate ECS read
synchronization, correlate a current submitted revision and its expected curves,
resolve all incident preview edges unambiguously, and report incomplete reads.
Later pathfinding processing and actual train traversal remain separate concerns.
Do not insert command-buffer writes after a barrier whose usage has closed.

Next implementation should be diagnostic-only: record submitted revision, observed
revision, candidate matching, and native lane evidence at this phase without
changing CanApply. Supervised acceptance: stationary preview, rapid 0.5/0.8/0.5
slider changes, cancel/reselect, then preview/Apply comparison. Only after those
checks should native verdicts participate in enabling newly supported geometry.
