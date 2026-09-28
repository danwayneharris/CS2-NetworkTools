# First native post-rebuild diagnostic probe

Added Debug-only NT_PreviewProbeSystem explicitly after ModificationEndBarrier.
The existing shape job publishes an immutable managed copy of its actual candidate
curves under a lock. The observer waits for that job, checks the current submission
ID and tool state, then compares original-mapped temporary edges (1 mm per control)
and reports edge track-lane counts. Duplicate edge mappings, curve mismatches,
missing buffers and dirty state are logged. Identical messages are suppressed.

This is deliberately a first diagnostic probe, NOT complete revision protection:
submission IDs advance at scheduling, not every input invalidation. It does not
certify unselected incident geometry, current prefab inputs, junction connector
semantics, or Apply eligibility. validationReady=false always. CanApply and the
interior-junction rejection are unchanged. The bridge remains responsible for
full junction captures during this experiment. Probe scans cap at 4096 temp edges;
oversized scans are skipped. Runtime cost/live timing need observation.

Full Debug build, postprocessing, generated parameters, UI build and deployment
passed: 0 errors, 33 compiler warnings. Build-generated package-lock additions for
optional platforms and nested TypeScript removal were reviewed and discarded;
no dependency update intended. Release not rebuilt (probe is excluded).
Installed NetworkTools DLL SHA256:
E22113B5E8671FF6E1F388DC209101AE0AC3119DC46A8F310688B0C72488AF70.

User confirmed game closed before deployment. Next supervised test: load toy save,
pause, select branch endpoint to junction at 0.5; inspect PreviewProbe log and bridge
snapshot. Then rapid 0.5/0.8/0.5 changes and cancel/reselect. No in-game result yet.
