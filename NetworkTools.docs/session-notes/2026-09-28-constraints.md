# Junction constraint prototype — 2026-09-28

Added scripts/rail-junction-constraints.py and four regression tests. Preservation
requires existing observed directed connections. Repair explicitly names missing
intended connections and retains existing ones. Candidate identities include entity
versions; missing/duplicate/nonfinite/inconsistent metrics reject. Fractional reserve
provides optional headroom. Reports utilization, minimum span at fixed angle, and
maximum angle at fixed span using the installed game's curvature formula.

Four tests pass using actual captured composition/prefab data. Broken merge passes
preservation of its two remaining mainline connectors but fails repair obligations
for both branch directions. Working merge preserves all four. This distinguishes
intent from observations rather than silently defining a broken state as success.

Not integrated: candidate composition must be recomputed/observed after geometry
changes. This is an offline constraint prototype, not a centerline optimizer or a
complete native connectivity oracle. validationReady false. No change to topology,
elevation, interior-junction rejection, runtime behavior or installed mods.

Automation reconnaissance: bridge Workflow.cs SaveCheckpoint already calls native
GameManager.Save asynchronously with a unique checkpoint name and RequireControl.
No load command exists in the command catalog. The bridge runs inside CS2, so cannot
start its own absent process; that needs an external launcher. NetworkTools public
Template/SmoothingFactor/RequestApply are accessible, but path HandleAddNode is
protected and depends on selection lifecycle. A deliberate public test adapter is
preferable to reflective invocation of protected methods. Full launch/load/control
loop requires save discovery, loading/session transitions, checkpoints and selection
initialization. Defer that broader loop while core preview diagnostics mature.

Timing: shape-job completion currently precedes native lane reconstruction. A paused
simulation frame can remain constant while tool previews rebuild, so it cannot serve
as a preview revision. Updated=false and repeated equal snapshots alone do not prove
association with the current slider/selection. Need a monotonic tool request revision
captured when definitions are emitted and echoed through their native products, or
an explicit pending state until provenance is established. Retain diagnostic-only
status meanwhile; never enable Apply solely from these snapshot checks.
