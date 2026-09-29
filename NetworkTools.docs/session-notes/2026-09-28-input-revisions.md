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

## First live input-revision observation

Captured initial-trace.log and initial-preview.json under captures/input-revisions-20260928.
New session ce6704b3297a4ba69c94fa10a9f8ce64: submission 1 at strength 0.5 has
inputRevision=16, submittedRevision=16, revisionMatches=True, dirty=False. All four
selected edge curves match, zero ambiguous mappings/missing buffers. Bridge snapshot
in citySession b70629112dc64f638a7d3c1be595b7d1 resolves junction 53266:3, complete
with no errors and four junction track lanes. Stationary association passed; this
does not exercise an invalidated/stale observation. Next: slider roundtrip, then
cancel/reselect to check monotonic revision advancement and final candidate matches.

## Live revision slider roundtrip

User completed 0.5 -> 0.8 -> 0.5. Trace includes intermediate zero inputs:
submissions 1..5 have strengths 0.5, 0, 0.8, 0, 0.5 and input/submitted revisions
16,17,18,19,20 respectively. Each observed pair matches, with all four candidate
curves matching and no missing lane buffers/ambiguous edges. Final complete bridge
snapshot has four junction track lanes and all three incident curves identical to
the initial 0.5 snapshot. Evidence: roundtrip-trace.log and roundtrip-preview.json
in captures/input-revisions-20260928. Returning to the same value advances the
revision; no revision-mismatch window was observed, so stale rejection remains
unexercised by this test. Next cancel/reselect without Apply.

## Live revision cancel/reselect

User cleared/reselected the same endpoints. New submission 6 at 0.5 reports
inputRevision=23 and submittedRevision=23 (previously 20), revisionMatches=True,
four matching selected curves, zero ambiguity/missing buffers. Bridge resolves
53266:5, complete without errors, four junction track lanes; all three incident
curves match initial 0.5. Captures: reselection-trace.log/reselection-preview.json.
This verifies revision advancement through the observed selection lifecycle.
No stale mismatch was induced; no production gate or general race-freedom claim.
The manual stationary/slider/reselection checks for this instrumentation are done.
Next independent work: external-input fingerprints and explicit stale-observation
regressions before integrating any native connectivity acceptance decision.
