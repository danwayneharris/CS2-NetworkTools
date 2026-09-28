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
