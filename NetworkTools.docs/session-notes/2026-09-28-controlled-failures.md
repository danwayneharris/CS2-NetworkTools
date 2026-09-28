# Controlled failure-path diagnostics

Added shared Observe revision check ahead of original-input comparison. Debug probe
now alters only a copied entity version and supplies a missing copied snapshot,
logging changed/unavailable expectations once per matching revision. It also holds
the previous observed revision/input copy and submits that old token to Observe
when a later revision arrives, expecting stale_revision. No ECS writes or Apply
gating changes; validationReady remains false. This simulates delayed observation
at the comparison boundary, not an actual delayed native job or external city edit.

Eleven actual shared C# checks pass. Full Debug build, postprocessing, UI and local
deployment passed with zero errors and 33 compiler warnings while CS2 was closed.
Existing package-lock modification retained, not included. Live failure-test output
still pending. Next: select toy branch to junction at 0.5, then change to 0.8 without
Apply. Inspect PreviewFailureTest entries for copied input and held revision results.

Installed DLL SHA256: 417483F6778513802045267175E9B244B04D99D901A2B48FAAABA90F95F4A009

## Live controlled failures passed

User selected at 0.5 then changed to 0.8 while paused. Captured trace.log and
preview-080.json under captures/controlled-failures-20260928. Revisions 16,17,18
reported copiedEntityVersion=changed and missingCopy=unavailable. Held revision
16 against current17 and held17 against current18 both reported stale_revision.
Actual observations remained revisionMatches=True, originalInputs=matches, four
matched candidate curves, zero ambiguity/missing lane buffers. Final submission3
is strength0.8. Bridge resolves53266:3, complete/error-free with four junction track
lanes. These exercise the shared rejection function on controlled copies in-game;
they do not simulate an actual late native job or establish full Apply safety.
No Apply or game mutation issued. Next work can move toward native junction
connection obligations while retaining current interior-junction rejection.
