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

## Deployment for supervised revision test

User confirmed game closed; verified no Cities2 process. Full Debug bootstrap build
passed with 0 errors and 33 compiler warnings; postprocessing, webpack and local
copy completed. Installed DLL SHA256:
2443C2F1A9C3A284ADC9F9404C41C6DEDDA0E16A312EAB284100925905287E40.
Same incidental npm optional-platform additions/nested TypeScript removal as prior
build were reviewed and discarded. Bridge unchanged. Next load paused toy save,
select branch endpoint to junction at 0.5, inspect inputRevision/submittedRevision
and revisionMatches before the slider and reselection tests. Live result pending.
