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
