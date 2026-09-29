# Actual C# original comparison checks

Extracted the probe comparison into Geometry/OriginalInputComparison.cs; runtime
calls the same helper compiled by scripts/test-original-input-comparison.ps1.
Eight checks pass: equal copies, mutated value after snapshot, entity-version
change, optional-component presence change, reordered values, changed length,
and either missing snapshot. Debug /t:Compile passes; no deployment.

Initial harness incorrectly mutated a PowerShell-boxed struct shared with its saved
array and got matches. Moved value construction and mutation entirely into C#,
matching the runtime copy-then-box path. This was a harness aliasing issue, not
proof that the runtime struct snapshot aliases. A shell rewrite attempt failed
parsing and a sandbox invocation hit execution policy; no policy was changed,
and the harness ran successfully as the toolchain owner.

These tests exercise the actual comparison, not Unity EntityManager collection,
component-specific Equals implementations, or post-rebuild scheduling. Live matches
was tested previously; deliberate live external edit remains untested. No acceptance
gate added. Existing package-lock modification preserved.
