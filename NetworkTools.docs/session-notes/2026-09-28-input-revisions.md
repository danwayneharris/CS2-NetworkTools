# Input revision diagnostics

Added a Debug-only input revision separate from the scheduled SmoothTrace ID.
Parameter OnChanged callbacks invalidate immediately; path refresh/invalidation,
MarkDirty, tool start and ResetToIdle also advance it. Scheduling captures the
revision after the prior job completes. Post-barrier observation reports current
and submitted revisions and revisionMatches, also requiring clean/valid path data.
Returning to the same slider value cannot restore an earlier revision.

This remains diagnostic, not a production acceptance gate. Candidate matching still
checks selected temporary edge controls, not all incident inputs/prefabs. External
network edits not causing cache invalidation are not independently detected. Tool
world/session scoping and complete external-input fingerprints remain follow-up.
Interior-junction rejection and CanApply are unchanged; validationReady=false.

Verification: Debug MSBuild /t:Compile passed against installed assemblies. This
avoids AfterBuild and therefore does not run postprocessing, UI build or deployment.
No full build or in-game verification of this revision yet. Next supervised run
requires game closed, full Debug build/deploy, then stationary, slider roundtrip,
and cancel/reselect tests with inputRevision/submittedRevision fields captured.
