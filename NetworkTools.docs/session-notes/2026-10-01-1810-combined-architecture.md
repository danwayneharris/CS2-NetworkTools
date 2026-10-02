# Combined sprint: baseline and architecture

New isolated nt-combined-sprint / dan/combined-smoothing-sprint starts from verified
origin/main 5cc24c0. Original checkout has unrelated lockfile and user notes/audits;
none were modified. Common initialized at pinned d5e28d7.

Identified existing paused toy city session f4923e4b5e584ffc9fa64ebdcf16b65a and
preserved the user-edited trumpet in verified package
CitiesIIAgentBridge-combined-before-trumpet-preservation-20261002-010804-dd6b1f48.cok.
This is not an unsmoothed baseline. Dan is building a separate baseline; no further
live control while he works. Raw checkpoint responses remain ignored in artifacts.

Step 0 source review identified one-span vertical fitting, original rather than
candidate selected-incident input to the surface model, and two formerly exclusive
native feedback loops as the focused integration seams. See
[architecture checkpoint](../combined-smoothing-architecture.md). No runtime code
changed or combined behavior claimed at this checkpoint.

## Sectioned vertical primitive

Added SectionedVerticalProfile: caller supplies fixed anchors and oriented
incoming/outgoing grade constraints; each interval reuses VerticalLinearProfile.
Outputs are scratch and must all be discarded after any failure. The primitive
does not guess pin policy or change the existing independent tool paths.

60 focused assertions pass: fixed heights, offsets, nonuniform stationing, reversed
storage/traversal, equal pin grades, explicitly distinct junction grades, changed
horizontal length, repeat/deterministic calculation and invalid/late-span failures.
The full existing geometry executable also passes, including its 605 ordinary
vertical-profile assertions and native captured replay. These are not live tests.

First invocation incorrectly passed --source to dotnet run; the executable treated
it as a replay filename and failed before assertions. Corrected invocation uses
--no-restore after successful build/restore. Both logs are retained under artifacts.
No game interaction since the preservation checkpoint; user is creating a baseline.
