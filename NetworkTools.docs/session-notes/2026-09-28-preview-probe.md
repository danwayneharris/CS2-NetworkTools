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

## First live probe result

User prepared 0.5 preview after restarting. Current selected nodes are 85028:1 to
junction 85022:1 (fresh identities, not the earlier save-session IDs). Submission 1
reports dirty=False, expectedEdges=4, matchedEdges=4, ambiguousEdges=0,
curveMismatches=0, missingLaneBuffers=0, edgeTrackLanes=8 after ModificationEndBarrier.
Bridge independently resolves connected junction 53266:3, complete with no errors,
and four junction-owned track lanes. Captured original/preview and trace under
captures/post-barrier-20260928. This verifies the observer runs and sees matching
candidate geometry for a stationary preview; rapid-change correctness is not yet tested.
Next ask user to move 0.5 -> 0.8 -> 0.5 without Apply and leave the final preview.
